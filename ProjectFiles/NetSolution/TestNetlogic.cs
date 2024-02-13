#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.NativeUI;
using FTOptix.NetLogic;
using FTOptix.HMIProject;
using FTOptix.MelsecFX3U;
using FTOptix.UI;
using FTOptix.S7TiaProfinet;
using FTOptix.MelsecQ;
using FTOptix.CODESYS;
using FTOptix.EventLogger;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.OPCUAServer;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Alarm;
using FTOptix.CommunicationDriver;
using FTOptix.OPCUAClient;
using FTOptix.Core;
using FTOptix.SQLiteStore;
using FTOptix.S7TCP;
using FTOptix.RAEtherNetIP;
#endregion

public class TestNetlogic : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void ExeTest()
    {
        var Test = ResourceUri.FromApplicationRelativePath(LogicObject.GetVariable("Variabile1").Value);
        ////Controllo se devo aggiornare i dati
        //if (string.Compare(Session.User.BrowseName, "Anonymous") == 0)
        //{
        //    int i = 1;
        //    return;
        //}
    }


    //[ExportMethod]
    //public void ExeQuery()
    //{
    //    NodeId NodeMyStore = LogicObject.GetVariable("DataStore").Value;
    //    var myStore = (Store)LogicObject.Context.GetNode(NodeMyStore);  // Tiro su il nodeId dello store

    //    //string sqlQuery = " SELECT Utente, Attivo, LoginTime, LogoutTime" +
    //    //                  ", NPezzi_Prodotti, NMetri_Prodotti FROM   dbo.CntProduzione" +                         
    //    //                  " ORDER BY LoginTime ASC";

    //    //myStore.Query(sqlQuery, out string[] header, out object[,] resultSet);

    //    //if (resultSet.Rank != 2 && resultSet.GetLength(0) > 0)      //l'operatore && valuta l'exp a sx se è vera valuta anche quella a destra altrimenti non la valuta per niente.
    //    //    return true;

    //    //var cultureInfo = new CultureInfo(LogicObject.Context.Sessions.CurrentSessionInfo.ActualLocaleIds[0]);      //tiro su il cultrue info dell'utente attualmente loggato

    //    //for (int row = 0; row < resultSet.GetLength(0); row++)
    //    //{
    //    //    var Data = ((DateTime)resultSet[row, Array.IndexOf(header, "LoginTime")]).ToString("d", cultureInfo);
    //    //    var Data1 = ((DateTime)resultSet[row, Array.IndexOf(header, "LoginTime")]).ToString("G", cultureInfo);
    //    //}


    //    //string sqlQuery = $" SELECT * FROM dbo.CntProduzione" +
    //    //                  $" ORDER BY LoginTime ASC";

    //    //myStore.Query(sqlQuery, out string[] header, out object[,] resultSet);

    //    //string[] Intestazione = new string[10] { "User", "LoginTime", "LogoutTime", "NPezzi_Prodotti", "NMetri_Prodotti", "Durata accensione", "% In ciclo", "% In allarme", "% In stop", "% In pausa" };

    //    //object[,] Risultati = new object[resultSet.GetLength(0), 10];
    //    //var cultureInfo = new CultureInfo(LogicObject.Context.Sessions.CurrentSessionInfo.ActualLocaleIds[0]);      //tiro su il cultrue info dell'utente attualmente loggato
    //    ////Preparo la matrice di risultati della query
    //    //for (int row = 0; row < resultSet.GetLength(0); row++)
    //    //{
    //    //    Risultati[row, 0] = resultSet[row, 0];      //Nome utente
    //    //    //Risultati[row, 1] = resultSet[row, 1];      //Utente attivo
    //    //    Risultati[row, 1] = resultSet[row, 2];      //LoginTime
    //    //    Risultati[row, 2] = resultSet[row, 3];      //LogoutTime
    //    //    Risultati[row, 3] = resultSet[row, 4];      //NPezzi_Prodotti
    //    //    Risultati[row, 4] = resultSet[row, 5];      //NMetri_Prodotti
    //    //    Risultati[row, 5] = resultSet[row, 6] + "h : " + resultSet[row, 7] + "min";      //Durata accensione quadro

    //    //    var DurataAccMin = Convert.ToDouble(resultSet[row, 6]) * 60 + Convert.ToDouble(resultSet[row, 7]);
    //    //    if (DurataAccMin > 0)
    //    //    {
    //    //        Risultati[row, 6] = (((Convert.ToDouble(resultSet[row, 8]) * 60) + Convert.ToDouble(resultSet[row, 9])) / DurataAccMin).ToString("P1", cultureInfo);      //% In ciclo. Il metodo ToString() crea una stringa simile 13,5%
    //    //        Risultati[row, 7] = (((Convert.ToDouble(resultSet[row, 10]) * 60) + Convert.ToDouble(resultSet[row, 11])) / DurataAccMin).ToString("P1", cultureInfo);    //% In allarme
    //    //        Risultati[row, 8] = (((Convert.ToDouble(resultSet[row, 12]) * 60) + Convert.ToDouble(resultSet[row, 13])) / DurataAccMin).ToString("P1", cultureInfo);    //% In stop
    //    //        Risultati[row, 9] = (((Convert.ToDouble(resultSet[row, 14]) * 60) + Convert.ToDouble(resultSet[row, 15])) / DurataAccMin).ToString("P1", cultureInfo);   //% In pausa
    //    //    }
    //    //    else
    //    //    {
    //    //        Risultati[row, 6] = "0%";
    //    //        Risultati[row, 7] = "0%";
    //    //        Risultati[row, 8] = "0%";
    //    //        Risultati[row, 9] = "0%";
    //    //    }
    //    //}

    //    GetProduzPerUtente(DateTime.Now, out string[] header, out object[,] resultSet);
    //}

    //private bool GetProduzPerUtente(DateTime GiornoRicerca, out string[] header, out object[,] resultSet)
    //{
    //    NodeId NodeMyStore = LogicObject.GetVariable("DataStore").Value;
    //    var myStore = (Store)LogicObject.Context.GetNode(NodeMyStore);  // Tiro su il nodeId dello store

    //    string sqlQuery = $"SELECT * FROM CntProduzione" +
    //                      $" ORDER BY LoginTime ASC";

    //    myStore.Query(sqlQuery, out string[] Testata, out object[,] QueryResult);

    //    var LocalID = Project.Current.GetVariable("UI/Panels/Statistiche di processo/GestioneStatistiche/ImpostazOem/LinguaReport").Value.Value.ToString();
    //    //LogicObject.GetVariable("ImpostazOem/LinguaReport").Value.Value.ToString()
    //    var cultureInfo = new CultureInfo(LocalID);

    //    header = new string[10] { "User", "LoginTime", "LogoutTime"
    //                              , InformationModel.LookupTranslation(new LocalizedText("Pezzi lavorati"), new List<string>(){ cultureInfo.ToString() }).Text
    //                              , InformationModel.LookupTranslation(new LocalizedText("Metri lavorati"), new List<string>(){ cultureInfo.ToString() }).Text
    //                              , InformationModel.LookupTranslation(new LocalizedText("Durata accensione"), new List<string>(){ cultureInfo.ToString() }).Text
    //                              , InformationModel.LookupTranslation(new LocalizedText("PercentCicloOn"), new List<string>(){ cultureInfo.ToString() }).Text
    //                              , InformationModel.LookupTranslation(new LocalizedText("PercentAllarmeOn"), new List<string>(){ cultureInfo.ToString() }).Text
    //                              , InformationModel.LookupTranslation(new LocalizedText("PercentStopOn"), new List<string>(){ cultureInfo.ToString() }).Text
    //                              , InformationModel.LookupTranslation(new LocalizedText("PercentPausaOn"), new List<string>(){ cultureInfo.ToString() }).Text
    //                            };

    //    resultSet = new string[QueryResult.GetLength(0), 10];      //Ridefinisco la matrice con le dimensioni effettive

    //    if (QueryResult.Rank != 2 || QueryResult.GetLength(0) <= 0)      //l'operatore || valuta l'exp a sx se è vera valuta anche quella a destra altrimenti non la valuta per niente.
    //        return false;

    //    //Preparo la matrice di risultati della query
    //    for (int row = 0; row < QueryResult.GetLength(0); row++)
    //    {
    //        resultSet[row, 0] = QueryResult[row, 0].ToString();      //Nome utente
    //        //resultSet[row, 1] = QueryResult[row, 1].ToString();      //Utente attivo
    //        resultSet[row, 1] = ((DateTime)QueryResult[row, 2]).ToString("G", cultureInfo);      //LoginTime
    //        resultSet[row, 2] = ((DateTime)QueryResult[row, 3]).ToString("G", cultureInfo);      //LogoutTime
    //        resultSet[row, 3] = QueryResult[row, 4].ToString();      //NPezzi_Prodotti
    //        resultSet[row, 4] = QueryResult[row, 5].ToString();      //NMetri_Prodotti
    //        resultSet[row, 5] = QueryResult[row, 6] + "h : " + QueryResult[row, 7] + "min";      //Durata accensione quadro

    //        var DurataAccMin = (Convert.ToDouble(QueryResult[row, 6]) * 60) + Convert.ToDouble(QueryResult[row, 7]);          
    //        resultSet[row, 6] = DurataAccMin <= 0 ? "0%" : (((Convert.ToDouble(QueryResult[row, 8]) * 60) + Convert.ToDouble(QueryResult[row, 9])) / DurataAccMin).ToString("P1", cultureInfo);      //% In ciclo. Il metodo ToString() crea una stringa simile 13,5%
    //        resultSet[row, 7] = DurataAccMin <= 0 ? "0%" : (((Convert.ToDouble(QueryResult[row, 10]) * 60) + Convert.ToDouble(QueryResult[row, 11])) / DurataAccMin).ToString("P1", cultureInfo);    //% In allarme
    //        resultSet[row, 8] = DurataAccMin <= 0 ? "0%" : (((Convert.ToDouble(QueryResult[row, 12]) * 60) + Convert.ToDouble(QueryResult[row, 13])) / DurataAccMin).ToString("P1", cultureInfo);    //% In stop
    //        resultSet[row, 9] = DurataAccMin <= 0 ? "0%" : (((Convert.ToDouble(QueryResult[row, 14]) * 60) + Convert.ToDouble(QueryResult[row, 15])) / DurataAccMin).ToString("P1", cultureInfo);   //% In pausa       
    //    }
    //    return true;     
    //}

    //private bool GetProduzGiornaliera(DateTime GiornoRicerca, out string[] header, out object[,] resultSet)
    //{
    //    NodeId NodeMyStore = LogicObject.GetVariable("DataStore").Value;
    //    var myStore = (Store)LogicObject.Context.GetNode(NodeMyStore);  // Tiro su il nodeId dello store

    //    //Creazione tabella di appoggio        
    //    string tblAppoggio = $"(SELECT EXTRACT(DAY FROM LoginTime) AS Giorno, EXTRACT(MONTH FROM LoginTime) AS Mese, EXTRACT(YEAR FROM LoginTime) AS Anno" +
    //                         $", NPezzi_Prodotti" +
    //                         $", NMetri_Prodotti" +
    //                         $", CntMinutiAccensQuadro" +
    //                         $", CntOreAccensQuadro" +
    //                         $", CntMinutiCicloOn" +
    //                         $", CntOreCicloOn" +
    //                         $", CntMinutiAllarmeOn" +
    //                         $", CntOreAllarmeOn" +
    //                         $", CntMinutiStopOn" +
    //                         $", CntOreStopOn" +
    //                         $", CntMinutiPausaOn" +
    //                         $", CntOrePausaOn" +
    //                         $" FROM CntProduzione) AS TblAppoggio";

    //    //Creazione query finale
    //    string sqlQuery = $"SELECT Giorno, Mese, Anno" +
    //                      $", SUM(NPezzi_Prodotti) AS NPezzi_Prodotti" +
    //                      $", SUM(NMetri_Prodotti) AS NMetri_Prodotti" +
    //                      $", SUM(CntMinutiAccensQuadro) AS CntMinutiAccensQuadro" +
    //                      $", SUM(CntOreAccensQuadro) AS CntOreAccensQuadro" +
    //                      $", SUM(CntMinutiCicloOn) AS CntMinutiCicloOn" +
    //                      $", SUM(CntOreCicloOn) AS CntOreCicloOn" +
    //                      $", SUM(CntMinutiAllarmeOn) AS CntMinutiAllarmeOn " +
    //                      $", SUM(CntOreAllarmeOn) AS CntOreAllarmeOn" +
    //                      $", SUM(CntMinutiStopOn) AS CntMinutiStopOn" +
    //                      $", SUM(CntOreStopOn) AS CntOreStopOn" +
    //                      $", SUM(CntMinutiPausaOn) AS CntMinutiPausaOn" +
    //                      $", SUM(CntOrePausaOn) AS CntOrePausaOn" +
    //                      $" FROM {tblAppoggio}" +  //la sorgente è la tbl di appoggio
    //                      $" GROUP BY Anno, Mese, Giorno" +
    //                      $" ORDER BY Anno ASC, Mese ASC, Giorno ASC";

    //    myStore.Query(sqlQuery, out string[] Testata, out object[,] QueryResult);


    //    var LocalID = Project.Current.GetVariable("UI/Panels/Statistiche di processo/GestioneStatistiche/ImpostazOem/LinguaReport").Value.Value.ToString();
    //    //LogicObject.GetVariable("ImpostazOem/LinguaReport").Value.Value.ToString()
    //    var cultureInfo = new CultureInfo(LocalID);

    //    header = new string[8] {    InformationModel.LookupTranslation(new LocalizedText("Data"), new List<string>(){ cultureInfo.ToString() }).Text
    //                              , InformationModel.LookupTranslation(new LocalizedText("Pezzi lavorati"), new List<string>(){ cultureInfo.ToString() }).Text
    //                              , InformationModel.LookupTranslation(new LocalizedText("Metri lavorati"), new List<string>(){ cultureInfo.ToString() }).Text
    //                              , InformationModel.LookupTranslation(new LocalizedText("Durata accensione"), new List<string>(){ cultureInfo.ToString() }).Text
    //                              , InformationModel.LookupTranslation(new LocalizedText("PercentCicloOn"), new List<string>(){ cultureInfo.ToString() }).Text
    //                              , InformationModel.LookupTranslation(new LocalizedText("PercentAllarmeOn"), new List<string>(){ cultureInfo.ToString() }).Text
    //                              , InformationModel.LookupTranslation(new LocalizedText("PercentStopOn"), new List<string>(){ cultureInfo.ToString() }).Text
    //                              , InformationModel.LookupTranslation(new LocalizedText("PercentPausaOn"), new List<string>(){ cultureInfo.ToString() }).Text
    //                            };
    //    resultSet = new string[QueryResult.GetLength(0), 8];      //Ridefinisco la matrice con le dimensioni effettive

    //    if (QueryResult.Rank != 2 || QueryResult.GetLength(0) <= 0)      //l'operatore || valuta l'exp a sx se è vera valuta anche quella a destra altrimenti non la valuta per niente.
    //        return false;

    //    //Preparo la matrice di risultati della query
    //    for (int row = 0; row < QueryResult.GetLength(0); row++)
    //    {
    //        resultSet[row, 0] = new DateTime(Convert.ToInt32(QueryResult[row, 2]), Convert.ToInt32(QueryResult[row, 1]), Convert.ToInt32(QueryResult[row, 0])).ToString("d", cultureInfo);      //Data
    //        //resultSet[row, 1] = QueryResult[row, 1].ToString();      //Utente attivo
    //        resultSet[row, 1] = QueryResult[row, 3].ToString();      //NPezzi_Prodotti
    //        resultSet[row, 2] = QueryResult[row, 4].ToString();      //NMetri_Prodotti
    //        resultSet[row, 3] = QueryResult[row, 5] + "h : " + QueryResult[row, 6] + "min";      //Durata accensione quadro      

    //        var DurataAccMin = (Convert.ToDouble(QueryResult[row, 5]) * 60) + Convert.ToDouble(QueryResult[row, 6]);
    //        resultSet[row, 4] = DurataAccMin <= 0 ? "0%" : (((Convert.ToDouble(QueryResult[row, 7]) * 60) + Convert.ToDouble(QueryResult[row, 8])) / DurataAccMin).ToString("P1", cultureInfo);      //% In ciclo. Il metodo ToString() crea una stringa simile 13,5%
    //        resultSet[row, 5] = DurataAccMin <= 0 ? "0%" : (((Convert.ToDouble(QueryResult[row, 9]) * 60) + Convert.ToDouble(QueryResult[row, 10])) / DurataAccMin).ToString("P1", cultureInfo);    //% In allarme
    //        resultSet[row, 6] = DurataAccMin <= 0 ? "0%" : (((Convert.ToDouble(QueryResult[row, 11]) * 60) + Convert.ToDouble(QueryResult[row, 12])) / DurataAccMin).ToString("P1", cultureInfo);    //% In stop
    //        resultSet[row, 7] = DurataAccMin <= 0 ? "0%" : (((Convert.ToDouble(QueryResult[row, 13]) * 60) + Convert.ToDouble(QueryResult[row, 14])) / DurataAccMin).ToString("P1", cultureInfo);   //% In pausa        
    //    }
    //    return true;
    //}
}
