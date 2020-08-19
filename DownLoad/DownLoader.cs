using UnityEngine.Networking;
using System.IO;
using UnityEngine;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace LitEngine.LoadAsset.DownLoad
{
    public class DownLoader : IDownLoad
    {
        #region event
        public event System.Action<DownLoader> OnStart = null;
        public event System.Action<DownLoader> OnComplete = null;
        public event System.Action<DownLoader> OnProgress = null;
        #endregion
        #region 属性
        public string DestinationPath { get; private set; }
        public string SourceURL { get; private set; }
        public string TempFile { get; private set; }
        public string CompleteFile { get; private set; }
        public string FileName { get; private set; }
        public string Error { get; private set; }
        public float Progress
        {
            get
            {
                return Mathf.Clamp01(ContentLength > 0 ? (float)DownLoadedLength / ContentLength : 0);
            }
        }
        
        public DownloadState State { get; private set; }
        public bool IsDone { get;private set;}

        public long ContentLength { get; private set; }
        public long DownLoadedLength { get; private set; }
        public long InitContentLength { get; private set; }


        private bool mIsClear = false;

        private bool mThreadRuning = false;

        private HttpWebRequest mReqest;
        private WebResponse mResponse;
        private Stream mHttpStream;


        #endregion
        #region 构造析构
        public DownLoader(string pSourceurl, string pDestination,long pLength, bool pClear)
        {
            SourceURL = pSourceurl;
            DestinationPath = pDestination;
            mIsClear = pClear;
            InitContentLength = pLength;

            Error = null;

            string[] turlstrs = SourceURL.Split('/');
            FileName = turlstrs[turlstrs.Length - 1];

            TempFile = DestinationPath + "/" + FileName + ".temp";
            CompleteFile = DestinationPath + "/" + FileName;

            State = DownloadState.normal;
        }

        ~DownLoader()
        {
            Dispose(false);
        }

        private bool mDisposed = false;
        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        private void Dispose(bool pDisposing)
        {
            if (mDisposed)
                return;
            mDisposed = true;

            mThreadRuning = false;
            CloseHttpClient();
        }
        #endregion

        public void RestState()
        {
            if (State != DownloadState.finished || Error == null) return;
            State = DownloadState.normal;
            IsDone = false;
        }

        public void StartAsync()
        {
            if (State != DownloadState.normal) return;
            State = DownloadState.downloading;
            mThreadRuning = true;
            OnStart?.Invoke(this);
            Task.Run((System.Action)ReadNetByte);
        }

        private void ReadNetByte()
        {
            FileStream ttempfile = null;
            try
            {
                if(!Directory.Exists(DestinationPath))
                {
                    Directory.CreateDirectory(DestinationPath);
                }
                long thaveindex = 0;
                if (File.Exists(TempFile))
                {

                    if (!mIsClear)
                    {
                        ttempfile = System.IO.File.OpenWrite(TempFile);
                        thaveindex = ttempfile.Length;
                        ttempfile.Seek(thaveindex, SeekOrigin.Current);
                    }
                    else
                    {
                        File.Delete(TempFile);
                        thaveindex = 0;
                    }

                }

                mReqest = (HttpWebRequest)HttpWebRequest.Create(SourceURL);
                mReqest.Timeout = 20000;

                if (SourceURL.Contains("https://"))
                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);

                if (thaveindex > 0)
                    mReqest.AddRange((int)thaveindex);

                int tindex = 0;
                while (tindex++ < 4 && mThreadRuning)
                {
                    try
                    {
                        mResponse = mReqest.GetResponse();
                        break;
                    }
                    catch (System.Exception _error)
                    {
                        Debug.LogFormat("ReadNetByte 获取Response失败,尝试次数{0},error = {1}", tindex, _error);
                    }
                }

                if (mResponse == null)
                    throw new System.NullReferenceException("ReadNetByte 获取Response失败.");

                mHttpStream = mResponse.GetResponseStream();
                ContentLength = mResponse.ContentLength;
                InitContentLength = ContentLength;//重置为实际大小

                if (ttempfile == null)
                    ttempfile = System.IO.File.Create(TempFile);

                int tcount = 0;
                int tlen = 1024;
                byte[] tbuffer = new byte[tlen];
                int tReadSize = 0;
                tReadSize = mHttpStream.Read(tbuffer, 0, tlen);
                while (tReadSize > 0 && mThreadRuning)
                {
                    DownLoadedLength += tReadSize;
                    ttempfile.Write(tbuffer, 0, tReadSize);
                    tReadSize = mHttpStream.Read(tbuffer, 0, tlen);

                    if (++tcount >= 512)
                    {
                        ttempfile.Flush();
                        tcount = 0;
                    }

                }
            }
            catch (System.Exception _error)
            {
                Error = _error.ToString();
            }

            if (ttempfile != null)
                ttempfile.Close();

            if (DownLoadedLength == ContentLength)
            {
                if (File.Exists(TempFile))
                {
                    if (File.Exists(CompleteFile))
                    {
                        File.Delete(CompleteFile);
                    }
                    File.Move(TempFile, CompleteFile);
                }
            }
            else
            {
                if (Error == null)
                    Error = "文件未能完成下载.Stream 被中断.";
            }

            CloseHttpClient();
            State = DownloadState.finished;
            
        }
        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true;
        }
        private void CloseHttpClient()
        {
            if (mHttpStream != null)
            {
                mHttpStream.Close();
                mHttpStream.Dispose();
                mHttpStream = null;
            }

            if (mResponse != null)
            {
                mResponse.Close();
                mResponse = null;
            }

            if (mReqest != null)
            {
                mReqest.Abort();
                mReqest = null;
            }
        }
    

        void CallComplete()
        {
            if(IsDone) return;
            IsDone = true;
            try
            {
                 OnComplete?.Invoke(this);
            }
            catch (System.Exception e)
            {
                Debug.LogErrorFormat("DownLoader->CallComplete 出现错误.Url = {0},erro = {1}",SourceURL,e.ToString());
            }
           
        }
        public void Update()
        {
            if (IsDone) return;
            switch (State)
            {
                case DownloadState.downloading:
                    {
                        OnProgress?.Invoke(this);
                    }
                    break;
                case DownloadState.finished:
                    {
                        CallComplete();
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
