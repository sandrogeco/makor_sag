#region Using directives
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
#endregion

public class GestStatisticheLogic : BaseNetLogic
{
    /// <summary>
    /// Questo metodo estrae dal DB i dati di produzione di tutti gli utenti filtrati in base al giorno. 
    /// </summary>
    /// <param name="GiornoRicerca"></param>
    /// <param name="header">Array di stringhe tradotte in base alla lingua selezionata per il report</param>
    /// <param name="Risultati">Matrice di stringhe con i risultati</param>
    /// <returns></returns>
    private bool GetDatiProduzPerUtente(DateTime GiornoRicerca, out string[] header, out string[,] Risultati)
    {
        string sqlQuery = $"SELECT * FROM CntProduzione" +
                          $" WHERE LoginTime BETWEEN '{GiornoRicerca.Date:s}' AND '{GiornoRicerca.Date.AddSeconds(86399):s}'" +    //Considero l'arco di tutta la giornata, il formattatore 's' serve per recuperare la data in formato ISO (2022-09-15T23:59:59))
                          $" ORDER BY LoginTime ASC";

        myStore.Query(sqlQuery, out _, out object[,] QueryResult);

        var cultureInfo = new CultureInfo(LogicObject.GetVariable("ImpostazOem/LinguaReport").Value.Value.ToString());

        //Creazione riga intestazione con i nomi delle colonne tradotte in base alla lingua selezionata per il report
        header = new string[10] { "User", "LoginTime", "LogoutTime"
                                  , InformationModel.LookupTranslation(new LocalizedText("Pezzi lavorati"), new List<string>(){ cultureInfo.ToString() }).Text
                                  , InformationModel.LookupTranslation(new LocalizedText("Metri lavorati"), new List<string>(){ cultureInfo.ToString() }).Text
                                  , InformationModel.LookupTranslation(new LocalizedText("Durata accensione"), new List<string>(){ cultureInfo.ToString() }).Text
                                  , InformationModel.LookupTranslation(new LocalizedText("PercentCicloOn"), new List<string>(){ cultureInfo.ToString() }).Text
                                  , InformationModel.LookupTranslation(new LocalizedText("PercentAllarmeOn"), new List<string>(){ cultureInfo.ToString() }).Text
                                  , InformationModel.LookupTranslation(new LocalizedText("PercentStopOn"), new List<string>(){ cultureInfo.ToString() }).Text
                                  , InformationModel.LookupTranslation(new LocalizedText("PercentPausaOn"), new List<string>(){ cultureInfo.ToString() }).Text
                                };

        Risultati = new string[QueryResult.GetLength(0), 10];      //Ridefinisco la matrice con le dimensioni effettive

        if (QueryResult.Rank != 2 || QueryResult.GetLength(0) <= 0)      //l'operatore || valuta l'exp a sx se è vera valuta anche quella a destra altrimenti non la valuta per niente.
            return false;

        //Preparo la matrice di risultati della query
        for (int row = 0; row < QueryResult.GetLength(0); row++)
        {
            Risultati[row, 0] = QueryResult[row, 0].ToString();      //Nome utente
            //resultSet[row, 1] = QueryResult[row, 1].ToString();      //Utente attivo
            Risultati[row, 1] = ((DateTime)QueryResult[row, 2]).ToString("G", cultureInfo);      //LoginTime
            Risultati[row, 2] = QueryResult[row, 3] is null ? "" : ((DateTime)QueryResult[row, 3]).ToString("G", cultureInfo);      //LogoutTime
            Risultati[row, 3] = QueryResult[row, 4].ToString();      //NPezzi_Prodotti
            Risultati[row, 4] = QueryResult[row, 5].ToString();      //NMetri_Prodotti
            Risultati[row, 5] = QueryResult[row, 6] + "h : " + QueryResult[row, 7] + "min";      //Durata accensione quadro

            var DurataAccMin = (Convert.ToDouble(QueryResult[row, 6]) * 60) + Convert.ToDouble(QueryResult[row, 7]);
            Risultati[row, 6] = DurataAccMin <= 0 ? "0%" : (((Convert.ToDouble(QueryResult[row, 8]) * 60) + Convert.ToDouble(QueryResult[row, 9])) / DurataAccMin).ToString("P1", cultureInfo);      //% In ciclo. Il metodo ToString() crea una stringa simile 13,5%
            Risultati[row, 7] = DurataAccMin <= 0 ? "0%" : (((Convert.ToDouble(QueryResult[row, 10]) * 60) + Convert.ToDouble(QueryResult[row, 11])) / DurataAccMin).ToString("P1", cultureInfo);    //% In allarme
            Risultati[row, 8] = DurataAccMin <= 0 ? "0%" : (((Convert.ToDouble(QueryResult[row, 12]) * 60) + Convert.ToDouble(QueryResult[row, 13])) / DurataAccMin).ToString("P1", cultureInfo);    //% In stop
            Risultati[row, 9] = DurataAccMin <= 0 ? "0%" : (((Convert.ToDouble(QueryResult[row, 14]) * 60) + Convert.ToDouble(QueryResult[row, 15])) / DurataAccMin).ToString("P1", cultureInfo);   //% In pausa       
        }
        return true;
    }

