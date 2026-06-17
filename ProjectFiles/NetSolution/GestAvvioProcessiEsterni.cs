#region Using directives
using FTOptix.Core;
using FTOptix.NetLogic;
using System.Diagnostics;
using FTOptix.Recipe;
using FTOptix.System;
using FTOptix.S7TiaProfinet;
#endregion

public class GestAvvioProcessiEsterni : BaseNetLogic
{
    [ExportMethod]
    public static void Start_VLCPlayer()
    {
        var Path = ResourceUri.FromProjectRelativePath("_MakorUtility/ConnIPCam.bat");   // con FromProjectRelativePath vado a cercare nella cartella "ProjectFiles"
        string BatPath = Path.Uri;          // la proprietà URi contiene il percorso assoluto (formato URI) del file/cartella ricercata 

        Process Batch = new();
        Batch.StartInfo.FileName = "cmd.exe";
        Batch.StartInfo.Arguments = "/C " + BatPath;
        Batch.Start();
    }
}
