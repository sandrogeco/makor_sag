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
using FTOptix.EventLogger;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.OPCUAServer;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.OPCUAClient;
using FTOptix.Core;
using FTOptix.Recipe;
using FTOptix.System;
#endregion

public class SelezDataLogic_R1 : BaseNetLogic
{
    public override void Start()
    {
        Owner.GetVariable("Anno").Value = DateTime.Now.Year;
        Owner.GetVariable("Mese").Value = DateTime.Now.Month;
        Owner.GetVariable("Giorno").Value = DateTime.Now.Day;
        GetDate();
    }

    [ExportMethod]
    public void GetDate()
    {
		try
		{
			Owner.GetVariable("DataSelez").Value = new DateTime(Owner.GetVariable("Anno").Value, Owner.GetVariable("Mese").Value, Owner.GetVariable("Giorno").Value);
		}
		catch (Exception)
		{
			Owner.GetVariable("Giorno").Value = DateTime.Now.Day;
		}
	}
}
