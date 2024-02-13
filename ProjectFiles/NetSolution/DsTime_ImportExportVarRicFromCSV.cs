#region Using directives
using FTOptix.Core;
using FTOptix.CoreBase;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using System;
using System.Linq;
using UAManagedCore;
#endregion

public class DsTime_ImportExportVarRicFromCSV : BaseNetLogic
{
    const int PercorsoDriverCommIndex = 0;
    const int NomeStructIndex = 1;
    const int NomeTagPlcIndex = 2;
    const int IndiceArrayIndex = 3;
    const int NomeTagHmiIndex = 4;
    const int TipoTagIndex = 5;
    const int NomeObjectL1Index = 6;
    //const int NomeObjectL2Index = 8;
    //const int NomeObjectL3Index = 10;
    readonly string[] FileHeader = { "PercorsoDriverComm(su Qstudio)", "NomeStruct", "NomeTagPLC", "IndiceArray", "NomeTagHmi", "TipoTag", "NomeObjectL1", "ObjectTypeL1", "NomeObjectL2", "ObjectTypeL2", "NomeObjectL32", "ObjectTypeL3" };

    [ExportMethod]
    public void CreaObjectType()
    {
        var ObjectTest = InformationModel.MakeObjectType("TestZona");
        DesignTime_Utility.CreaNodo(ObjectTest, "TestVar1", DesignTime_Utility.Tipo.variabile, DesignTime_Utility.GetOpcUaDataType("Int16"));
        Owner.Add(ObjectTest);

        var Test = InformationModel.MakeObject("TestObject", ObjectTest.NodeId);
        Owner.Add(Test);
    }

    [ExportMethod]
    public void ImportaTag()
    {
        if (LogicObject.GetVariable("CSVPath") == null)
        {
            Log.Error("Errore import variabili ricetta", "Variabile CSVPath non trovata");
            return;
        }

        string csvPath = new ResourceUri(LogicObject.GetVariable("CSVPath").Value).Uri;

        if (string.IsNullOrEmpty(csvPath))
        {
            Log.Error("Errore export variabili ricetta", "Nessun file CSV trovato");
            return;
        }

        //controllo se il carattere separatore è valdo oppure no
        char? characterSeparator = CheckCharacterSeparator(",");
        if (characterSeparator == null || characterSeparator == '\0')
        {
            Log.Error("Errore import variabili ricetta", "Inserire un carattere separatore");
            return;
        }

        bool wrapFields = LogicObject.GetVariable("WrapFields").Value;
        int Row = 1;
        try
        {
            //Apro lo stream verso il file 
            using var csvReader = new CSVFileReader(csvPath) { FieldDelimiter = characterSeparator.Value, WrapFields = wrapFields };
            if (csvReader.EndOfFile())
            {
                Log.Error("ImportAndExportTranslations", $"The file {csvPath} is empty");
                return;
            }

            var fileLines = csvReader.ReadAll();
            if (fileLines.Count == 0 || fileLines[0].Count == 0)
                return;

            for (Row = 1; Row < fileLines.Count; ++Row)       //Salto la prima riga che è quella di intestazione
            {
                var DriverRef = fileLines[Row][PercorsoDriverCommIndex];
                if (!string.IsNullOrEmpty(DriverRef) && !string.IsNullOrWhiteSpace(DriverRef)) //se la riga è vuota salto alla successiva
                {
                    if (Project.Current.Get(DriverRef) == null)
                    {
                        Log.Error("Errore import variabili ricetta", $"Il percosro del driver '{DriverRef}' non è stato trovato");
                        continue;
                    }

                    IUANode NodoPadre = Owner;
                    //Ciclo for per creare la struttura variabili
                    for (int i = NomeObjectL1Index; i < fileLines[Row].Count; i += 2)
                    {
                        var NomeObject = fileLines[Row][i];
                        if (!string.IsNullOrEmpty(NomeObject))
                        {
                            if (!DesignTime_Utility.CheckNodo(NodoPadre, NomeObject, out IUANode Nodo_Object))
                            {
                                Nodo_Object = string.IsNullOrEmpty(fileLines[Row][i + 1]) ?
                                    DesignTime_Utility.CreaNodo(NodoPadre, NomeObject, DesignTime_Utility.Tipo.Object) :
                                    DesignTime_Utility.CreaNodo(NodoPadre, NomeObject, DesignTime_Utility.Tipo.ObjectType, Project.Current.Find(fileLines[Row][i + 1]).NodeId);
                            }
                            NodoPadre = Nodo_Object;
                        }
                    }
                    NodeId TagType = DesignTime_Utility.GetOpcUaDataType(fileLines[Row][TipoTagIndex]);

                    var NomeTagHmi = fileLines[Row][NomeTagHmiIndex];
                    if (!DesignTime_Utility.CheckNodo(NodoPadre, NomeTagHmi, out IUANode Nodo_VarRic))   //Controllo se la variabile esiste                    
                        Nodo_VarRic = DesignTime_Utility.CreaNodo(NodoPadre, NomeTagHmi, DesignTime_Utility.Tipo.variabile, TagType);  // Se non essite la creo  e tiro su il nodo

                    //Creo il collegamento dinamico. Prima verifico se la tag PLC è scalare oppure un array
                    if (LogicObject.GetVariable("CreaCollegamentiDinamici").Value)
                    {
                        IUAVariable LinkVar = string.IsNullOrEmpty(fileLines[Row][NomeStructIndex])
                            ? Project.Current.Get(fileLines[Row][PercorsoDriverCommIndex]).Find<IUAVariable>(fileLines[Row][NomeTagPlcIndex])
                            : Project.Current.Get(fileLines[Row][PercorsoDriverCommIndex]).Find(fileLines[Row][NomeStructIndex]).GetVariable(fileLines[Row][NomeTagPlcIndex]);

                        if (LinkVar != null)
                        {
                            if (string.IsNullOrEmpty(fileLines[Row][IndiceArrayIndex]))
                                ((IUAVariable)Nodo_VarRic).SetDynamicLink(LinkVar, DynamicLinkMode.ReadWrite);
                            else
                                ((IUAVariable)Nodo_VarRic).SetDynamicLink(LinkVar, Convert.ToUInt16(fileLines[Row][IndiceArrayIndex]), DynamicLinkMode.ReadWrite);     //Imposto il collegamento dinamico
                        }
                        else
                        {
                            string PlcTagPath = string.IsNullOrEmpty(fileLines[Row][NomeStructIndex]) ? fileLines[Row][NomeTagPlcIndex] : fileLines[Row][NomeStructIndex] + "/" + fileLines[Row][NomeTagPlcIndex];
                            Log.Error("Errore import variabili ricetta", "Impossibile creare collegamento dinamico. La variabile '" + fileLines[Row][PercorsoDriverCommIndex] + "/" + PlcTagPath + "' non è stata trovata!");
                        }
                    }

                }
            }
        }
        catch (NullReferenceException)
        {
            Log.Error("Errore import variabili ricetta", "Riferimento a oggetto nullo alla riga " + ++Row);
        }
        catch (Exception ex)
        {
            Log.Error("Errore import variabili ricetta", $"Non è stato possibile leggere il file CSV: {ex}");
        }
    }

