using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
namespace LitEngine.LoadAsset.DownLoad
{
    public enum DownloadState
    {
        normal = 1,
        downloading,
        finished,
    }

    public class DownLoadGroup : IDownLoad
    {
        public event Action<DownLoadGroup> onComplete;
        public event Action<DownLoadGroup> OnProgress;
        public long ContentLength { get; private set; }
        public long DownLoadedLength { get; private set; }
        public float Progress { get; private set; }

        public bool IsDone { get; private set; }
        public DownloadState State { get; private set; }
        public string Key { get; private set; }
        public string Error { get; private set; }
        private List<DownLoader> _groupList = new List<DownLoader>();
        public List<DownLoader> groupList { get { return _groupList; } }

        public DownLoadGroup(string newKey)
        {
            Key = newKey;
            State = DownloadState.normal;
            Error = null;
            ContentLength = 0;
        }
        ~DownLoadGroup()
        {
            Dispose(false);
        }

        #region dispose
        protected bool mDisposed = false;
        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        protected void Dispose(bool _disposing)
        {
            if (mDisposed)
                return;
            mDisposed = true;
            DisposeGC();
        }

        protected void DisposeGC()
        {
            for (int i = 0; i < groupList.Count; i++)
            {
                groupList[i].Dispose();
            }
            groupList.Clear();

            onComplete = null;
            OnProgress = null;
        }
        #endregion

        public void AddByUrl(string pSourceurl, string pDestination, long pLength, bool pClear)
        {
            if (State != DownloadState.normal)
            {
                Debug.LogError("已经开始的任务不可插入新内容.");
                return;
            }
            if (IsHaveURL(pSourceurl)) return;
            Add(new DownLoader(pSourceurl, pDestination, pLength, pClear));
        }

        private void Add(DownLoader newObject)
        {
            groupList.Add(newObject);
        }

        private bool IsHaveURL(string pSourceurl)
        {
            int tindex = groupList.FindIndex((a) => a.SourceURL.Contains(pSourceurl));
            if (tindex != -1)
            {
                Debug.LogError("重复添加下载,url = " + pSourceurl);
                return true;
            }

            return false;
        }

        public void StartAsync()
        {
            if (State != DownloadState.normal) return;
            State = DownloadState.downloading;
            ContentLength = 0;
            for (int i = 0; i < groupList.Count; i++)
            {
                groupList[i].StartAsync();
                ContentLength += groupList[i].InitContentLength;
            }
            DownLoadManager.Add(Key,this);
        }

        public void ReTryAsync()
        {
            if (State != DownloadState.finished || Error == null) return;
            State = DownloadState.normal;
            IsDone = false;
            for (int i = groupList.Count - 1; i >= 0; i--)
            {
                if (groupList[i].IsDone && groupList[i].Error == null)
                    groupList.RemoveAt(i);
                else
                    groupList[i].RestState();
            }
            if (groupList.Count > 0)
                StartAsync();
        }


        bool UpdateChild()
        {
            if(groupList.Count == 0) return true;
            bool isAllDone = true;
            DownLoadedLength = 0;
            Progress = 0;
            for (int i = 0; i < groupList.Count; i++)
            {
                var item = groupList[i];
                DownLoadedLength += item.DownLoadedLength;
                Progress += item.Progress;
                item.Update();
                if (!item.IsDone)
                {
                    isAllDone = false;
                }
            }
            Progress = Progress / groupList.Count;
            return isAllDone;
        }

        void DownLoadComplete()
        {
            if (IsDone) return;
            IsDone = true;

            List<string> tcmpfiles = new List<string>();
            string terror = "";
            for (int i = 0; i < groupList.Count; i++)
            {
                if (groupList[i].Error != null)
                {
                    terror += groupList[i].Error + "|";
                    tcmpfiles.Add(groupList[i].SourceURL);
                }
            }

            Error = terror;

            var tfinished = onComplete;

            try
            {
                if (tfinished != null)
                    tfinished(this);
            }
            catch (System.Exception _error)
            {
                Debug.LogError(_error);
            }
        }
        public void Update()
        {
            if (IsDone) return;

            switch (State)
            {
                case DownloadState.downloading:
                    {
                        if (UpdateChild())
                        {
                            State = DownloadState.finished;
                        }
                        if (OnProgress != null)
                        {
                            OnProgress(this);
                        }
                    }
                    break;
                case DownloadState.finished:
                    {
                        DownLoadComplete();
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
