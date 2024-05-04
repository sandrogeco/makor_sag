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
using static System.Net.Mime.MediaTypeNames;

#endregion

public class Creazione_Ottiche : BaseNetLogic
{
    [ExportMethod]
    public void CreaRettOttiche()
    {
		var tagPlc = Project.Current.Get("CommDrivers/CODESYSDriver1/PLC_Next/Tags/Next/GvOsc").GetVariable("i_abyBarrOttLetta");
		byte[] arrPlc = tagPlc.Value;
		var imgStatoOttiche = LogicObject.Owner.FindObject("StatoOttiche");
		var leftMargin = LogicObject.GetVariable("LeftMargin").Value;

		int cnt = 0;
		for (uint i = 0; i < arrPlc.Length; i++)
		{
			for (uint ibit = 0; ibit < 8; ibit++)
			{
				if (i==16 & ibit == 7)
					break;

				var rect = InformationModel.Make<Rectangle>("Rect" + cnt);

				rect.Height = 39;
				rect.Width = 10;
				rect.VerticalAlignment = VerticalAlignment.Center;
				rect.FillColor = new Color(0xff, 0, 255, 25);
				rect.LeftMargin = leftMargin + cnt * 10;
				rect.VisibleVariable.SetDynamicLink(tagPlc, i, DynamicLinkMode.Read);
				rect.VisibleVariable.GetVariable("DynamicLink").Value = rect.VisibleVariable.GetVariable("DynamicLink").Value + "[" + i + "]." + (7-ibit);
				imgStatoOttiche.Add(rect);

				cnt++;
			}
		}
	}

	[ExportMethod]
	public void CreaRettMaschera()
	{
		var tagPlc = Project.Current.Get("CommDrivers/CODESYSDriver1/PLC_Next/Tags/Next/GvOsc").GetVariable("abyMascheraBarr");
		byte[] arrPlc = tagPlc.Value;
		var imgStatoOttiche = LogicObject.Owner.FindObject("StatoMaschera");
		var leftMargin = LogicObject.GetVariable("LeftMargin").Value;

		int cnt = 0;
		for (uint i = 0; i < arrPlc.Length; i++)
		{
			for (uint ibit = 0; ibit < 8; ibit++)
			{
				if (i == 16 & ibit == 7)
					break;

				var rect = InformationModel.Make<Rectangle>("Rect" + cnt);

				rect.Height = 39;
				rect.Width = 10;
				rect.VerticalAlignment = VerticalAlignment.Center;
				rect.FillColor = new Color(0xff, 0, 255, 25);
				rect.LeftMargin = leftMargin + cnt * 10;
				rect.VisibleVariable.SetDynamicLink(tagPlc, i, DynamicLinkMode.Read);
				rect.VisibleVariable.GetVariable("DynamicLink").Value = rect.VisibleVariable.GetVariable("DynamicLink").Value + "[" + i + "]." + ibit;

				imgStatoOttiche.Add(rect);

				cnt++;
			}
		}
	}
}
