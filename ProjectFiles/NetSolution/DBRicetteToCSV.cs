#region Using directives
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Store;
using System;
using System.IO;
using UAManagedCore;
using FTOptix.System;
using FTOptix.S7TiaProfinet;
using FTOptix.S7TCP;



#endregion

public class DBRicetteToCSV : BaseNetLogic
{

	/// <summary>
	/// Script che esporta/importa ricette su/da un file cvs
	///	viene salvato: NomeRic, Desrcizione per la tabella Ricette/RicettePiastra
	/// viene salvato: PercorsoTag, Valore per la tabella RicetteDettagli/RicettePiastraDettagli
	/// Nel caso di importazione di ricette gia esistenti le sovrascrive con i nuovi valori
	/// </summary>
	[ExportMethod]
	public void ToCSV()
	{
		Store MyStore = InformationModel.Get<Store>(LogicObject.GetVariable("DBToExport").Value);
		String path = new ResourceUri(LogicObject.GetVariable("CSVpath").Value).Uri;
		String tableName = LogicObject.GetVariable("tableName").Value;
		String tableNameDetails = tableName + "Dettagli";


		if (!path.Contains(".csv"))
		{
			path += ".csv";
		}

		String query = $"SELECT Nome, Descrizione, PercorsoTag, Valore FROM {tableName} INNER JOIN {tableNameDetails} ON {tableName}.ID_Ric = {tableNameDetails}.ID_Ric";
		String intestazioneColonne = "Name\tDescription\tTagPath\tValue";

		MyStore.Query(query, out string[] header, out object[,] resultSet);

		using (StreamWriter sw = File.CreateText(path)) //Scrivo nel file CSV i dati presi dal DB
		{
			sw.WriteLine(intestazioneColonne);  //Assegno l'intestazione alle colonne

			for (int i = 0; i < resultSet.GetLength(0); i++)
			{
				//Log.Info($"Scrittura linea: {i}");
				for (int j = 0; j < resultSet.GetLength(1); j++)
				{
					if (j < resultSet.GetLength(1) - 1)
					{
						sw.Write($"{resultSet[i, j]}\t");
					}
					else
					{
						sw.Write($"{resultSet[i, j]}");
					}
				}
				sw.WriteLine("");
			}
		}
	}
    /// <summary>
    /// Script che esporta/importa ricette su/da un file cvs
    ///	viene salvato: NomeRic, Desrcizione per la tabella Ricette/RicettePiastra
    /// viene salvato: PercorsoTag, Valore per la tabella RicetteDettagli/RicettePiastraDettagli
    /// Nel caso di importazione di ricette gia esistenti le sovrascrive con i nuovi valori
    /// </summary>
    [ExportMethod]
	public void FromCSV()
	{
		Store MyStore = InformationModel.Get<Store>(LogicObject.GetVariable("DBToExport").Value);
		String path = new ResourceUri(LogicObject.GetVariable("CSVpath").Value).Uri;
		String tableName = LogicObject.GetVariable("tableName").Value;
		String tableNameDetails = tableName + "Dettagli";

		//MyStore.Query("DELETE FROM " + tableName, out string[] a, out object[,] a1);
		//MyStore.Query("DELETE FROM " + tableNameDetails, out string[] b, out object[,] b1);

		String[] lines = File.ReadAllLines(path);
		String[,] fileContent = new String[lines.GetLength(0), lines[0].Split('\t').GetLength(0)];      //Creo l'array con le dimensioni giuste dove verrà salvato il contenuto del file


		for (int i = 0; i < fileContent.GetLength(0); i++)                          //Ciclo che scorre tutte le linee del csv
		{                                                                           //salvando tutto dentro a l'array bidimensionale fileContent
			for (int j = 0; j < fileContent.GetLength(1); j++)
				if (lines[i].Split('\t').GetLength(0) > 0)                          //Controllo che la riga non sia vuota
					fileContent[i, j] = lines[i].Split('\t')[j];
		}

		string[] ColonneRic = { "Nome", "Descrizione", "DataOraCreazione" };
		string[] ColonneRicDettagli = { "ID_Ric", "PercorsoTag", "Valore" };
		var tempRic = new String[1, 3];
		var tempRicDettagli = new String[fileContent.GetLength(0) - 1, 3];

		var tempName = "";
		String idRic = "";


		//	Scorro tutto il contenuto del file, quando trovo un nome(ricetta) diverso (anche all'inizio visto che la variabile tempName è vuota)
		//	Controllo se esiste nel database, se si lo sovrascrivo, altrimenti lo creo, salvandomi l'ID_Ric in entrambi i casi
		//	Durante tutto il ciclo mi salvo nell'array bidimensionale i record per la tabella dei dettagli con l'ID_Ric giusto
		//	E alla fine li inserisco tutti insieme con una sola query nel database


		for (int i = 1; i < fileContent.GetLength(0); i++)   //Salto la prima riga perche è l'instestazione
		{
			if (fileContent[i, 0] != tempName)
			{
				MyStore.Query($"SELECT ID_Ric FROM {tableName} WHERE Nome = '{fileContent[i, 0]}'", out string[] header1, out object[,] resultSet1);
				if (resultSet1.GetLength(0) < 1)  //La ricetta non esiste
				{
					tempRic[0, 0] = fileContent[i, 0];
					tempRic[0, 1] = fileContent[i, 1];
					tempRic[0, 2] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
					MyStore.Insert(tableName, ColonneRic, tempRic);
					tempName = tempRic[0, 0];
					MyStore.Query($"SELECT ID_Ric FROM {tableName} WHERE Nome = '{tempName}'", out string[] header2, out object[,] resultSet2);
					idRic = resultSet2[0, 0].ToString();
				}
				else   //La ricetta è gia presente sul DB
				{
					idRic = resultSet1[0, 0].ToString();
					var date = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
					String query = $"UPDATE {tableName} SET Nome = '{fileContent[i, 0]}', Descrizione = '{fileContent[i, 1]}', DataOraCreazione = '{date}' WHERE ID_Ric = {idRic}";
					MyStore.Query(query, out string[] c, out object[,] c1);
					tempName = fileContent[i, 0];
					MyStore.Query($"DELETE FROM {tableNameDetails} WHERE ID_Ric = {idRic}", out string[] d, out object[,] d1);
				}
			}
			tempRicDettagli[i - 1, 0] = idRic;
			tempRicDettagli[i - 1, 1] = fileContent[i, 2];
			tempRicDettagli[i - 1, 2] = fileContent[i, 3];
		}

		MyStore.Insert(tableNameDetails, ColonneRicDettagli, tempRicDettagli);
	}
}
