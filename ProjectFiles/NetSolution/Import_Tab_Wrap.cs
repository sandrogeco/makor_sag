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
using FTOptix.OPCUAClient;
using FTOptix.Retentivity;
using FTOptix.EventLogger;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using System.IO;
using FTOptix.System;
using FTOptix.S7TiaProfinet;
using FTOptix.S7TCP;

#endregion

public class Import_Tab_Wrap : BaseNetLogic
{
	/// <summary>
	///		Importa le traduzioni da un file csv che è separato con i /t ed racchiuso tra "..."
	///		(es.   "..."/t"..."/t"..."/n )
	/// </summary>
	[ExportMethod]
	public void ImportTranslations()
	{
		var csvPath = GetCSVFilePath();

		if (string.IsNullOrEmpty(csvPath))
		{
			Log.Error("ImportAndExportTranslations", "No CSV file chosen, please fill the CSVPath variable");
			return;
		}

		var localizationDictionary = GetDictionary();
		if (localizationDictionary == null)
		{
			Log.Error("ImportAndExportTranslations", "No translation table found");
			return;
		}

		if (!File.Exists(csvPath))
		{
			Log.Error("ImportAndExportTranslations", $"The file {csvPath} does not exist");
			return;
		}

		try
		{
			using (var csvReader = new StreamReader(csvPath))
			{
				var fileLines = csvReader.ReadToEnd();							//leggo l'intero file e salvo tutto in un unica stringa

				string line1 = fileLines.Split("\r\n")[0];                      //salvo l'intestazione
				int nColonne = fileLines.Split("\r\n")[0].Split("\t").Length;	//trovo il numero colonne

				fileLines = fileLines.Remove(0, line1.Length + 2);				//tolgo l'intestazione +2 perche ci sono i caratteri '/n' e '/r'
				fileLines = fileLines.Replace("\t", "");						//tolgo tutti i tab, uso come separatore le due "..."

				string[] cells = fileLines.Split(new string[] { "\"", "\"" }, StringSplitOptions.RemoveEmptyEntries); //separo il tutto con "..."

				int nRow = 0;
				foreach (var cell in cells)			//mi calcolo il numero righe
					if (cell == "\r\n")
						nRow++;

				var importedTranslations = new string[nRow, nColonne];		//array che conterrà tutte le traduzioni

				importedTranslations[0, 0] = "";							//la prima cella è sempre vuota	
				int cnt = 1;
				foreach (string lingua in line1.Replace("\t", "").Split(new string[] { "\"", "\"" }, StringSplitOptions.RemoveEmptyEntries))
				{
					importedTranslations[0, cnt] = lingua;					//Scrivo l'instestazione salvando le lingue presenti nel file
					cnt++;
				}


				cnt = 0;										//cnt -> scorre tutte le celle lette dal file
				for (var r = 1; r < nRow; ++r)					//r -> riga delle traduzioni da importare
					for (var c = 0; c < nColonne; ++c)          //c -> colonna delle traduzioni da importare
						if (cells[cnt] == "\r\n")				//controllo se la linea sia finita
						{										
							if (c == 0)                         //se "\r\n" è sulla prima colonna allora salto la cella e ritorno sulla prima colonna
							{
								cnt++;
								c--;
								continue;
							}
							importedTranslations[r, c] = "";	//nel caso manchino le traduzioni allora le metto "" finchè non soddisfo la condizione sotto
							if (c == nColonne - 1)              //controllo se è l'ultima colonna, se si allora passo alla riga dopo saltando la cella contenente "\r\n"
							{
								cnt++;
							}
						}
						else									//se trovo un valore allora lo salvo e proseguo
						{
							importedTranslations[r, c] = cells[cnt];
							cnt++;
						}

				localizationDictionary.Value = new UAValue(importedTranslations);		//alla fine salvo tutto nelle traduzioni del progetto
			}

			Log.Info("ImportAndExportTranslations", $"Translations successfully imported into {localizationDictionary.BrowseName} localization dictionary");
		}
		catch (Exception ex)
		{
			Log.Error("ImportAndExportTranslations", $"Unable to import the translations: {ex}");
		}
	}

	private string GetCSVFilePath()
	{
		var csvPathVariable = LogicObject.GetVariable("CSVPath");
		if (csvPathVariable == null)
		{
			Log.Error("ImportAndExportTranslations", "CSVPath variable not found");
			return "";
		}

		return new ResourceUri(csvPathVariable.Value).Uri;
	}

	private IUAVariable GetDictionary()
	{
		var dictionaryVariable = LogicObject.GetVariable("LocalizationDictionary");
		if (dictionaryVariable == null)
		{
			Log.Info("ImportAndExportTranslations", "The first localization dictionary found will be used since the LocalizationDictionary variable cannot be not found");
			return GetDefaultDictionary();
		}

		NodeId nodeIdDictionaryValue = dictionaryVariable.Value;
		if (nodeIdDictionaryValue == null)
		{
			Log.Info("ImportAndExportTranslations", "The first localization dictionary found will be used since the LocalizationDictionary variable is not set");
			return GetDefaultDictionary();
		}

		var dictionaryNode = InformationModel.Get(nodeIdDictionaryValue);
		if (dictionaryNode == null)
		{
			Log.Error("ImportAndExportTranslations", "The node pointed by the LocalizationDictionary variable cannot be found");
			return null;
		}

		var resultDictionaryVariable = dictionaryNode as IUAVariable;
		if (resultDictionaryVariable == null || !resultDictionaryVariable.IsInstanceOf(FTOptix.Core.VariableTypes.LocalizationDictionary))
			Log.Error("ImportAndExportTranslations", "The node pointed by the LocalizationDictionary variable is not a localization dictionary");

		return resultDictionaryVariable;
	}

	private IUAVariable GetDefaultDictionary()
	{
		var localizationDictionaryType = Project.Current.Context.GetNode(FTOptix.Core.VariableTypes.LocalizationDictionary);
		var localizationDictionaries = localizationDictionaryType.InverseRefs.GetNodes(OpcUa.ReferenceTypes.HasTypeDefinition);

		foreach (var dictionaryNode in localizationDictionaries)
		{
			if (dictionaryNode.NodeId.NamespaceIndex == Project.Current.NodeId.NamespaceIndex)
				return (IUAVariable)dictionaryNode;
		}

		return null;
	}
}
