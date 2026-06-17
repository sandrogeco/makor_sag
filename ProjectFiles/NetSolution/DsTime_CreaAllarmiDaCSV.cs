using FTOptix.Alarm;
using FTOptix.Core;
using FTOptix.CoreBase;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using System;
using System.Collections.Generic;
using UAManagedCore;
using FTOptix.WebUI;
using FTOptix.Recipe;
using FTOptix.System;
using FTOptix.S7TiaProfinet;
using OpcUa = UAManagedCore.OpcUa;
public class DsTime_CreaAllarmiDaCSV : BaseNetLogic
{
    private static NodeId CategoryDigitalAlarmNID;
    private bool CategoryDigitalAlarmValidRef;

    /// <summary>
    /// Questo netlogic legge le informazioni da un file CSV e crea gli allarmi digitali. Assegna agli allarmi il messaggio e inserisce la chiave nella tabelle delle traduzioni e le eventuali traduzioni nelle varie lingue.
    /// </summary>
    [ExportMethod]
    public void CreaAllarmi()
    {
        var AlarmFolder = Owner;
        const int DriverIndex = 0;          //Numero colona driver collegamento driver comunicazione
        const int AlmNameIndex = 1;         //Numero colona con il nome dell'allarme
        const int AlmArrayIndex = 2;        //Numero colona con l'indice dell'array se gli alalrmi sono strutturati in formato array
        const int CategoryIndex = 3;        //numero colonna con il nome dell'allarme tipizzato da istanziare per creare l'allarme
        const int AlmSubFolderIndex = 4;    //numero colonna con nome della cartella da creare sotto la cartella allarmi di Uniqo
        const int HmiAlmPrefixIndex = 5;    //numero colonna con il prefisso da mettere davanti all'allarme HMI (Prefix_AlmName)
        const int AlmSeverityIndex = 6;     //numero colonna con con la severity da assegnare all'allarme
        const int TxtIdIndex = 7;           //numero colonna con la chiave di testo da inserire nella tabella delle traduzioni (Traduz_Alm)
        const int LocaleIdsStrtIndex = 8;   //Numero colonna con la traduzione (in italiano) da asegnare al messaggio dell'allarme

        string csvPath = new ResourceUri(value: LogicObject.GetVariable("CSVPath").Value).Uri;

        if (string.IsNullOrEmpty(csvPath))
        {
            Log.Error("Creazione allarmi", "File CSV non trovato");
            return;
        }

        //controllo se il carattere separatore ? valdo oppure no
        char? characterSeparator = CheckCharacterSeparator(LogicObject.GetVariable("CharacterSeparator").Value);
        if (characterSeparator == null || characterSeparator == '\0')
        {
            Log.Error("Creazione allarmi", "Wrong CharacterSeparator configuration. Please insert a char");
            return;
        }
		if(LogicObject.GetVariable("CharacterSeparator").Value == "tab")
		{
            characterSeparator = '\t';
		}

		try
        {
            bool wrapFields = LogicObject.GetVariable("WrapFields").Value;     //indica che le colonne non sono incapsulate tra doppi apici

            //Apro lo stream verso il file.
            using (var csvReader = new CSVFileReader(csvPath) { FieldDelimiter = characterSeparator.Value, WrapFields = wrapFields })
            {
                if (csvReader.EndOfFile())
                {
                    Log.Error("Creazione allarmi", $"Il file {csvPath} ? vuoto");
                    return;
                }

                var fileLines = csvReader.ReadAll();
                if (fileLines.Count == 0 || fileLines[0].Count == 0)
                    return;

                int numColumns = fileLines[0].Count;

                //leggo la riga di intestazione per creare la lista dei localeIds
                List<string> LocaleIds = new();
                for (int i = LocaleIdsStrtIndex; i < numColumns; i++)
                    LocaleIds.Add(fileLines[0][i]);

                var AlmType = Project.Current.Find("CategoryDigitalAlarmType");
                CheckCategoryRef();     //Recupero il NodeId dell'allarme tipizzato

                //Leggo una riga per volta. Salto la prima riga che ? quella di intestazione
                for (var r = 1; r < fileLines.Count; ++r)
                {
                    if (string.IsNullOrEmpty(fileLines[r][AlmNameIndex]))      //salto le righe vuote
                        continue;

                    string NomeTabAlmPlc = fileLines[r][AlmSubFolderIndex];     //leggo il nome della cartella nella quale vanno creati gli allarmi
                    string AlmPrefix = fileLines[r][HmiAlmPrefixIndex];
                    string AlmTextId = fileLines[r][TxtIdIndex];
                    string Category = fileLines[r][CategoryIndex];
                    ushort Gravita = fileLines[r][AlmSeverityIndex] == "" ? (ushort)0 : Convert.ToUInt16(fileLines[r][AlmSeverityIndex]);
                    int sourceArrayIndex = string.IsNullOrEmpty(fileLines[r][AlmArrayIndex]) || string.IsNullOrWhiteSpace(fileLines[r][AlmArrayIndex])
                            ? -1
                            : Convert.ToInt32(fileLines[r][AlmArrayIndex]);

                    string AlmName = sourceArrayIndex == -1 ? fileLines[r][AlmNameIndex] : fileLines[r][AlmNameIndex] + sourceArrayIndex;
                    AlmName = string.IsNullOrEmpty(AlmPrefix) ? AlmName : $"{AlmPrefix}_{AlmName}";  //imposto il nome dell'allarme con o senza prefisso

                    IUAVariable AlmInputTag = null;
                    if (string.IsNullOrEmpty(fileLines[r][DriverIndex]))   //Controllo se l'allarme deve essere collegato a una variabile input
                    {
                        Log.Warning("Creazione allarmi:", $"L'allarme {fileLines[r][AlmNameIndex]} al rigo {r} non ha nessuna variabile input. Prevedere di collegare la variabile o la condizione di attivazione dell'allarme");
                    }
                    else
                    {
                        AlmInputTag = (IUAVariable)Project.Current.Get(fileLines[r][DriverIndex]).Find(fileLines[r][AlmNameIndex]);
                        if (AlmInputTag is null)
                        {
                            Log.Warning("Creazione allarmi:", $"Non ? stato possibile recuperare l'allarme {fileLines[r][AlmNameIndex]} nel PLC: {fileLines[r][DriverIndex]}");
                            continue;
                        }
                    }

                    //controllo se la cartella 'NomeTabAlmPlc' esiste sotto il Nodo "Allarmi"
                    IUANode Nodo_TblAlm = null;
                    if (string.IsNullOrEmpty(NomeTabAlmPlc))
                    {
                        Nodo_TblAlm = AlarmFolder;
                    }
                    else
                    {
                        if (!DesignTime_Utility.CheckNodo(AlarmFolder, NomeTabAlmPlc, out Nodo_TblAlm))
                            Nodo_TblAlm = DesignTime_Utility.CreaNodo(AlarmFolder, NomeTabAlmPlc, DesignTime_Utility.Tipo.Folder);  // Se non essite la creo  e tiro su il nodo 
                    }

                    DigitalAlarm HmiAlm = Nodo_TblAlm.Children.Get<DigitalAlarm>(AlmName);
                    switch (HmiAlm)
                    {
                        case null:
                            HmiAlm = InformationModel.MakeObject<DigitalAlarm>(AlmName, CategoryDigitalAlarmNID);
                            InizializeAlm(HmiAlm, Category, Gravita, AlmInputTag, AlmPrefix.Length, sourceArrayIndex);
                            Nodo_TblAlm.Add(HmiAlm);
                            break;
                        default:
                            InizializeAlm(HmiAlm, Category, Gravita, AlmInputTag, AlmPrefix.Length, sourceArrayIndex);
                            break;
                    }
                    //Creo la lista con le traduzioni da assegnare all'allarme nelle varie lingue
                    List<string> LocaleText = new();
                    for (int i = LocaleIdsStrtIndex; i < numColumns; i++)
                        LocaleText.Add(fileLines[r][i]);

                    HmiAlm.LocalizedMessage = DesignTime_Utility.GetAllTranslation(AlmTextId, Project.Current.GetVariable("Translations/Traduz_Alm"), LocaleIds, LocaleText);
                }
            }

            Log.Info("Creazione allarmi", $"File CSV importato correttamente");
        }
        catch (Exception ex)
        {
            Log.Error("Creazione allarmi", $"Non ? stato possibile leggere il file CSV: {ex}");
        }
    }

