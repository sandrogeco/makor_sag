#region Using directives
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using System.Diagnostics;
using UAManagedCore;
#endregion

public class AppCloseBtn_logic : BaseNetLogic
{
    [ExportMethod]
    public void QApplicationClose()
    {
        Project.Current.Get<NetLogicObject>("UI/Panels/GestStatistiche/GestStatisticheLogic").ExecuteMethod("TerminaSessione");    // serve per aggiornare le statistiche
        var psi = new ProcessStartInfo("taskkill", "/F /IM QRuntime.exe")
        {
            CreateNoWindow = true,
            UseShellExecute = false
        };
        Process.Start(psi);
    }
}
