#region Using directives
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using System;
using System.IO;
using System.Runtime.ConstrainedExecution;
using UAManagedCore;
using FTOptix.System;
using FTOptix.S7TiaProfinet;
#endregion

public class CreaFileCSV_DaTagCommDrivers : BaseNetLogic
{

	/// <summary>
	///		Script che crea il file csv per la creazione degli allarmi leggendo i tag dal plc
	///		Rileva tutti i tag che iniziano per bAlm, bWnr, bMsg
	///		anche se sono dentro a delle strutture o se fossero degli array di allarmi
	/// </summary>
	[ExportMethod]
	public void CreaFileCSV()
	{
		string pathCSV = new ResourceUri(LogicObject.GetVariable("CSVpath").Value).Uri;
		string nomeMacchina = new ResourceUri(LogicObject.GetVariable("nomeMacchina").Value).Uri;

		var Stazione = Project.Current.Get($"CommDrivers/CODESYSDriver1/PLC_Next/Tags/{nomeMacchina}");
		using (StreamWriter sw = File.CreateText(pathCSV)) //Apro lo stream per scrivere nel file CSV i dati
		{
			sw.WriteLine("PercorsoNodoPadre\tNomeAllarme (NomeTagPLC)\tArrayIndex (Optional)\tCategoria (Es. nome macchina)\tCartellaAlmDigitali\tPrefixAllarme(Optional, Es. DB)\tGravità allarme (1= msg, 500= wrn, 1000= alm)\tChiave(TextId nella tabella Traduz.)\tit-IT");  //Assegno l'intestazione alle colonne
			foreach (var TblVarAlmNode in Stazione.GetNodesByType<TagStructure>())
			{
				if (TblVarAlmNode is null)
				{
					continue;
				}
				string NomeTabVarAlm = TblVarAlmNode.BrowseName;
				AlarmFinder(TblVarAlmNode, nomeMacchina, NomeTabVarAlm, sw);
			}
		}
		Log.Info("File creato correttamente in: " + pathCSV);
	}


	/// <summary>
	/// 	Funzione che dato un nodo mi restituisce il percorso
	/// </summary>
	/// <param name="node"></param>
	/// <returns></returns>
	private static string MakeBrowsePath(IUANode node)
	{
		string path = node.BrowseName;
		var current = node.Owner;

		while (current != Project.Current)
		{
			path = current.BrowseName + "/" + path;
			current = current.Owner;
		}
		return path;
	}


