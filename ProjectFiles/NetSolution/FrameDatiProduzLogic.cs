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
using FTOptix.OPCUAClient;
using FTOptix.Retentivity;
using FTOptix.EventLogger;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
#endregion

public class FrameDatiProduzLogic : BaseNetLogic
{
    public enum TipoFiltroGestStat
    {
        SettimanaCorrente,
        SettimanaPrec,
        Oggi,
        Ieri,
        MeseCorrente,
        MeseScorso,
        PerData
    }

    public override void Start()
    {
        var FilterObject = Owner.GetObject("FilterObject");
        FilterObject.GetVariable("AnnoStart").Value = DateTime.Now.Year;
        FilterObject.GetVariable("AnnoStop").Value = DateTime.Now.Year;
        FilterObject.GetVariable("MeseStart").Value = DateTime.Now.Month;
        FilterObject.GetVariable("MeseStop").Value = DateTime.Now.Month;
        FilterObject.GetVariable("GiornoStart").Value = DateTime.Now.Day;
        FilterObject.GetVariable("GiornoStop").Value = DateTime.Now.Day;
    }
}
