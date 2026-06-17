#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.NativeUI;
using FTOptix.NetLogic;
using FTOptix.HMIProject;
using FTOptix.UI;
using FTOptix.OPCUAServer;
using FTOptix.CoreBase;
using FTOptix.Core;
using System.Linq;
using System.Collections.Generic; 
using FTOptix.CODESYS;
using FTOptix.SQLiteStore;
using FTOptix.S7TCP;
using FTOptix.RAEtherNetIP;
using FTOptix.Recipe;
using FTOptix.System;
using FTOptix.S7TiaProfinet;
#endregion

public class MailingListLogic_R0 : BaseNetLogic
{
    [ExportMethod]
    public void CreaUtente(string UserName, string EmailAddres)
    {
        NodeId NodeLista = Owner.GetVariable("ListaMail").Value;
        var MailingList = Owner.Context.GetNode(NodeLista);  // Tiro su il nodo dove sta la lista mail

        //Query syntax
        List<IUAVariable> UserTrovato = (from user in MailingList.GetNodesByType<IUAVariable>()
                                         where user.BrowseName == UserName
                                         select user).ToList();       //il tolist serve ad ottenere i risultati, alternativa sarebbe quella di usare un ciclo foreach

        if (UserTrovato.Count == 0)
        {
            var NewUser = InformationModel.MakeVariable(UserName, OpcUa.DataTypes.String);
            NewUser.Value = EmailAddres;
            MailingList.Add(NewUser);
        }
        else
        {
            UserTrovato[0].Value = EmailAddres;     //prendo il primo elemento della lista
        }

        Owner.Children.Get<DataGrid>("MailList").Refresh();
    }

    [ExportMethod]
    public void CancUtente(string UserName)
    {
        NodeId NodeLista = Owner.GetVariable("ListaMail").Value;
        var MailingList = Owner.Context.GetNode(NodeLista);  // Tiro su il nodo dove sta la lista mail

        //Query syntax        
        List<IUAVariable> UserTrovato = (from user in MailingList.GetNodesByType<IUAVariable>()
                                         where user.BrowseName == UserName
                                         select user).ToList();

        if (UserTrovato.Count != 0)        
            MailingList.Remove(MailingList.Children.Get(UserTrovato[0].BrowseName));

        Owner.Children.Get<DataGrid>("MailList").Refresh();
    }
}
