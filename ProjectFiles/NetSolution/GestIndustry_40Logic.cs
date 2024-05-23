#region Using directives
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Store;
using System;
using System.Collections.Generic;
using UAManagedCore;
#endregion

public class GestIndustry_40Logic : BaseNetLogic
{
    public enum Industry_40_ErrCode
    {
        NoErr,
        RecipeNotUpdated,
        Err,
        PlcCommErr,
        RecipeNotFound
    }
            
    public static void StartLavoraz(ref short ErrCode)
    {
        var o_MachineStatus = Project.Current.Get<stMachineStatusType>("Model/Industry_40/o_MachineStatus");
        var i_NewJob = Project.Current.Get<stNewJobType>("Model/Industry_40/i_NewJob");
        var o_CurrentJob = Project.Current.Get<stJobInfoType>("Model/Industry_40/o_CurrentJob");
        var o_LastJob = Project.Current.Get<stJobInfoType>("Model/Industry_40/o_LastJob");

        //Se c'è una lavorazione in corso la chiudo
        if (o_MachineStatus.bJobInProduc)
            EndLavoraz();

        //Inializzo i contatori della nuova lavorazione
        o_CurrentJob.ChildrenRemoteWrite(new List<RemoteChildVariableValue>()
        {
            new("sJobID",i_NewJob.sNextJobID),
            new("dwProcessedMt", 0),
            new("dwProcessedSqareMt", 0),
            new("dwProcessedPcs", 0),
            new("dwProducHour", 0),
            new("dwProducMin", 0),
            new("dtStartDateTime", DateTime.Now)
        });

        //Resetto i bit di handshaking
        i_NewJob.bLoadNewJobID = false;
        o_MachineStatus.bJobInProduc = false;

        //Gestione Apertura Ricetta. Apro la ricetta solo se il nome è diverso dalla precedente oppure se è uguale allora deve essere abilitato l'apertura da user
        var NewRic = i_NewJob.sNextRecipeName;
        if (!string.IsNullOrEmpty(NewRic) && (!NewRic.Equals(o_LastJob.sRecipeName) || Project.Current.GetVariable("Model/Industry_40/EnbOverwriteRicDaMes").Value))
        {
            using GestRicette m_GestRicette = new("Ricette", "RicetteDettagli", "RicettaProduzione", ((Store)Project.Current.Get("DataStores/DatabaseRicette")).NodeId, Project.Current.Get("Model/VariabiliRicettaProduz").NodeId);

            //Controllo se la ricetta è nel databse della macchina
            if (!m_GestRicette.IsRecipePresent(NewRic))
            {
                ErrCode = (short)Industry_40_ErrCode.RecipeNotFound;
                return;
            }

            if (m_GestRicette.ApriRic(NewRic))
            {
                //Aggiorno Nome Ricetta
                o_CurrentJob.sRecipeName = NewRic;
                ErrCode = (short)Industry_40_ErrCode.NoErr;
            }
        }
        else
        {
            ErrCode = (short)Industry_40_ErrCode.RecipeNotUpdated;
        }

        //Metto in produzione il Job
        o_MachineStatus.bJobInProduc = true;
    }

    [ExportMethod]
    public static void EndLavoraz()
    {
        var o_CurrentJob = Project.Current.Get<stJobInfoType>("Model/Industry_40/o_CurrentJob");
        var o_LastJob = Project.Current.Get<stJobInfoType>("Model/Industry_40/o_LastJob");

        //aggiorno l'ora di fine lavorazione
        o_CurrentJob.dtEndDateTime = DateTime.Now;

        //Sposto i contatori dalla lav attuale alla lav precedente
        o_LastJob.ChildrenRemoteWrite(o_CurrentJob.ChildrenRemoteRead());

        Project.Current.GetVariable("Model/Industry_40/o_MachineStatus/dwProcessedJobsAmount").Value++;
        Project.Current.GetVariable("Model/Industry_40/o_MachineStatus/bJobInProduc").Value = false;
    }
}
