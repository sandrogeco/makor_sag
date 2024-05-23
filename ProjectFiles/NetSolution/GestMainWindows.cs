#region Using directives
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Store;
using System;
using System.IO;
using UAManagedCore;
using static GestIndustry_40Logic;

#endregion

public class GestMainWindows : BaseNetLogic
{
    private IUAVariable bLoadNewJobID;
    public override void Start()
    {
        //Controllo se eseiste la variabile che identifica il tipo di sessione
        var isNativeUI = Session.GetVariable("IsNativeUI");
        //if (isNativeUI == null)        
        //    Session.Add(InformationModel.MakeVariable("IsNativeUI", OpcUa.DataTypes.Boolean));        

        var presentationEngine = GetFindPresentationEngine();
        if (presentationEngine != null)
            isNativeUI.Value = presentationEngine.IsInstanceOf(FTOptix.NativeUI.ObjectTypes.NativeUIPresentationEngine);

        if (isNativeUI.Value)
        {
            bLoadNewJobID = Project.Current.GetVariable("Model/Industry_40/i_NewJob/bLoadNewJobID");
            bLoadNewJobID.VariableChange += CaricaNuovaCommessa_VariableChange;
        }
    }

    private IUAObject GetFindPresentationEngine()
    {
        IUANode currentNode = Session;
        while (true)
        {
            if (currentNode == null)
                return null;

            var currentObject = (IUAObject)currentNode;
            if (currentObject != null && currentObject.IsInstanceOf(FTOptix.UI.ObjectTypes.PresentationEngine))
                return currentObject;

            currentNode = currentNode.Owner;
        }
    }

    public override void Stop()
    {
        bLoadNewJobID.VariableChange -= CaricaNuovaCommessa_VariableChange;
    }

    private void CaricaNuovaCommessa_VariableChange(object sender, VariableChangeEventArgs e)
    {
        if (e.NewValue)
        {
            short ErrCode = 0;
            string Err = "";
            try
            {
                //Se sono connesso al PLC allora entro nella gestione altrimenti salto
                if (Project.Current.Get<CommunicationStation>("CommDrivers/CODESYSDriver1/PLC_Next").OperationCode == CommunicationOperationCode.Connected)
                {
                    var NewRic = Project.Current.GetVariable("Model/Industry_40/i_NewJob/sNextRecipeName").Value.Value.ToString();

                    //Per avviare il Job controllo se la ricetta è nel databse della macchina oppure è stata passata una ricetta vuota
                    using GestRicette m_GestRicette = new("Ricette", "RicetteDettagli", "RicettaProduzione", ((Store)Project.Current.Get("DataStores/DatabaseRicette")).NodeId, Project.Current.Get("Model/VariabiliRicettaProduz").NodeId);
                    if (string.IsNullOrEmpty(NewRic) || m_GestRicette.IsRecipePresent(NewRic))
                    {
                        GestIndustry_40Logic.StartLavoraz(ref ErrCode);
                    }
                    else
                    {
                        ErrCode = (short)Industry_40_ErrCode.RecipeNotFound;
                    }
                }
                else
                {
                    ErrCode = (short)Industry_40_ErrCode.PlcCommErr;
                }
            }
            catch (Exception Ex)
            {
                ErrCode = (short)Industry_40_ErrCode.Err;
                Err = Ex.Message;
            }
            finally
            {
                //Se ho un errore lo porto all'attenzione dell'operatore
                if (ErrCode >= 2)
                {
                    //var AliasNode = ErrCode switch
                    //{
                    //    (short)Industry_40_ErrCode.PlcCommErr => LogicObject.Children.Get<ContextDialogConferma_R2>("GestIndustria4_0_RecipeNotFound"),
                    //    (short)Industry_40_ErrCode.RecipeNotFound => LogicObject.Children.Get<ContextDialogConferma_R2>("GestIndustria4_0_RecipeNotFound"),
                    //    _ => LogicObject.Children.Get<ContextDialogConferma_R2>("GestIndustria4_0_Err"),
                    //};

                    ContextDialogConferma_R2 AliasNode;
                    switch (ErrCode)
                    {
                        case (short)Industry_40_ErrCode.PlcCommErr:
                            AliasNode = LogicObject.Children.Get<ContextDialogConferma_R2>("GestIndustria4_0_NoPlcConn");
                            Log.Warning("Industry 4.0", "PLC non Connesso");
                            break;
                        case (short)Industry_40_ErrCode.RecipeNotFound:
                            AliasNode = LogicObject.Children.Get<ContextDialogConferma_R2>("GestIndustria4_0_RecipeNotFound");
                            Log.Warning("Industry 4.0", "La ricetta richiesta dal MES non esiste nel database della macchina");
                            break;
                        default:
                            AliasNode = LogicObject.Children.Get<ContextDialogConferma_R2>("GestIndustria4_0_Err");
                            Log.Warning("Industry 4.0", "Errore: " + Err);
                            break;
                    }

                    Runtime_Utility.ConfermaUser(Owner, AliasNode);
                }

                Project.Current.GetVariable("Model/Industry_40/wLoadNewJobErrCode").Value = ErrCode;
                bLoadNewJobID.Value = false;
            }
        }
    }
}