	/// <summary>
	///		Funzione ricorsiva che cerca gli allarmi nei tag importati	
	///		Rileva quelli che iniziano con bAlm, bWnr, bMsg 
	/// </summary>
	/// <param name="TblVarAlmNode"></param>
	/// <param name="nomeMacchina"></param>
	/// <param name="NomeTabVarAlm"></param>
	/// <param name="sw"></param>
	private void AlarmFinder(TagStructure TblVarAlmNode, string nomeMacchina, string NomeTabVarAlm, StreamWriter sw)
	{
		foreach (var Alm in TblVarAlmNode.GetNodesByType<IUAVariable>())
		{

			string Notifica = "";
			var NomeAlm = Alm.BrowseName;
			if (NomeAlm.Length >= 4)
				Notifica = NomeAlm.Substring(0, 4);
			bool NotificaTrovata;
			ushort Severita;

			switch (Notifica)
			{
				case "bAlm":
					NotificaTrovata = true;
					Severita = 1000;
					break;

				case "bWrn":
					NotificaTrovata = true;
					Severita = 500;
					break;

				case "bMsg":
					NotificaTrovata = true;
					Severita = 1;
					break;

				default:
					NotificaTrovata = false;
					Severita = 0;
					break;
			}

			if (!NotificaTrovata)
			{
				continue;
			}

			var pathAlm = MakeBrowsePath(Alm.Owner);
			string[] nomeCartella = pathAlm.Split($"/Tags/{nomeMacchina}/");
			string[] str2 = nomeCartella[1].Split('/');
			string translationkey;

			if (Alm.ValueRank.ToString() == "Scalar")           //Mi chiedo se è una variabile scalare, se no allora è un array
			{
				//Log.Info("Scalar");
			}
			else
			{
				//Log.Info("Array");
				GestArrTag(Alm, nomeMacchina, NomeTabVarAlm, sw, pathAlm, NomeAlm, Severita);   //Nel caso fosse un array eseguo la funzione "GestArrTag" e vado al prossimo tag
				continue;
			}

			if (str2.Length > 1)                                                //Mi chiedo se è dentro una struttura  
				translationkey = $"{NomeTabVarAlm}_{str2[1]}_{NomeAlm}";        //se si allora la chiave per la raduzione sara "GvOsc_stAlm_nomeAlm"
			else                                                                //altrimenti solo "GvOsc_nomeAlm"
				translationkey = $"{NomeTabVarAlm}_{NomeAlm}";

			//Incomincio a scrivere
			sw.Write($"{pathAlm}\t");
			sw.Write($"{NomeAlm}\t");
			sw.Write("\t");
			sw.Write($"{nomeMacchina}\t");
			sw.Write($"{NomeTabVarAlm}\t");
			if (str2.Length > 1)                    //nel caso che l'alm sia dentro una struttura gli metto come prefisso il nome della struttura
				sw.Write($"{NomeTabVarAlm}_{str2[1]}\t");
			else
				sw.Write($"{NomeTabVarAlm}\t");
			sw.Write($"{Severita}\t");
			LocalizedText alarmKey = new LocalizedText(LogicObject.NodeId.NamespaceIndex, translationkey);
			//Log.Info(translationkey+":  "+InformationModel.LookupTranslation(alarmKey).Text);
			sw.Write($"{translationkey}\t");
			sw.Write($"{InformationModel.LookupTranslation(alarmKey).Text}\t");         //Cerco nelle traduzioni la chiave e scrivo la traduzione, se non esiste lascio vuoto
			sw.WriteLine("");

		}
		foreach (var structAlm in TblVarAlmNode.GetNodesByType<TagStructure>())
		{
			AlarmFinder(structAlm, nomeMacchina, NomeTabVarAlm, sw);                    //Rieseguo me stesso se ci fossero altre strutture sotto alla struttura attuale
		}                                                                               //per trovare altri alm dentro di esse
	}



	/// <summary>
	///		Funzione che mi scorre l'array di alm e mi scrive nel file una riga per ogni alm nell'array
	/// </summary>
	/// <param name="Alm"></param>
	/// <param name="nomeMacchina"></param>
	/// <param name="NomeTabVarAlm"></param>
	/// <param name="sw"></param>
	/// <param name="pathAlm"></param>
	/// <param name="NomeAlm"></param>
	/// <param name="Severita"></param>
	private void GestArrTag(IUAVariable Alm, string nomeMacchina, string NomeTabVarAlm, StreamWriter sw, string pathAlm, string NomeAlm, ushort Severita)
	{
		string[] nomeCartella = pathAlm.Split($"/Tags/{nomeMacchina}/");
		string[] str2 = nomeCartella[1].Split('/');
		string translationkey;
		bool[] arrPlc = Alm.Value;

		for (int i = 0; i < arrPlc.Length; i++)
		{
			if (str2.Length > 1)
				translationkey = $"{NomeTabVarAlm}_{str2[1]}_{NomeAlm}[{i}]";
			else
				translationkey = $"{NomeTabVarAlm}_{NomeAlm}[{i}]";

			sw.Write($"{pathAlm}\t");
			sw.Write($"{NomeAlm}\t");
			sw.Write($"{i}\t");
			sw.Write($"{nomeMacchina}\t");
			sw.Write($"{NomeTabVarAlm}\t");
			if (str2.Length > 1)
				sw.Write($"{NomeTabVarAlm}_{str2[1]}\t");
			else
				sw.Write($"{NomeTabVarAlm}\t");
			sw.Write($"{Severita}\t");
			LocalizedText alarmKey = new LocalizedText(LogicObject.NodeId.NamespaceIndex, translationkey);
			//Log.Info(translationkey+":  "+InformationModel.LookupTranslation(alarmKey).Text);
			sw.Write($"{translationkey}\t");
			sw.Write($"{InformationModel.LookupTranslation(alarmKey).Text}\t");
			sw.WriteLine("");
		}
	}
}
