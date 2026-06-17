#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.NativeUI;
using FTOptix.WebUI;
using FTOptix.CommunicationDriver;
using FTOptix.Alarm;
using FTOptix.CoreBase;
using FTOptix.CODESYS;
using FTOptix.S7TiaProfinet;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.OPCUAServer;
using FTOptix.OPCUAClient;
using FTOptix.Retentivity;
using FTOptix.EventLogger;
using FTOptix.Core;
#endregion

public class AggiornaViewer : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void Method1()
    {
        // Insert code to be executed by the method
        var wb = Owner.Get<WebBrowser>("WebContainer/WebBrowser1");
        wb.URL = ResourceUri.FromProjectRelativePath("").Uri.ToString().Replace("\\", "/") + "/widget/index.html#larghezza=1200;altezza=900;lingua=it;um=mm;mode=viewer;csv=file:///actualCSV.csv";
    }
}
