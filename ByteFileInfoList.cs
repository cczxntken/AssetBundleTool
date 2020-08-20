using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
namespace LitEngine.LoadAsset
{
    public class ByteFileInfo
    {
        public string fileFullPath { get; set; }
        public string resName = "";
        public string fileMD5 = "";
        public long fileSize = 0;
    }
    public class ByteFileInfoList
    {

        public Dictionary<string, ByteFileInfo> fileMap { get { return _fileMap; } }
        Dictionary<string, ByteFileInfo> _fileMap = new Dictionary<string, ByteFileInfo>();

        public ByteFileInfoList(byte[] pData)
        {
           Load(pData);
        }

        public void Load(byte[] pData)
        {
            if (pData != null)
            {
                fileMap.Clear();
                List<string> tlines = new List<string>();
                StreamReader treader = new StreamReader(new MemoryStream(pData));
                string item = treader.ReadLine();
                while (item != null)
                {
                    tlines.Add(item);
                    item = treader.ReadLine();
                }
                LoadByLines(tlines.ToArray());
            }
        }

        public void Save(string pFullPath)
        {
            try
            {
                string tfilePath = pFullPath;
                if (File.Exists(tfilePath))
                {
                    File.Delete(tfilePath);
                }
                var tlist = new List<ByteFileInfo>(fileMap.Values);
                StringBuilder tstrbd = new StringBuilder();

                for (int i = 0,tcount = tlist.Count; i < tcount; i++)
                {
                    var item = tlist[i];
                    string tline = UnityEngine.JsonUtility.ToJson(item);
                    tstrbd.AppendLine(tline);
                }
                File.AppendAllText(tfilePath, tstrbd.ToString());
            }
            catch (System.Exception ex)
            {
                Debug.LogError("生成文件信息出现错误, error:" + ex.Message);
            }

            Debug.Log("生成文件信息完成.");
        }

        private void LoadByLines(string[] pLines)
        {
            if (pLines == null || pLines.Length == 0) return;
            for (int i = 0, len = pLines.Length; i < len; i++)
            {
                try
                {
                    ByteFileInfo tinfo = UnityEngine.JsonUtility.FromJson<ByteFileInfo>(pLines[i]);
                    fileMap.Add(tinfo.resName, tinfo);
                }
                catch (System.Exception erro)
                {
                    Debug.LogErrorFormat("初始化json数据出现错误.line = {0},str = {1},erro = {2}", i, pLines[i], erro.Message);
                }
            }
        }
    
        public List<ByteFileInfo> Comparison(Dictionary<string, ByteFileInfo> pSor)
        {
            List<ByteFileInfo> ret = new List<ByteFileInfo>();
            foreach (var item in pSor)
            {
                bool isNeedUpdate = false;
                if(!fileMap.ContainsKey(item.Key))
                {
                    isNeedUpdate = true;
                }
                else
                {
                    var ttar = fileMap[item.Key];
                    if(ttar.fileSize != item.Value.fileSize || !ttar.fileMD5.Equals(item.Value.fileMD5))
                    {
                        isNeedUpdate = true;
                    }
                }


                if(isNeedUpdate)
                {
                    ret.Add(item.Value);
                }
            }
            return ret;
        }
    }
}
