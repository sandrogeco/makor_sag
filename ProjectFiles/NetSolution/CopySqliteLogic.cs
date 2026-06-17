#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.Retentivity;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.CoreBase;
using FTOptix.Core;
using FTOptix.NetLogic;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.DataLogger;
using System.IO;
using FTOptix.Recipe;
using FTOptix.System;
using FTOptix.S7TiaProfinet;
#endregion

public class CopySqliteLogic : BaseNetLogic
{
    public override void Start()
    {
        string fileFlagPath = ResourceUri.FromApplicationRelativePath($"{LogicObject.BrowseName}.txt").Uri;
        if (File.Exists(fileFlagPath)) return;   
        try
        {
            SQLiteStore targetDB = (SQLiteStore)LogicObject.GetAlias("TargetDB");
            targetDB.Stop();
            string targetDBFileName = targetDB.Filename + (targetDB.Filename.Contains(".sqlite") ? "" : ".sqlite");
            string targetDBFilePath = ResourceUri.FromApplicationRelativePath(targetDBFileName).Uri;
            if (File.Exists(targetDBFilePath))
            {
                File.Delete(targetDBFilePath);
                Log.Debug(LogicObject.BrowseName, "Deleted datastore file");
            }
            ResourceUri sourceFilePath = ResourceUri.FromProjectRelativePath(targetDBFileName).Uri;
            File.Copy(sourceFilePath, targetDBFilePath);
            using (StreamWriter writeTxtFile = new StreamWriter(fileFlagPath))
            {
                writeTxtFile.WriteLine(DateTime.Now.ToString());
            }
            targetDB.Start();
        }
        catch (Exception ex) 
        {
            Log.Error(LogicObject.BrowseName, ex.Message);
            if (File.Exists(fileFlagPath)) File.Delete(fileFlagPath);
        }
        
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }
}
