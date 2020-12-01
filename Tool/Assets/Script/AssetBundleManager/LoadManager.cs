using UnityEngine;
using System.IO;
using System.Collections.Generic;
namespace LitEngine.LoadAsset
{
    public class LoadManager : MonoBehaviour
    {
        public const string resPath = "Data/ResData/";
        public const string exportPath = "Assets/ExportResources/";
        public const string sSuffixName = ".bytes";
        public const string ManifestName = "AppManifest";
        public const string byteFileInfoFileName = "bytefileinfo.txt";

        private static object lockobj = new object();
        private static LoadManager sInstance = null;
        public static LoadManager Instance
        {
            get
            {
                if (sInstance == null)
                {
                    lock (lockobj)
                    {

                        if (sInstance == null)
                        {
                            GameObject tobj = new GameObject("LoadManager");
                            GameObject.DontDestroyOnLoad(tobj);
                            sInstance = tobj.AddComponent<LoadManager>();
                            sInstance.Init();
                        }
                    }
                }
                return sInstance;
            }
        }

        #region 属性
        private BundleVector mBundleList = null;
        private LoadTaskVector mBundleTaskList = null;
        private WaitingList mWaitLoadBundleList = null;
        public AssetBundleManifest Manifest { get; private set; }
        public ByteFileInfoList ByteInfoData{ get; private set; }
        private bool mInited = false;
        private bool isDisposed = false;
        #endregion

        #region init
        private LoadManager()
        {
            
        }
        public void Init()
        {
            if (mInited) return;
            mInited = true;

            mWaitLoadBundleList = new WaitingList();
            mBundleList = new BundleVector();
            mBundleTaskList = new LoadTaskVector();

            LoadResInfo();
        }

        public void LoadResInfo()
        {
            LoadMainfest();
            LoadByteFileInfoList();
        }

        public void LoadMainfest()
        {
            AssetBundle tbundle = AssetBundle.LoadFromFile(GetFullPath(ManifestName));
            if (tbundle != null)
            {
                Manifest = tbundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                tbundle.Unload(false);
            }
        }

        public void LoadByteFileInfoList()
        {
            ByteInfoData = new ByteFileInfoList();
            AssetBundle tinfobundle = AssetBundle.LoadFromFile(GetFullPath(byteFileInfoFileName));
            if (tinfobundle != null)
            {
                TextAsset tass = tinfobundle.LoadAsset<TextAsset>(byteFileInfoFileName);
                if (tass != null)
                {
                    ByteInfoData.Load(tass.bytes);
                }
                tinfobundle.Unload(false);
            }
        }

        protected void OnDestroy()
        {
            if (isDisposed) return;
            isDisposed = true;
        }
        static public void DestroyLoaderManager()
        {
            if(sInstance != null)
            {
                GameObject.Destroy(sInstance.gameObject);
                sInstance = null;
            }
        }
        #endregion
        #region resTool
        static public string[] GetAllDependencies(string _assetBundleName)
        {
            if (Instance.Manifest == null) return null;
            return Instance.Manifest.GetAllDependencies(CombineSuffixName(_assetBundleName));
        }
        static public string[] GetDirectDependencies(string _assetBundleName)
        {
            if (Instance.Manifest == null) return null;
            return Instance.Manifest.GetDirectDependencies(CombineSuffixName(_assetBundleName));
        }
        static public string[] GetAllAssetBundles()
        {
            if (Instance.Manifest == null) return null;
            return Instance.Manifest.GetAllAssetBundles();
        }
        void ActiveLoader(bool _active)
        {
            if (gameObject.activeSelf == _active) return;
            gameObject.SetActive(_active);
        }
        private LoadTask CreatTaskAndStart(string _key, BaseBundle _bundle, System.Action<string, object> _callback, bool _retain)
        {
            LoadTask ret = new LoadTask(_key, _bundle, _callback, _retain);
            mBundleTaskList.Add(ret);
            return ret;
        }
        private void AddmWaitLoadList(BaseBundle _bundle)
        {
            mWaitLoadBundleList.Add(_bundle);
        }

        private void AddCache(BaseBundle _bundle)
        {
            mBundleList.Add(_bundle);
        }

        static public void ReleaseAsset(string _key)
        {
            Instance.mBundleList.ReleaseBundle(BaseBundle.DeleteSuffixName(_key.ToLowerInvariant()));
        }

        private void RemoveAllAsset()
        {
            mBundleList.Clear();
        }

        public void ReleaseUnUsedAssets()
        {
            List<BaseBundle> tlist = new List<BaseBundle>(mBundleList.values);
            for (int i = tlist.Count - 1; i >= 0; i--)
            {
                BaseBundle tbundle = tlist[i];
                if (tbundle.WillBeReleased && tbundle.Loaded)
                {
                    tbundle.Release();
                }

            }
        }
        static public void RemoveAsset(string _AssetsName)
        {
            Instance.mBundleList.Remove(BaseBundle.DeleteSuffixName(_AssetsName.ToLowerInvariant()));
        }
        #endregion

