using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using LitEngine.LoadAsset;
namespace LitEngineEditor
{
    public class EXportMenuTool
    {
        
        [UnityEditor.MenuItem("Export/IOS生成资源表及资源")]
        static public void BuildIOSAssetProcess()
        {
            BuildAndroidAssetProcess(1);
        }

        [UnityEditor.MenuItem("Export/Android生成资源表及资源")]
        static public void BuildAndroidAssetProcess()
        {
            BuildAndroidAssetProcess(0);
        }

        static public void BuildAndroidAssetProcess(int platm)
        {
            CreatModelAsset();
            ExportSetting.Instance.sSelectedPlatm = platm;
            ExportSetting.Instance.sCompressed = 0;
            ExportSetting.Instance.sBuildType = 1;
            ExportObject.ExportAllBundleFullPath(ExportObject.sBuildTarget[ExportSetting.Instance.sSelectedPlatm]);
            ExportObject.MoveBUndleToStreamingPath(ExportObject.sBuildTarget[ExportSetting.Instance.sSelectedPlatm]);
        }

        [UnityEditor.MenuItem("Export/生成Resources预留资源表")]
        static public void CreatModelAsset()
        {
            string tgamepath = "Assets/Resources/Game/Models";
            string texpaths = "Assets/Resources/Game/ModelsTexture";
            string texports = "Assets/ExportResources";

            List<AssetMap.AssetObject> tfiles = GetFileList(tgamepath,true);
            tfiles.AddRange(GetFileList(texpaths,true));
            tfiles.AddRange(GetFileList(texports,false));
            AssetMap asset = ScriptableObject.CreateInstance<AssetMap>();
            asset.assets = tfiles.ToArray();
            AssetDatabase.CreateAsset(asset, "Assets/Resources/ResourcesMap.asset");
            AssetDatabase.Refresh();
            Debug.Log("生成资源表成功.");
        }

        static List<AssetMap.AssetObject> GetFileList(string pFullPath,bool isInSide)
        {
            List<AssetMap.AssetObject> tassetNames = new List<AssetMap.AssetObject>();
            if (!Directory.Exists(pFullPath))
            {
                return tassetNames;
            }
            string trepPath = Application.dataPath;
            DirectoryInfo tdirfolder = new DirectoryInfo(pFullPath);   
            FileInfo[] tfileinfos = tdirfolder.GetFiles("*.*", System.IO.SearchOption.AllDirectories);
            for (int i = 0, tmax = tfileinfos.Length; i < tmax; i++)
            {
                FileInfo tfile = tfileinfos[i];
                if (!ExportObject.IsResFile(tfile.Name)) continue;
                string tresPath = ExportObject.GetFormatPath(tfile.FullName).Replace(trepPath,"").ToLowerInvariant();
                string tfindstr = "Resources/".ToLowerInvariant();
                int tindex = tresPath.IndexOf(tfindstr) + tfindstr.Length;
                tresPath = tresPath.Substring(tindex, tresPath.Length - tindex);
                AssetMap.AssetObject tobj = new AssetMap.AssetObject(tresPath);
                tobj.isInSide = isInSide;
                tassetNames.Add(tobj);
                EditorUtility.DisplayProgressBar(pFullPath + "文件夹", tresPath , (float)i / tmax);
            }
            EditorUtility.ClearProgressBar();
            return tassetNames;
        }

        [UnityEditor.MenuItem("Export/CreatDirectory For App")]
        static void CreatDirectoryForApp()
        {
            if (!Directory.Exists(ExportBase.Config.sResourcesPath))
                Directory.CreateDirectory(ExportBase.Config.sResourcesPath);
            if (!Directory.Exists(ExportBase.Config.sDefaultFolder))
                Directory.CreateDirectory(ExportBase.Config.sDefaultFolder);

            CreatLitEngineFolders(ExportBase.Config.sStreamingBundleFolder);
            CreatLitEngineFolders(ExportBase.Config.sEditorBundleFolder);
        }

        static void CreatLitEngineFolders(string rootPath)
        {
            string tresfolder = "ResData/";

            if (!Directory.Exists(rootPath))
                Directory.CreateDirectory(rootPath);

            CreatDirectory(rootPath, tresfolder);
        }

        static void CreatDirectory(string rootPath, string forlder)
        {
            string tpath = rootPath + forlder;
            if (!Directory.Exists(tpath))
                Directory.CreateDirectory(tpath);
        }
    }
}



