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
using FTOptix.System;
#endregion

public class CmbBoxMeseLogic : BaseNetLogic
{
    public override void Start()
    {
        var ModelMesi = InformationModel.MakeObject("ModelMesi");
        ModelMesi.Children.Clear();

        for (int i = 1; i <= 12; i++)
        {
            var Mese = InformationModel.MakeVariable(i.ToString(), OpcUa.DataTypes.Int16);
            Mese.Value = i;
            ModelMesi.Add(Mese);
        }

        LogicObject.Add(ModelMesi);
        ((ComboBox)Owner).Model = ModelMesi.NodeId;        
    }
}
