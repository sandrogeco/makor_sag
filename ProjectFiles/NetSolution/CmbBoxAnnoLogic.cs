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

public class CmbBoxAnnoLogic : BaseNetLogic
{
    public override void Start()
    {
        var ModelAnni = InformationModel.MakeObject("ModelAnni");
        ModelAnni.Children.Clear();
        var AnnoAttuale = DateTime.Now.Year;

        for (int i = 0; i < 5 ; i++)
        {
            var AnnoValue = AnnoAttuale - i;
            var Anno = InformationModel.MakeVariable(AnnoValue.ToString(), OpcUa.DataTypes.Int32);
            Anno.Value = AnnoValue;
            ModelAnni.Add(Anno);
        }

        LogicObject.Add(ModelAnni);
        ((ComboBox)Owner).Model = ModelAnni.NodeId;
    }
}