    /// <summary>
    /// Imposta le propriet? dell'allarme
    /// </summary>
    /// <param name="allarme"></param>
    /// <param name="AlmVar"></param>
    /// <param name="Categoria"></param>
    /// <param name="PrefixLength"></param>
    /// <param name="sourceArrayIndex"></param>
    /// <param name="PlcTagIsArray"></param>
    public static void InizializeAlm(DigitalAlarm allarme, string Categoria, ushort Severity, IUAVariable AlmVar = null, int PrefixLength = 0, int sourceArrayIndex = -1)
    {
        //imposto le propriet? dell'allarme
        string NomeAlm = allarme.BrowseName;
        allarme.AutoAcknowledge = true;
        allarme.AutoConfirm = true;

        if (Categoria != "")
            allarme.SetOptionalVariableValue("Category", Categoria.ToString());


        switch (Severity)
        {
            case 0:         //nel caso la gravit? non fosse indicata nel file csv provo a calcolarla leggendola dal nome dell'allarme
                {
                    //Definisco la severit? dell'allarme leggendone il nome. Controllo le prime 3 lettere del nome se l'allarme ? senza prefisso altrimenti controllo le prime 3 cifre dopo il prefisso.
                    string CtrlString = PrefixLength > 0 ? NomeAlm.Substring(PrefixLength + 1, 3) : NomeAlm.Substring(0, 3);
                    allarme.Severity = CtrlString switch
                    {
                        "Alm" => 1000,
                        "Err" => 1000,
                        "Wrn" => 500,
                        "Msg" => 1,
                        _ => 1000,
                    };
                    break;
                }

            default:
                allarme.Severity = Severity;
                break;
        }

        if (AlmVar is not null)
        {
            if (sourceArrayIndex == -1)
                allarme.InputValueVariable.SetDynamicLink(AlmVar, DynamicLinkMode.ReadWrite);
            else
                allarme.InputValueVariable.SetDynamicLink(AlmVar, (uint)sourceArrayIndex, DynamicLinkMode.ReadWrite);
        }
    }

