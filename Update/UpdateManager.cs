using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using LitEngine.LoadAsset;
using LitEngine.DownLoad;
using System.IO;
using System.Collections.Generic;
using System.Text;
namespace LitEngine.UpdateTool
{
    public class UpdateManager : MonoBehaviour
    {
        public const string checkfile = "checkInfoData.txt";
        public const string downloadedfile = "downloadedData.txt";
        public const string upateMgrData = "jsonData/updateData";
        private static object lockobj = new object();
        private static UpdateManager sInstance = null;
        private static UpdateManager Instance
        {
            get
            {
                if (sInstance == null)
                {
                    lock (lockobj)
                    {

                        if (sInstance == null)
                        {
                            GameObject tobj = new GameObject("UpdateManager");
                            GameObject.DontDestroyOnLoad(tobj);
                            sInstance = tobj.AddComponent<UpdateManager>();
                            sInstance.Init();
                        }
                    }
                }
                return sInstance;
            }
        }

        public UpdateData updateData;

        private bool mInited = false;
        private UpdateManager()
        {

        }
        private void Init()
        {
            if (mInited) return;
            mInited = true;

            updateData = new UpdateData();
            TextAsset datatxt = Resources.Load<TextAsset>(upateMgrData);
            if (datatxt != null)
            {
                UnityEngine.JsonUtility.FromJsonOverwrite(datatxt.text, updateData);
            }
            else
            {
                StringBuilder tstrbd = new StringBuilder();
                tstrbd.AppendLine(string.Format("please check the file in Resources. filename:{0}.txt", upateMgrData));
                tstrbd.AppendLine("format json:");
                tstrbd.AppendLine("{");
                tstrbd.AppendLine("\"version\":\"1\",");
                tstrbd.AppendLine("\"server\":\"http://localhost/Resources/\"");
                tstrbd.AppendLine("}");

                Debug.LogError(tstrbd.ToString());

            }
        }

        private void OnDestroy()
        {

        }
        private void OnDisable()
        {
            Stop();
            ReleaseGroupLoader();
            ReleaseCheckLoader();
        }

        #region prop
        static public float DownloadProcess { get; private set; }
        static public long DownLoadLength { get; private set; }
        static public long ContentLength { get; private set; }
        static public DownLoadGroup updateGroup
        {
            get
            {
                return Instance.downLoadGroup;
            }
        }
        static public DownLoader checkDL
        {
            get
            {
                return Instance.checkLoader;
            }
        }
        static public bool IsRuningUpdate
        {
            get
            {
                if (isUpdateing || isChecking) return true;
                return false;
            }
        }
        static bool isUpdateing = false;
        static bool isChecking = false;

        int ReTryMaxCount = 20;
        int ReTryCount = 0;
        int ReTryCheckCount = 0;
        DownLoadGroup downLoadGroup;
        DownLoader checkLoader;

        ByteFileInfoList curInfo;
        UpdateComplete curOnComplete;
        bool curAutoRetry;
        #endregion

        #region update
        public delegate void UpdateComplete(ByteFileInfoList info, string error);
        static public void StopAll()
        {
            Instance.Stop();
        }
        static public bool ReStart()
        {
           return Instance.ReTryGroupDownload();
        }

        private void Stop()
        {
            StopAllCoroutines();
            ReTryCount = 0;
            ReTryCheckCount = 0;
            isUpdateing = false;
            isChecking = false;
            StopGroupLoader();
        }
        void ReleaseGroupLoader()
        {
            if (downLoadGroup == null) return;
            downLoadGroup.Dispose();
            downLoadGroup = null;
        }
        void StopGroupLoader()
        {
            if (downLoadGroup == null) return;
            downLoadGroup.Stop();
        }
        bool ReTryGroupDownload()
        {
            if (isUpdateing)
            {
                Debug.LogError("更新中,请勿重复调用.");
                return false;
            }
            if (downLoadGroup == null) return false;
            isUpdateing = true;
            downLoadGroup.ReTryAsync();
            StartCoroutine(WaitUpdateDone());
            return true;
        }
        static public void UpdateRes(ByteFileInfoList pInfo, UpdateComplete onComplete, bool autoRetry)
        {
            if (isUpdateing)
            {
                Debug.LogError("Updateing.");
                return;
            }
            isUpdateing = true;
            Instance.StartCoroutine(Instance.WaitStarUpdate(0.1f, pInfo, onComplete, autoRetry));
        }