    [ExportMethod]
    public void EsportaTag()
    {
        var csvPathVariable = LogicObject.GetVariable("CSVPath");
        if (csvPathVariable == null)
        {
            Log.Error("Errore export variabili ricetta", "Variabile CSVPath non trovata");
            return;
        }

        string csvPath = new ResourceUri(csvPathVariable.Value).Uri;

        if (string.IsNullOrEmpty(csvPath))
        {
            Log.Error("Errore export variabili ricetta", "Nessun file CSV trovato");
            return;
        }

        //controllo se il carattere separatore è valdo oppure no
        char? characterSeparator = CheckCharacterSeparator(",");
        if (characterSeparator == null || characterSeparator == '\0')
        {
            Log.Error("Errore export variabili ricetta", "Inserire un carattere separatore");
            return;
        }

        bool wrapFields = LogicObject.GetVariable("WrapFields").Value;     //indica che le colonne devono essere incapsualte tra doppi apici

        try
        {
            //Apro lo stream verso il file 
            using (var csvWriter = new CSVFileWriter(csvPath) { FieldDelimiter = characterSeparator.Value, WrapFields = wrapFields })
            {
                //Creo l'intestazione della prima riga                
                csvWriter.WriteLine(FileHeader);

                AggiornaRighe(Owner.NodeId, csvWriter, FileHeader.Length, "");
            }
            Log.Info("Export variabili", $"Variabili esportate nel file csv: {csvPath}");
        }
        catch (Exception ex)
        {
            Log.Error("Errore export variabili ricetta", $"Non è stato possibile creare il file CSV: {ex}");
        }
    }

    private void AggiornaRighe(NodeId NodeIdPadre, CSVFileWriter csvWriter, int NumColonne, string PercorsoPadre)
    {
        var row = new string[NumColonne];
        var NodoPadre = LogicObject.Context.GetNode(NodeIdPadre);

        foreach (var item in NodoPadre.GetNodesByType<IUAVariable>())
        {
            row[NomeTagHmiIndex] = item.BrowseName; //Assegno nome TagHmi
            row[TipoTagIndex] = item.GetType().ToString();   //Assegno il tipo di tag

            //Assegno i nomi alle eventuali strutture e sottostrutture
            var PathPadre = PercorsoPadre.Split('/');
            for (int i = 0; i < PathPadre.Length; i++)
                row[NomeObjectL1Index + i] = PathPadre[i];

            //Verifico se la tag ha un collegamento dinalico
            var DynLink = item.Children.GetVariable("DynamicLink");
            if (DynLink is not null)
            {
                var DynamicLinkString = DynLink.Value.Value.ToString().Replace("../", "").Split('/');

                row[PercorsoDriverCommIndex] = $"{DynamicLinkString[0]}/{DynamicLinkString[1]}/{DynamicLinkString[2]}";     //Percorso Driver Comm
                                                                                                                            //Project.Current.GetVariable(item.Children.GetVariable("DynamicLink").Value.Value.ToString()).Owner.Equals(FTOptix.CommunicationDriver.TagStructure);
                row[5] =
                row[NomeTagPlcIndex] = $"{DynamicLinkString[^1]}";
            }

            csvWriter.WriteLine(row);
        }

        foreach (var Struct in NodoPadre.GetNodesByType<IUAObject>().Where(StructLevel1 => StructLevel1.ObjectType.BrowseName.Equals("BaseObjectType")))
        {
            AggiornaRighe(Struct.NodeId, csvWriter, NumColonne, string.IsNullOrEmpty(PercorsoPadre) ? Struct.BrowseName : PercorsoPadre + "/" + Struct.BrowseName);
        }
    }
    private char? CheckCharacterSeparator(string separator)
    {
        return separator.Length != 1 || separator == string.Empty ? null : char.TryParse(separator, out char result) ? result : (char?)null;
    }
}
