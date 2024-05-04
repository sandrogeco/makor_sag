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
        //IUAVariable Where1 = Owner.GetObject("SqlQuery").GetVariable("Where1");
        //IUAVariable Where2 = Owner.GetObject("SqlQuery").GetVariable("Where2");
        IUAVariable Group = Owner.GetObject("SqlQuery").GetVariable("Group");
        IUAVariable Order = Owner.GetObject("SqlQuery").GetVariable("Order");
        IUANode OggettoFiltro = Owner.GetAlias("OggettoFiltro");

        //Creazione Query estrapolare i dati da visualizzare sulla griglia
        //Nota: a causa delle limitazioni dovute allo Standard ANSI Sql92 e della mancanza di istruzioni(es 'CAST') non supportate dal QStudio, la progettazione della query è stata molto complicata. 
        //Per estrapolare i dati necessari è stata utilizzata una tabella di appoggio perchè i dati finali devono essere raggruppati per data ma su QStudio la funzione EXTRACT non è supportata nella clausola GROUP BY. Prima sono stati tirati su i dati convertendo il logintime in giorno, mese e anno
        //Poi sono stati fatti i raggruppamenti.

        //Significato valori var Filtro (0 = Settimana, 1=Settimana Prec. 2= Oggi, 3 = Ieri, 4 = Mese corrente, 5 =mese scorso, 6 = Per data)        
        string Where = "";
        switch ((byte)OggettoFiltro.GetVariable("TipoFiltro").Value)
        {
            case 0:
                break;
            case 1:
                break;
            case 2:
                Where = $"WHERE LoginTime BETWEEN '{DateTime.Now.Date}' AND '{DateTime.Now.Date.AddSeconds(86399)}'";
                break;
            case 3:                
                var Ieri = DateTime.Now.AddDays(-1);                
                Where = $"WHERE LoginTime BETWEEN '{Ieri.Date}' AND '{Ieri.Date.AddSeconds(86399)}'";
                break;
            case 4:                
                Where = $"WHERE EXTRACT(MONTH FROM LoginTime) = {DateTime.Now.Month} AND EXTRACT(YEAR FROM LoginTime) = {DateTime.Now.Year}";
                break;
            case 5:
                var Mesescorso = DateTime.Now.AddMonths(-1);
                Where = $"WHERE EXTRACT(MONTH FROM LoginTime) = {Mesescorso.Month} AND EXTRACT(YEAR FROM LoginTime) = {Mesescorso.Year}";
                break;
            case 6:
                var DataStart = new DateTime(OggettoFiltro.GetVariable("AnnoStart").Value, OggettoFiltro.GetVariable("MeseStart").Value, OggettoFiltro.GetVariable("GiornoStart").Value);
                var DataStop = new DateTime(OggettoFiltro.GetVariable("AnnoStop").Value, OggettoFiltro.GetVariable("MeseStop").Value, OggettoFiltro.GetVariable("GiornoStop").Value);
                Where = $"WHERE LoginTime BETWEEN '{DataStart}' AND '{DataStop.AddSeconds(86399)}'";
                break;
        }

        //Creazione tabella di appoggio        
        string tblAppoggio = $"(SELECT EXTRACT(DAY FROM LoginTime) AS Giorno, EXTRACT(MONTH FROM LoginTime) AS Mese, EXTRACT(YEAR FROM LoginTime) AS Anno" +
                             $", NPezzi_Prodotti" +
                             $", NMetri_Prodotti" +
                             $", CntMinutiAccensQuadro" +
                             $", CntOreAccensQuadro" +
                             $", CntMinutiCicloOn" +
                             $", CntOreCicloOn" +
                             $", CntMinutiAllarmeOn" +
                             $", CntOreAllarmeOn" +
                             $", CntMinutiStopOn" +
                             $", CntOreStopOn" +
                             $", CntMinutiPausaOn" +
                             $", CntOrePausaOn" +
                             $" FROM CntProduzione {Where}) AS TblAppoggio";                             

        //Creazione query finale
        Select.Value = $"SELECT Giorno, Mese, Anno" +
                       $", SUM(NPezzi_Prodotti) AS NPezzi_Prodotti" +
                       $", SUM(NMetri_Prodotti) AS NMetri_Prodotti" +                                                     
                       $", SUM(CntMinutiAccensQuadro) AS CntMinutiAccensQuadro" +
                       $", SUM(CntOreAccensQuadro) AS CntOreAccensQuadro" +
                       $", SUM(CntMinutiCicloOn) AS CntMinutiCicloOn" +
                       $", SUM(CntOreCicloOn) AS CntOreCicloOn" +
                       $", SUM(CntMinutiAllarmeOn) AS CntMinutiAllarmeOn " +
                       $", SUM(CntOreAllarmeOn) AS CntOreAllarmeOn" +
                       $", SUM(CntMinutiStopOn) AS CntMinutiStopOn" +
                       $", SUM(CntOreStopOn) AS CntOreStopOn" +
                       $", SUM(CntMinutiPausaOn) AS CntMinutiPausaOn" +
                       $", SUM(CntOrePausaOn) AS CntOrePausaOn" +
                       $" FROM {tblAppoggio}";   //la sorgente è la tbl di appoggio

        

        Group.Value = "GROUP BY Anno, Mese, Giorno";
        Order.Value = "ORDER BY Anno ASC, Mese ASC, Giorno ASC";
    }
}
