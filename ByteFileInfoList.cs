using UnityEngine;
using System.Collections.Generic;
using System.IO;
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
