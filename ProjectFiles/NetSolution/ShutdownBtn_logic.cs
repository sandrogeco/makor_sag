#region Using directives
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using System.Diagnostics;
using UAManagedCore;
#endregion

public class ShutdownBtn_logic : BaseNetLogic
{
    [ExportMethod]
    public void PcShutdown()
    {
        Project.Current.Get<NetLogicObject>("UI/Panels/GestStatistiche/GestStatisticheLogic").ExecuteMethod("TerminaSessione");    // serve per aggiornare le statistiche
        var psi = new ProcessStartInfo("shutdown", "/s /t 0")
        {
            CreateNoWindow = true,
            UseShellExecute = false
        };
        Process.Start(psi);
    }
}
