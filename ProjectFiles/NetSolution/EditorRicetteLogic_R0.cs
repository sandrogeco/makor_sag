#region Using directives
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using System;
using UAManagedCore;
using System.Collections.Generic;
using FTOptix.Recipe;
using FTOptix.System;
using FTOptix.S7TiaProfinet;
using FTOptix.S7TCP;
#endregion

public class EditorRicetteLogic_R0 : BaseNetLogic
{
    private GestRicette m_GestRicette;
    
    public override void Start()
    {        
        m_GestRicette = new GestRicette(Owner.GetVariable("NomeTabellaRicetta").Value,
                                          Owner.GetVariable("NomeTabellaDettagli").Value,
                                          Owner.GetVariable("TipologiaRicetta").Value,
                                          Owner.GetVariable("MyStore").Value,
                                          Owner.GetVariable("SourceVarRicetta").Value);   //creo l'istanza della classe gestione ricette
    }

    /// <summary>
    /// Controlla se la ricetta selezionata è presente nel database 
    /// </summary>
    /// <param name="NomeRic"></param>
    [ExportMethod]
    public void CheckSalvataggioRic(string NomeRic)
    {
        // In base al fatto se la ricetta è presente viene mostrato il dialogbox corrispondente alla creazione o al salvataggio ricetta
        IUANode AliasNode = m_GestRicette.IsRecipePresent(NomeRic) ? Owner.Get("CmdRicetta/SalvaRicBtn/ConfirmationDialog_SovraScriviRic") : Owner.Get("CmdRicetta/SalvaRicBtn/ConfirmationDialog_CreaRic");
        Runtime_Utility.ConfermaUser(Owner, AliasNode);
    }

    [ExportMethod]
    public void CreaRic(string NomeRic, string DescrizRicetta)
    {
        Owner.GetVariable("DisabilitaTastiRicetta").Value = true;
        try
        {
            m_GestRicette.CreaRic(NomeRic, DescrizRicetta);
            Owner.GetVariable("NomeRicAttuale").Value = NomeRic;
            AggiornaLista(NomeRic);
        }
        catch (Exception ex)
        {
            Log.Error("Recipe save failed: " + ex.ToString());
            var AliasNode = Owner.Children.Get<ContextDialogConferma_R2>("ConfirmationDialog_Dummy");
            AliasNode.Message = InformationModel.LookupTranslation(new LocalizedText("Errore salvataggio ricetta. Riprovare."));
            Runtime_Utility.ConfermaUser(Owner, AliasNode);
        }
        Owner.GetVariable("DisabilitaTastiRicetta").Value = false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="NomeRic"></param>
    [ExportMethod]
    public void SalvaRic(string NomeRic, string DescrizRicetta)
    {
        Owner.GetVariable("DisabilitaTastiRicetta").Value = true;
        try
        {
            m_GestRicette.SalvaRic(NomeRic, DescrizRicetta);
            Owner.GetVariable("NomeRicAttuale").Value = NomeRic;
            AggiornaLista(NomeRic);
        }
        catch (Exception ex)
        {
            Log.Error("Recipe save failed: " + ex.ToString());
            var AliasNode = Owner.Children.Get<ContextDialogConferma_R2>("ConfirmationDialog_Dummy");
            AliasNode.Message = InformationModel.LookupTranslation(new LocalizedText("Errore salvataggio ricetta. Riprovare."));
            Runtime_Utility.ConfermaUser(Owner, AliasNode);
        }
        Owner.GetVariable("DisabilitaTastiRicetta").Value = false;
    }

    /// <summary>
    /// Controlla se la ricetta selezionata è presente nel database 
    /// </summary>
    /// <param name="NomeRic"></param>
    [ExportMethod]
    public void ClonaRic(string NomeRicDaClonare, string NomeRic, string Descriz)
    {
        //controllo se è possibile creare la ricetta con il nome assegnato
        var ID_Ric = m_GestRicette.GetIdRic(NomeRic);
        if (ID_Ric != 0)
        {
            var AliasNode = Owner.Children.Get<ContextDialogConferma_R2>("ConfirmationDialog_Dummy");
            List<object> Elementi = new() { NomeRic };
            var Traduz = InformationModel.LookupTranslation(new LocalizedText("Ricetta '{0}' non sovrascrivibile"));
            AliasNode.Message.Text = string.Format(Traduz.Text, Elementi.ToArray());
            Runtime_Utility.ConfermaUser(Owner, AliasNode);
        }
        else
        {
            try
            {
                var ID_RicDaClonare = m_GestRicette.GetIdRic(NomeRicDaClonare);
                m_GestRicette.ClonaRicetta(ID_RicDaClonare, NomeRic, Descriz);
                AggiornaLista(NomeRic);
            }
            catch (Exception ex)
            {
                Log.Error("Recipe cloning error: " + ex.ToString());
                var AliasNode = Owner.Children.Get<ContextDialogConferma_R2>("ConfirmationDialog_Dummy");
                AliasNode.Message = InformationModel.LookupTranslation(new LocalizedText("Errore clonazione ricetta. Riprovare."));
                Runtime_Utility.ConfermaUser(Owner, AliasNode);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="NomeRic"></param>
    [ExportMethod]
    public void ApriRic(string NomeRic)
    {
        Owner.GetVariable("DisabilitaTastiRicetta").Value = true;
		Owner.GetVariable("RicAperta").Value = false;
		try
		{
            m_GestRicette.ApriRic(NomeRic);
            Owner.GetVariable("NomeRicAttuale").Value = NomeRic;
        }
        catch (Exception ex)
        {
            Log.Error("Recipe Open failed: " + ex.ToString());
            var AliasNode = Owner.Children.Get<ContextDialogConferma_R1>("ConfirmationDialog_Dummy");
            AliasNode.Message = InformationModel.LookupTranslation(new LocalizedText("Errore apertura ricetta. Riprovare."));
            Runtime_Utility.ConfermaUser(Owner, AliasNode);
        }
        Owner.GetVariable("DisabilitaTastiRicetta").Value = false;
		Owner.GetVariable("RicAperta").Value = true;

	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="NomeRic"></param>
	[ExportMethod]
    public void CancRic(string NomeRic)
    {
        try
        {
            m_GestRicette.CancRic(NomeRic);
            AggiornaLista();
        }
        catch (Exception ex) { Log.Error("Recipe delete failed: " + ex.ToString()); }
    }

    private void AggiornaLista(string NomeRicDaSelez="")
    {
        var Node_RecipeList = Owner.Children.Get("ListBox").Children.Get<DataGrid>("RecipesList");
        Node_RecipeList.Refresh();
        //Node_RecipeList.SelectedItem = NomeRicDaSelez;
    }

	[ExportMethod]
	public void publicAggiornaLista()
	{
		var dataGrid = Owner.Children.Get("ListBox").Children.Get<DataGrid>("RecipesList");
		dataGrid.Refresh();	
	}
}
