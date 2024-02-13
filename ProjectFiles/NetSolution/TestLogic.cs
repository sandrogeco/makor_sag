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
using System.Linq;
using System.Collections.Generic;
#endregion

public class TestLogic : BaseNetLogic
{
    [ExportMethod]
    public void Method1()
    {
        var myVariables = new List<RemoteChildVariable>();

        // lettura parametri di tipo scalare
        var NodoParStep = Owner.Get<IUAObject>("Struct1");
        foreach (var Par in NodoParStep.GetNodesByType<IUAVariable>())
        { myVariables.Add(new RemoteChildVariable(Par.BrowseName)); }

        // lettura parametri di tipo object
        foreach (var Par in NodoParStep.GetNodesByType<IUAObject>())
        {
            var ScalarVar = Par.Children.Get<IUAVariable>("ScalarVariable");
            myVariables.Add(new RemoteChildVariable(Par.BrowseName + "/" + ScalarVar.BrowseName));
        }

        var reads = NodoParStep.ChildrenRemoteRead(myVariables);
        //int Len = GetScalarVarAmount(0, Owner);
        //var Dettagli = new object[Len, 4];
        //int i = 0;

        //AggiungiDettagliPadre(1, "", Owner, ref Dettagli, ref i);          //Aggiungo i dati generici
    }

    //private static int GetLength(int Len, IUANode Struttura)
    //{
    //    Len += (from ScalarTag in Struttura.GetNodesByType<IUAVariable>() select ScalarTag).Count();      //Conteggio dei parametri generali della ricetta
    //    foreach (var Struttura1 in Struttura.GetNodesByType<IUAObject>().Where(Struttura1 => Struttura1.GetType().Equals(typeof(UAObject))))  //filtro solo gli obejct type     
    //        Len = GetLength(Len, Struttura1);

    //    return Len;
    //}


    ///*** Creo la funzione ricorsivo locale per fare l'aggiornamento dei dati
    /// Legge (TUTTE) le variabili di campo che si trovano sotto un determinato nodo passato come NodoPadre. Dopo la lettura va a popolare l'array dei dettagli per poterlo salvare nel database
    /// </summary>
    /// <param name="NodoPadre">Nodo padre sotto il quale si trovano le variabili da inseire nel database</param>
    /// <param name="mID_Ric">Indice ricetta</param>
    /// <param name="Gruppo">Nome del raggruppantento dove si trova la variabile. Es. 'Zona1.Q1_Ftt.Usr..' </param>
    /// <param name="arrDettagli">Riferimento all'array dei dettagli</param>
    /// <param name="j">Riferimento all'indice dell'array dei dettagli</param>
    void AggiungiDettagliPadre(int mID_Ric, string Gruppo, IUANode NodoPadre, ref object[,] arrDettagli, ref int j)
    {
        try
        {
            //var myVariables = NodoPadre.GetNodesByType<IUAVariable>().Select(Par => new RemoteChildVariable(Par.BrowseName)).ToList();
            //var reads = NodoPadre.ChildrenRemoteRead(myVariables);
            //foreach (var Par in reads)
            foreach(IUAVariable read in NodoPadre.GetNodesByType<IUAVariable>())
            {
                arrDettagli[j, 0] = mID_Ric;
                arrDettagli[j, 1] = Gruppo;
                //arrDettagli[j, 2] = Par.RelativePath;
                //arrDettagli[j, 3] = Par.Value.Value.ToString();       // per leggere i valori delle var di campo va usata la Remoteread()                
                j++;
                Log.Info("Tag " + Gruppo +"."+ read.BrowseName);
            }

            foreach (var Struct1 in NodoPadre.GetNodesByType<IUAObject>().Where(Struct1 => Struct1.GetType().Equals(typeof(UAObject))))
            {
                Gruppo = Gruppo == "" ? Struct1.BrowseName : Gruppo + "." + Struct1.BrowseName;                
                AggiungiDettagliPadre(mID_Ric, Gruppo, Struct1, ref arrDettagli, ref j);
            }
        }
        catch (Exception ex)
        {
            Log.Error("Savlvataggio ricetta", "Errore salvataggio " + NodoPadre.BrowseName + ": " + ex.ToString());
            throw;
        }
    }

    public static int GetScalarVarAmount(int Len, IUANode Struttura)
    {
        Len += (from ScalarTag in Struttura.GetNodesByType<IUAVariable>() select ScalarTag).Count();      //Conteggio dei parametri generali della ricetta
        foreach (var Struttura1 in Struttura.GetNodesByType<IUAObject>().Where(Struttura1 => Struttura1.GetType().Equals(typeof(UAObject))))  //filtro solo gli obejct type     
            Len = GetScalarVarAmount(Len, Struttura1);

        return Len;
    }




}
 
