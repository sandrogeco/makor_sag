#region Using directives
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using System.Diagnostics;
using UAManagedCore;
using FTOptix.Recipe;
using FTOptix.System;
using FTOptix.S7TiaProfinet;
using FTOptix.S7TCP;
#endregion

public class ShutdownBtn_logic : BaseNetLogic
{
    [ExportMethod]
    public void PcShutdown()
    {
        try
        {
            AppCloseBtn_logic.UnloadApp();
        }
        catch (System.Exception ex)
        {
            Log.Error("Chiusura applicazione", "Errore durante la chiusura dell'applicazione. Errore :" + ex.Message);
        }

        var psi = new ProcessStartInfo("shutdown", "/s /t 0")
        {
            CreateNoWindow = true,
            UseShellExecute = false
        };
        Process.Start(psi);
    }
}
