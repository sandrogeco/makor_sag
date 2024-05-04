#region Using directives
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using UAManagedCore;
using FTOptix.Recipe;
using OpcUa = UAManagedCore.OpcUa;
#endregion

public class GestRicette : BaseNetLogic
{
    private readonly string mTblRicetta;
    private readonly string mTblDettagli;
    private readonly string mTipoRicetta;
    private readonly Store MyStore;
    private readonly IUANode NodeTagRicetta;  /// NodeId da dove pescare le variabili ricetta 
    private readonly string ID_RicColumnName;  //Nome della colonna ID_Ric. In SqlLite l'indice PRIMARY KEY si chiama ROWID

    #region "Costruttori"
    public GestRicette() { }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="TblRicetta">Tabella con le informazione generali della ricetta</param>
    /// <param name="TblDettagli">Tabella con i dati da salvare</param>
    /// <param name="tipoRicetta">Tipologia di ricetta.Esempio RicettaMacchina, ricetta ventilazione, RicettaLinea</param>
    /// <param name = "DataStoreNodeId" > NodeId da dove pescare le variabili ricetta</param>
    /// <param name="VarRicettaNodeId">NodeId da dove pescare le variabili ricetta</param>
    public GestRicette(string TblRicetta, string TblDettagli, string tipoRicetta, NodeId DataStoreNodeId, NodeId VarRicettaNodeId)
    {
        mTblRicetta = TblRicetta;
        mTblDettagli = TblDettagli;
        mTipoRicetta = tipoRicetta;
        MyStore = InformationModel.Get<Store>(DataStoreNodeId);
        NodeTagRicetta = InformationModel.Get(VarRicettaNodeId);
        ID_RicColumnName = "ID_Ric";
    }
    #endregion

    /// <summary>
    /// Controlla se la ricetta in ingresso è presente nel DB
    /// </summary>
    /// <param name="NomeRicerca"></param>
    /// <returns>True = Ricetta presente</returns> 
    public bool IsRecipePresent(string NomeRicerca)
    {
        string myQuery = "SELECT * FROM " + mTblRicetta + " WHERE Nome ='" + NomeRicerca + "'";
        MyStore.Query(myQuery, out _, out object[,] resultSet);

        return (resultSet.Rank == 2) && resultSet.GetLength(0) == 1;   //se l'exp a sx è falsa esce subito altrimenti valuta acnhe quella a destra
    }

    /// <summary>
    /// Cancella dal DB la ricetta passata come ingresso
    /// </summary>
    /// <param name="NomeRic"></param>
    public void CancRic(string NomeRic)
    {
        // Elimino i dettagli ricetta
        MyStore.Query("DELETE FROM " + mTblDettagli + " WHERE ID_Ric = " + GetIdRic(NomeRic), out _, out _);

        // Elimino la ricetta
        MyStore.Query("DELETE FROM " + mTblRicetta + " WHERE Nome='" + NomeRic + "'", out _, out _);
    }

    public void CreaRic(string NomeRic, string Descriz)
    {
        string[] ColonneRic = { "Nome", "Descrizione", "TipoRicetta", "DataOraCreazione" };  // creo la stringa per aggiungere la ricetta nella tabella delle Ricette                                                                                                                    
        var ValoriRic = new object[1, 4];

        ValoriRic[0, 0] = NomeRic;
        ValoriRic[0, 1] = Descriz;
        ValoriRic[0, 2] = mTipoRicetta;
        ValoriRic[0, 3] = DateTime.Now;

        MyStore.Insert(mTblRicetta, ColonneRic, ValoriRic);    // aggiornamento database

        _ = SalvaDettagli(NomeRic);               // aggiunta dettagli ricetta  
    }

