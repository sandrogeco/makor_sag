#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.NetLogic;
using FTOptix.NativeUI;
using FTOptix.HMIProject;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.UI;
using FTOptix.CoreBase;
using FTOptix.Core;
using FTOptix.SQLiteStore;
using FTOptix.S7TCP;
using FTOptix.RAEtherNetIP;
#endregion

public class ProduzUtenteLogic : BaseNetLogic
{
    public override void Start()
    {        
        PopolaQuery();
    }

    [ExportMethod]
    public void PopolaQuery()
    {
        IUAVariable Select = Owner.GetVariable("SqlQuery/Select");
        IUAVariable Where1 = Owner.GetObject("SqlQuery").GetVariable("Where1");
        IUAVariable Where2 = Owner.GetObject("SqlQuery").GetVariable("Where2");
        //IUAVariable Group = Owner.GetObject("SqlQuery").GetVariable("Group");
        IUAVariable Order = Owner.GetObject("SqlQuery").GetVariable("Order");
        IUANode OggettoFiltro = Owner.GetAlias("OggettoFiltro");

        Select.Value = "SELECT * FROM CntProduzione";
        //Significato valori var Filtro (0 = Settimana, 1=Settimana Prec. 2= Oggi, 3 = Ieri, 4 = Mese corrente, 5 =mese scorso, 6 = Per data)        
        switch ((byte)OggettoFiltro.GetVariable("TipoFiltro").Value)
        {
            case 0:
                break;
            case 1:
                break;
            case 2:
                Where1.Value = $"WHERE LoginTime BETWEEN '{DateTime.Now.Date}' AND '{DateTime.Now.Date.AddSeconds(86399)}'";
                break;
            case 3:
                var Ieri = DateTime.Now.AddDays(-1);
                Where1.Value = $"WHERE LoginTime BETWEEN '{Ieri.Date}' AND '{Ieri.Date.AddSeconds(86399)}'";
                break;
            case 4:
                Where1.Value = $"WHERE EXTRACT(MONTH FROM LoginTime) = {DateTime.Now.Month} AND EXTRACT(YEAR FROM LoginTime) = {DateTime.Now.Year}";
                break;
            case 5:
                var Mesescorso = DateTime.Now.AddMonths(-1);
                Where1.Value = $"WHERE EXTRACT(MONTH FROM LoginTime) = {Mesescorso.Month} AND EXTRACT(YEAR FROM LoginTime) = {Mesescorso.Year}";
                break;
            case 6:
                var DataStart = new DateTime(OggettoFiltro.GetVariable("AnnoStart").Value, OggettoFiltro.GetVariable("MeseStart").Value, OggettoFiltro.GetVariable("GiornoStart").Value);
                var DataStop = new DateTime(OggettoFiltro.GetVariable("AnnoStop").Value, OggettoFiltro.GetVariable("MeseStop").Value, OggettoFiltro.GetVariable("GiornoStop").Value);
                Where1.Value = $"WHERE LoginTime BETWEEN '{DataStart}' AND '{DataStop.AddSeconds(86399)}'";
                break;
        }

        Where2.Value = $"AND Utente like '%{OggettoFiltro.GetVariable("UtenteFiltro").Value.Value}%'";
        Order.Value = "ORDER BY LoginTime ASC";
    }
}
