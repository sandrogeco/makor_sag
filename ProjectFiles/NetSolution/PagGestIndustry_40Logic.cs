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
using FTOptix.Retentivity;
using FTOptix.EventLogger;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.OPCUAClient;
using FTOptix.Core;
#endregion

public class PagGestIndustry_40Logic : BaseNetLogic
{
    [ExportMethod]
    public void StartLavoraz()
    {
        var NewRic = Project.Current.GetVariable("Model/Industry_40/i_NewJob/sNextRecipeName").Value.Value.ToString();

        //Controllo se la ricetta è nel databse della macchina
        using GestRicette m_GestRicette = new("Ricette", "RicetteDettagli", "RicettaProduzione", ((Store)Project.Current.Get("DataStores/DatabaseRicette")).NodeId, Project.Current.Get("Model/VariabiliRicettaProduz").NodeId);
        if (m_GestRicette.IsRecipePresent(NewRic))
        {
            var Metodo = Project.Current.Get<NetLogicObject>("Scripts/GestIndustry_40Logic");
            Metodo.ExecuteMethod("StartLavoraz");
        }
        else
        {
            Log.Warning("Industry 4.0", "La ricetta richiesta dal MES non esiste nel database della macchina");
            var AlisNode = Owner.Children.Get("RicettaMesNonCorretta");
            Runtime_Utility.ConfermaUser(Owner, AlisNode);
        }
    }

    [ExportMethod]
    public void EndLavoraz()
    {
        var Metodo = Project.Current.Get<NetLogicObject>("Scripts/GestIndustry_40Logic");
        Metodo.ExecuteMethod("EndLavoraz");
    }
}