    /// <summary>
    /// Questo metodo estrae dal DB la sommatoria dei dati di produzione riferita a un particolare giorno
    /// </summary>
    /// <param name="GiornoRicerca"></param>
    /// <param name="header">Array di stringhe tradotte in base alla lingua selezionata per il report</param>
    /// <param name="Risultati">Matrice di stringhe con i risultati</param>
    /// <returns></returns>
    private bool GetDatiProduzGiornaliera(DateTime GiornoRicerca, out string[] header, out string[,] Risultati)
    {
        //Creazione Query per estrapolare i dati da visualizzare sulla griglia
        //Nota: a causa delle limitazioni dovute allo Standard ANSI Sql92 e della mancanza di istruzioni(es 'CAST') non supportate da QStudio, la progettazione della query è stata molto complicata. 
        //Per estrapolare i dati necessari è stata utilizzata una tabella di appoggio perchè i dati finali devono essere raggruppati per data ma su QStudio la funzione EXTRACT non è supportata nella clausola GROUP BY. Prima sono stati tirati su i dati convertendo il logintime in giorno, mese e anno
        //Poi sono stati fatti i raggruppamenti.

        //Creazione tabella di appoggio        
        string tblAppoggio = $"(SELECT EXTRACT(DAY FROM LoginTime) AS Giorno, EXTRACT(MONTH FROM LoginTime) AS Mese, EXTRACT(YEAR FROM LoginTime) AS Anno" +
                             $", NPezzi_Prodotti" +
                             $", NMetri_Prodotti" +
                             $", CntOreAccensQuadro" +
                             $", CntMinutiAccensQuadro" +
                             $", CntOreCicloOn" +
                             $", CntMinutiCicloOn" +
                             $", CntOreAllarmeOn" +
                             $", CntMinutiAllarmeOn" +
                             $", CntOreStopOn" +
                             $", CntMinutiStopOn" +
                             $", CntOrePausaOn" +
                             $", CntMinutiPausaOn" +
                             $" FROM CntProduzione" +
                             $" WHERE LoginTime BETWEEN '{GiornoRicerca.Date:s}' AND '{GiornoRicerca.Date.AddSeconds(86399):s}') AS TblAppoggio";  //il formattatore 's' serve per recuperare la data in formato ISO (2022-09-15T23:59:59)

        //Creazione query finale
        string sqlQuery = $"SELECT Giorno, Mese, Anno" +
                          $", SUM(NPezzi_Prodotti) AS NPezzi_Prodotti" +
                          $", SUM(NMetri_Prodotti) AS NMetri_Prodotti" +
                          $", SUM(CntOreAccensQuadro) AS CntOreAccensQuadro" +
                          $", SUM(CntMinutiAccensQuadro) AS CntMinutiAccensQuadro" +
                          $", SUM(CntOreCicloOn) AS CntOreCicloOn" +
                          $", SUM(CntMinutiCicloOn) AS CntMinutiCicloOn" +
                          $", SUM(CntOreAllarmeOn) AS CntOreAllarmeOn" +
                          $", SUM(CntMinutiAllarmeOn) AS CntMinutiAllarmeOn " +
                          $", SUM(CntOreStopOn) AS CntOreStopOn" +
                          $", SUM(CntMinutiStopOn) AS CntMinutiStopOn" +
                          $", SUM(CntOrePausaOn) AS CntOrePausaOn" +
                          $", SUM(CntMinutiPausaOn) AS CntMinutiPausaOn" +
                          $" FROM {tblAppoggio}" +  //la sorgente è la tbl di appoggio
                          $" GROUP BY Anno, Mese, Giorno" +
                          $" ORDER BY Anno ASC, Mese ASC, Giorno ASC";

        myStore.Query(sqlQuery, out _, out object[,] QueryResult);

        var cultureInfo = new CultureInfo(LogicObject.GetVariable("ImpostazOem/LinguaReport").Value.Value.ToString());

        //Creazione riga intestazione con i nomi delle colonne tradotte in base alla lingua selezionata per il report
        header = new string[8] {    InformationModel.LookupTranslation(new LocalizedText("Data"), new List<string>(){ cultureInfo.ToString() }).Text
                                  , InformationModel.LookupTranslation(new LocalizedText("Pezzi lavorati"), new List<string>(){ cultureInfo.ToString() }).Text
                                  , InformationModel.LookupTranslation(new LocalizedText("Metri lavorati"), new List<string>(){ cultureInfo.ToString() }).Text
                                  , InformationModel.LookupTranslation(new LocalizedText("Durata accensione"), new List<string>(){ cultureInfo.ToString() }).Text
                                  , InformationModel.LookupTranslation(new LocalizedText("PercentCicloOn"), new List<string>(){ cultureInfo.ToString() }).Text
                                  , InformationModel.LookupTranslation(new LocalizedText("PercentAllarmeOn"), new List<string>(){ cultureInfo.ToString() }).Text
                                  , InformationModel.LookupTranslation(new LocalizedText("PercentStopOn"), new List<string>(){ cultureInfo.ToString() }).Text
                                  , InformationModel.LookupTranslation(new LocalizedText("PercentPausaOn"), new List<string>(){ cultureInfo.ToString() }).Text
                                };

        Risultati = new string[QueryResult.GetLength(0), 8];      //Ridefinisco la matrice con le dimensioni effettive

        if (QueryResult.Rank != 2 || QueryResult.GetLength(0) <= 0)      //l'operatore || valuta l'exp a sx se è vera valuta anche quella a destra altrimenti non la valuta per niente.
            return false;

        //Preparo la matrice di risultati della query
        for (int row = 0; row < QueryResult.GetLength(0); row++)
        {
            Risultati[row, 0] = new DateTime(Convert.ToInt32(QueryResult[row, 2]), Convert.ToInt32(QueryResult[row, 1]), Convert.ToInt32(QueryResult[row, 0])).ToString("d", cultureInfo);      //Data
            //resultSet[row, 1] = QueryResult[row, 1].ToString();      //Utente attivo
            Risultati[row, 1] = QueryResult[row, 3].ToString();      //NPezzi_Prodotti
            Risultati[row, 2] = QueryResult[row, 4].ToString();      //NMetri_Prodotti
            Risultati[row, 3] = QueryResult[row, 5] + "h : " + QueryResult[row, 6] + "min";      //Durata accensione quadro      

            var DurataAccMin = (Convert.ToDouble(QueryResult[row, 5]) * 60) + Convert.ToDouble(QueryResult[row, 6]);
            Risultati[row, 4] = DurataAccMin <= 0 ? "0%" : (((Convert.ToDouble(QueryResult[row, 7]) * 60) + Convert.ToDouble(QueryResult[row, 8])) / DurataAccMin).ToString("P1", cultureInfo);      //% In ciclo. Il metodo ToString() crea una stringa es. 13,5%
            Risultati[row, 5] = DurataAccMin <= 0 ? "0%" : (((Convert.ToDouble(QueryResult[row, 9]) * 60) + Convert.ToDouble(QueryResult[row, 10])) / DurataAccMin).ToString("P1", cultureInfo);    //% In allarme
            Risultati[row, 6] = DurataAccMin <= 0 ? "0%" : (((Convert.ToDouble(QueryResult[row, 11]) * 60) + Convert.ToDouble(QueryResult[row, 12])) / DurataAccMin).ToString("P1", cultureInfo);    //% In stop
            Risultati[row, 7] = DurataAccMin <= 0 ? "0%" : (((Convert.ToDouble(QueryResult[row, 13]) * 60) + Convert.ToDouble(QueryResult[row, 14])) / DurataAccMin).ToString("P1", cultureInfo);   //% In pausa        
        }
        return true;
    }

