#region Using directives
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Store;
using System;
using System.IO;
using UAManagedCore;
using FTOptix.Recipe;

#endregion

public class DBRicetteToCSV : BaseNetLogic
{
	[ExportMethod]
	public void ToCSV()
	{
		NodeId DBToExport = LogicObject.GetVariable("DBToExport").Value;
		Store MyStore = InformationModel.Get<Store>(DBToExport);
		String path = new ResourceUri(LogicObject.GetVariable("CSVpath").Value).Uri;

		String query = "SELECT Nome, PercorsoTag, Valore FROM RicettePiastraDettagli INNER JOIN RicettePiastra ON RicettePiastra.ID_Ric = RicettePiastraDettagli.ID_Ric";
		String intestazioneColonne = "Nome\tPercorsoTag\tValore";

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
	[ExportMethod]
	public void FromCSV()
	{
		NodeId DBToExport = LogicObject.GetVariable("DBToExport").Value;
		Store MyStore = InformationModel.Get<Store>(DBToExport);
		String path = new ResourceUri(LogicObject.GetVariable("CSVpath").Value).Uri;

		String[] lines = File.ReadAllLines(path);
		String[,] fileContent = new String[lines.GetLength(0), lines[0].Split('\t').GetLength(0)];      //Creo l'array con le dimensioni giuste dove verrà salvato il contenuto del file


		for (int i = 0; i < fileContent.GetLength(0); i++)                         //Ciclo che scorre tutte le linee del csv
		{                                                                          //salvando tutto dentro a l'array bidimensionale fileContent
			for (int j = 0; j < fileContent.GetLength(1); j++)
			{
				fileContent[i, j] = lines[i].Split('\t')[j];
			}
		}

		int idRicetta = 0;
		for (int i = 1; i < fileContent.GetLength(0); i++)       //Salto la prima riga perche è l'intestazione
		{
			String prepQuery = "";
			for (int j = 1; j < fileContent.GetLength(1); j++)		//Salto la prima colonna perche è il nome
			{
				if (j < fileContent.GetLength(1) - 1)
				{
					prepQuery += fileContent[i, j] + ',';
				}
				else
				{
					prepQuery += fileContent[i, j];
				}
			}
			if (i == 1)
			{
				String query1 = $"INSERT INTO RicettePiastra (Nome) VALUES ({fileContent[i,0]});";

				String query = $"SELECT ID_Ric FROM RicettePiastra WHERE Nome = '{fileContent[i, 0]}'";
				MyStore.Query(query, out string[] header, out object[,] resultSet);
				idRicetta = (int)resultSet[0,0];

				String query2 = $"INSERT INTO RicettePiastraDettagli (ID_Ric, PercorsoTag, Valore) VALUES ({idRicetta +","+prepQuery});";
			}
			else { 
				if (fileContent[i, 0] != fileContent[i - 1, 0])
				{
					String query1 = $"INSERT INTO RicettePiastra (Nome) VALUES ({fileContent[i, 0]});";

					String query = $"SELECT ID_Ric FROM RicettePiastra WHERE Nome = '{fileContent[i, 0]}'";
					MyStore.Query(query, out string[] header, out object[,] resultSet);
					idRicetta = (int)resultSet[0, 0];

					String query2 = $"INSERT INTO RicettePiastraDettagli (ID_Ric, PercorsoTag, Valore) VALUES ({idRicetta + "," + prepQuery});";
				}
				else
				{
					String query2 = $"INSERT INTO RicettePiastraDettagli (ID_Ric, PercorsoTag, Valore) VALUES ({idRicetta + "," + prepQuery});";
				}
			}
		}
		
	}
}
