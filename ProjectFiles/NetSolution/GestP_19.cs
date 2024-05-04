#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Alarm;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.WebUI;
using FTOptix.CODESYS;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.OPCUAServer;
using FTOptix.Retentivity;
using FTOptix.EventLogger;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.OPCUAClient;
using FTOptix.Core;
using static System.Net.Mime.MediaTypeNames;
using System.IO;
using FTOptix.Recipe;

#endregion

public class GestP_19 : BaseNetLogic
{
    IUAVariable varPlc;
    public override void Start()
    {
		varPlc = LogicObject.GetAlias("stOemInv").GetVariable("uP19");
		var cBox = LogicObject.Owner.FindObject("ComboBox1");
		var obj = Project.Current.Get("Model/Variabili_HMI").GetObject("ObjInverter");
		if (varPlc.Value == (UInt16)65520)
		{
			obj.GetVariable("Default").Value = 0;
			cBox.GetVariable("SelectedItem").Value = obj.GetVariable("8888").NodeId;
		}
		else if (varPlc.Value == (UInt16)65521)
		{
			obj.GetVariable("Default").Value = 0;
			cBox.GetVariable("SelectedItem").Value = obj.GetVariable("9999").NodeId;
		}
		else
		{
			obj.GetVariable("Default").Value = varPlc.Value;
			cBox.GetVariable("SelectedItem").Value = obj.GetVariable("Default").NodeId;
		}
	}

	[ExportMethod]
	public void OnCBox_Change()
	{
		if (varPlc.Value == (UInt16)65520)
		{
			//varPlc.Value = 65520;
		}
		else if (varPlc.Value == (UInt16)65521)
		{
			//varPlc.Value = 65521;
		}
		else
		{
			Project.Current.Get("Model/Variabili_HMI").GetObject("ObjInverter").GetVariable("Default").Value = varPlc.Value;
			LogicObject.Owner.FindObject("ComboBox1").GetVariable("SelectedItem").Value = Project.Current.Get("Model/Variabili_HMI").GetObject("ObjInverter").GetVariable("Default").NodeId;
		}
	}
}
