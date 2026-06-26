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

public class AppCloseBtn_logic : BaseNetLogic
{
    [ExportMethod]
    public void QApplicationClose()
    {
        try
        {
            UnloadApp();
        }
        catch (System.Exception ex)
        {
            Log.Error("Chiusura applicazione", "Errore durante la chiusura dell'applicazione. Errore :" + ex.Message);            
        }
        
        var psi = new ProcessStartInfo("taskkill", "/F /IM FTOptixRuntime.exe")
        {
            CreateNoWindow = true,
            UseShellExecute = false
        };
        Process.Start(psi);
    }


    /// <summary>
    /// Questo metodo si occupa di eseguire tutte quelle procedure necessario prima di chiudere l'applicazione. Ad es. salvataggio della gestione statistiche
    /// </summary>
    public static void UnloadApp()
    {
        Project.Current.Get<NetLogicObject>("UI/Panels/GestStatistiche/GestStatisticheLogic").ExecuteMethod("TerminaSessione");    // serve per aggiornare le statistiche
    }
}
