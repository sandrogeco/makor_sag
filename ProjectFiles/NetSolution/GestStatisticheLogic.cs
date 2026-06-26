#region Using directives
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Store;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UAManagedCore;
using FTOptix.System;
using FTOptix.S7TiaProfinet;
using FTOptix.S7TCP;
#endregion

public class GestStatisticheLogic : BaseNetLogic
{
    private IUAVariable AbiltaGestione, DataUltimoSalvataggio, UtenteAttuale;
    private DelayedTask ActionCreaReport;
    private PeriodicTask ActionCheckCambioGiornoAndAgiornaDati;
    private IUAObject Impostazioni;
    private IUANode CntProduzPlc;
    private Store myStore;
    private CommunicationStation PlcStation;
    private bool CreaCsvReport, AvvioApp;
    private DateTime GiornoRicerca;

    /// <summary>
    /// Questo metodo estrae dal DB la sommatoria dei dati di produzione riferita a un particolare giorno
    /// </summary>
    /// <param name="GiornoRicerca"></param>
    /// <param name="header">Array di stringhe tradotte in base alla lingua selezionata per il report</param>
    /// <param name="Risultati">Matrice di stringhe con i risultati</param>
    /// <returns></returns>
    private bool GetDatiProduzGiornaliera(DateTime GiornoRicerca, out string[] CsvHeader, out string[,] ResultMatrix)
    {
        string sqlQuery = $"SELECT LoginDate " +
                            $", SUM(CntPezziLav) AS CntPezziLav" +
                            $", SUM(CntMetriLav) AS CntMetriLav" +
                            $", SUM(CntMetriQLav) AS CntMetriQLav" +
                            $", SUM(CntOreCicloOn) AS CntOreCicloOn" +
                            $", SUM(CntMinCicloOn) AS CntMinCicloOn" +
                            $", SUM(CntOrePowerOn) AS CntOrePowerOn" +
                            $", SUM(CntMinPowerOn) AS CntMinPowerOn" +
                            $", SUM(CntKwOra) AS CntKwOra" +
                            $", SUM(CntMetriTrasp) AS CntMetriTrasp" +
                            $" FROM CntProduzione WHERE LoginDate = '{GiornoRicerca:yyyy-MM-dd}'" +
                            $" GROUP BY LoginDate" +
                            $" ORDER BY LoginDate ASC";

        string[] Header;
        object[,] QueryResult;

        try
        {
            myStore.Query(sqlQuery, out Header, out QueryResult);
        }
        catch (Exception ex)
        {
            Log.Error("Creazione report statistiche", "GetDatiProduzPerUtente. Errore durante esecuzione query. Errore: " + ex.Message);
            throw;
        }

        var cultureInfo = new CultureInfo(LogicObject.GetVariable("ImpostazOem/LinguaReport").Value.Value.ToString());

        //Creazione riga intestazione con i nomi delle colonne tradotte in base alla lingua selezionata per il report
        CsvHeader = new string[] { "LoginDate"
                                   , InformationModel.LookupTranslation(new LocalizedText("Pezzi"), new List<string>(){ cultureInfo.ToString() }).Text
                                   , "m²"
                                   , "m²/m²ₘₐₓ"
                                   , "m²/h"
                                   , InformationModel.LookupTranslation(new LocalizedText("Durata ciclo ON"), new List<string>(){ cultureInfo.ToString() }).Text
                                   , InformationModel.LookupTranslation(new LocalizedText("Durata accensione"), new List<string>(){ cultureInfo.ToString() }).Text
                                   , InformationModel.LookupTranslation(new LocalizedText("PercentCicloOn"), new List<string>(){ cultureInfo.ToString() }).Text
                                   , InformationModel.LookupTranslation(new LocalizedText("Consumo (kWh)"), new List<string>(){ cultureInfo.ToString() }).Text
        };

        ResultMatrix = new string[QueryResult.GetLength(0), CsvHeader.Length];      //Ridefinisco la matrice con le dimensioni effettive

        if (QueryResult.Rank != 2 || QueryResult.GetLength(0) <= 0)      //l'operatore || valuta l'exp a sx se è vera valuta anche quella a destra altrimenti non la valuta per niente.
            return false;

        //Preparo la matrice di risultati della query
        for (int row = 0; row < QueryResult.GetLength(0); row++)
        {
            double MqProcessati = QueryResult[row, Array.IndexOf(Header, "CntMetriQLav")] is not null ? (double)QueryResult[row, Array.IndexOf(Header, "CntMetriQLav")] : 0;  //Metri quadri prodotti
            double MtTrasp = QueryResult[row, Array.IndexOf(Header, "CntMetriTrasp")] is not null ? (double)QueryResult[row, Array.IndexOf(Header, "CntMetriTrasp")] : 0;  //Metri quadri prodotti;  //Metri trasporto percorsi
            long OreCicloOn = QueryResult[row, Array.IndexOf(Header, "CntOreCicloOn")] is not null ? (long)QueryResult[row, Array.IndexOf(Header, "CntOreCicloOn")] : 0; //Ore ciclo ON
            long MinCicloOn = QueryResult[row, Array.IndexOf(Header, "CntMinCicloOn")] is not null ? (long)QueryResult[row, Array.IndexOf(Header, "CntMinCicloOn")] : 0; //Minuti ciclo ON
            long OrePowerOn = QueryResult[row, Array.IndexOf(Header, "CntOrePowerOn")] is not null ? (long)QueryResult[row, Array.IndexOf(Header, "CntOrePowerOn")] : 0;  //Ore accensione ON
            long MinPowerOn = QueryResult[row, Array.IndexOf(Header, "CntMinPowerOn")] is not null ? (long)QueryResult[row, Array.IndexOf(Header, "CntMinPowerOn")] : 0;  //Minuti ciclo ON

            long MinTotCicloOn = (OreCicloOn * 60) + MinCicloOn;
            long MinTotPowerOn = (OrePowerOn * 60) + MinPowerOn;
            double PercCiclo = MinTotPowerOn != 0 ? MinTotCicloOn / (double)MinTotPowerOn : 0;
            double LoadEfficiency = MqProcessati / (MtTrasp * Project.Current.GetVariable("Model/Variabili_HMI/Macchina/LarghTraspMacchina").Value);

            double ProdutRelativa = MinTotCicloOn != 0 ? MqProcessati * 60 / MinTotCicloOn : 0;         //Mq/OreCiclo

            DateTime DataLogin = QueryResult[row, Array.IndexOf(Header, "LoginDate")] is not null
                ? DateTime.Parse(QueryResult[row, Array.IndexOf(Header, "LoginDate")].ToString())
                : DateTime.Now;

            ResultMatrix[row, 0] = QueryResult[row, Array.IndexOf(Header, "LoginDate")] is not null ? DataLogin.ToString("d", cultureInfo) : "";      //Nome utente            
            ResultMatrix[row, 1] = QueryResult[row, Array.IndexOf(Header, "CntPezziLav")] is not null ? QueryResult[row, Array.IndexOf(Header, "CntPezziLav")].ToString() : "0";   //Conta pezzi
                                                                                                                                                                                   //ResultMatrix[row, 2] = QueryResult[row, Array.IndexOf(Header, "CntMetriLav")] != null ? QueryResult[row, Array.IndexOf(Header, "CntMetriLav")].ToString() : "0";  //Metri lineari prodotti
            ResultMatrix[row, 2] = MqProcessati.ToString("f1");  //Metri quadri prodotti
            ResultMatrix[row, 3] = LoadEfficiency.ToString("P1", cultureInfo);
            ResultMatrix[row, 4] = ProdutRelativa.ToString("f1");
            ResultMatrix[row, 5] = MinTotCicloOn / 60 + " h : " + MinTotCicloOn % 60 + " min";      //Durata tempo ciclo
            ResultMatrix[row, 6] = MinTotPowerOn / 60 + " h : " + MinTotPowerOn % 60 + " min";      //Durata tempo accensione
            ResultMatrix[row, 7] = PercCiclo.ToString("P1", cultureInfo);       //Percentuale ciclo
            ResultMatrix[row, 8] = QueryResult[row, Array.IndexOf(Header, "CntKwOra")] is not null ? ((double)QueryResult[row, Array.IndexOf(Header, "CntKwOra")]).ToString("f1") : "0";   //Potenza assosribta

        }
        return true;
    }