        #region 同步
        static public UnityEngine.Object LoadAsset(string _AssetsName)
        {
            return (UnityEngine.Object)Instance.LoadAssetRetain(_AssetsName.ToLowerInvariant()).Retain();
        }
        private BaseBundle LoadAssetRetain(string _AssetsName)
        {
            if (string.IsNullOrEmpty(_AssetsName)) return null;
            if (!mBundleList.Contains(_AssetsName))
            {
                AssetsBundleHaveDependencie tbundle = new AssetsBundleHaveDependencie(_AssetsName, LoadAssetRetain);
                AddCache(tbundle);
                tbundle.Load();
            }
            return mBundleList[_AssetsName];
        }
        #endregion

        #region 异步
        static public void LoadAssetAsync(string _key, string _AssetsName, System.Action<string, object> _callback)
        {
            Instance.LoadAssetAsyncRetain(_key, _AssetsName.ToLowerInvariant(), _callback, true);
        }
        protected void LoadBundleAsync(BaseBundle _bundle, string _key, System.Action<string, object> _callback, bool _retain)
        {
            AddCache(_bundle);
            _bundle.Load();
            AddmWaitLoadList(_bundle);
            CreatTaskAndStart(_key, _bundle, _callback, _retain);
            ActiveLoader(true);
        }

        private BaseBundle LoadAssetAsyncRetain(string _key, string _AssetsName, System.Action<string, object> _callback, bool _retain)
        {

            if (_AssetsName.Length == 0)
            {
                Debug.LogError("LoadAssetAsyncRetain -- _AssetsName 的长度不能为空");
                if (_callback != null)
                    _callback(_key, null);
                return null;
            }
            if (_callback == null)
            {
                Debug.LogError("LoadAssetAsyncRetain -- CallBack Fun can not be null");
                return null;
            }
            if (mBundleList.Contains(_AssetsName))
            {
                if (mBundleList[_AssetsName].Loaded)
                {
                    if (mBundleList[_AssetsName].Asset == null)
                        Debug.LogError("LoadAssetAsyncRetain-erro in vector。文件载入失败,请检查文件名:" + _AssetsName);
                    if (_retain)
                        mBundleList[_AssetsName].Retain();
                    _callback(_key, mBundleList[_AssetsName].Asset);

                }
                else
                {
                    CreatTaskAndStart(_key, mBundleList[_AssetsName], _callback, _retain);
                    ActiveLoader(true);
                }

            }
            else
            {

                LoadBundleAsync(new AssetsBundleHaveDependencieAsync(_AssetsName, LoadAssetAsyncRetain), _key, _callback, _retain);
            }
            return mBundleList[_AssetsName];
        }
        #endregion

        #region tool
        static string _sidePath = null;
        static public string sidePath
        {
           get{
                if(_sidePath == null)
                {
                    _sidePath = Application.persistentDataPath + "/" + resPath;
                }
                return _sidePath;
           }
        }
        static string _streamPath = null;
        static public string streamPath
        {
           get{
                if(_streamPath == null)
                {
                    _streamPath = Application.streamingAssetsPath + "/" + resPath;
                }
                return _streamPath;
           }
        }
        static public string GetFullPath(string _filename)
        {
            _filename = CombineSuffixName(_filename);
#if ASSET_INSIDE
            return streamPath + _filename;
#else
            string tfullpathname = sidePath + _filename;
            if (!File.Exists(tfullpathname))
            {
                tfullpathname = streamPath + _filename;
            }

            return tfullpathname;
#endif

        }

        static public string CombineSuffixName(string _assetsname)
        {
            string ret = _assetsname;
            if (!_assetsname.EndsWith(sSuffixName))
                return _assetsname + sSuffixName;
            return _assetsname;
        }
        #endregion


        #region update
        void Update()
        {
            if (mWaitLoadBundleList.Count > 0)
            {
                for (int i = mWaitLoadBundleList.Count - 1; i >= 0; i--)
                {
                    BaseBundle tbundle = mWaitLoadBundleList[i];
                    if (tbundle.IsDone())
                        mWaitLoadBundleList.RemoveAt(i);
                }
            }

            if (mBundleTaskList.Count > 0)
            {
                for (int i = mBundleTaskList.Count - 1; i >= 0; i--)
                {
                    mBundleTaskList[i].IsDone();
                }
            }


            if (mWaitLoadBundleList.Count == 0 && mBundleTaskList.Count == 0)
                ActiveLoader(false);
        }
        #endregion
    }
}