    public override void Start()
    {
        Impostazioni = LogicObject.GetObject("ImpostazOem");
        AbiltaGestione = Impostazioni.GetVariable("AbilitaGestione");

        AbiltaGestione.VariableChange += AbiltaGestione_VariableChange;    //Sottoscrivo l'evento cambiamento variabile

        if (AbiltaGestione.Value)
            AbilitaGestione();
    }

    [ExportMethod]
    public void TerminaSessione()
    {
        if (AbiltaGestione.Value)
        {
            LogoutUtente(UtenteAttuale.Value, DateTime.Now);
            DisabilitaGestione();
        }

        AbiltaGestione.VariableChange -= AbiltaGestione_VariableChange;    //Tolgo sottoscrizione cambiamento variabile per liberare risorse
    }

    private void AbiltaGestione_VariableChange(object sender, VariableChangeEventArgs e)
    {
        if (e.NewValue)
            AbilitaGestione();
        else
            DisabilitaGestione();
    }

    /// <summary>
    /// 
    /// </summary>
    private void AbilitaGestione()
    {
        CntProduzPlc = LogicObject.Context.GetNode(LogicObject.GetVariable("CntProduzPlc").Value);  // tiro su il nodo dove si trovano le variabili PLC

        myStore = (Store)LogicObject.Context.GetNode(LogicObject.GetVariable("DataStore").Value);  // Tiro su il nodeId dello store

        AbilitaCreazReport = Impostazioni.GetVariable("AbilitaCreazReport");
        DataUltimoSalvataggio = Impostazioni.GetVariable("DataUltimoSalvataggio");

        UtenteAttuale = LogicObject.GetVariable("UtenteAttuale");
        UtenteAttuale.VariableChange += UtenteAttuale_VariableChange;       //Sottoscrivo il cambiamento dell'utente

        ActionCheckCambioGiornoAndAgiornaDati = new PeriodicTask(CheckCambioGiornoAndAgiornaDati, Impostazioni.GetVariable("TempoAggiornaDati_min").Value * 60000, LogicObject);
        //ActionCheckCambioGiornoAndAgiornaDati = new PeriodicTask(CheckCambioGiornoAndAgiornaDati, Impostazioni.GetVariable("TempoAggiornaDati_min").Value * 5000, LogicObject);
        ActionCheckCambioGiornoAndAgiornaDati.Start();          //Sottoscrivo il task a tempo e lo avvio per l'aggiornamento dei dati sul DB e controllo se deve essere fatto il report

        LoginUtente();      //Creo una Riga per l'utente all'avvio
    }