    /// <summary>
    /// Questo metodo estrae dal DB i dati di produzione di tutti gli utenti filtrati in base al giorno. 
    /// </summary>
    /// <param name="GiornoRicerca"></param>
    /// <param name="CsvHeader">Array di stringhe tradotte in base alla lingua selezionata per il report</param>
    /// <param name="ResultMatrix">Matrice di stringhe con i risultati</param>
    /// <returns></returns>
    private bool GetDatiProduzPerUtente(DateTime GiornoRicerca, out string[] CsvHeader, out string[,] ResultMatrix)
    {
        string sqlQuery = $"SELECT * FROM CntProduzione" +
                          $" WHERE LoginDate = '{GiornoRicerca:yyyy-MM-dd}'" +    //Considero l'arco di tutta la giornata, il formattatore 's' serve per recuperare la data in formato ISO (2022-09-15T23:59:59))
                          $" ORDER BY LoginTime ASC";

        string[] Header;
        object[,] QueryResult;

        try
        {
            myStore.Query(sqlQuery, out Header, out QueryResult);
        }
        catch (Exception ex)
        {
            Log.Error("Creazione report statistiche", "GetDatiProduzPerUtente. Errore durante esecuzione query. Errore: " + ex.Message);
            throw;
        }

        var cultureInfo = new CultureInfo(LogicObject.GetVariable("ImpostazOem/LinguaReport").Value.Value.ToString());

        //Creazione riga intestazione con i nomi delle colonne tradotte in base alla lingua selezionata per il report
        CsvHeader = new string[] { "User", "LoginTime", "LogoutTime"
                                   , InformationModel.LookupTranslation(new LocalizedText("Pezzi"), new List<string>(){ cultureInfo.ToString() }).Text
                                   , "m²"
                                   , "m²/m²ₘₐₓ"
                                   , "m²/h"
                                   , InformationModel.LookupTranslation(new LocalizedText("Durata ciclo ON"), new List<string>(){ cultureInfo.ToString() }).Text
                                   , InformationModel.LookupTranslation(new LocalizedText("Durata accensione"), new List<string>(){ cultureInfo.ToString() }).Text
                                   , InformationModel.LookupTranslation(new LocalizedText("PercentCicloOn"), new List<string>(){ cultureInfo.ToString() }).Text
                                   , InformationModel.LookupTranslation(new LocalizedText("Consumo (kWh)"), new List<string>(){ cultureInfo.ToString() }).Text
                                };

        ResultMatrix = new string[QueryResult.GetLength(0), CsvHeader.Length];      //Ridefinisco la matrice con le dimensioni effettive

        if (QueryResult.Rank != 2 || QueryResult.GetLength(0) <= 0)      //l'operatore || valuta l'exp a sx se è vera valuta anche quella a destra altrimenti non la valuta per niente.
            return false;

        //Preparo la matrice di risultati della query
        for (int row = 0; row < QueryResult.GetLength(0); row++)
        {
            double MqProcessati = QueryResult[row, Array.IndexOf(Header, "CntMetriQLav")] is not null ? (double)QueryResult[row, Array.IndexOf(Header, "CntMetriQLav")] : 0;  //Metri quadri prodotti
            double MtTrasp = QueryResult[row, Array.IndexOf(Header, "CntMetriTrasp")] is not null ? (double)QueryResult[row, Array.IndexOf(Header, "CntMetriTrasp")] : 0;  //Metri quadri prodotti;  //Metri trasporto percorsi
            long OreCicloOn = QueryResult[row, Array.IndexOf(Header, "CntOreCicloOn")] is not null ? (long)QueryResult[row, Array.IndexOf(Header, "CntOreCicloOn")] : 0; //Ore ciclo ON
            long MinCicloOn = QueryResult[row, Array.IndexOf(Header, "CntMinCicloOn")] is not null ? (long)QueryResult[row, Array.IndexOf(Header, "CntMinCicloOn")] : 0; //Minuti ciclo ON
            long OrePowerOn = QueryResult[row, Array.IndexOf(Header, "CntOrePowerOn")] is not null ? (long)QueryResult[row, Array.IndexOf(Header, "CntOrePowerOn")] : 0;  //Ore accensione ON
            long MinPowerOn = QueryResult[row, Array.IndexOf(Header, "CntMinPowerOn")] is not null ? (long)QueryResult[row, Array.IndexOf(Header, "CntMinPowerOn")] : 0;  //Minuti ciclo ON

            long MinTotCicloOn = (OreCicloOn * 60) + MinCicloOn;
            long MinTotPowerOn = (OrePowerOn * 60) + MinPowerOn;
            double PercCiclo = MinTotPowerOn != 0 ? MinTotCicloOn / (double)MinTotPowerOn : 0;
            double LoadEfficiency = MqProcessati / (MtTrasp * Project.Current.GetVariable("Model/Variabili_HMI/Macchina/LarghTraspMacchina").Value);

            double ProdutRelativa = MinTotCicloOn != 0 ? MqProcessati * 60 / MinTotCicloOn : 0;         //Mq/OreCiclo

            ResultMatrix[row, 0] = QueryResult[row, Array.IndexOf(Header, "Utente")] is not null ? QueryResult[row, Array.IndexOf(Header, "Utente")].ToString() : "";      //Nome utente            
            ResultMatrix[row, 1] = QueryResult[row, Array.IndexOf(Header, "LoginTime")] is not null ? ((DateTime)QueryResult[row, Array.IndexOf(Header, "LoginTime")]).ToString("G", cultureInfo) : "";       //LoginTime
            ResultMatrix[row, 2] = QueryResult[row, Array.IndexOf(Header, "LogoutTime")] is not null ? ((DateTime)QueryResult[row, Array.IndexOf(Header, "LogoutTime")]).ToString("G", cultureInfo) : "";      //LogoutTime
            ResultMatrix[row, 3] = QueryResult[row, Array.IndexOf(Header, "CntPezziLav")] is not null ? QueryResult[row, Array.IndexOf(Header, "CntPezziLav")].ToString() : "0";   //Conta pezzi
            //ResultMatrix[row, 4] = QueryResult[row, Array.IndexOf(Header, "CntMetriLav")] != null ? QueryResult[row, Array.IndexOf(Header, "CntMetriLav")].ToString() : "0";  //Metri lineari prodotti
            ResultMatrix[row, 4] = MqProcessati.ToString("f1");  //Metri quadri prodotti
            ResultMatrix[row, 5] = LoadEfficiency.ToString("P1", cultureInfo);
            ResultMatrix[row, 6] = ProdutRelativa.ToString("f1");
            ResultMatrix[row, 7] = MinTotCicloOn / 60 + " h : " + MinTotCicloOn % 60 + " min";      //Durata tempo ciclo
            ResultMatrix[row, 8] = MinTotPowerOn / 60 + " h : " + MinTotPowerOn % 60 + " min";      //Durata tempo accensione
            ResultMatrix[row, 9] = PercCiclo.ToString("P1", cultureInfo);       //Percentuale ciclo
            ResultMatrix[row, 10] = QueryResult[row, Array.IndexOf(Header, "CntKwOra")] is not null ? ((double)QueryResult[row, Array.IndexOf(Header, "CntKwOra")]).ToString("f1") : "0";   //Potenza assosribta                                  
        }
        return true;
    }

