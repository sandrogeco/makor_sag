#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.NativeUI;
using FTOptix.NetLogic;
using FTOptix.HMIProject;
using FTOptix.MelsecFX3U;
using FTOptix.UI;
using FTOptix.S7TiaProfinet;
using FTOptix.MelsecQ;
using FTOptix.EventLogger;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.OPCUAServer;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Alarm;
using FTOptix.CommunicationDriver;
using FTOptix.OPCUAClient;
using FTOptix.Core;
using System.Collections.Generic;
using System.Linq;
using FTOptix.CODESYS;
using FTOptix.SQLiteStore;
using FTOptix.Recipe;
using FTOptix.System;
using FTOptix.S7TCP;
#endregion

public class GestUtentiLogic : BaseNetLogic
{
	[ExportMethod]
	public void SaveUser(string username, string password, string locale, int MeasureSystem, string Gruppo)
    {
		var NodoUtenti = InformationModel.Get(Owner.GetVariable("Users").Value);
        if (NodoUtenti is null)
			return;

        var BrowsePathGruppi = "Users/Groups/";
        var user = NodoUtenti.Get<MakorUserType>(username);
        if (user is null)
        {
            user = InformationModel.MakeObject<MakorUserType>(username);
            NodoUtenti.Add(user);
		}
        else
        {
            //se l'utente � presente e il gruppo di appartenenza � diverso da quello selezionato allora esco dalla routine 
            if (user.GruppoAppartenenza != Gruppo)
            {
                LogicObject.GetVariable("Gruppo").Value = Project.Current.Get<Group>(BrowsePathGruppi + user.GruppoAppartenenza).DisplayName;
                IUANode DialogConfOperatore = Project.Current.Get("UI/ObjectTypes/Panels/ConfirmationDialog");
                ((DialogType)DialogConfOperatore).SetAlias("ConfirmationDialogContext", Owner.Children.Get<ContextDialogConferma_R1>("ContextDialogNoOverwrite"));    // imposto l'alias       
                UICommands.OpenDialog(Owner, (DialogType)DialogConfOperatore);
                return;
            }
        }

        _ = Session.ChangePasswordInternal(username, password);

        user.LocaleId =  locale ;
        user.GruppoAppartenenza = Gruppo;

        //in base al gruppo di appartenenza dell'utente lo devo assegnare anche ai gruppi di livello inferiore        
        var OperatoriMacchinaNode = Project.Current.Get(BrowsePathGruppi + "OperatoriMacchina");
        var UtentiManutenzioneNode = Project.Current.Get(BrowsePathGruppi + "UtentiManutenzione");
        var UtentiSpecializzatiNode = Project.Current.Get(BrowsePathGruppi + "UtentiSpecializzati");
        var UtentiMakorNode = Project.Current.Get(BrowsePathGruppi + "UtentiMakor");

        switch (Gruppo)
        {
            case "OpertoriMacchina":
                user.Refs.AddReference(FTOptix.Core.ReferenceTypes.HasGroup, InformationModel.Get(OperatoriMacchinaNode.NodeId));
                break;
            case "UtentiManutenzione":
                user.Refs.AddReference(FTOptix.Core.ReferenceTypes.HasGroup, InformationModel.Get(OperatoriMacchinaNode.NodeId));
                user.Refs.AddReference(FTOptix.Core.ReferenceTypes.HasGroup, InformationModel.Get(UtentiManutenzioneNode.NodeId));
                break;
            case "UtentiSpecializzati":
                user.Refs.AddReference(FTOptix.Core.ReferenceTypes.HasGroup, InformationModel.Get(OperatoriMacchinaNode.NodeId));
                user.Refs.AddReference(FTOptix.Core.ReferenceTypes.HasGroup, InformationModel.Get(UtentiManutenzioneNode.NodeId));
                user.Refs.AddReference(FTOptix.Core.ReferenceTypes.HasGroup, InformationModel.Get(UtentiSpecializzatiNode.NodeId));
                break;
            case "UtentiMakor":
                user.Refs.AddReference(FTOptix.Core.ReferenceTypes.HasGroup, InformationModel.Get(OperatoriMacchinaNode.NodeId));
                user.Refs.AddReference(FTOptix.Core.ReferenceTypes.HasGroup, InformationModel.Get(UtentiManutenzioneNode.NodeId));
                user.Refs.AddReference(FTOptix.Core.ReferenceTypes.HasGroup, InformationModel.Get(UtentiSpecializzatiNode.NodeId));
                user.Refs.AddReference(FTOptix.Core.ReferenceTypes.HasGroup, InformationModel.Get(UtentiMakorNode.NodeId));
                break;
        }
        

        switch (MeasureSystem)
        {
            case 1:
                user.MeasurementSystem = MeasurementSystem.InternationalSystemOfUnits;
                break;
            case 2:
                user.MeasurementSystem = MeasurementSystem.USCustomaryMeasurementSystem;
                break;
            case 3:
                user.MeasurementSystem = MeasurementSystem.BritishImperialUnits;
                break;
            default:
                user.MeasurementSystem = MeasurementSystem.InternationalSystemOfUnits;
                break;
        }
    }

	[ExportMethod]
    public void CancUser(NodeId userToDelete)
    {
        if (InformationModel.Get(userToDelete) != null)        
            InformationModel.Get(Owner.GetVariable("Users").Value)?.Remove(InformationModel.Get(userToDelete));        
        else        
            Log.Error("UserEditor", "Cannot obtain the selected user.");

    }
}