    private char? CheckCharacterSeparator(string separator)
    {
        return separator == string.Empty ? null : separator == "tab" ? '\t' : char.TryParse(separator, out char result) ? result : null;
    }

    private void CheckCategoryRef()
    {
        var nodoCatAl = LogicObject.GetVariable("CustomCategoryAlarmType");
        if (nodoCatAl != null && nodoCatAl.DataType.GetType() == typeof(NodeId))
        {
            CategoryDigitalAlarmNID = (NodeId)nodoCatAl.DataValue.Value;
            CategoryDigitalAlarmValidRef = true;
        }
    }

    [ExportMethod]
    public void ExportAllarmi()
    {
        // metodo con cui esporto gli allarmi nello stesso file da cui li ho esportati --> lo aggiorno (se ci sono state modifiche su Qstudio)

        // variabili utili
        const int DriverIndex = 0;          //Numero colona driver collegamento driver comunicazione
        const int AlmNameIndex = 1;         //Numero colona con il nome dell'allarme
        const int AlmArrayIndex = 2;        //Numero colona con l'indice dell'array se gli alalrmi sono strutturati in formato array
        const int CategoryIndex = 3;        //numero colonna con il nome dell'allarme tipizzato da istanziare per creare l'allarme
        const int AlmSubFolderIndex = 4;    //numero colonna con nome della cartella da creare sotto la cartella allarmi di Uniqo
        const int HmiAlmPrefixIndex = 5;    //numero colonna con il prefisso da mettere davanti all'allarme HMI (Prefix_AlmName)
        const int AlmSeverityIndex = 6;     //numero colonna con con la severity da assegnare all'allarme
        const int TxtIdIndex = 7;           //numero colonna con la chiave di testo da inserire nella tabella delle traduzioni (Traduz_Alm)

        string csvPath = new ResourceUri(value: LogicObject.GetVariable("CSVPath").Value).Uri;
        char? characterSeparator = CheckCharacterSeparator(LogicObject.GetVariable("CharacterSeparator").Value);
		if (LogicObject.GetVariable("CharacterSeparator").Value == "tab")
		{
			characterSeparator = '\t';
		}
		bool WrapFieldsEnable = LogicObject.GetVariable("WrapFields").Value;

        int rowTot = 1;
        int rowCount = 1;                   // salto la prima riga
        string[] components;

        try
        {
            // creo le dimensioni della matrice
            foreach (var AlarmFolder in Owner.GetNodesByType<Folder>())
            {
                foreach (var alarm in AlarmFolder.GetNodesByType<DigitalAlarm>())
                {
                    rowTot++;
                }
                rowTot++;
            }

            string[,] almDictionary = GetTransaltions();

            var rowDicCount = almDictionary.GetLength(0);
            var columnDicCount = almDictionary.GetLength(1);

            string[,] AlmExp = new string[rowTot, 8 + columnDicCount - 1];          // Contenitore degli allarmi esportati, sfrutto la stessa struttura usata da CreaAllarmi()

            // configuro la prima riga
            AlmExp[0, 0] = "PercorsoNodoPadre";
            AlmExp[0, 1] = "NomeAllarme (NomeTagPLC)";
            AlmExp[0, 2] = "ArrayIndex (Optional)";
            AlmExp[0, 3] = "Categoria (Es. nome macchina)";
            AlmExp[0, 4] = "CartellaAlmDigitali";
            AlmExp[0, 5] = "PrefixAllarme(Optional, Es. DB)";
            AlmExp[0, 6] = "Gravit? allarme (1= msg, 500= wrn, 1000= alm) ";
            AlmExp[0, 7] = "Chiave(TextId nella tabella Traduz.)";

            // entra in tutte le cartelle degli allarmi e salva le caratteristiche dei suddetti in una matrice
            foreach (var AlarmFolder in Owner.GetNodesByType<Folder>())
            {
                foreach (var alarm in AlarmFolder.GetNodesByType<DigitalAlarm>())
                {
                    string DynamicLinkString = ExportAlarmVariable(alarm.InputValueVariable);
                    SeparatePathString(DynamicLinkString, out components);

                    GetParts(components, out AlmExp[rowCount, DriverIndex], out AlmExp[rowCount, AlmNameIndex], out AlmExp[rowCount, AlmArrayIndex]);
                    AlmExp[rowCount, CategoryIndex] = alarm.GetVariable("Category").Value;
                    AlmExp[rowCount, HmiAlmPrefixIndex] = GetPrefix(alarm);
                    AlmExp[rowCount, AlmSubFolderIndex] = AlarmFolder.BrowseName;
                    AlmExp[rowCount, AlmSeverityIndex] = (string)alarm.GetVariable("Severity").Value;
                    AlmExp[rowCount, TxtIdIndex] = alarm.LocalizedMessage.TextId;
                    AddTranslations(AlmExp, almDictionary, rowCount, TxtIdIndex);

                    rowCount++;
                }
                for (int i = 0; i < AlmExp.GetLength(1); ++i)
                    AlmExp[rowCount, i] = "";
                rowCount++;
            }

            // aggiorna il file .csv e crea un file .txt in cui scrive data della modifica e allarmi aggiunti.
            using (var csvWriter = new CSVFileWriter(csvPath) { FieldDelimiter = (char)characterSeparator, WrapFields = WrapFieldsEnable })
            {
                for (var r = 0; r < rowCount; ++r)
                {
                    var row = new string[AlmExp.GetLength(1)];
                    for (var c = 0; c < AlmExp.GetLength(1); ++c)
                        row[c] = AlmExp[r, c];
                    csvWriter.WriteLine(row);
                }
            }

            Log.Info("File esportato");
        }
        catch (Exception ex)
        {
            Log.Error("ExportAlarms", $"Unable to export the alarms: {ex}");
        }
    }

