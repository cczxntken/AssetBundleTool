using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace LitEngine.LoadAsset.DownLoad
{
    public class DownLoadManager : MonoBehaviour
    {

        private static object lockobj = new object();
        private static DownLoadManager sInstance = null;
        private static DownLoadManager Instance
        {
            get
            {
                if (sInstance == null)
                {
                    lock (lockobj)
                    {

                        if (sInstance == null)
                        {
                            GameObject tobj = new GameObject("DownLoadManager");
                            GameObject.DontDestroyOnLoad(tobj);
                            sInstance = tobj.AddComponent<DownLoadManager>();
                            sInstance.Init();
                        }
                    }
                }
                return sInstance;
            }
        }
        public const int MaxThread = 3;

        private Hashtable sDownLoadMap = Hashtable.Synchronized(new Hashtable());
        private ArrayList sDownLoading = ArrayList.Synchronized(new ArrayList());
        private ArrayList sWaitDownLoad = ArrayList.Synchronized(new ArrayList());

        private Hashtable sGroupMap = Hashtable.Synchronized(new Hashtable());
        private bool mInited = false;

        private DownLoadManager()
        {
            
        }

        private void OnDestroy()
        {
            ArrayList tkeys = new ArrayList(sDownLoadMap.Keys);
            for (int i = 0 ,length = tkeys.Count; i < length; i++)
            {
                object tk = tkeys[i];
                DownLoader item = (DownLoader)sDownLoadMap[tk];
                item.Dispose();
            }

            ArrayList tgkeys = new ArrayList(sGroupMap.Keys);
            for (int i = 0 ,length = tgkeys.Count; i < length; i++)
            {
                object tk = tgkeys[i];
                DownLoadGroup item = (DownLoadGroup)sGroupMap[tk];
                item.Dispose();
            }
        }

        public void Init()
        {
            if (mInited) return;
            mInited = true;
        }

        public static DownLoader DownLoadFileAsync(string sourceurl, string destination,string pFileName,string pMD5,long pLength, System.Action<DownLoader> finished, System.Action<DownLoader> progress = null, bool isClear = true)
        {
            DownLoader ret = null;
            if (Instance.sDownLoadMap.ContainsKey(sourceurl))
            {
                Debug.LogWarning("为正在下载的文件添加回调.url = " + sourceurl);
                ret = (DownLoader)Instance.sDownLoadMap[sourceurl];
            }
            else
            {
                ret = new DownLoader(sourceurl, destination,pFileName, pMD5, pLength, isClear);
            }
            DownLoadFileAsync(ret, finished, progress, isClear);
            return ret;
        }

        public static void DownLoadFileAsync(DownLoader pLoader,System.Action<DownLoader> finished, System.Action<DownLoader> progress = null, bool isClear = true)
        {
            if(pLoader == null) return;
            if (finished != null)
                pLoader.OnComplete += finished;
            if (progress != null)
                pLoader.OnProgress += progress;

            Add(pLoader);
        }

        public static void AddGroup(DownLoadGroup pGroup)
        {
            if (!Instance.sGroupMap.ContainsKey(pGroup.Key))
            {
                Instance.sGroupMap.Add(pGroup.Key, pGroup);
            }
        }

        public static void RemoveGroup(object pKey)
        {
            if(Instance.sGroupMap.ContainsKey(pKey))
            {
                Instance.sGroupMap.Remove(pKey);
            }
        }

        public static void Add(DownLoader pDownLoader)
        {
            if (!Instance.sDownLoadMap.ContainsKey(pDownLoader.Key))
            {
                Instance.sDownLoadMap.Add(pDownLoader.Key, pDownLoader);
                Instance.sWaitDownLoad.Add(pDownLoader);
            }
        }
        

        public static void Remove(object pKey)
        {
            if(Instance.sDownLoadMap.ContainsKey(pKey))
            {
                var item = Instance.sDownLoadMap[pKey];
                Instance.sDownLoadMap.Remove(pKey);


                var tlist = Instance.sDownLoading;
                int tindex = tlist.IndexOf(item);
                
                if(tindex == -1)
                {
                    tlist = Instance.sWaitDownLoad;
                    tindex = tlist.IndexOf(item);
                }

                if (tindex != -1)
                {
                    tlist.RemoveAt(tindex);
                }
            }
        }


        private void Update()
        {
            UpdateLoader();
            UpdateGroup();
        }

        void UpdateLoader()
        {
            if (sDownLoading.Count == 0 && sWaitDownLoad.Count == 0) return;
            UpdateDownLoading();
            UpdateWaitQue();
        }

        void UpdateDownLoading()
        {
            for (int i = sDownLoading.Count - 1; i >= 0; i--)
            {
                IDownLoad item = (IDownLoad)sDownLoading[i];
                item.Update();
                if (item.IsDone)
                {
                    Remove(item.Key);
                }
            }
        }

        void UpdateWaitQue()
        {
            if (sWaitDownLoad.Count > 0 && sDownLoading.Count < MaxThread)
            {
                int tneed = MaxThread - sDownLoading.Count;
                for (int i = 0; i < tneed; i++)
                {
                    IDownLoad item = (IDownLoad)sWaitDownLoad[0];
                    sWaitDownLoad.RemoveAt(0);
                    sDownLoading.Add(item);
                    item.StartAsync();

                    if (sWaitDownLoad.Count == 0)
                    {
                        break;
                    }
                }

            }
        }

        void UpdateGroup()
        {
            if(sGroupMap.Count == 0) return;
            ArrayList tkeys = new ArrayList(sGroupMap.Keys);
            for (int i = 0,length = tkeys.Count; i < length; i++)
            {
                var tkey = tkeys[i];
                DownLoadGroup item = (DownLoadGroup)sGroupMap[tkey];
                item.Update();
                if (item.IsDone)
                {
                    RemoveGroup(tkey);
                }
            }
        }

    }
}
