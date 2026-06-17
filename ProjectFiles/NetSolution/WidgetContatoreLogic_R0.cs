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
using FTOptix.RAEtherNetIP;
using FTOptix.Recipe;
using FTOptix.System;
#endregion

public class WidgetContatoreLogic_R0 : BaseNetLogic
{

    override
    public void Start()
    {
		if (Owner.GetVariable("AbilitazGestDataOraRst").Value == true)
		{
			var retentiveObj = Project.Current.GetObject("Model/Variabili_HMI/Var_Ritentive/CntRetentive");
			//retentiveObj.Children.Clear();	
			var varName = MakeBrowsePath(Owner).Split("UIRoot/")[1];

			if (retentiveObj.Children.GetVariable(varName) == null)
				return;

			string [] retentiveVar = retentiveObj.Children.GetVariable(varName).Value;

			Owner.GetVariable("DataOraRst").Value = DateTime.Parse(retentiveVar[0]);
			try
			{
				Owner.GetVariable("Val_Rst").Value = float.Parse(retentiveVar[1]);
			}
			catch (Exception){ }
			Owner.GetVariable("Val_Rst").Value = retentiveVar[1];
		}
	}

    [ExportMethod]
    public void ResetCnt()
    {        
        if (Owner.GetVariable("AbilitazGestDataOraRst").Value == true)
        {
			string value;
			var date = DateTime.Now;
			if (Owner.GetVariable("DisposizioneVerticale1/CntParziale/Val/Text") != null)
				value = ((LocalizedText)Owner.GetVariable("DisposizioneVerticale1/CntParziale/Val/Text").Value.Value).Text;
			else
				value = Owner.GetVariable("Val_CntParziali").Value;

			var varName = MakeBrowsePath(Owner).Split("UIRoot/")[1];

			Owner.GetVariable("DataOraRst").Value = date;
            Owner.GetVariable("Val_Rst").Value = value;

			var retentiveObj = Project.Current.GetObject("Model/Variabili_HMI/Var_Ritentive/CntRetentive");
			if (retentiveObj.Children.GetVariable(varName)==null)
			{
				var retentiveVar = InformationModel.MakeVariable(varName, OpcUa.DataTypes.String, new uint[] { 2 });
				retentiveVar.Value = new string[] { date.ToString(), value.ToString() };
				retentiveObj.Add(retentiveVar);
			}
			else
			{
				retentiveObj.Children.GetVariable(varName).Value = new string[] { date.ToString(), value.ToString() };
			}
		}

        Owner.GetVariable("Bit_RstCntParziali").Value = true;

        if (Owner.GetVariable("AbilitazRstCntTotali").Value == true)
            Owner.GetVariable("Bit_RstCntTotali").Value = true;
    }

	private static string MakeBrowsePath(IUANode node)
	{
		string path = node.BrowseName;
		var current = node.Owner;

		while (current != Project.Current)
		{
			path = current.BrowseName + "/" + path;
			current = current.Owner;
		}
		return path;
	}
}