        IEnumerator WaitStarUpdate(float delayTime, ByteFileInfoList pInfo, UpdateComplete onComplete, bool autoRetry)
        {
            yield return new WaitForSeconds(delayTime);
            Instance.FileUpdateing(pInfo, onComplete, autoRetry);
        }

        void FileUpdateing(ByteFileInfoList pInfo, UpdateComplete onComplete, bool autoRetry)
        {
            curInfo = pInfo;
            curOnComplete = onComplete;
            curAutoRetry = autoRetry;
            ReleaseGroupLoader();
            downLoadGroup = new DownLoadGroup("updateGroup");
            foreach (var item in pInfo.fileInfoList)
            {
                string turl = GetServerUrl(item.resName);
                var tloader = downLoadGroup.AddByUrl(turl, LoadManager.sidePath, item.resName, item.fileMD5, item.fileSize, false);
                tloader.priority = item.priority;
                tloader.OnComplete += (a) =>
                {
                    if (a.IsCompleteDownLoad)
                    {
                        OnUpdateOneComplete(pInfo[a.FileName]);
                    }
                };
            }
            downLoadGroup.StartAsync();
            UpdateProcess();
            
            StartCoroutine(WaitUpdateDone());

        }

        IEnumerator WaitUpdateDone()
        {
            while (!downLoadGroup.IsDone)
            {
                UpdateProcess();
                yield return null;
            }
            UpdateProcess();
            if (downLoadGroup.IsCompleteDownLoad)
            {
                UpdateFileFinished();
            }
            else
            {
                UpdateFileFail();
            }
        }

        IEnumerator WaitReTryUpdate()
        {
            yield return new WaitForSeconds(5f);
            downLoadGroup.ReTryAsync();
            StartCoroutine(WaitUpdateDone());
        }

        void UpdateProcess()
        {
            if(downLoadGroup == null) return;
            DownloadProcess = downLoadGroup.Progress;
            ContentLength = downLoadGroup.ContentLength;
            DownLoadLength = downLoadGroup.DownLoadedLength;
        }

        void UpdateFileFinished()
        {
            isUpdateing = false;
            UpdateLocalList();
            CallUpdateOnComplete(curOnComplete, null, null);
        }

        void UpdateFileFail()
        {
            isUpdateing = false;
            
            if (ReTryCount >= ReTryMaxCount)
            {
                curAutoRetry = false;
            }
            if (!curAutoRetry)
            {
                ByteFileInfoList erroListInfo = GetErroListInfo(downLoadGroup, curInfo);
                CallUpdateOnComplete(curOnComplete, erroListInfo,downLoadGroup.Error);
            }
            else
            {
                Debug.Log(downLoadGroup.Error);
                ReTryCount++;
                StartCoroutine(WaitReTryUpdate());
            }
        }

        void CallUpdateOnComplete(UpdateComplete onComplete, ByteFileInfoList pInfo, string pError)
        {
            try
            {
                ReleaseGroupLoader();
                ReTryCount = 0;
                onComplete?.Invoke(pInfo, pError);
            }
            catch (System.Exception erro)
            {
                Debug.LogError("CheckUpdate->" + erro.ToString());
            }
        }

        ByteFileInfoList GetErroListInfo(DownLoadGroup pGroup, ByteFileInfoList pInfo)
        {
            var tlist = pGroup.GetNotCompletFileNameTable();
            pInfo.RemoveRangeWithOutList(tlist);
            return pInfo;
        }
        void OnUpdateOneComplete(ByteFileInfo pInfo)
        {
            if (pInfo == null) return;
            try
            {
                string tline = UnityEngine.JsonUtility.ToJson(pInfo);
                List<string> tlines = new List<string>();
                tlines.Add(tline);
                string tdedfile = CombinePath(LoadManager.sidePath, downloadedfile);
                File.AppendAllLines(tdedfile, tlines);
            }
            catch (System.Exception erro)
            {
                Debug.LogError(erro.Message);
            }
        }
        #endregion

