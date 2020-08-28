using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitEngine.DownLoad;
using LitEngine.UpdateTool;
using LitEngine.LoadAsset;
public class testload : MonoBehaviour
{
    // Start is called before the first frame update
    IEnumerator Start()
    {
        string tqq = "https://down.qq.com/qqweb/PCQQ/PCQQ_EXE/PCQQ2020.exe";
        string wx = "https://dldir1.qq.com/weixin/android/weixin7017android1720_arm64.apk";
        //    DownLoadGroup testload = new DownLoadGroup("testgroup");
        //    testload.onComplete += DownloadCompleteG;
        //    testload.OnProgress += DownloadProcessG;

        //    testload.AddByUrl(tqq,Application.dataPath+"/../","testdownload/qq.exe","3434fdf99df9sfaa",10,true);
        //    testload.AddByUrl(wx,Application.dataPath+"/../","testdownload/wx.apk","ddddffff44",10,true);

        //    testload.StartAsync();
        Debug.Log(UpdateAssetManager.Ins.checkType);
        UpdateAssetManager.Ins.CheckUpdate();
        Debug.Log(UpdateAssetManager.Ins.checkType);
        while (UpdateAssetManager.Ins.checkType == UpdateAssetManager.CheckType.checking)
        {
            yield return null;
        }
        Debug.Log(UpdateAssetManager.Ins.checkType);
        if (UpdateAssetManager.Ins.checkType == UpdateAssetManager.CheckType.needUpdate)
        {
            Debug.Log(UpdateAssetManager.Ins.updateType);
            UpdateAssetManager.Ins.UpdateAssets();
            Debug.Log(UpdateAssetManager.Ins.updateType);
            while (UpdateAssetManager.Ins.updateType == UpdateAssetManager.UpdateType.updateing)
            {
                yield return null;
            }
            Debug.Log(UpdateAssetManager.Ins.updateType);
        }
    }

    void UpdateComplete(ByteFileInfoList pInfo,string pError)
    {
        Debug.Log(pError);
        if(pError == null)
        {

        }
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
