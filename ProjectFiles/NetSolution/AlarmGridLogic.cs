#region StandardUsing
using System;
using FTOptix.Core;
using FTOptix.CoreBase;
using FTOptix.HMIProject;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.NetLogic;
using FTOptix.OPCUAServer;
using FTOptix.UI;
using FTOptix.Alarm;
using FTOptix.MelsecFX3U;
using FTOptix.MelsecQ;
using FTOptix.CODESYS;
using FTOptix.SQLiteStore;
#endregion

public class AlarmGridLogic : FTOptix.NetLogic.BaseNetLogic
{
    public override void Start()
    {
        alarmsDatagrid = Owner.Children.Get<DataGrid>("AlarmsDataGrid");
        alarmsDatagridModel = alarmsDatagrid.Children.GetVariable("Model");

        affinityId = alarmsDatagrid.Context.AssignAffinityId();
        RegisterObserverOnSessionLocaleIdChanged(alarmsDatagrid.Context);
    }

    public override void Stop()
    {
        if (localeIdsRegistration != null)
        {
            localeIdsRegistration.Dispose();
            localeIdsRegistration = null;
        }

        if (localeIdChangedObserver != null)
            localeIdChangedObserver = null;

    }

    public void RegisterObserverOnSessionLocaleIdChanged(IContext context)
    {
        var currentSessionLocaleIds = context.Sessions.CurrentSessionInfo.SessionObject.Children["ActualLocaleIds"];

        localeIdChangedObserver = new CallbackVariableChangeObserver((IUAVariable variable, UAValue newValue, UAValue oldValue, uint[] _, ulong __) =>
        {
			//reset datagrid model variable to trigger locale changed event
			var dynamicLink = alarmsDatagridModel.GetVariable("DynamicLink");
			if (dynamicLink == null)
				return;

			string dynamicLinkValue = dynamicLink.Value;
			dynamicLink.Value = string.Empty;
			dynamicLink.Value = dynamicLinkValue;
        });

        localeIdsRegistration = currentSessionLocaleIds.RegisterEventObserver(
            localeIdChangedObserver, EventType.VariableValueChanged, affinityId);
    }

    private IEventRegistration localeIdsRegistration;
    private IEventObserver localeIdChangedObserver;
    private uint affinityId;
    private DataGrid alarmsDatagrid;
    private IUAVariable alarmsDatagridModel;
}
