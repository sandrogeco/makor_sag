#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.WebUI;
using FTOptix.Alarm;
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
using FTOptix.System;
using FTOptix.S7TiaProfinet;
using FTOptix.S7TCP;
#endregion

public class GestErrBackUpDB : BaseNetLogic
{
    public override void Start()
    {
        IUAVariable errBackUpDB = Project.Current.GetVariable("Model/Variabili_HMI/Generiche/ErrBackUpDB");
        errBackUpDB.VariableChange += GestErrBackUpDB_Method;
    }
    private void GestErrBackUpDB_Method(object sender, VariableChangeEventArgs e)
    {
        if (e.NewValue == true)
        {
            var myDialog = (DialogType)Project.Current.Get("UI/Panels/Prj_DialogBox/PopUp_ErrBackUpDB");
            _ = UICommands.OpenDialog(Owner, myDialog);
        }
    }
}