    [ExportMethod]
    public void AvviaSessione()
    {
        Impostazioni = LogicObject.GetObject("ImpostazOem");
        AbiltaGestione = Impostazioni.GetVariable("AbilitaGestione");

        if (AbiltaGestione.Value)
        {
            CntProduzPlc = LogicObject.Context.GetNode(LogicObject.GetVariable("CntProduzPlc").Value);  // tiro su il nodo dove si trovano le variabili PLC

            myStore = (Store)LogicObject.Context.GetNode(LogicObject.GetVariable("DataStore").Value);  // Tiro su il nodeId dello store

            PlcStation = (CommunicationStation)LogicObject.Context.GetNode(LogicObject.GetVariable("PlcStation").Value);  // Tiro su il nodeId della stazione PLC

            DataUltimoSalvataggio = Impostazioni.GetVariable("DataUltimoSalvataggio");

            UtenteAttuale = LogicObject.GetVariable("UtenteAttuale");
            UtenteAttuale.VariableChange += UtenteAttuale_VariableChange;       //Sottoscrivo il cambiamento dell'utente

            ActionCheckCambioGiornoAndAgiornaDati = new PeriodicTask(CheckCambioGiornoAndAgiornaDati, Impostazioni.GetVariable("TempoAggiornaDati_min").Value * 60000, LogicObject);      //for production
            //ActionCheckCambioGiornoAndAgiornaDati = new PeriodicTask(CheckCambioGiornoAndAgiornaDati, Impostazioni.GetVariable("TempoAggiornaDati_min").Value * 5000, LogicObject);         //for debug
            ActionCheckCambioGiornoAndAgiornaDati.Start();          //Sottoscrivo il task a tempo e lo avvio per l'aggiornamento dei dati sul DB e controllo se deve essere fatto il report

            AvvioApp = true;          //Creo una Riga per l'utente all'avvio
        }
    }

