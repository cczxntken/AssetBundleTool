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


        private Hashtable sDownLoadMap = Hashtable.Synchronized(new Hashtable());
        private bool mInited = false;

        private DownLoadManager()
        {
            
        }

        public void Init()
        {
            if (mInited) return;
            mInited = true;
        }

        public static bool DownLoadFileAsync(string sourceurl, string destination,long pLength, System.Action<DownLoader> finished, System.Action<DownLoader> progress = null,bool isClear = true)
        {
            if (Instance.sDownLoadMap.ContainsKey(sourceurl))
            {
                Debug.LogError("有相同URL文件正在下载当中.URL = " + sourceurl);
                return false;
            }
            DownLoader ttaskdown = new DownLoader(sourceurl, destination, pLength, isClear);
            if (finished != null)
                ttaskdown.OnComplete += finished;
            if (progress != null)
                ttaskdown.OnProgress += progress;

            ttaskdown.StartAsync();
            Add(sourceurl,ttaskdown);
            return true;
        }

        public static void Add(string pKey, IDownLoad pDownLoader)
        {
            if (!Instance.sDownLoadMap.ContainsKey(pKey))
            {
                Instance.sDownLoadMap.Add(pKey, pDownLoader);
            }
        }

        public static void Remove(string pKey)
        {
            if(Instance.sDownLoadMap.ContainsKey(pKey))
            {
                Instance.sDownLoadMap.Remove(pKey);
            }
        }

        private void Update()
        {
            ArrayList tkeys = new ArrayList(sDownLoadMap.Keys);
            for (int i = tkeys.Count - 1; i >= 0; i--)
            {
                object key = tkeys[i];
                IDownLoad item = (IDownLoad)sDownLoadMap[key];
                item.Update();
                if(item.IsDone)
                {
                    sDownLoadMap.Remove(key);
                }
            }
        }

    }
}