    private void DisabilitaGestione()
    {
        UtenteAttuale.VariableChange -= UtenteAttuale_VariableChange;
        ActionCheckCambioGiornoAndAgiornaDati.Dispose();
        ActionCheckCambioGiornoAndAgiornaDati = null;
    }

    private void CheckCambioGiornoAndAgiornaDati()
    {
        //Controllo se è cambiato il giorno         
        if ((DateTime.Now.Date - ((DateTime)DataUltimoSalvataggio.Value).Date).Days != 0)
        {
            try
            {
                //Verifico se c'è qualche utente loggato per eseguire le operazioni sul database
                //if (!string.IsNullOrWhiteSpace(UtenteAttuale.Value) && string.Compare(UtenteAttuale.Value, "Anonymous") != 0)                
                if (!string.IsNullOrWhiteSpace(UtenteAttuale.Value))
                {
                    LogoutUtente(UtenteAttuale.Value, DateTime.Now.Date.AddSeconds(-1));        //Come logout time passo la mezzanotte meno 1 secondo del giorno precedente
                    LoginUtente();      //Apro una nuova riga per lo User loggato
                }

                //Se la creazione del report è abilitata allora avvio il task che crerà il report
                if (!CreaCsvReport && AbilitaCreazReport.Value)
                {
                    CreaCsvReport = true;
                    GiornoRicerca = DataUltimoSalvataggio.Value;
                    ActionCreaReport = new DelayedTask(CreaReport, new TimeSpan(0, 0, 5, 0), LogicObject);      // creo il report dopo 5 minuti
                    //ActionCreaReport = new DelayedTask(CreaReport, new TimeSpan(0, 0, 0, 30), LogicObject);      // creo il report dopo 30s
                    ActionCreaReport.Start();
                }
            }
            catch
            {
                return;
            }

            DataUltimoSalvataggio.Value = DateTime.Now;
            return;     //ritorno il controllo al chiamante
        }

        try
        {
            // creo la stringa per aggiornare la tabella CntProduzione            
            StringBuilder query = new("UPDATE CntProduzione SET Attivo = true");

            var myVariables = CntProduzPlc.GetNodesByType<IUAVariable>().Select(Par => new RemoteChildVariable(Par.BrowseName)).ToList();  //Creo la lista per leggere le var dal campo.
            var reads = CntProduzPlc.ChildrenRemoteRead(myVariables);
            foreach (var Par in reads)
                query.Append(", " + Par.RelativePath + " = " + Par.Value.Value.ToString());

            query.Append($" WHERE Utente = '{UtenteAttuale.Value.Value}' AND Attivo = true");

            myStore.Query(query.ToString(), out string[] header, out object[,] resultSet);
        }
        catch (Exception ex)
        {
            Log.Error("CheckCambioGiornoAndAgiornaDati", "Errore lettura variabili di campo: " + ex.ToString());
            throw;
        }
    }