    [ExportMethod]
    public void TerminaSessione()
    {
        if (AbiltaGestione.Value)
        {
            if (PlcStation.OperationCode != CommunicationOperationCode.Connected)
            {
                Log.Error("Gestione statistiche", "Errore TerminaSessione. Connessione PLC assente");
                return;
            }

            LogoutUtente(UtenteAttuale.Value, DateTime.Now);
            DisabilitaGestione();
        }
    }

    private void DisabilitaGestione()
    {
        UtenteAttuale.VariableChange -= UtenteAttuale_VariableChange;
        ActionCheckCambioGiornoAndAgiornaDati?.Dispose();
        ActionCheckCambioGiornoAndAgiornaDati = null;
    }

    private void UtenteAttuale_VariableChange(object sender, VariableChangeEventArgs e)
    {
        if (PlcStation.OperationCode != CommunicationOperationCode.Connected)
        {
            Log.Error("Gestione statistiche", "Errore gestione cambio utente. Connessione PLC assente");
            return;
        }

        if (!string.IsNullOrWhiteSpace(e.OldValue))
            LogoutUtente(e.OldValue, DateTime.Now);

        if (!string.IsNullOrWhiteSpace(e.NewValue))
            LoginUtente();
    }

    private void CheckCambioGiornoAndAgiornaDati()
    {
        if (AvvioApp)
        {

            var Giorno = DateTime.Now.AddDays(-Impostazioni.GetVariable("GiorniStorico").Value).Date.ToString("s");
            string Query = $"DELETE FROM \"CntProduzione\" WHERE LoginTime < '{Giorno}'";
            try
            {
                myStore.Query(Query, out _, out _);
            }
            catch (Exception Ex)
            {
                Log.Error("Gestione statistiche", "Errore cancellazione vecchi dati dalla tabella CntProduzione. Errore: " + Ex.Message);
                return;
            }
        }

        //Verifico la connessione con PLC
        if (PlcStation.OperationCode != CommunicationOperationCode.Connected)
            return;

        //Controllo se è cambiato il giorno         
        if ((DateTime.Now.Date - ((DateTime)DataUltimoSalvataggio.Value).Date).Days != 0)
        {
            if (!string.IsNullOrWhiteSpace(UtenteAttuale.Value))
            {
                if (!AvvioApp)
                {
                    LogoutUtente(UtenteAttuale.Value, DateTime.Now.Date.AddSeconds(-1));        //Come logout time passo la mezzanotte meno 1 secondo del giorno precedente
                }

                LoginUtente();      //Apro una nuova riga per lo User loggato
                AvvioApp = false;
            }

            //Se la creazione del report è abilitata allora avvio il task che crerà il report
            if (!CreaCsvReport && Impostazioni.GetVariable("AbilitaCreazReport").Value)
            {
                CreaCsvReport = true;
                GiornoRicerca = DataUltimoSalvataggio.Value;
                ActionCreaReport = new DelayedTask(CreaReport, new TimeSpan(0, 0, 1, 0), LogicObject);      // creo il report dopo 1 minuto
                //ActionCreaReport = new DelayedTask(CreaReport, new TimeSpan(0, 0, 0, 30), LogicObject);      // creo il report dopo 30s
                ActionCreaReport.Start();
            }

            DataUltimoSalvataggio.Value = DateTime.Now;
            return;     //ritorno il controllo al chiamante                        
        }
        else if (AvvioApp)
        {
            LoginUtente();
            AvvioApp = false;
            return;
        }

        // creo la stringa per aggiornare la tabella CntProduzione            
        UpdateUtente(UtenteAttuale.Value.Value.ToString());
    }

