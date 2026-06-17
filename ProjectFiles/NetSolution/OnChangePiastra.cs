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
using FTOptix.Recipe;
using FTOptix.System;
using FTOptix.S7TiaProfinet;
#endregion

public class OnChangePiastra : BaseNetLogic
{
    public override void Start()
    {
        Owner.GetVariable("RicAperta").VariableChange += OnChange;
	}

	public void OnChange(object sender, VariableChangeEventArgs e)
    {
        if (e.NewValue == true)
        {
			Project.Current.GetVariable("CommDrivers/CODESYSDriver1/PLC_Next/Tags/PLC/GvCirc/bHmiCnfCambioPiastra").RemoteWrite(false);
			((Dialog)Owner.Owner).Close();
		}
        
    }
}