    public void SalvaRic(string NomeRic, string Descriz)
    {
        // creo la stringa per aggiornare la tabella ricette
        string query = "UPDATE " + mTblRicetta + " SET Nome ='" + NomeRic + "', Descrizione ='" + Descriz + "', DataOraModifica='" + DateTime.Now.ToString("s") + "' WHERE Nome='" + NomeRic + "'";
        MyStore.Query(query, out _, out object[,] resultSet);

        if (resultSet.Rank != 2)
            throw new Exception();

        // cancella gli elementi nella tabella detagli per poi inserirli nuovamente con una insert
        MyStore.Query("DELETE FROM " + mTblDettagli + " WHERE ID_Ric = " + GetIdRic(NomeRic), out _, out resultSet);

        // se query non andata a buon fine
        if (resultSet.Rank != 2)
            throw new Exception();

        _ = SalvaDettagli(NomeRic);                          // aggiunta dettagli ricetta            
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ID_RicDaCopiare"></param>
    /// <param name="NomeRic"></param>
    /// <param name="Descriz"></param>
    public void ClonaRicetta(long ID_RicDaCopiare, string NomeRic, string Descriz)
    {
        if (GetIdRic(NomeRic) > 0)      //controllo se la ricetta è già presente nel database se si allora esco senza fare e genero l'eccezione che può essere catturata esternamente
            throw new Exception();

        string[] ColonneRic = { "Nome", "Descrizione", "TipoRicetta", "DataOraCreazione" };  // creo la stringa per aggiungere la ricetta nella tabella delle Ricette                                                                                                                    
        var ValoriRic = new object[1, 4];

        ValoriRic[0, 0] = NomeRic;
        ValoriRic[0, 1] = Descriz;
        ValoriRic[0, 2] = mTipoRicetta;
        ValoriRic[0, 3] = DateTime.Now;

        MyStore.Insert(mTblRicetta, ColonneRic, ValoriRic);    // aggiornamento database        

        //Per fare la copia dei dati leggo semplicemente la tabella dettagli e sostituisco l'id ricetta vecchio con quello nuovo, successivamente eseguo una insert nella tabella.
        string query = $"SELECT ID_Ric, PercorsoTag, Valore FROM {mTblDettagli} WHERE ID_Ric = {ID_RicDaCopiare}";
        MyStore.Query(query, out string[] header, out object[,] resultSet);

        if (resultSet.Rank != 2 || resultSet.GetLength(0) == 0)
            throw new Exception();

        var ID_Ric = GetIdRic(NomeRic);
        for (int i = 0; i < resultSet.GetLength(0); i++)
        {
            resultSet[i, Array.IndexOf(header, "ID_Ric")] = ID_Ric;
        }

        string[] ColonneRicDettagli = { "ID_Ric", "PercorsoTag", "Valore" };

        MyStore.Insert(mTblDettagli, ColonneRicDettagli, resultSet);    // aggiornamento database
    }


    public bool ApriRic(string NomeRic)
    {
        var ID_Ric = GetIdRic(NomeRic);
        if (ID_Ric == 0)
            return false;

        int CntParScritti = 0;

        //Apertura parametri generici
        string myQuery = "SELECT * FROM " + mTblDettagli + " WHERE ID_Ric = " + ID_Ric;     //lettura parametri generici
        ApriDettaglio(myQuery, NodeTagRicetta);

        Log.Info("Numero parametri scritti:" + CntParScritti);
        return true;

        ///* Creo la funzione locale per aprire i dettagli
        /// <summary>
        /// 
        /// </summary>
        /// <param name="TagNode">Nodo padre sotto il quale si trovano le variabili da aggiornare</param>
        void ApriDettaglio(string Query, IUANode TagNode)
        {
            MyStore.Query(Query, out string[] header, out object[,] resultSet);

            //se query non andata a buon fine
            if (resultSet.Rank != 2)
                throw new Exception();

            int IndexTag = Array.IndexOf(header, "PercorsoTag");
            int IndexValore = Array.IndexOf(header, "Valore");

            try
            {
                var ListParam = new List<RemoteChildVariableValue>();
                int i = 0;
                while (i < resultSet.GetLength(0))
                {
                    var figlio = TagNode.GetVariable(resultSet[i, IndexTag].ToString());

                    if (figlio is null)
                    {
                        i++;
                        continue;
                    }

                    if (figlio.DataType.Equals(typeof(bool[])))
                    {
                        IUAVariable myVar = TagNode.GetVariable(resultSet[i, IndexTag].ToString());
                        bool[] newArr = myVar.Value;

                        myVar.Value = new UAValue(Runtime_Utility.GetBoolArrFromString(Convert.ToInt32(resultSet[i, IndexValore]), newArr.Length));
                    }
                    else if (figlio.DataType.Equals(OpcUa.DataTypes.Boolean))
                    {
                        ListParam.Add(new RemoteChildVariableValue(resultSet[i, IndexTag].ToString(), Convert.ToBoolean(resultSet[i, IndexValore])));
                    }
                    else if (figlio.DataType.Equals(OpcUa.DataTypes.Int16))
                    {
                        ListParam.Add(new RemoteChildVariableValue(resultSet[i, IndexTag].ToString(), Convert.ToInt16(resultSet[i, IndexValore])));
                    }
                    else if (figlio.DataType.Equals(OpcUa.DataTypes.Int32))
                    {
                        ListParam.Add(new RemoteChildVariableValue(resultSet[i, IndexTag].ToString(), Convert.ToInt32(resultSet[i, IndexValore])));
                    }
                    else if (figlio.DataType.Equals(OpcUa.DataTypes.Float))
                    {
                        ListParam.Add(new RemoteChildVariableValue(resultSet[i, IndexTag].ToString(), Convert.ToSingle(resultSet[i, IndexValore])));
                    }
                    else if (figlio.DataType.Equals(OpcUa.DataTypes.String))
                    {
                        ListParam.Add(new RemoteChildVariableValue(resultSet[i, IndexTag].ToString(), resultSet[i, IndexValore].ToString()));
                    }

                    i++;
                }

                TagNode.ChildrenRemoteWrite(ListParam);     //scrivo tutti i parametri in un colpo solo
                CntParScritti += i;
            }
            catch (Exception ex)
            {
                Log.Error("Apertura ricetta", "Errore apertura " + TagNode.BrowseName + ": " + ex.ToString());
                throw;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="NomeRic">Nome Ricetta</param>
    private bool SalvaDettagli(string NomeRic)
    {
        var ID_Ric = GetIdRic(NomeRic);

        // creo la stringa per aggiungere la ricetta nella tabella dei dettagli
        string[] ColonneRicDettagli = { "ID_Ric", "PercorsoTag", "Valore" };

        object[,] Dettagli = AggiungiDettagli(ID_Ric, NodeTagRicetta);

        MyStore.Insert(mTblDettagli, ColonneRicDettagli, Dettagli);              // aggiornamento database   

        Log.Info($"Salvataggio ricetta {NomeRic}: Parametri salvati = {Dettagli.GetLength(0)}");

        return true;
    }

    private object[,] AggiungiDettagli(long mID_Ric, IUANode NodoPadre)
    {
        try
        {
            string Path = "";
            var MyVariables = new List<RemoteChildVariable>();
            CreaListaTags(NodoPadre, ref Path, ref MyVariables);   //Crea la lista delle tags per poter leggere le tags in un unico colpo

            int Len = MyVariables.Count;
            object[,] arrDettagli = new object[Len, 3];
            int j = 0;

            var reads = NodoPadre.ChildrenRemoteRead(MyVariables);  // Leggo la lista delle tags e ottengo i valori
            foreach (var Par in reads)
            {
                arrDettagli[j, 0] = mID_Ric;
                arrDettagli[j, 1] = Par.RelativePath;
                arrDettagli[j, 2] = Par.Value.Value.ToString();

                j++;
            }

            return arrDettagli;  // conversione in quanto si necessita di una matrice
        }
        catch (Exception ex)
        {
            Log.Error("ChildrenRemoteWrite failed: " + ex.ToString());
            throw;
        }


        //Aggiorna la lista con le RemoteChildeVariables
        void CreaListaTags(IUANode Nodo, ref string PercorsoTag, ref List<RemoteChildVariable> MyVar)
        {
            string path = PercorsoTag == "" ? "": PercorsoTag + "/";

            MyVar.AddRange(Nodo.GetNodesByType<IUAVariable>().Select(Par => new RemoteChildVariable(path + Par.BrowseName)).ToList());     // popola la lista delle variabili di ricetta (tutte le variabili del singolo nodo)

            foreach (var SubStruct in Nodo.GetNodesByType<IUAObject>().Where(Struct => Struct.GetType().Equals(typeof(UAObject))))    // Qui avviene la ricorsione ogni volta che un nodo ha dei nodi figli
            {
                string SubGruppo = PercorsoTag == "" ? SubStruct.BrowseName : PercorsoTag + "/" + SubStruct.BrowseName;
                CreaListaTags(SubStruct, ref SubGruppo, ref MyVar);
            }
        }
    }

    public long GetIdRic(string NomeRic)
    {
        MyStore.Query($"SELECT {ID_RicColumnName} FROM {mTblRicetta} WHERE Nome ='{NomeRic}'", out _, out object[,] resultSet);

        //se query non andata a buon fine
        if (resultSet.Rank != 2)
            return 0;

        return resultSet.GetLength(0) > 0 ? Convert.ToInt64(resultSet[0, 0]) : 0;
    }

    public static int GetScalarVarAmount(int Len, IUANode Struttura)
    {
        Len += (from ScalarTag in Struttura.GetNodesByType<IUAVariable>() select ScalarTag).Count();      //Conteggio dei parametri generali della ricetta
        foreach (var Struttura1 in Struttura.GetNodesByType<IUAObject>().Where(Struttura1 => Struttura1.GetType().Equals(typeof(UAObject))))  //filtro solo gli obejct type     
            Len = GetScalarVarAmount(Len, Struttura1);

        return Len;
    }
}