    private void LoginUtente()
    {
        try
        {
            //Disattivo eventuali utenti attivi nel DB
            myStore.Query("UPDATE CntProduzione SET Attivo = false ", out _, out _);

            int len = myStore.Tables.Get("CntProduzione").Columns.Count;

            string[] NomiColonne = new string[len]; //header
            var MatrixValori = new object[1, len];

            NomiColonne[0] = "Utente";
            NomiColonne[1] = "Attivo";
            NomiColonne[2] = "LoginDate";
            NomiColonne[3] = "LoginTime";
            NomiColonne[4] = "LogoutTime";

            int i = 5;

            var myVariables = new List<RemoteChildVariable>();
            foreach (var Par in CntProduzPlc.GetNodesByType<IUAVariable>())
            {
                myVariables.Add(new RemoteChildVariable(Par.BrowseName));
                NomiColonne[i] = Par.BrowseName;
                i++;
            }

            MatrixValori[0, 0] = UtenteAttuale.Value.Value;
            MatrixValori[0, 1] = true;
            MatrixValori[0, 2] = DateTime.Now.ToString("yyyy-MM-dd");
            MatrixValori[0, 3] = DateTime.Now;

            var reads = CntProduzPlc.ChildrenRemoteRead(myVariables);
            i = 5;
            foreach (var Par in reads)
            {
                MatrixValori[0, i] = Par.Value.Value.ToString();
                i++;
            }

            myStore.Insert("CntProduzione", NomiColonne, MatrixValori);              // aggiornamento database
        }
        catch (Exception ex)
        {
            Log.Error("Gestione statistiche", "Errore login utente. Errore: " + ex.ToString());
            throw;
        }
    }

