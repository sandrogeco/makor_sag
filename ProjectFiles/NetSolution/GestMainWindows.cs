#region Using directives
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Store;
using System;
using System.IO;
using UAManagedCore;

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
            var NewRic = Project.Current.GetVariable("Model/Industry_40/i_NewJob/sNextRecipeName").Value.Value.ToString();

            //Controllo se la ricetta è nel databse della macchina
            using GestRicette m_GestRicette = new("Ricette", "RicetteDettagli", "RicettaProduzione", ((Store)Project.Current.Get("DataStores/DatabaseRicette")).NodeId, Project.Current.Get("Model/VariabiliRicettaProduz").NodeId);
            if (string.IsNullOrEmpty(NewRic) || m_GestRicette.IsRecipePresent(NewRic))
            {
                try
                {
                    //var Metodo = Project.Current.Get<NetLogicObject>("Scripts/GestIndustry_40Logic");
                    //Metodo.ExecuteMethod("StartLavoraz");
                    GestIndustry_40Logic.StartLavoraz();
                }
                catch (Exception)
                {
                    Log.Warning("Industry 4.0", "Errore gestione");
                }                
            }
            else
            {
                Log.Warning("Industry 4.0", "La ricetta richiesta dal MES non esiste nel database della macchina");
                var AlisNode = LogicObject.Children.Get("RicettaMesNonCorretta");
                Runtime_Utility.ConfermaUser(Owner, AlisNode);
                Project.Current.GetVariable("Model/Industry_40/wLoadNewJobErrCode").Value = 2;
            }

            bLoadNewJobID.Value = false;
        }
    }
}
