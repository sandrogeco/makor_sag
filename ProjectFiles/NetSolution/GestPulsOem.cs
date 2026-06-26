#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Alarm;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.WebUI;
using FTOptix.CODESYS;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.OPCUAServer;
using FTOptix.OPCUAClient;
using FTOptix.Retentivity;
using FTOptix.EventLogger;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using FTOptix.System;
using FTOptix.S7TiaProfinet;
using FTOptix.S7TCP;

#endregion

public class GestPulsOem : BaseNetLogic
{
    /// <summary>
    /// Metodo che apre il popUp di login se il pulsante setup oem viene cliccato quando l'user non è Oem
    /// Dopo eseguito il login controlla se l'utente loggato è Oem se si apre il setup Oem
    /// </summary>
    [ExportMethod]
    public void gestOpeningOemPage()
    {
        var isOem = Session.GetVariable("Groups/UtentiMakor");
        PanelLoader mainPanel = Session.GetPanelLoader("MainPanelLoader");

        if (isOem.Value)                                                            //Mi chiedo se l'user attuale e Oem
        {
            mainPanel.ChangePanel(Project.Current.Get("UI/Panels/Setup/Oem/Setup_Oem_Main").NodeId);    //Se si apro il setup Oem e chiudo il menu
            Session.Get("UIRoot").GetObject("Menu").ExecuteMethod("Close");
        }
        else
        {
            _ = UICommands.OpenDialog(LogicObject.Owner, Project.Current.Get("UI/Panels/Prj_DialogBox").Get<DialogType>("Login"), NodeId.Empty);        //Se no apro il PopUp per il login
            var myTask = new LongRunningTask(task, LogicObject);                //E creo una funzione che si svolgerà in parallelo (non blocca l'hmi durante l'esecuzione)
            myTask.Start();
        }

        void task()
        {
            while (Session.Get("UIRoot").GetObject("Login") != null)        //Aspetto finche il PopUp di login non sia chiuso controllando ogni 0.5 sec
            {
                Thread.Sleep(500);
            }
            if (isOem.Value)                                                //Quando il PopUp di login si è chiuso mi chiedo se l'user attuale è Oem
                mainPanel.ChangePanel(Project.Current.Get("UI/Panels/Setup/Oem/Setup_Oem_Main").NodeId);	//se si apro il setup Oem altrimenti non faccio nulla
            Session.Get("UIRoot").GetObject("Menu").ExecuteMethod("Close");
        }
    }
}
