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
using FTOptix.CODESYS;
using FTOptix.SQLiteStore;
using FTOptix.S7TCP;
using FTOptix.RAEtherNetIP;
using FTOptix.Recipe;
#endregion

public class MeasureComboBoxLogic : BaseNetLogic
{
    public override void Start()
    {
        var MeasureSysModel = InformationModel.MakeObject("MeasureSys");
        MeasureSysModel.Children.Clear();
               
        var SysInternazionale = InformationModel.MakeVariable(browseName: "SysMisuraInternazionale", dataTypeId: OpcUa.DataTypes.String);
        SysInternazionale.Value = 1;
        SysInternazionale.DisplayName = InformationModel.LookupTranslation(new LocalizedText("SysMisuraInternazionale"));

        var SysAmericano = InformationModel.MakeVariable("SysMisuraAmericano", OpcUa.DataTypes.String);
        SysAmericano.Value = 2;
        SysAmericano.DisplayName = InformationModel.LookupTranslation(new LocalizedText("SysMisuraAmericano"));


        var SysBritannico = InformationModel.MakeVariable("SysMisuraBritannico", OpcUa.DataTypes.String);
        SysBritannico.Value = 3;
        SysBritannico.DisplayName = InformationModel.LookupTranslation(new LocalizedText("SysMisuraBritannico"));

        MeasureSysModel.Add(SysInternazionale);
        MeasureSysModel.Add(SysAmericano);
        MeasureSysModel.Add(SysBritannico);
        
        LogicObject.Add(MeasureSysModel);
        ((ComboBox)Owner).Model = MeasureSysModel.NodeId;
    }
}
