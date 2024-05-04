#region Using directives
using FTOptix.Core;
using FTOptix.NetLogic;
using FTOptix.SQLiteStore;
using System;
using System.IO;
using UAManagedCore;
using FTOptix.Recipe;
#endregion

public class GestBackupSqlLite_R0 : BaseNetLogic
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
        if ((DateTime.Now - Owner.GetVariable("DataUltimoSalvataggio").Value).Days > 0)
            BackupDB();
    }

    [ExportMethod]
    public void BackupDB()
    {
        var NumBack = Owner.GetVariable("NumBackup");
        if (NumBack.Value < 0 || NumBack.Value > 30)
            NumBack.Value = 1;

        string PathDbSavingFolder;            // percorso di salvataggio backup

        if (Owner.GetVariable("TargetLinux").Value)
            PathDbSavingFolder = new ResourceUri("%USB1%").Uri;
        else
            PathDbSavingFolder = Owner.GetVariable("PathDbSavingFolder").Value;

        if (!Directory.Exists(PathDbSavingFolder))
            Directory.CreateDirectory(PathDbSavingFolder);

        ResourceUri NomeFIleDaSalva = ResourceUri.FromAbsoluteFilePath(PathDbSavingFolder + "/BackupDB_" + NumBack.Value + ".sqlite");

        ((SQLiteStore)Owner).Backup(NomeFIleDaSalva);   //eseguo il backup

        NumBack.Value++;
        Owner.GetVariable("DataUltimoSalvataggio").Value = DateTime.Now;
    }
}
