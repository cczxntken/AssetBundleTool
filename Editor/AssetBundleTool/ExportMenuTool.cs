using UnityEditor;
using UnityEngine;
using System.IO;
namespace LitEngineEditor
{
    public class EXportMenuTool
    {
        [UnityEditor.MenuItem("Export/CreatDirectory For App")]
        static void CreatDirectoryForApp()
        {
            if (!Directory.Exists(ExportBase.Config.sResourcesPath))
                Directory.CreateDirectory(ExportBase.Config.sResourcesPath);
            if (!Directory.Exists(ExportBase.Config.sDefaultFolder))
                Directory.CreateDirectory(ExportBase.Config.sDefaultFolder);

            CreatLitEngineFolders(ExportBase.Config.sStreamingBundleFolder);
            CreatLitEngineFolders(ExportBase.Config.sEditorBundleFolder);
            CreatLitEngineFolders("Assets/Resources/Data/");
        }

        static void CreatLitEngineFolders(string rootPath)
        {
            string tconfigfolder = "ConfigData/";
            string tdllfolder = "LogicDll/";
            string tresfolder = "ResData/";

            if (!Directory.Exists(rootPath))
                Directory.CreateDirectory(rootPath);

            CreatDirectory(rootPath, tconfigfolder);
            CreatDirectory(rootPath, tdllfolder);
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