        #region check
        public delegate void CheckComplete(ByteFileInfoList info, string error);
        public static bool autoUseCacheCheck = false;
        void ReleaseCheckLoader()
        {
            if (checkLoader == null) return;
            checkLoader.Dispose();
            checkLoader = null;
        }
        static public void CheckUpdate(CheckComplete onComplete, bool useCache, bool needRetry)
        {
            if (isChecking || isUpdateing)
            {
                Debug.LogError("Checking or Updateing.");
                return;
            }
            isChecking = true;
            Instance.StartCoroutine(sInstance.WaitStartCheck(0.1f, onComplete, useCache, needRetry));
        }

        IEnumerator WaitStartCheck(float delayTime, CheckComplete onComplete, bool useCache, bool needRetry)
        {
            yield return new WaitForSeconds(delayTime);
            Instance.StartCoroutine(sInstance.CheckingUpdate(onComplete, useCache, needRetry));
        }

        IEnumerator CheckingUpdate(CheckComplete onComplete, bool useCache, bool needRetry)
        {
            ReleaseCheckLoader();

            string tdicpath = string.Format("{0}/{1}/", updateData.server, updateData.version);
            string tuf = GetServerUrl(LoadManager.byteFileInfoFileName + LoadManager.sSuffixName);
            string tcheckfile = GetCheckFileName();
            string tfilePath = CombinePath(LoadManager.sidePath, tcheckfile);
            if (!useCache || !File.Exists(tfilePath))
            {
                checkLoader = DownLoadManager.DownLoadFileAsync(tuf, LoadManager.sidePath, tcheckfile, null, 0, null);
                while (!checkLoader.IsDone)
                {
                    yield return null;
                }
                DownLoadCheckFileEnd(checkLoader, onComplete, useCache, needRetry);
            }
            else
            {
                DownLoadCheckFileFinished(onComplete);
            }
        }

        IEnumerator WaitRetryCheck(float dt,CheckComplete onComplete, bool useCache, bool needRetry)
        {
            yield return new WaitForSeconds(dt);
            CheckUpdate(onComplete, useCache, needRetry);
        }

        void DownLoadCheckFileFinished(CheckComplete onComplete)
        {
            isChecking = false;
            ByteFileInfoList ret = GetNeedDownloadFiles(GetUpdateList());
            CallCheckOnComplete(onComplete, ret);
        }

        void DownLoadCheckFileFail(DownLoader dloader,CheckComplete onComplete, bool useCache, bool needRetry)
        {
            isChecking = false;
            string tfilePath = CombinePath(LoadManager.sidePath, GetCheckFileName());
            bool isfileExit = File.Exists(tfilePath);
            if(dloader.IsCompleteDownLoad && isfileExit)
            {
                DownLoadCheckFileFinished(onComplete);
            }
            else
            {
                if (ReTryCheckCount >= ReTryMaxCount)
                {
                    needRetry = false;
                }

                if (needRetry)
                {
                    ReTryCheckCount++;
                    if(autoUseCacheCheck && isfileExit)
                    {
                        StartCoroutine(WaitRetryCheck(0.1f,onComplete, true, needRetry));
                    }
                    else
                    {
                        StartCoroutine(WaitRetryCheck(3,onComplete, useCache, needRetry));
                    }
                    
                }
                else
                {
                    CallCheckOnComplete(onComplete, null);
                }
            }
        }

        void DownLoadCheckFileEnd(DownLoader dloader, CheckComplete onComplete, bool useCache, bool needRetry)
        {
            if (dloader.Error == null)
            {
                DownLoadCheckFileFinished(onComplete);
            }
            else
            {
                Debug.Log(dloader.Error);
                DownLoadCheckFileFail(dloader,onComplete, useCache, needRetry);
            }
        }

