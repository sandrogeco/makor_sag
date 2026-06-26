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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using FTOptix.System;
using FTOptix.S7TiaProfinet;
using FTOptix.S7TCP;

#endregion

public class OemToCSV : BaseNetLogic
{
	List<RemoteChildVariable> tagList = new();
	List<String[]> fileLines = new();
	List<bool> taskSlot = new();

	/// <summary>
	///		Script che crea il file csv salvando i tag dal plc
	///		Rileva tutti i tag che iniziano per bAlm, bWnr, bMsg
	///		anche se sono dentro a delle strutture o se fossero degli array di allarmi
	/// </summary>
	[ExportMethod]
	public void CreaFileCSV()
	{
		try
		{
            ((Rectangle)LogicObject.Owner.GetObject("loadingRect")).Visible = true;
            var start = DateTime.Now;
			Log.Info($"Inizio: {start}");
			string pathCSV = new ResourceUri(LogicObject.GetVariable("CSVpath").Value).Uri;
			var Stazione = Project.Current.Get($"CommDrivers/CODESYSDriver1/PLC_Next/Tags/PLC");
			RemoteChildVariableValue[] tags = { };

			if (!pathCSV.Contains(".csv"))
			{
				pathCSV += ".csv";
			}

			foreach (var TblVarTagNode in Stazione.GetNodesByType<TagStructure>())
			{
				if (TblVarTagNode is null)
				{
					continue;
				}
				TagFinder(TblVarTagNode);									//Cerco tutti i tag nel progetto e riempo le liste "fileLines" e "tagList"
			}																//poi usate per scrivere nel file
			tags = Project.Current.ChildrenRemoteRead(tagList).ToArray();	
				
			File.Delete(pathCSV);								//Elimino il file nel caso esistesse, e poi lo ricreo
			using (StreamWriter sw = File.CreateText(pathCSV))	//Apro lo stream per scrivere nel file CSV i dati
			{
				sw.WriteLine("PercorsoNodoPadre\tNomeTagPLC\tArrayIndex (Optional)\tValore\tType");  //Assegno l'intestazione alle colonne

				foreach (RetentivityStorage storage in Project.Current.Get($"Retentivity").GetNodesByType<RetentivityStorage>())
					foreach (var node in storage.Nodes)
						if (node.BrowseName == "Nodo2" || node.BrowseName == "Nodo6")				//Escludo il database e utenti per velocizzare la procedura
							continue;
						else
							RetentiveVarFinder(InformationModel.GetObject(node.Value), sw);			//Cerco e scrivo le variabili ritentive nel file
					
				int i = 0;
				foreach (var line in fileLines)									//Incomincio la scrittura dei tag
				{
					if (line[4].Contains('['))									//Mi chiedo se è un array
					{
						GestArrTag(tags[i], sw, line[0], line[1], line[4]);		//se si, eseguo la funzione per gestirlo
						i++;													//e passo al prossimo tag
						continue;
					}

					line[4] = (line[4].Contains("Int")) ? "Int" : line[4];		//per semplificare tratto tutti gli interi come Int32

					sw.Write($"{line[0]}\t");                                   //0 path tag
					sw.Write($"{line[1]}\t");                                   //1 nome tag
					sw.Write("\t");                                             //2 indice elemento tag array
					sw.Write($"{tags[i].Value.Value}\t");                       //3 valore
					sw.Write($"{line[4]}\t");                                   //4 tipo
					sw.WriteLine("");

					i++;
				}
			}
			Log.Info($"Fine: {DateTime.Now}");
			Log.Info($"Durata: {DateTime.Now - start}");
			Log.Info("File creato correttamente in: " + pathCSV);
            ((Rectangle)LogicObject.Owner.GetObject("loadingRect")).Visible = false;
        }
        catch (Exception ex)													//Nel caso si fosse verificato un qualsiasi errore lo metto a schermo e sulla console
		{
			Log.Error("Import failed: " + ex.ToString());
			((Image)LogicObject.Owner.GetObject("loadingRect/loadImage")).Visible = false;
			((Label)LogicObject.Owner.GetObject("loadingRect/errorLabel")).Visible = true;
			((Label)LogicObject.Owner.GetObject("loadingRect/errorMsg")).Text = "Export failed: " + ex.ToString().Split('\n')[0];
			((Button)LogicObject.Owner.GetObject("loadingRect/errorButton")).Visible = true;
			return;
		}

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
	///		Funzione ricorsiva che cerca i tag importati	
	///		salvandoli nella variabile globale "tagList"
	/// </summary>
	/// <param name="TblVarTagNode"></param>
	private void TagFinder(TagStructure TblVarTagNode)
	{
		foreach (var Tag in TblVarTagNode.GetNodesByType<IUAVariable>())
		{
			if (Tag.VariableType.ToString() == "FTOptix.CommunicationDriver.TagStructureType")
			{
				continue;
			}

			var NomeTag = Tag.BrowseName;
			var pathTag = MakeBrowsePath(Tag.Owner);

			string type = Tag.Value.Value.GetType().ToString().Split(".")[1];

			tagList.Add(new RemoteChildVariable(pathTag + "/" + NomeTag));		//Aggiungo alla lista dei tag la varibile
			string[] str = { pathTag, NomeTag, "", "", type };
			fileLines.Add(str);													//Aggiungo alla lista delle linee del file la riga quasi pronta

		}
		foreach (var structTag in TblVarTagNode.GetNodesByType<TagStructure>())
		{
			TagFinder(structTag);					//Rieseguo me stesso se ci fossero altre strutture sotto alla struttura attuale
		}											//per trovare altri tag dentro di esse
	}

	/// <summary>
	///		Funzione che mi scorre l'array di tag e mi scrive nel file una riga per ogni indice del tag
	/// </summary>
	/// <param name="Tag"></param>
	/// <param name="sw"></param>
	/// <param name="pathTag"></param>
	/// <param name="NomeTag"></param>
	/// <param name="type"></param>
	private void GestArrTag(RemoteChildVariableValue Tag, StreamWriter sw, string pathTag, string NomeTag, string type)
	{
		object obj = Tag.Value.Value;
		switch (obj.GetType().ToString().Split(".")[1])
		{
			case "Byte[]":
				byte[] arrPlc1 = Tag.Value;
				for (int i = 0; i < arrPlc1.Length; i++)
					scrivifile(arrPlc1[i], i, "Byte");
				break;
			case "Int16[]":
				short[] arrPlc2 = Tag.Value;
				for (int i = 0; i < arrPlc2.Length; i++)
					scrivifile(arrPlc2[i], i, "Int");
				break;
			case "Int32[]":
				int[] arrPlc3 = Tag.Value;
				for (int i = 0; i < arrPlc3.Length; i++)
					scrivifile(arrPlc3[i], i, "Int");
				break;
			case "Boolean[]":
				bool[] arrPlc4 = Tag.Value;
				for (int i = 0; i < arrPlc4.Length; i++)
					scrivifile(arrPlc4[i], i, "Boolean");
				break;
			case "Double[]":
				bool[] arrPlc5 = Tag.Value;
				for (int i = 0; i < arrPlc5.Length; i++)
					scrivifile(arrPlc5[i], i, "Double");
				break;
		}

		void scrivifile(object ValTag, int i, string type)
		{
			sw.Write($"{pathTag}\t");
			sw.Write($"{NomeTag}\t");
			sw.Write($"{i}\t");
			sw.Write($"{ValTag}\t");
			sw.Write($"{type}\t");
			sw.WriteLine("");
		}
	}

	/// <summary>
	///		Funzione che cerca tutte le variabili ritentive
	///		e le scrive nel file csv
	/// </summary>
	/// <param name="retentiveObj"></param>
	/// <param name="sw"></param>
	private void RetentiveVarFinder(IUAObject retentiveObj, StreamWriter sw)
	{
		foreach (var variable in retentiveObj.GetNodesByType<UAVariable>())
		{
			scriviFile(variable, sw);
		}
		foreach (var obj in retentiveObj.GetNodesByType<UAObject>())
		{
			RetentiveVarFinder(obj, sw);                //Rieseguo me stesso se ci fossero altri oggetti dentro all'oggetto attuale                                                       
		}                                               //per trovare altre variabili dentro di essi

		void scriviFile(IUAVariable variable, StreamWriter sw)
		{
			var NomeVar = variable.BrowseName;
			var pathVar = MakeBrowsePath(variable.Owner);
			string type = variable.Value.Value.GetType().ToString().Split(".")[1];

			if (type == "LocalizedText")
				return;
			if (type.Contains("Int"))
				type = "Int";

			if (variable.ValueRank.ToString() == "Scalar") { }          //Mi chiedo se è una variabile scalare, se no allora è un array
			else                                                        //Nel caso fosse un array eseguo la funzione "GestArrTag" e vado al prossimo tag
			{
				GestArrVar(variable, sw, pathVar, NomeVar, variable.Value.Value.ToString().Split(".")[1]);
				return;
			}

			sw.Write($"{pathVar}\t");
			sw.Write($"{NomeVar}\t");
			sw.Write("\t");
			sw.Write($"{variable.Value.Value}\t");
			sw.Write($"{type}\t");
			sw.WriteLine("");
		}
	}

	/// <summary>
	///		Funzione che gestisce gli array per le variabili nello store di ritentività
	/// </summary>
	/// <param name="variable"></param>
	/// <param name="sw"></param>
	/// <param name="pathVar"></param>
	/// <param name="NomeVar"></param>
	/// <param name="type"></param>
	private void GestArrVar(IUAVariable variable, StreamWriter sw, string pathVar, string NomeVar, string type)
	{
		object obj = variable.Value.Value;
		switch (obj.GetType().ToString().Split(".")[1])
		{
			case "Byte[]":
				byte[] arr1 = variable.Value;
				for (int i = 0; i < arr1.Length; i++)
					scriviFile(arr1[i], i, "Byte");
				break;
			case "Int16[]":
				short[] arr2 = variable.Value;
				for (int i = 0; i < arr2.Length; i++)
					scriviFile(arr2[i], i, "Int");
				break;
			case "Int32[]":
				int[] arr3 = variable.Value;
				for (int i = 0; i < arr3.Length; i++)
					scriviFile(arr3[i], i, "Int");
				break;
			case "Boolean[]":
				bool[] arr4 = variable.Value;
				for (int i = 0; i < arr4.Length; i++)
					scriviFile(arr4[i], i, "Boolean");
				break;
			case "String[]":
				string[] arr5 = variable.Value;
				for (int i = 0; i < arr5.Length; i++)
					scriviFile(arr5[i], i, "String");
				break;
			case "Double[]":
				bool[] arrPlc5 = variable.Value;
				for (int i = 0; i < arrPlc5.Length; i++)
					scriviFile(arrPlc5[i], i, "Double");
				break;
		}
		void scriviFile(object valVar, int i, string type)
		{
			sw.Write($"{pathVar}\t");
			sw.Write($"{NomeVar}\t");
			sw.Write($"{i}\t");
			sw.Write($"{valVar}\t");
			sw.Write($"{type}\t");
			sw.WriteLine("");
		}
	}

	/// <summary>
	///		Funzione che legge il file csv e importa tutti i tag e le variabili ritentive a runtime
	/// </summary>
	[ExportMethod]
	public void LeggiFileCSV()
	{
		try 
		{
            ((Rectangle)LogicObject.Owner.GetObject("loadingRect")).Visible = true;
            ((ColumnLayout)LogicObject.Owner.GetObject("FileSelectorRectangle/TagsMancanti")).Visible = true;
            var start = DateTime.Now;
			Log.Info($"Inizio: {start}");
			string pathCSV = new ResourceUri(LogicObject.GetVariable("CSVpath").Value).Uri;
			List<string[]> lines = new();
			using (StreamReader sw = File.OpenText(pathCSV))	//Apro lo stream per scrivere nel file CSV i dati
			{
				sw.ReadLine();								//salto l'intestazione
				while (!sw.EndOfStream)
				{
					lines.Add(sw.ReadLine().Split("\t"));	//Salvo tutto il contenuto del file nella lista "lines"
				}
			}

			List<object> arrObj = new();
			IUAVariable arrTag = null;
			string lastPath = "";

			List<RemoteChildVariableValue> tagList = new();
            IUAObject tagNotFoundObj = Owner.GetObject("TagsNotFound");			//Object che contiene la variabili che hanno dato errore, usato come modello in una data grid
            var cntVarNotFound = Owner.GetVariable("NVarNotFound");             //Variabile che conta il numero di variabili che hanno dato errore, mostrato a schermo
            int timeOut = Owner.GetVariable("TimeOut_Write_[ms]").Value;

            foreach (string[] line in lines)
			{
				var path = $"{line[0]}/{line[1]}";
                if (path.Contains("CntRetentive"))			//Salto l'oggetto "CntRetentive" perchè contiene lo storico dei contatori e genera un errore durante la scrittura
                    continue;

                var tag = Project.Current.GetVariable(path);
				if (arrObj.Count > 1 && lastPath != path)               //Mi chiedo se ho finito di leggere un array
				{                                                       //se si allora lo scrivo e resetto le variabili per il controllo
                    try
                    {
                        InformationModel.RemoteWrite(new List<RemoteVariableValue> { new(Project.Current.GetVariable(lastPath), arrObj.ToArray()) }, timeOut);
                        arrObj.Clear();
                        arrTag = null;
                        lastPath = "";
                    }
                    catch (Exception e)		//Controllo se si verifica un errore generico per ignorare la variabile e continuare con le altre
                    {
                        tagNotFoundObj.Add(InformationModel.MakeVariable(lastPath, OpcUa.DataTypes.Boolean));
                        cntVarNotFound.Value += 1;
                        Log.Warning("Variabile non trovata: " + lastPath+" Con errore: "+e.Message);
                        arrObj.Clear();
                        arrTag = null;
                        lastPath = "";
                    }
                }
				if (line[2] == "")          //Se line[2] è vuota allora è una variabile semplice
                    try
                    {
                        InformationModel.RemoteWrite(new List<RemoteVariableValue> { new(tag, stringParserValue(line[3], line[4])) }, timeOut);

                    }
                    catch (Exception)		//Controllo se si verifica un errore generico per ignorare la variabile e continuare con le altre
                    {
                        tagNotFoundObj.Add(InformationModel.MakeVariable(path, OpcUa.DataTypes.Boolean));
                        cntVarNotFound.Value += 1;
                        Log.Warning("Variabile non trovata: " + path);
                        continue;
                    }
                else						//altrimenti è un array
                {													
					arrTag = tag;										//Setto delle variabili che controllerò per vedere se ho finito di leggere l'array
					lastPath = path;																			
					arrObj.Add(stringParserObj(line[3], line[4]));      //Aggiungo ad una lista d'appoggio tutti i valori dell'array parsando la stringa nel tipo richiesto	
				}
			}

			Log.Info($"Fine: {DateTime.Now}");
			Log.Info($"Durata: {DateTime.Now - start}");
			Log.Info(pathCSV + " è stato importato correttamente");
            ((Rectangle)LogicObject.Owner.GetObject("loadingRect")).Visible = false;


            static UAValue stringParserValue(string input, string type)
			{
				return type switch
				{
					"Boolean" => bool.Parse(input),
					"Byte" => byte.Parse(input),
					"Int" => long.Parse(input),
					"Single" => float.Parse(input),
					"Double" => double.Parse(input),
					"DateTime" => DateTime.Parse(input),
					"String" => input,
					_ => 0,
				};
			}
			static object stringParserObj(string input, string type)
			{
				return type switch
				{
					"Boolean" => bool.Parse(input),
					"Byte" => byte.Parse(input),
					"Int" => long.Parse(input),
					"Single" => float.Parse(input),
					"Double" => double.Parse(input),
					"DateTime" => DateTime.Parse(input),
					"String" => input,
					_ => 0,
				};
			}
		}
		catch (Exception ex)            //Nel caso si fosse verificato un qualsiasi errore lo metto a schermo e sulla console
		{
			Log.Error("Export failed: " + ex.ToString());
			((Image)LogicObject.Owner.GetObject("loadingRect/loadImage")).Visible = false;
			((Label)LogicObject.Owner.GetObject("loadingRect/errorLabel")).Visible = true;
			((Label)LogicObject.Owner.GetObject("loadingRect/errorMsg")).Text = "Import failed: " + ex.ToString().Split('\n')[0];
			((Button)LogicObject.Owner.GetObject("loadingRect/errorButton")).Visible = true;
			return;
		}
	}



    // Pseudocodice dettagliato per ottimizzazione LeggiFileCSV_
    // 1. Carica tutte le righe del file CSV in memoria (già fatto)
    // 2. Usa un dizionario per raggruppare le righe per path (così puoi gestire array in modo batch)
    // 3. Per ogni path:
    //    a. Se tutte le righe hanno indice array vuoto, sono variabili semplici: scrivi in parallelo (Task + SemaphoreSlim)
    //    b. Se ci sono indici array, raccogli tutti i valori e scrivi l'array in un'unica chiamata
    // 4. Gestisci errori e aggiorna la UI come già fatto
    // 5. Elimina codice duplicato (parser, try/catch, ecc.)
    // 6. Usa Task.WhenAll per attendere tutte le scritture parallele

    [ExportMethod]
    public void LeggiFileCSV_()
    {
        try
        {
            ((Rectangle)LogicObject.Owner.GetObject("loadingRect")).Visible = true;
            ((ColumnLayout)LogicObject.Owner.GetObject("FileSelectorRectangle/TagsMancanti")).Visible = true;
            var start = DateTime.Now;
            DateTime scriptEnd;
            Log.Info($"Inizio: {start}");
            string pathCSV = new ResourceUri(LogicObject.GetVariable("CSVpath").Value).Uri;
            List<string[]> lines = new();
            using (StreamReader sw = File.OpenText(pathCSV))
            {
                sw.ReadLine();
                while (!sw.EndOfStream)
                    lines.Add(sw.ReadLine().Split("\t"));
            }

            var tagNotFoundObj = Owner.GetObject("TagsNotFound");
            var cntVarNotFound = Owner.GetVariable("NVarNotFound");
            var cntVarOK = Owner.GetVariable("VarScritteOK");
            int timeOut = Owner.GetVariable("TimeOut_Write_[ms]").Value;
			int nScrittureParallele = 100; // Numero di scritture parallele consentite

            // Raggruppa per path (path = $"{line[0]}/{line[1]}")
            var grouped = lines
                .Where(line => !($"{line[0]}/{line[1]}".Contains("CntRetentive")))
                .GroupBy(line => $"{line[0]}/{line[1]}");

            foreach (var group in grouped)
            {
                var path = group.Key;
                var tag = Project.Current.GetVariable(path);
                // Se tutte le righe hanno indice array vuoto, è una variabile semplice
                if (group.All(line => string.IsNullOrEmpty(line[2])))
                {
                    while (taskSlot.Count >= nScrittureParallele) { }

                    var line = group.First();
                    new LongRunningTask (func => {
                        try
                        {
                            taskSlot.Add(true);
                            InformationModel.RemoteWrite(new List<RemoteVariableValue> { new(tag, StringParserValue(line[3], line[4])) }, timeOut);
							cntVarOK.Value++;
                        }
                        catch (Exception e)
                        {
                            tagNotFoundObj.Add(InformationModel.MakeVariable(path, OpcUa.DataTypes.Boolean));
                            cntVarNotFound.Value++;
                            Log.Warning("Variabile non trovata: " + path + " " + e.Message);
                        }
                        finally
                        {
                            taskSlot.RemoveAt(0);
                        }
                    },LogicObject).Start();
                }
                else // Array: Ordina i valori in base all'indice e scrive l'array
                {
                    while (taskSlot.Count >= nScrittureParallele) { }

                    var arrayValues = group
                        .OrderBy(line => int.TryParse(line[2], out var idx) ? idx : 0)
                        .Select(line => StringParserObj(line[3], line[4]))
                        .ToArray();

                    new LongRunningTask(func => {
                        try
						{
                            taskSlot.Add(true);
                            InformationModel.RemoteWrite(new List<RemoteVariableValue> { new(tag, arrayValues) }, timeOut);
                            cntVarOK.Value++;
                        }
                        catch (Exception e)
						{
							tagNotFoundObj.Add(InformationModel.MakeVariable(path, OpcUa.DataTypes.Boolean));
							cntVarNotFound.Value++;
							Log.Warning("Variabile non trovata: " + path + " " + e.Message);
                        }
                        finally
                        {
                            taskSlot.RemoveAt(0);
                        }
                    }, LogicObject).Start();
                }
            }

            scriptEnd = DateTime.Now;
            while (taskSlot.Count > 0) {
				if ((DateTime.Now - scriptEnd).TotalMilliseconds > 30000)
					break; // Timeout di 30 secondi per evitare blocchi
            }

            Log.Info($"Fine: {DateTime.Now}");
            Log.Info($"Durata: {DateTime.Now - start}");
            Log.Info(pathCSV + " è stato importato correttamente");
            ((Rectangle)LogicObject.Owner.GetObject("loadingRect")).Visible = false;
        }
        catch (Exception ex)
        {
            Log.Error("Export failed: " + ex.ToString());
            ((Image)LogicObject.Owner.GetObject("loadingRect/loadImage")).Visible = false;
            ((Label)LogicObject.Owner.GetObject("loadingRect/errorLabel")).Visible = true;
            ((Label)LogicObject.Owner.GetObject("loadingRect/errorMsg")).Text = "Import failed: " + ex.ToString().Split('\n')[0];
            ((Button)LogicObject.Owner.GetObject("loadingRect/errorButton")).Visible = true;
            return;
        }

        static UAValue StringParserValue(string input, string type)
        {
            return type switch
            {
                "Boolean" => bool.Parse(input),
                "Byte" => byte.Parse(input),
                "Int" => long.Parse(input),
                "Single" => float.Parse(input),
                "Double" => double.Parse(input),
                "DateTime" => DateTime.Parse(input),
                "String" => input,
                _ => 0,
            };
        }
        static object StringParserObj(string input, string type)
        {
            return type switch
            {
                "Boolean" => bool.Parse(input),
                "Byte" => byte.Parse(input),
                "Int" => long.Parse(input),
                "Single" => float.Parse(input),
                "Double" => double.Parse(input),
                "DateTime" => DateTime.Parse(input),
                "String" => input,
                _ => 0,
            };
        }
    }
}