    private void UpdateUtente(string Utente)
    {
        try
        {
            var myVariables = CntProduzPlc.GetNodesByType<IUAVariable>().Select(Par => new RemoteChildVariable(Par.BrowseName)).ToList();

            // creo la stringa per aggiornare la tabella ricette            
            StringBuilder query = new("UPDATE CntProduzione SET Attivo = true");

            var reads = CntProduzPlc.ChildrenRemoteRead(myVariables);
            foreach (var Par in reads)
            {
                query.Append(", " + Par.RelativePath + " = " + (int)Par.Value);
            }

            query.Append(" WHERE Utente = '" + Utente + "' AND Attivo = true");

            myStore.Query(query.ToString(), out string[] header, out object[,] resultSet);
        }
        catch (Exception ex)
        {
            Log.Error("Gestione statistiche", "Errore update contatori utente . Errore: " + ex.ToString());
            throw;
        }
    }

    private void LogoutUtente(string Utente, DateTime LogoutTime)
    {
        try
        {
            var myVariables = CntProduzPlc.GetNodesByType<IUAVariable>().Select(Par => new RemoteChildVariable(Par.BrowseName)).ToList();

            // creo la stringa per aggiornare la tabella ricette            
            StringBuilder query = new("UPDATE CntProduzione SET Attivo = false, LogoutTime = '" + LogoutTime.ToString("s") + "'");

            var reads = CntProduzPlc.ChildrenRemoteRead(myVariables);
            foreach (var Par in reads)
            {
                query.Append(", " + Par.RelativePath + " = " + (int)Par.Value);
            }

            query.Append(" WHERE Utente = '" + Utente + "' AND Attivo = true");

            myStore.Query(query.ToString(), out string[] header, out object[,] resultSet);

            ResetCnt();     //Faccio il reset dei contatori
        }
        catch (Exception ex)
        {
            Log.Error("Gestione statistiche", "Errore logout utente. Errore: " + ex.ToString());
            throw;
        }
    }

    private void ResetCnt()
    {
        var PlcCntRst = Project.Current.GetObject("Model/GestStatistiche/PlcCntRst");
        try
        {
            List<RemoteChildVariableValue> remoteRead = (from Figlio in PlcCntRst.GetNodesByType<IUAVariable>()
                                                         select new RemoteChildVariableValue(Figlio.BrowseName, true)).ToList();

            PlcCntRst.ChildrenRemoteWrite(remoteRead);
        }
        catch (Exception ex)
        {
            Log.Error("Gestione statistiche", "Errore reset contatori per logout utente: " + ex.ToString());
            throw;
        }
    }

