#region Using directives
using FTOptix.NetLogic;
using System;
using UAManagedCore;
using static FrameDatiProduzLogic;
#endregion

public class ProduzMensileLogic : BaseNetLogic
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
        //IUAVariable Where2 = Owner.GetObject("SqlQuery").GetVariable("Where2");
        IUAVariable Group = Owner.GetObject("SqlQuery").GetVariable("Group");
        IUAVariable Order = Owner.GetObject("SqlQuery").GetVariable("Order");
        IUANode OggettoFiltro = Owner.GetAlias("OggettoFiltro");

        //Creazione Query estrapolare i dati da visualizzare sulla griglia
        //Nota: a causa delle limitazioni dovute allo Standard ANSI Sql92 e della mancanza di istruzioni(es 'CAST') non supportate dal QStudio, la progettazione della query è stata molto complicata. 
        //Per estrapolare i dati necessari è stata utilizzata una tabella di appoggio perchè i dati finali devono essere raggruppati per data ma su QStudio la funzione EXTRACT non è supportata nella clausola GROUP BY. Prima sono stati tirati su i dati convertendo il logintime in giorno, mese e anno
        //Poi sono stati fatti i raggruppamenti.

        var pippo = (byte)OggettoFiltro.GetVariable("TipoFiltro").Value.Value;
        string Where = "";
        switch ((TipoFiltroGestStat)pippo)
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
                Where = $"WHERE EXTRACT(MONTH FROM LoginTime) = '{DateTime.Now.Month}' AND EXTRACT(YEAR FROM LoginTime) = '{DateTime.Now.Year}'";
                break;
            case TipoFiltroGestStat.MeseScorso:
                var Mesescorso = DateTime.Now.AddMonths(-1);
                Where = $"WHERE EXTRACT(MONTH FROM LoginTime) = '{Mesescorso.Month}' AND EXTRACT(YEAR FROM LoginTime) = '{Mesescorso.Year}'";
                break;
            case TipoFiltroGestStat.PerData:
                var DataStart = new DateTime(OggettoFiltro.GetVariable("AnnoStart").Value, OggettoFiltro.GetVariable("MeseStart").Value, OggettoFiltro.GetVariable("GiornoStart").Value);
                var DataStop = new DateTime(OggettoFiltro.GetVariable("AnnoStop").Value, OggettoFiltro.GetVariable("MeseStop").Value, OggettoFiltro.GetVariable("GiornoStop").Value);
                Where = $"WHERE LoginTime BETWEEN '{DataStart}' AND '{DataStop.AddSeconds(86399)}'";
                break;
        }

        Select.Value = $"SELECT LoginDate " +
                       $", SUM(CntPezziLav) AS CntPezziLav" +
                       $", SUM(CntMetriLav) AS CntMetriLav" +
                       $", SUM(CntMetriQLav) AS CntMetriQLav" +
                       $", SUM(CntOreCicloOn) AS CntOreCicloOn" +
                       $", SUM(CntMinCicloOn) AS CntMinCicloOn" +
                       $", SUM(CntOrePowerOn) AS CntOrePowerOn" +
                       $", SUM(CntMinPowerOn) AS CntMinPowerOn " +
                       $", SUM(CntKwOra) AS CntKwOra" +
                       $", SUM(CntMetriTrasp) AS CntMetriTrasp" +
                       $" FROM CntProduzione";   //la sorgente è la tbl di appoggio
        
        Where1.Value = Where;
        Group.Value = "GROUP BY LoginDate";
        Order.Value = "ORDER BY LoginDate ASC";
    }
}
