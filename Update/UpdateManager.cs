using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using LitEngine.LoadAsset;
using LitEngine.DownLoad;
using System.IO;
using System.Collections.Generic;
namespace LitEngine.UpdateTool
{
    public class UpdateManager : MonoBehaviour
    {
        public const string checkfile = "checkInfoData.txt";
        public const string downloadedfile = "downloadedData.txt";
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
        public void Init()
        {
            if (mInited) return;
            mInited = true;

            updateData = new UpdateData();
            TextAsset datatxt = Resources.Load<TextAsset>("jsonData/updateData");
            if (datatxt != null)
            {
                UnityEngine.JsonUtility.FromJsonOverwrite(datatxt.text, updateData);
            }
        }

        #region update
        static bool isUpdateing = false;
        ByteFileInfoList updateInfoObject;
        DownLoadGroup downLoadGroup;
        static public void UpdateRes(ByteFileInfoList pInfo, System.Action<DownLoadGroup> onComplete)
        {
            if (isChecking)
            {
                Debug.LogError("更新中,请勿重复调用.");
                CallUpdateOnComplete(onComplete, null);
                return;
            }
            isUpdateing = true;
            Instance.StartCoroutine(Instance.FileUpdateing(pInfo,onComplete));
        }
        static void CallUpdateOnComplete(System.Action<DownLoadGroup> onComplete, DownLoadGroup dloader)
        {
            try
            {
                onComplete?.Invoke(dloader);
            }
            catch (System.Exception erro)
            {
                Debug.LogError("CheckUpdate->" + erro.ToString());
            }
        }

        IEnumerator FileUpdateing(ByteFileInfoList pInfo, System.Action<DownLoadGroup> onComplete)
        {
            updateInfoObject = pInfo;
            if(downLoadGroup != null)
            {
                downLoadGroup.Dispose();
            }
            downLoadGroup = new DownLoadGroup("updateGroup");
            foreach (var item in pInfo.fileInfoList)
            {
                string turl = item.resName;
                var tloader = downLoadGroup.AddByUrl(turl, LoadManager.sidePath, item.resName, item.fileMD5, item.fileSize, false);
                tloader.OnComplete += (a) =>
                {
                    OnUpdateOneComplete(pInfo.Get(a.FileName));
                };
            }
            downLoadGroup.StartAsync();

            while (!downLoadGroup.IsDone)
            {
                yield return null;
            }

            CallUpdateOnComplete(onComplete, downLoadGroup);
            isUpdateing = false;
        }

        void OnUpdateOneComplete(ByteFileInfo pInfo)
        {
            if (pInfo == null) return;
            try
            {
                string tline = UnityEngine.JsonUtility.ToJson(pInfo);
                List<string> tlines = new List<string>();
                tlines.Add(tline);
                string tdedfile = Path.Combine(LoadManager.sidePath, downloadedfile);
                File.AppendAllLines(tdedfile, tlines);
            }
            catch (System.Exception erro)
            {
                Debug.LogError(erro.Message);
            }
        }
        #endregion

        #region check
        static bool isChecking = false;
        static public void CheckUpdate(System.Action<ByteFileInfoList> onComplete)
        {
            if (isChecking)
            {
                Debug.LogError("检测中,请勿重复调用.");
                CallCheckOnComplete(onComplete, null);
                return;
            }
            isChecking = true;
            Instance.StartCoroutine(sInstance.CheckingUpdate(onComplete));
        }

        IEnumerator CheckingUpdate(System.Action<ByteFileInfoList> onComplete)
        {
            string tdicpath = string.Format("{0}/{1}/", updateData.server, updateData.version);
            string tuf = tdicpath + LoadManager.byteFileInfoFileName;

            DownLoader tdl = DownLoadManager.DownLoadFileAsync(tuf, LoadManager.sidePath, checkfile, null, 0, null);
            while (!tdl.IsDone)
            {
                yield return null;
            }

            ByteFileInfoList ret = null;
            if (tdl.Error == null)
            {
                ret = GeNeedDownloadFiles(GetUpdateList());
            }

            CallCheckOnComplete(onComplete, ret);

            isChecking = false;
        }

        static void CallCheckOnComplete(System.Action<ByteFileInfoList> onComplete, ByteFileInfoList pObj)
        {
            try
            {
                onComplete?.Invoke(pObj);
            }
            catch (System.Exception erro)
            {
                Debug.LogError("CheckUpdate->" + erro.ToString());
            }
        }

        ByteFileInfoList GeNeedDownloadFiles(List<ByteFileInfo> pList)
        {
            if (pList == null || pList.Count == 0) return null;


            var tcmp = new ByteFileInfoList();
            tcmp.AddRange(pList);

            string tdedfile = Path.Combine(LoadManager.sidePath, downloadedfile);
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
            string tfilePath = Path.Combine(LoadManager.sidePath, checkfile);
            if (File.Exists(tfilePath))
            {
                byte[] tdata = File.ReadAllBytes(tfilePath);
                tinfo.Load(tdata);
            }

            ret = LoadManager.Instance.ByteInfoData.Comparison(tinfo);
            return ret;
        }

        void UpdateLocalList()
        {
            string tfilePath = Path.Combine(LoadManager.sidePath, checkfile);
            if (File.Exists(tfilePath))
            {
                byte[] tdata = File.ReadAllBytes(tfilePath);
                LoadManager.Instance.ByteInfoData.Load(tdata);

                string tsavefile = Path.Combine(LoadManager.sidePath, LoadManager.byteFileInfoFileName);
                LoadManager.Instance.ByteInfoData.Save(tsavefile);
            }

            string tdedfile = Path.Combine(LoadManager.sidePath, downloadedfile);
            if (File.Exists(tdedfile))
            {
                File.Delete(tdedfile);
            }
        }
        #endregion
    }
}