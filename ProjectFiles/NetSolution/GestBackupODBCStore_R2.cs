#region Using directives
using FTOptix.Core;
using FTOptix.NetLogic;
using System;
using System.Diagnostics;
using System.IO;
using UAManagedCore;
using FTOptix.Recipe;
using FTOptix.System;
using FTOptix.S7TiaProfinet;
using FTOptix.S7TCP;
#endregion

public class GestBackupODBCStore_R2 : BaseNetLogic
{
    private DelayedTask TaskAvvioBackup;
    private PeriodicTask TaskBackupCambioGiorno;
    public override void Start()
    {
        if (Owner.GetVariable("EseguiBackupAvvio").Value)
        {
            TaskAvvioBackup = new DelayedTask(AvvioBackup, 10000, LogicObject);     //Avvio backup dopo 10 secondi
            TaskAvvioBackup.Start();
        }
    }

    public override void Stop()
    {
        TaskBackupCambioGiorno?.Dispose();
        TaskBackupCambioGiorno = null;
    }

    private void AvvioBackup()
    {
        BackupDB();

        TaskAvvioBackup.Dispose();
        TaskAvvioBackup = null;

        TaskBackupCambioGiorno = new PeriodicTask(CheckGiorno, 900000, LogicObject);
        TaskBackupCambioGiorno.Start();
    }

    private void CheckGiorno()
    {
        if ((DateTime.Now.Date - ((DateTime)Owner.GetVariable("DataUltimoSalvataggio").Value).Date).Days != 0)
            BackupDB();
    }

    /// <summary>
    /// Esegui il backup d'avvio del database della macchina
    /// </summary>
    /// <param name="BatFileName">Nome del file .bat</param>
    /// <param name="SavePath">Percroso della cartella dove verrà salvato il backup</param>
    /// <param name="SQL_InstanceName">Nome dell'istanza di SQL</param>
    /// <param name="DbName"></param>
    [ExportMethod]
    //public static void BackupDB(string BatFileName, string SavePath, string SQL_InstanceName, string DbName)
    public void BackupDB()
    {
        // Lettura dati da passare al file batch              
        var MyStore = ((MakorODBCType)Owner);

        // Con FromProjectRelativePath vado a cercare nella cartella "ProjectFiles"
        string BatPath = ResourceUri.FromProjectRelativePath("_MakorUtility/" + MyStore.DbBackupBatFileName).Uri;          // la proprietà URi contiene il percorso assoluto (formato URI) del file/cartella ricercata 

        if (string.IsNullOrEmpty(BatPath))
        {
            Log.Error("Errore backup Database", "File batch non trovato");
            return;
        }

        var NumBack = Owner.GetVariable("NumBackup");
        if (NumBack.Value < 0 || NumBack.Value > 30)
            NumBack.Value = 1;

        string PathDbSavingFolder;            // percorso di salvataggio backup

        if (Owner.GetVariable("TargetLinux").Value)
            PathDbSavingFolder = new ResourceUri("%USB1%").Uri;
        else
            PathDbSavingFolder = Owner.GetVariable("PathDbSavingFolder").Value;        // percorso di salvataggio backup

        if (!Directory.Exists(PathDbSavingFolder))
            Directory.CreateDirectory(PathDbSavingFolder);

        string NomeFIleDaSalva = PathDbSavingFolder + "\\BackupDB_" + MyStore.Database + "_" + NumBack.Value + ".bak";

        Backup_Restore(BatPath, MyStore.NomeIstanzaSQL, MyStore.Database, NomeFIleDaSalva, MyStore.Username, MyStore.Password);   //eseguo il backup

        NumBack.Value++;
        Owner.GetVariable("DataUltimoSalvataggio").Value = DateTime.Now;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="SavePath">Percorso dove salvare il file di backup</param>
    /// <param name="FileName">Nome da dare al file di backup</param>
    [ExportMethod]
    public void BackupManuale(string SavePath, string FileName)
    {
        // Lettura dati da passare al file batch              
        var MyStore = ((MakorODBCType)Owner);

        // Con FromProjectRelativePath vado a cercare nella cartella "ProjectFiles"
        string BatPath = ResourceUri.FromProjectRelativePath("_MakorUtility/" + MyStore.DbBackupBatFileName).Uri;          // la proprietà URi contiene il percorso assoluto (formato URI) del file/cartella ricercata 

        if (string.IsNullOrEmpty(BatPath))
        {
            Log.Error("Errore backup Database", "File batch non trovato");
            return;
        }

        if (!FileName.Contains(".bak"))
            FileName += ".bak";

        string NomeFIleDaSalva = new ResourceUri(SavePath).Uri;
        NomeFIleDaSalva = NomeFIleDaSalva + "\\" + FileName;

        Backup_Restore(BatPath, MyStore.NomeIstanzaSQL, MyStore.Database, NomeFIleDaSalva, MyStore.Username, MyStore.Password);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="RestoreFilePath">Percorso del file di restore</param>
    [ExportMethod]
    public void RestoreManuale(string RestoreFilePath)
    {
        // Lettura dati da passare al file batch              
        var MyStore = (MakorODBCType)Owner;

        // Con FromProjectRelativePath vado a cercare nella cartella "ProjectFiles"
        string BatPath = ResourceUri.FromProjectRelativePath("_MakorUtility/" + MyStore.DbRestoreBatFileName).Uri;          // la proprietà URi contiene il percorso assoluto (formato URI) del file/cartella ricercata 

        if (string.IsNullOrEmpty(BatPath))
        {
            Log.Error("Errore restore Database", "File batch non trovato");
            return;
        }

        string NomeFIleDaSalva = new ResourceUri(RestoreFilePath).Uri;
        Backup_Restore(BatPath, MyStore.NomeIstanzaSQL, MyStore.Database, NomeFIleDaSalva, MyStore.Username, MyStore.Password);
    }

    /// <summary>
    /// A seconda della scelta esegue il file .bat per fare il backup o il restore del DB 
    /// </summary>
    /// <param name="BatPath">Nome del file bat che eseguirà il backup o il resotre (compreso di percorso)</param>
    /// <param name="SQL_InstanceName">Nome istanza SQL</param>
    /// <param name="DbName">Nome del databse</param>
    /// <param name="SavePath">Nome del file dove fare il backup oppure da dove fare il resore (Compreso di perscorsco e l'estensione .bak)
    /// <param name="UserName">Nome utente per backup database</param>
    /// <param name="UserPass">Password utente per backup</param>
    /// </param>    
    public static void Backup_Restore(string BatPath, string SQL_InstanceName, string DbName, string SavePath, string UserName, string UserPass)
    {
        //Cero il processo per avviare la powershell alla power shell va indicato il percorso del file da eseguire. Il file richiede dei parametri in ingresso quindi vanno passati anche questi.
        Process Batch = new();
        Batch.StartInfo.FileName = "cmd.exe";
        //Per il backup al file .bat vanno passati dei parametri il primo è iil nome dell'istanza sql, il 2° è il nome utente, il 3° è la password, il quarto è il nome del db e il 5° è il nome del file da salvare con il relativo percorso dove salvarlo
        //Per il Restore al file .bat vanno passati dei parametri il primo è il nome dell'istanza sql, il 2° è il nome utente, il 3° è la password, il quarto è il nome del db e il 5° è il nome del file si restore con il relativo percorso 
        Batch.StartInfo.Arguments = "/C " + BatPath + " " + SQL_InstanceName + " " + UserName + " " + UserPass + " " + DbName + " " + SavePath;
        Batch.Start();
    }
}
