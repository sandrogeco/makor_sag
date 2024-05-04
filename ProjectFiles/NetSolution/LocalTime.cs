using System;
using UAManagedCore;
using System.Globalization;
using FTOptix.NetLogic;
using FTOptix.S7TCP;
using FTOptix.RAEtherNetIP;
using FTOptix.Recipe;

public class LocalTime : BaseNetLogic
{
    private PeriodicTask periodicTask;
    public override void Start()
	{
        // Insert code to be executed when the user-defined logic is started
        periodicTask = new PeriodicTask(UpdateTime, 1000, LogicObject);
        periodicTask.Start();
	}

	public override void Stop()
	{
        // Insert code to be executed when the user-defined logic is stopped
        periodicTask.Dispose();
        periodicTask = null;
	}

    private void UpdateTime()
    {
        LogicObject.GetVariable("Time").Value = DateTime.Now;

        var Locale = LogicObject.Context.Sessions.CurrentSessionInfo.ActualLocaleId[0];       //tiro su il localeId attuale della sessione

        var cultureInfo = new CultureInfo(Locale); 

        LogicObject.GetVariable("LocallizedTime").Value = DateTime.Now.ToString(cultureInfo);
    }    
}
