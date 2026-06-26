#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Alarm;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.WebUI;
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
using FTOptix.S7TiaProfinet;
using FTOptix.S7TCP;
using static System.Net.Mime.MediaTypeNames;

#endregion

public class Creazione_Ottiche : BaseNetLogic
{

	/// <summary>
	/// Metodo che crea n rettangoli per rappresentare lo stato di ogni ottica presente nella barriera
	///	n viene ottenuto in base alle dimensione dell'array (di byte) sul plc
    /// Quando un bit va off il rettangolo collegato ad esso diventa invisibile per far notare il problema sull'ottica in questione
	/// </summary>
    [ExportMethod]
    public void CreaRett()
    {
        var offset1stRect = (int)LogicObject.GetVariable("Offset1stRect").Value.Value;     //offset d'inizio del primo rettangolo
        var rectWidth  = (int)LogicObject.GetVariable("LarghRect").Value.Value;            //larghezza rettangolo
        var rectHeight = (int)LogicObject.GetVariable("AltezRect").Value.Value;            //altezza rettangolo

        var tagPlcOttiche = Owner.Context.GetVariable(LogicObject.GetVariable("TagStatoOttiche").Value);    //tag plc
        var imgStatoOttiche = LogicObject.Owner.FindObject("StatoOttiche");                                 //immagine barriera "padre" dei rect da creare
        CreaRett_OttMas(tagPlcOttiche, imgStatoOttiche, offset1stRect, rectWidth, rectHeight, false);

        var tagPlcMaschera = Owner.Context.GetVariable(LogicObject.GetVariable("TagStatoMaschera").Value);  //tag plc
        var imgStatoMaschera = LogicObject.Owner.FindObject("StatoMaschera");                               //immagine barriera "padre" dei rect da creare
        CreaRett_OttMas(tagPlcMaschera, imgStatoMaschera, offset1stRect, rectWidth, rectHeight, true);
    }

    /// <summary>
    ///     Funzione che crea i rettangoli con i parametri dati
    /// </summary>
    /// <param name="tagPlc"></param>
    /// <param name="img"></param>
    /// <param name="offset1stRect"></param>
    /// <param name="rectWidth"></param>
    /// <param name="rectHeight"></param>
	public void CreaRett_OttMas(IUAVariable tagPlc, IUAObject img, int offset1stRect, int rectWidth, int rectHeight, bool ott_mas)
	{
        byte[] arrPlc = tagPlc.Value;

        //Per ogni bit nell'array di byte creo un rettangolo assegnandoli le varie proprieta e il bit sulla visibilità
        int cnt = 0;
        for (uint i = 0; i < arrPlc.Length; i++)    //Scorro l'array di byte   
        {
            for (uint ibit = 0; ibit < 8; ibit++)   //Scorro il byte pendendo i singoli bit
            {
                if (i == arrPlc.Length-1 & ibit == 7)   //Salto l'ultimo bit dell'ultimo elemento
                    break;

                var rect = InformationModel.Make<Rectangle>("Rect" + cnt); //Creo l'istanza assegnandoli come nome "Rect0","Rect1", ...

                //Setto le varie proprieta
                rect.Height = rectHeight;
                rect.Width = rectWidth;
                rect.VerticalAlignment = VerticalAlignment.Center;
                rect.FillColor = new Color(0xff, 0, 255, 25);
                rect.LeftMargin = offset1stRect + rectWidth * cnt;  //Il margine sinistro è = margine iniziale + larghezza rect * numero di rect attuale

                //creo il collegamento dinamico assegnando alla visibilita del rect il bit attuale
                rect.VisibleVariable.SetDynamicLink(tagPlc, i, DynamicLinkMode.Read);
                if (ott_mas)
                    rect.VisibleVariable.GetVariable("DynamicLink").Value = rect.VisibleVariable.GetVariable("DynamicLink").Value + "[" + i + "]." + (7 - ibit);
                else
                    rect.VisibleVariable.GetVariable("DynamicLink").Value = rect.VisibleVariable.GetVariable("DynamicLink").Value + "[" + i + "]." + ibit;

                img.Add(rect);  //Aggiungo all'immagine padre il nuovo figlio "Rect..."
                cnt++;
            }
        }
    }
}