    private void UtenteAttuale_VariableChange(object sender, VariableChangeEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.OldValue))
            LogoutUtente(e.OldValue, DateTime.Now);

        if (!string.IsNullOrWhiteSpace(e.NewValue))
            LoginUtente();
    }

    private void LoginUtente()
    {
        //Disattivo eventuali utenti attivi nel DB
        myStore.Query("UPDATE CntProduzione SET Attivo = false ", out _, out _);

        ResetCnt();
        int len = 3 + CntProduzPlc.Children.Count;

        string[] NomiColonne = new string[len];
        NomiColonne[0] = "Utente";
        NomiColonne[1] = "Attivo";
        NomiColonne[2] = "LoginTime";

        var MatrixValori = new object[1, len];

        int i = 3;

        var myVariables = new List<RemoteChildVariable>();
        foreach (var Par in CntProduzPlc.GetNodesByType<IUAVariable>())
        {
            myVariables.Add(new RemoteChildVariable(Par.BrowseName));
            NomiColonne[i] = Par.BrowseName;
            i++;
        }

        try
        {
            MatrixValori[0, 0] = UtenteAttuale.Value.Value;
            MatrixValori[0, 1] = true;
            MatrixValori[0, 2] = DateTime.Now;

            var reads = CntProduzPlc.ChildrenRemoteRead(myVariables);
            i = 3;
            foreach (var Par in reads)
            {
                MatrixValori[0, i] = Par.Value.Value.ToString();
                i++;
            }

            myStore.Insert("CntProduzione", NomiColonne, MatrixValori);              // aggiornamento database
        }
        catch (Exception ex)
        {
            Log.Error("LoginUtente", "Errore lettura varibili di campo: " + ex.ToString());
            throw;
        }
    }


    private void LogoutUtente(string Utente, DateTime LogoutTime)
    {
        try
        {
            // creo la stringa per aggiornare la tabella ricette            
            StringBuilder query = new("UPDATE CntProduzione SET Attivo = false, LogoutTime = '" + LogoutTime.ToString("s") + "'");

            foreach (var Par in CntProduzPlc.ChildrenRemoteRead(CntProduzPlc.GetNodesByType<IUAVariable>().Select(Par => new RemoteChildVariable(Par.BrowseName)).ToList()))
            {
                query.Append(", " + Par.RelativePath + " = " + Par.Value.Value.ToString());
            }

            query.Append(" WHERE Utente = '" + Utente + "' AND Attivo = true");

            myStore.Query(query.ToString(), out string[] header, out object[,] resultSet);
        }
        catch (Exception ex)
        {
            Log.Error("LogoutUtente", "Errore lettura variabili di campo oppure aggiornamento database: " + ex.ToString());
            throw;
        }

        ResetCnt();     //Faccio il reset dei contatori
    }

    private void ResetCnt()
    {
        try
        {
            var remoteRead = (from Figlio in CntProduzPlc.GetNodesByType<IUAVariable>()
                              select new RemoteChildVariableValue(Figlio.BrowseName, 0)).ToList();

            CntProduzPlc.ChildrenRemoteWrite(remoteRead);
        }
        catch (Exception ex)
        {
            Log.Error("ChildrenRemoteWrite failed: " + ex.ToString());
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

        string Data = GiornoRicerca.ToString("d", cultureInfo).Replace("/", "_");         //creo stringa per la data e ora nel formato dd_MM_yyyy in base al culture info dell'utente loggato

        string CSVPath = @"" + Folder + "/MakorLineProcessDataReport_" + Data + ".csv";        //Storicizzo il nome dell'ultimo file salvato per l'invio della mail

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
            using (var csvWriter = new CSVFileWriter(csvPath) { FieldDelimiter = characterSeparator.Value, WrapFields = wrapFields })
            {
                //Tiro su i dati della produzione giornaliera da scrivere sul file csv                
                if (GetDatiProduzGiornaliera(GiornoRicerca, out string[] header, out string[,] resultSet))
                {
                    var EmptyLine = new string[header.Length];
                    for (int i = 0; i < header.Length; i++)
                        EmptyLine[i] = "";

                    //Creo l'intestazione della prima riga
                    var row = new string[header.Length];
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

                    csvWriter.WriteLine(EmptyLine);
                    csvWriter.WriteLine(EmptyLine);

                    //Tiro su i dati della produzione giornaliera per utente
                    if (GetDatiProduzPerUtente(GiornoRicerca, out header, out resultSet))
                    {
                        int rowCount = resultSet.GetLength(0);
                        int columnCount = header.Length;
                        row = new string[columnCount];

                        //creo la riga di intestazione
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
                InvioMail.ExecuteMethod("SendEmail_LongRunningTask", args: new object[] { "Makor line production report", "This is an automatically generated email please do not reply", CSVPath, null });
            }
        }
        catch (Exception ex)
        {
            Log.Error("Production report creation", $"Unable to create CSV file: {ex}");
        }

        CreaCsvReport = false;
        ActionCreaReport.Dispose();
        ActionCreaReport = null;
    }

    private IUAVariable AbiltaGestione, AbilitaCreazReport, DataUltimoSalvataggio, UtenteAttuale;
    private DelayedTask ActionCreaReport;
    private PeriodicTask ActionCheckCambioGiornoAndAgiornaDati;
    private IUAObject Impostazioni;
    private IUANode CntProduzPlc;
    private Store myStore;
    private bool CreaCsvReport;
    private DateTime GiornoRicerca;
}
