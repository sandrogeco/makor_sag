#region Using directives
using System;
using System.Globalization;
using System.IO;
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.S7TiaProfinet;
using FTOptix.Store;
using FTOptix.System;
using UAManagedCore;
#endregion

public class varToFromCSV : BaseNetLogic
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
		String path = new ResourceUri(LogicObject.GetVariable("CSVpath").Value).Uri;
		if (!path.Contains(".csv"))
			path += ".csv";

		var xGraphNode = Project.Current.GetVariable("Model/Profilo/xyGraph");
		var nPuntiNode = Project.Current.GetVariable("Model/VariabiliRicetta/nPunti");
		int nPunti = nPuntiNode.Value;

		if (xGraphNode?.Value?.Value is float[,] xyGraph)
		{
			using (StreamWriter sw = File.CreateText(path))
			{
				sw.WriteLine("X\tY");
				for (int i = 0; i < nPunti && i < xyGraph.GetLength(0); i++)
				{
					float x = xyGraph[i, 0] / 1000f;
					float y = xyGraph[i, 1] / 1000f;
					sw.WriteLine($"{x.ToString(CultureInfo.InvariantCulture)}\t{y.ToString(CultureInfo.InvariantCulture)}");
				}
			}
		}
	}
    [ExportMethod]
    public void FromCSV()
    {
            String path = new ResourceUri(LogicObject.GetVariable("CSVpath").Value).Uri;
            var csvName = Project.Current.GetVariable("Model/VariabiliRicetta/nomeFilePunti");
        
            csvName.Value = new UAValue(path);
            File.Copy(path, "c:\\tmp\\actualCSV.csv", overwrite: true);
            var lines = File.ReadAllLines(path);

            // Recupero il nodo variabile (oggetto nodo) invece di prendere direttamente il suo UAValue
            var xGraphNode = Project.Current.GetVariable("Model/Profilo/xyGraph");
            var nPuntiNode = Project.Current.GetVariable("Model/VariabiliRicetta/nPunti");
            nPuntiNode.Value = new UAValue(lines.Length-1);

            // Verifico che il nodo e il suo valore contengano una matrice float[,]
            if (xGraphNode?.Value?.Value is float[,] xyGraph)
            {
                for (int i = 1; i < lines.Length; i++)
                {
                    var sep = lines[i].Contains('\t') ? '\t' : ','; // tenta a riconoscere il separatore
                    var cols = lines[i].Split(sep);
                    if (cols.Length < 2) continue;

                    if (float.TryParse(cols[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v0) &&
                        float.TryParse(cols[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v1))
                    {
                        if (i - 1 < xyGraph.GetLength(0))
                        {
                        
                            xyGraph[i - 1, 0] = v0*1000;
                            xyGraph[i - 1, 1] = v1*1000;
                            var PointNodeX = Project.Current.GetVariable("Model/Profilo/Points/"+i.ToString()+"/x");
                            var PointNodeY = Project.Current.GetVariable("Model/Profilo/Points/" + i.ToString() + "/y");
                            PointNodeX.Value = new UAValue(xyGraph[i - 1, 0]);
                            PointNodeY.Value = new UAValue(xyGraph[i - 1, 1]);
                        }
                    }
                    else
                    {
                        // loggare/rilevare riga malformata
                    }
                }

                // NON assegnare a UAValue.Value (proprietà read-only).
                // Assegnare un nuovo UAValue contenente la matrice al Value del nodo variabile.
                xGraphNode.Value= new UAValue(xyGraph);
            }
            else
            {
                // loggare: variabile xGraph mancante o tipo diverso
            }
        }

    [ExportMethod]
    public void FromCSVDaRcp()
    {
            var csvName = Project.Current.GetVariable("Model/VariabiliRicetta/nomeFilePunti");
            String path = csvName.Value;
            File.Copy(path, "c:\\tmp\\actualCSV.csv", overwrite: true);
            var lines = File.ReadAllLines(path);

            // Recupero il nodo variabile (oggetto nodo) invece di prendere direttamente il suo UAValue
            var xGraphNode = Project.Current.GetVariable("Model/Profilo/xyGraph");
            var nPuntiNode = Project.Current.GetVariable("Model/VariabiliRicetta/nPunti");
            nPuntiNode.Value = new UAValue(lines.Length-1);

            // Verifico che il nodo e il suo valore contengano una matrice float[,]
            if (xGraphNode?.Value?.Value is float[,] xyGraph)
            {
                for (int i = 1; i < lines.Length; i++)
                {
                    var sep = lines[i].Contains('\t') ? '\t' : ','; // tenta a riconoscere il separatore
                    var cols = lines[i].Split(sep);
                    if (cols.Length < 2) continue;

                    if (float.TryParse(cols[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v0) &&
                        float.TryParse(cols[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v1))
                    {
                        if (i - 1 < xyGraph.GetLength(0))
                        {
                        
                            xyGraph[i - 1, 0] = v0*1000;
                            xyGraph[i - 1, 1] = v1*1000;
                            var PointNodeX = Project.Current.GetVariable("Model/Profilo/Points/"+i.ToString()+"/x");
                            var PointNodeY = Project.Current.GetVariable("Model/Profilo/Points/" + i.ToString() + "/y");
                            PointNodeX.Value = new UAValue(xyGraph[i - 1, 0]);
                            PointNodeY.Value = new UAValue(xyGraph[i - 1, 1]);
                        }
                    }
                    else
                    {
                        // loggare/rilevare riga malformata
                    }
                }

                // NON assegnare a UAValue.Value (proprietà read-only).
                // Assegnare un nuovo UAValue contenente la matrice al Value del nodo variabile.
                xGraphNode.Value= new UAValue(xyGraph);
            }
            else
            {
                // loggare: variabile xGraph mancante o tipo diverso
            }
        }
}
