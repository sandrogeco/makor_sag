#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.NativeUI;
using FTOptix.NetLogic;
using FTOptix.HMIProject;
using FTOptix.UI;
using FTOptix.Alarm;
using FTOptix.S7TiaProfinet;
using FTOptix.MelsecFX3U;
using FTOptix.MelsecQ;
using FTOptix.CODESYS;
using FTOptix.EventLogger;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.OPCUAServer;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.OPCUAClient;
using FTOptix.Core;
using FTOptix.SQLiteStore;
using FTOptix.S7TCP;
using FTOptix.Recipe;
#endregion

public class CmbBoxGiornoLogic : BaseNetLogic
{
    public override void Start()
    {
        var ModelGiorni = InformationModel.MakeObject("ModelGiorni");
        ModelGiorni.Children.Clear();

        for (int i = 1; i <= 31; i++)
        {
            var Giorno = InformationModel.MakeVariable(i.ToString(), OpcUa.DataTypes.Int16);
            Giorno.Value = i;
            ModelGiorni.Add(Giorno);
        }

        LogicObject.Add(ModelGiorni);
        ((ComboBox)Owner).Model = ModelGiorni.NodeId;
    }
}