    private void AddTranslations(string[,] mat, string[,] dic, int r, int c)
    {
        if ((r - 1) == 0)               // aggiorno le IdLocale se ci sono
        {
            for (int j = 1; j < dic.GetLength(1); ++j)
            {
                mat[0, c + j] = dic[0, j];
            }
        }
        // controllo la chiave ed aggiungo le traduzioni nella giusta riga
        for (int i = 1; i < mat.GetLength(0); ++i)
        {
            if (dic[i, 0] == mat[r, c])
            {
                for (int j = 1; j < dic.GetLength(1); ++j)
                {
                    mat[r, c + j] = dic[i, j];
                }
                break;
            }
        }
    }

    private string[,] GetTransaltions()
    {
        // recupero le traduzione e le aggiungo alla matrice
        string[,] dictionary;
        var localizationDictionary = GetDictionary();

        return dictionary = (string[,])localizationDictionary.Value.Value;
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

        var dictionaryNode = LogicObject.Context.GetNode(nodeIdDictionaryValue);
        if (dictionaryNode == null)
        {
            Log.Error("ImportAndExportTranslations", "The node pointed by the LocalizationDictionary variable cannot be found");
            return null;
        }

        var resultDictionaryVariable = dictionaryNode as IUAVariable;
        if (resultDictionaryVariable == null || !resultDictionaryVariable.IsInstanceOf(FTOptix.Core.VariableTypes.LocalizationDictionary))
            Log.Error("The node pointed by the LocalizationDictionary variable is not a localization dictionary");

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

    private void SeparatePathString(string PathString, out string[] components)
    {
        // separa il PercorsoNodoPadre in sottostringhe delimitate dal carattere '/'
        components = PathString.Split('/');
    }

    private void GetParts(string[] components, out string Driver, out string almName, out string almArrayIndex)
    {
        // usa l'ultima sottostringa ottenuta con il metodo sopra per ottenere il PercorsoNodoPadre, Nome dell'allarme, e se ? un array il suo indice
        string DriverComponents = components[0];

        almName = components[components.GetLength(0) - 1];
        almArrayIndex = "";

        if (almName.Contains("["))
        {
            almArrayIndex = almName.Substring(almName.IndexOf("[") + 1, almName.IndexOf("]") - almName.IndexOf("[") - 1);
            almName = almName.Substring(0, almName.IndexOf("["));
        }

        for (int i = 1; i < components.GetLength(0) - 1; ++i)
        {
            DriverComponents = $"{DriverComponents}/{components[i]}";
        }

        Driver = DriverComponents;
    }

    private string GetPrefix(DigitalAlarm alarm)
    {
        // ottengo il Prefix nel modo inverso in cui viene creato il nome dell'allarme: ==> {prefix}_{nomeallarme}
        string prefix = "";
        string name = alarm.BrowseName;

        if (name.Contains("_"))     // controllo che il prefisso effettivamente sia presente
        {
            prefix = name.Substring(0, name.IndexOf("_"));
        }

        return prefix;
    }

    private string ExportAlarmVariable(IUAVariable varToAnalyze)
    {
        string pathToInputValueNode = "";
        // Get the DynamicLink (variable linked) of the Dynamic Link
        DynamicLink inputPath = (DynamicLink)varToAnalyze.GetVariable("DynamicLink");
        // If inputPath is null, return empty string
        if (inputPath == null) return "";
        // Resolve the path of variable linked to the field
        PathResolverResult resolvePathResult = LogicObject.Context.ResolvePath(varToAnalyze, inputPath.Value);
        // If resolvePathResult is null, return empty string
        if (resolvePathResult == null) return "";
        // Check if is an Alias or Variable
        if (resolvePathResult.AliasSpecification != null && resolvePathResult.AliasSpecification.AliasTokenPath != "")
        {
            // If is alias return the full value of inputPath like {aliasName}\<struct>
            pathToInputValueNode = inputPath.Value;
        }
        else
        {
            // Get the path in string format of the variable for write to CSV file
            pathToInputValueNode = MakeBrowsePath(resolvePathResult.ResolvedNode);
            // if the Indexes is plus then 0, mean is an array (more indexex, more dimension of array)       
            if (resolvePathResult.Indexes != null && resolvePathResult.Indexes.Length > 0)
            {
                // Open square brackets for string notation 
                pathToInputValueNode += "[";
                // for each index append the value on the string with a , as separator
                for (int i = 0; i < resolvePathResult.Indexes.Length; i++)
                {
                    pathToInputValueNode += resolvePathResult.Indexes[i];
                    // if not the last element add a ,
                    if (i != resolvePathResult.Indexes.Length - 1) pathToInputValueNode += ",";

                }
                // Close the square brackets for string notation
                pathToInputValueNode += "]";
            }
        }
        return pathToInputValueNode;
    }

    private string MakeBrowsePath(IUANode node)
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
}