        void CallCheckOnComplete(CheckComplete onComplete, ByteFileInfoList pObj)
        {
            try
            {
                string error = checkLoader != null ? checkLoader.Error : null;

                ReleaseCheckLoader();
                ReTryCheckCount = 0;

                onComplete?.Invoke(pObj, error);
            }
            catch (System.Exception erro)
            {
                Debug.LogError("CheckUpdate->" + erro.ToString());
            }
        }

        ByteFileInfoList GetNeedDownloadFiles(List<ByteFileInfo> pList)
        {
            if (pList == null || pList.Count == 0) return null;


            var tcmp = new ByteFileInfoList();
            tcmp.AddRange(pList);

            string tdedfile = CombinePath(LoadManager.sidePath, downloadedfile);
            var tdedinfo = new ByteFileInfoList(tdedfile);
            var tneedList = tdedinfo.Comparison(tcmp);

            if (tneedList.Count == 0)
            {
                UpdateLocalList();
                return null;
            }
            else
            {
                ByteFileInfoList ret = new ByteFileInfoList();
                ret.AddRange(tneedList);
                return ret;
            }

        }

        List<ByteFileInfo> GetUpdateList()
        {
            List<ByteFileInfo> ret = null;
            var tinfo = new ByteFileInfoList();
            string tfilePath = CombinePath(LoadManager.sidePath, GetCheckFileName());

            if (File.Exists(tfilePath))
            {
                AssetBundle tinfobundle = AssetBundle.LoadFromFile(tfilePath);
                if (tinfobundle != null)
                {
                    TextAsset tass = tinfobundle.LoadAsset<TextAsset>(LoadManager.byteFileInfoFileName);
                    if (tass != null)
                    {
                        tinfo.Load(tass.bytes);
                    }
                    tinfobundle.Unload(false);
                }
            }

            ret = LoadManager.Instance.ByteInfoData.Comparison(tinfo);
            return ret;
        }

        string GetCheckFileName()
        {
            return string.Format("{0}_{1}", updateData.version, checkfile);
        }

        void UpdateLocalList()
        {
            string tfilePath = CombinePath(LoadManager.sidePath, GetCheckFileName());
            string tsavefile = CombinePath(LoadManager.sidePath, LoadManager.byteFileInfoFileName + LoadManager.sSuffixName);

            if (File.Exists(tfilePath))
            {
                if (File.Exists(tsavefile))
                {
                    File.Delete(tsavefile);
                }
                File.Copy(tfilePath, tsavefile);

                LoadManager.Instance.LoadResInfo();
            }

            string tdedfile = CombinePath(LoadManager.sidePath, downloadedfile);
            if (File.Exists(tdedfile))
            {
                File.Delete(tdedfile);
            }
        }
        #endregion

        public string GetServerUrl(string pFile)
        {
            string serverUrl = S3SendClient.GetHTTPPATH();
#if UNITY_IOS
            string assetPath = string.Format("{0}/{1}/{2}/{3}", updateData.server, "ios", updateData.version,pFile);
#elif UNITY_ANDROID
            string assetPath = string.Format("{0}/{1}/{2}/{3}", updateData.server, "android", updateData.version, pFile);
#else
            string assetPath = string.Format("{0}/{1}/{2}/{3}", updateData.server, Application.platform.ToString(), updateData.version,pFile);
#endif
            return serverUrl + assetPath;
        }
        
        static System.Text.StringBuilder cCombineBuilder = new System.Text.StringBuilder();
        static public string CombinePath(params string[] paths)
        {
            if (paths == null || paths.Length == 0) return null;
            cCombineBuilder.Clear();
            for (int i = 0, length = paths.Length; i < length; i++)
            {
                bool thavenext = i + 1 < length;
                string item = paths[i];
                string next = thavenext ? paths[i + 1] : "";
                bool thv = item.EndsWith("/");
                bool tnexthv = !thavenext || next.StartsWith("/");

                cCombineBuilder.Append(item);

                if (!thv && !tnexthv)
                {
                    cCombineBuilder.Append("/");
                }
            }

            return cCombineBuilder.ToString();
        }
    }
}