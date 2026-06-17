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
using FTOptix.Recipe;
using FTOptix.System;
using FTOptix.S7TiaProfinet;
using static FrameDatiProduzLogic;
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
        
        //Significato valori var Filtro (0 = Settimana, 1=Settimana Prec. 2= Oggi, 3 = Ieri, 4 = Mese corrente, 5 =mese scorso, 6 = Per data)        
        var TipoFiltro = (byte)OggettoFiltro.GetVariable("TipoFiltro").Value.Value;

        string Where = "";
        switch ((TipoFiltroGestStat)TipoFiltro)
        {
            case TipoFiltroGestStat.SettimanaCorrente:
            case TipoFiltroGestStat.SettimanaPrec:
                break;
            case TipoFiltroGestStat.Oggi:
                Where = $"WHERE LoginDate = '{DateTime.Now:yyyy-MM-dd}'";
                break;
            case TipoFiltroGestStat.Ieri:
                Where = $"WHERE LoginDate = '{DateTime.Now.AddDays(-1):yyyy-MM-dd}'";
                break;
            case TipoFiltroGestStat.MeseCorrente:
                Where = $"WHERE EXTRACT(MONTH FROM LoginTime) = '{DateTime.Now.Month:D2}' AND EXTRACT(YEAR FROM LoginTime) = '{DateTime.Now.Year:D2}'";
                break;
            case TipoFiltroGestStat.MeseScorso:
                var Mesescorso = DateTime.Now.AddMonths(-1);
                Where = $"WHERE EXTRACT(MONTH FROM LoginTime) = '{Mesescorso.Month:D2}' AND EXTRACT(YEAR FROM LoginTime) = '{Mesescorso.Year:D2}'";
                break;
            case TipoFiltroGestStat.PerData:
                var DataStart = new DateTime(OggettoFiltro.GetVariable("AnnoStart").Value, OggettoFiltro.GetVariable("MeseStart").Value, OggettoFiltro.GetVariable("GiornoStart").Value);
                var DataStop = new DateTime(OggettoFiltro.GetVariable("AnnoStop").Value, OggettoFiltro.GetVariable("MeseStop").Value, OggettoFiltro.GetVariable("GiornoStop").Value);
                Where = $"WHERE LoginTime BETWEEN '{DataStart:s}' AND '{DataStop.AddSeconds(86399):s}'";
                break;
        }

        Select.Value = "SELECT * FROM CntProduzione";
        Where1.Value = Where;
        Where2.Value = $"AND Utente like '%{OggettoFiltro.GetVariable("UtenteFiltro").Value.Value}%'";
        Order.Value = "ORDER BY LoginTime DESC";
    }
}
