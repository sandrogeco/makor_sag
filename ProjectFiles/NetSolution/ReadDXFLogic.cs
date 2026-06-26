#region Using directives
using System;
using System.Collections.Generic;
using UAManagedCore;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.S7TiaProfinet;
using FTOptix.S7TCP;
#endregion

public class ReadDXFLogic : BaseNetLogic
{
    [ExportMethod]
    public void CaricaDXF()
    {
        IUAVariable statusVar = null;
        try
        {
            statusVar = Project.Current.GetVariable("Model/StatusMessage");
        }
        catch { }

        try
        {
            string filePath = @"C:\Users\Utente\Downloads\prova.dxf";

            if (statusVar != null)
                statusVar.Value = "Caricamento DXF...";

            Log.Info("=== INIZIO DEBUG DXF ===");
            Log.Info("File: " + filePath);

            // Leggi il file
            string[] lines = System.IO.File.ReadAllLines(filePath);
            Log.Info("Righe totali nel file: " + lines.Length);

            // Mostra le prime 50 righe per vedere la struttura
            Log.Info("--- Prime 50 righe del DXF ---");
            for (int i = 0; i < Math.Min(50, lines.Length); i++)
            {
                Log.Info("Riga " + i + ": " + lines[i]);
            }

            List<double> xPoints = new List<double>();
            List<double> yPoints = new List<double>();

            int lineCount = 0;
            int polyCount = 0;

            // Cerca le entità LINE
            for (int i = 0; i < lines.Length - 1; i++)
            {
                string line = lines[i].Trim();

                // LINE entity
                if (line == "LINE")
                {
                    lineCount++;
                    Log.Info("Trovata LINE alla riga " + i);

                    double x1 = 0, y1 = 0, x2 = 0, y2 = 0;

                    for (int j = i; j < i + 50 && j < lines.Length - 1; j++)
                    {
                        if (lines[j].Trim() == "10")
                        {
                            double.TryParse(lines[j + 1].Trim(), out x1);
                            Log.Info("  X1 = " + x1);
                        }
                        if (lines[j].Trim() == "20")
                        {
                            double.TryParse(lines[j + 1].Trim(), out y1);
                            Log.Info("  Y1 = " + y1);
                        }
                        if (lines[j].Trim() == "11")
                        {
                            double.TryParse(lines[j + 1].Trim(), out x2);
                            Log.Info("  X2 = " + x2);
                        }
                        if (lines[j].Trim() == "21")
                        {
                            double.TryParse(lines[j + 1].Trim(), out y2);
                            Log.Info("  Y2 = " + y2);
                        }
                    }

                    xPoints.Add(x1);
                    yPoints.Add(y1);
                    xPoints.Add(x2);
                    yPoints.Add(y2);
                }

                // LWPOLYLINE entity
                if (line == "LWPOLYLINE")
                {
                    polyCount++;
                    Log.Info("Trovata LWPOLYLINE alla riga " + i);

                    for (int j = i; j < lines.Length - 1; j++)
                    {
                        if (lines[j].Trim() == "0" && j > i + 5)
                            break;

                        if (lines[j].Trim() == "10")
                        {
                            double x, y;
                            if (double.TryParse(lines[j + 1].Trim(), out x))
                            {
                                if (j + 2 < lines.Length && lines[j + 2].Trim() == "20")
                                {
                                    if (double.TryParse(lines[j + 3].Trim(), out y))
                                    {
                                        Log.Info("  Punto: X=" + x + ", Y=" + y);
                                        xPoints.Add(x);
                                        yPoints.Add(y);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Log.Info("LINE trovate: " + lineCount);
            Log.Info("LWPOLYLINE trovate: " + polyCount);
            Log.Info("Punti totali estratti: " + xPoints.Count);

            var xArray = Project.Current.GetVariable("Model/XPoints");
            var yArray = Project.Current.GetVariable("Model/YPoints");

            xArray.Value = xPoints.ToArray();
            yArray.Value = yPoints.ToArray();

            string successMsg = "Caricati " + xPoints.Count + " punti! (LINE:" + lineCount + " POLY:" + polyCount + ")";

            if (statusVar != null)
                statusVar.Value = successMsg;

            Log.Info(successMsg);
            Log.Info("=== FINE DEBUG ===");
        }
        catch (Exception ex)
        {
            string errorMsg = "ERRORE: " + ex.Message;

            if (statusVar != null)
                statusVar.Value = errorMsg;

            Log.Error("Errore: " + ex.Message);
            Log.Error("Stack trace: " + ex.StackTrace);
        }
    }
}
