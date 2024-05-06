#region Using directives
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using System;
using System.IO;
using UAManagedCore;

#endregion

public class CreaFileCSV_DaTagCommDrivers : BaseNetLogic
{
	[ExportMethod]
	public void CreaFileCSV()
	{
		String pathCSV = new ResourceUri(LogicObject.GetVariable("CSVpath").Value).Uri;
		String nomeMacchina = new ResourceUri(LogicObject.GetVariable("nomeMacchina").Value).Uri;

		var Stazione = Project.Current.Get("CommDrivers/CODESYSDriver1/PLC_Next/Tags/Next");
		using (StreamWriter sw = File.CreateText(pathCSV)) //Scrivo nel file CSV i dati
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

	private void AlarmFinder(TagStructure TblVarAlmNode, string nomeMacchina, string NomeTabVarAlm, StreamWriter sw)
	{
		foreach (var Alm in TblVarAlmNode.GetNodesByType<IUAVariable>())
		{

			string Notifica = "";
			var NomeAlm = Alm.BrowseName;
			if (NomeAlm.Length >= 4)
				Notifica = NomeAlm.Substring(0, 4);
			bool NotificaTrovata;
			ushort Severità;

			switch (Notifica)
			{
				case "bAlm":
					NotificaTrovata = true;
					Severità = 1000;
					break;

				case "bWrn":
					NotificaTrovata = true;
					Severità = 500;
					break;

				case "bMsg":
					NotificaTrovata = true;
					Severità = 1;
					break;

				default:
					NotificaTrovata = false;
					Severità = 0;
					break;
			}

			if (!NotificaTrovata)
			{
				continue;
			}

			var pathAlm = MakeBrowsePath(Alm.Owner);
			String[] nomeCartella = pathAlm.Split($"/Tags/{nomeMacchina}/");
			string[] str2 = nomeCartella[1].Split('/');
			string translationkey;
			if (str2.Length > 1)
				translationkey = $"{str2[1]}.{NomeAlm}";
			else
				translationkey = NomeAlm;

			if (Alm.ValueRank.ToString() == "Scalar")           //Mi chiedo se è una variabile scalare, se no allora è un array
			{
				//Log.Info("Scalar");
			}
			else
			{
				//Log.Info("Array");
				GestArrTag(Alm, nomeMacchina, NomeTabVarAlm, sw, pathAlm, NomeAlm, Severità);
				continue;
			}

			sw.Write($"{pathAlm}\t");
			sw.Write($"{NomeAlm}\t");
			sw.Write("\t");
			sw.Write($"{nomeMacchina}\t");
			sw.Write($"{NomeTabVarAlm}\t");
			if (str2.Length > 1)
				sw.Write($"{str2[1]}\t");
			else
				sw.Write($"\t");
			sw.Write($"{Severità}\t");
			LocalizedText alarmKey = new LocalizedText(LogicObject.NodeId.NamespaceIndex, translationkey);
			//Log.Info(translationkey+":  "+InformationModel.LookupTranslation(alarmKey).Text);
			sw.Write($"{translationkey}\t");
			sw.Write($"{InformationModel.LookupTranslation(alarmKey).Text}\t");
			sw.WriteLine("");

		}
		foreach (var structAlm in TblVarAlmNode.GetNodesByType<TagStructure>())
		{
			AlarmFinder(structAlm, nomeMacchina, NomeTabVarAlm, sw);
		}
	}

	private void GestArrTag(IUAVariable Alm, string nomeMacchina, string NomeTabVarAlm, StreamWriter sw, string pathAlm, string NomeAlm, ushort Severità)
	{
		String[] nomeCartella = pathAlm.Split($"/Tags/{nomeMacchina}/");
		string[] str2 = nomeCartella[1].Split('/');
		string translationkey;
		bool[] arrPlc = Alm.Value;

		for (int i = 0; i < arrPlc.Length; i++)
		{
			if (str2.Length > 1)
				translationkey = $"{str2[1]}.{NomeAlm}[{i}]";
			else
				translationkey = NomeAlm + "[" + i + "]";

			sw.Write($"{pathAlm}\t");
			sw.Write($"{NomeAlm}\t");
			sw.Write($"{i}\t");
			sw.Write($"{nomeMacchina}\t");
			sw.Write($"{NomeTabVarAlm}\t");
			if (str2.Length > 1)
				sw.Write($"{str2[1]}\t");
			else
				sw.Write($"\t");
			sw.Write($"{Severità}\t");
			LocalizedText alarmKey = new LocalizedText(LogicObject.NodeId.NamespaceIndex, translationkey);
			//Log.Info(translationkey+":  "+InformationModel.LookupTranslation(alarmKey).Text);
			sw.Write($"{translationkey}\t");
			sw.Write($"{InformationModel.LookupTranslation(alarmKey).Text}\t");
			sw.WriteLine("");
		}
	}
}