    private void CreaReport()
    {
        //Controlla se la directory Report esiste nel percorso indicato altrimenti la crea
        string CsvReportSavingPath = LogicObject.GetVariable("TargetLinux").Value
            ? new ResourceUri("%USB1%").Uri
            : (string)Impostazioni.GetVariable("CsvReportSavingPath").Value;

        string Folder = $"{CsvReportSavingPath}/Report";
        if (!Directory.Exists(Folder))
            Directory.CreateDirectory(Folder);

        //Cancella i file vecchi più di 31 giorni per liberare memoria
        foreach (var elemento in new DirectoryInfo(Folder).GetFiles("*.*").Where(elemento => elemento.CreationTime < DateTime.Now.AddDays(-31)))
            elemento.Delete();// Elimina il file

        var cultureInfo = new CultureInfo(LogicObject.GetVariable("ImpostazOem/LinguaReport").Value.Value.ToString());

        string Macchina = Project.Current.GetVariable("Model/Variabili_HMI/Var_Ritentive/sNomeMacchAndMatr").Value.Value.ToString();        //Es. Next_18000

        string Data = GiornoRicerca.ToString("d", cultureInfo).Replace("/", "_");         //creo stringa per la data e ora nel formato dd_MM_yyyy in base al culture info dell'utente loggato

        string CSVPath = @"" + Folder + "/" + Macchina + "_ProcessData_" + Data + ".csv";        //Storicizzo il nome dell'ultimo file salvato per l'invio della mail

        string csvPath = new ResourceUri(value: CSVPath).Uri;

        if (string.IsNullOrEmpty(csvPath))
        {
            Log.Error("Production report creation", "No CSV file found");
            return;
        }

        //controllo se il carattere separatore è valdo oppure no
        char? characterSeparator = Runtime_Utility.CheckCharacterSeparator(",");
        if (characterSeparator == null || characterSeparator == '\0')
        {
            Log.Error("Production report creation", "Wrong CharacterSeparator configuration. Please insert a char");
            return;
        }

        bool wrapFields = true;     //indica che le colonne devono essere incapsualte tra doppi apici
        bool ReportCreated = false;

        try
        {
            //Apro lo stream verso il file 
            using (CSVFileWriter csvWriter = new(csvPath) { FieldDelimiter = characterSeparator.Value, WrapFields = wrapFields })
            {
                //Tiro su i dati della produzione giornaliera da scrivere sul file csv                
                if (GetDatiProduzGiornaliera(GiornoRicerca, out string[] header, out string[,] resultSet))
                {

                    //Creo la prima riga con il nome della macchina
                    var row = new string[header.Length];
                    row[0] = Macchina;
                    for (var c = 1; c < header.Length; ++c)
                    {
                        row[c] = "";
                    }
                    csvWriter.WriteLine(row);


                    //Creo l'intestazione per la produzione giornaliera
                    for (var c = 0; c < header.Length; ++c)
                    {
                        row[c] = header[c];
                    }
                    csvWriter.WriteLine(row);

                    //scrivo la riga dei risultati
                    for (var c = 0; c < header.Length; ++c)
                    {
                        row[c] = resultSet[0, c];
                    }
                    csvWriter.WriteLine(row);

                    //inserisco 2 righe vuote
                    string[] EmptyLine = new string[header.Length];
                    for (int i = 0; i < header.Length; i++)
                        EmptyLine[i] = "";

                    csvWriter.WriteLine(EmptyLine);
                    csvWriter.WriteLine(EmptyLine);

                    //Tiro su i dati della produzione giornaliera per utente
                    if (GetDatiProduzPerUtente(GiornoRicerca, out header, out resultSet))
                    {
                        int rowCount = resultSet.GetLength(0);
                        int columnCount = header.Length;
                        row = new string[columnCount];

                        //Creo la riga di intestazione
                        for (var c = 0; c < header.Length; ++c)
                        {
                            row[c] = header[c];
                        }
                        csvWriter.WriteLine(row);

                        //Creo le righe con i risultati
                        for (var r = 0; r < rowCount; ++r)      //ciclo per le righe
                        {
                            for (var c = 0; c < columnCount; ++c)  //ciclo per le colonne
                            {
                                row[c] = resultSet[r, c];
                            }
                            csvWriter.WriteLine(row);
                        }
                    }
                    Log.Info("Production report creation", $"CSV successfully created to {csvPath}");
                    ReportCreated = true;
                }
            }

            //Eseguo il codice per l'invio della mail se abilitato. 
            if (ReportCreated & Impostazioni.GetVariable("AbilitaInvioMail").Value)
            {
                var InvioMail = Project.Current.Get<NetLogicObject>("Scripts/EmailSenderLogic");
                InvioMail.ExecuteMethod("SendEmail_LongRunningTask", args: new object[] { Macchina + " production report", "This is an automatically generated email please do not reply", CSVPath, null });
            }
        }
        catch (Exception ex)
        {
            Log.Error("Production report creation", $"Unable to create CSV file: {ex}");
        }

        CreaCsvReport = false;
        ActionCreaReport?.Dispose();
        ActionCreaReport = null;
    }
}
