using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitEngine.LoadAsset.DownLoad;
public class testload : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        string tqq = "https://down.qq.com/qqweb/PCQQ/PCQQ_EXE/PCQQ2020.exe";
        string wx = "https://dldir1.qq.com/weixin/android/weixin7017android1720_arm64.apk";
       //DownLoadManager.DownLoadFileAsync(tqq,Application.dataPath+"/../",0,DownloadComplete,DownloadProcess);

       DownLoadGroup testload = new DownLoadGroup("testgroup");
       testload.onComplete += DownloadCompleteG;
       testload.OnProgress += DownloadProcessG;

       testload.AddByUrl(tqq,Application.dataPath+"/../","testdownload/qq.exe","3434fdf99df9sfaa",10,true);
       testload.AddByUrl(wx,Application.dataPath+"/../","testdownload/wx.apk","ddddffff44",10,true);

       testload.StartAsync();
    }

    void DownloadComplete(DownLoader pSender)
    {
        Debug.Log("DownloadComplete-" + pSender.CompleteFile + " - error:" + pSender.Error);
    }
    void DownloadProcess(DownLoader pSender)
    {
        Debug.Log(pSender.Progress);
    }

    void DownloadCompleteG(DownLoadGroup pSender)
    {
        Debug.Log("DownloadComplete-" + pSender.Key + " - error:" + pSender.Error);
    }
    void DownloadProcessG(DownLoadGroup pSender)
    {
        Debug.Log(pSender.Progress);
    }
}
