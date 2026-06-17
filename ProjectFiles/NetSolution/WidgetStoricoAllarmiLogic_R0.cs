#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.NativeUI;
using FTOptix.NetLogic;
using FTOptix.HMIProject;
using FTOptix.UI;
using FTOptix.Alarm;
using FTOptix.S7TCP;
using FTOptix.S7TiaProfinet;
using FTOptix.MelsecFX3U;
using FTOptix.MelsecQ;
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
#endregion

public class WidgetStoricoAllarmiLogic_R0 : BaseNetLogic
{
    public override void Start()
    {
        SeverityFilter();
    }

    [ExportMethod]
    public void SeverityFilter()
    {
        string SeverityFilter = Owner.GetVariable("SqlQuery/FilterMsg").Value ? "Severity = 1" : "Severity <0";
        SeverityFilter = Owner.GetVariable("SqlQuery/FilterWrn").Value ? SeverityFilter + " OR Severity = 500" : SeverityFilter + "";
        SeverityFilter = Owner.GetVariable("SqlQuery/FilterAlm").Value ? SeverityFilter + " OR Severity = 1000" : SeverityFilter + "";

        if (SeverityFilter == "")
            SeverityFilter = "Severity > 0";

        Owner.GetVariable("SqlQuery/SeverityFilter").Value = SeverityFilter;
    }

    [ExportMethod]
    public void ApplicaFiltro()
    {
        //Attenzione: mettere la variabile datetime racchiusa tra apici altrimenti non funziona
        DateTime End = Owner.GetVariable("Filtri/SelezDataStartEnd/DataFine").Value;
		DateTime Start = Owner.GetVariable("Filtri/SelezDataStartEnd/DataInizio").Value;
		String query = $" AND Time >'{Start:yyyy-MM-dd}' AND Time <'{End.AddDays(1):yyyy-MM-dd}'";
		((SqlQuery_R0)Owner.GetObject("SqlQuery")).Where2 = query;
		//((SqlQuery_R0)Owner.GetObject("SqlQuery")).Where2 = " AND Time >'" + Owner.GetVariable("Filtri/SelezDataStartEnd/DataInizio").Value + "' AND Time <'" + End.AddDays(1) + "'";
    }

    [ExportMethod]
    public void RimuoviFiltro()
    {
        ((SqlQuery_R0)Owner.GetObject("SqlQuery")).Where2 = "";
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tableNodeId">Node id della tabella allarmi</param>
    [ExportMethod]
    public void ClearAlm()
    {
        // Getting the object table from its NodeId
        var tableNodeId = Owner.GetVariable("TabellaAllarmiDB").Value;
        var tableObject = LogicObject.Context.GetObject(tableNodeId);

        if (tableObject == null)
            return;

        // Getting the Tables collection
        var tablesCollection = tableObject.Owner;

        if (tablesCollection == null)
            return;

        (tablesCollection.Owner as Store).Query("DELETE FROM \"" + tableObject.BrowseName + "\"", out _, out object[,] resultSet);

        // Check if the resultSet is a bidimensional array
        if (resultSet.Rank != 2)
            return;

        RefreshGrid();
    }

    [ExportMethod]
    public void RefreshGrid() => Owner.Get<DataGrid>("AlarmsArchieve").Refresh();
}

