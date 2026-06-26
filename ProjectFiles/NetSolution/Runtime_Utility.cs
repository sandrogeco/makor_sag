#region Using directives
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using UAManagedCore;
using FTOptix.Recipe;
using FTOptix.System;
using FTOptix.S7TiaProfinet;
using FTOptix.S7TCP;
#endregion

public class Runtime_Utility : BaseNetLogic
{
    /// <summary>
    /// Trasforma array di bool passato in ingresso in un intero a 32 bit
    /// </summary>
    /// <param name="boolArray"></param>
    /// <returns></returns>
    public static int GetInt32FromBitArray(bool[] boolArray)
    {
        BitArray arr = new(boolArray);
        byte[] array = new byte[4];
        arr.CopyTo(array, 0);
        return BitConverter.ToInt32(array, 0);
    }

    public static Int16 GetInt16FromBitArray(bool[] bitArray)
    {
        byte[] data = new byte[2];
        new BitArray(bitArray).CopyTo(data, 0);

        return BitConverter.ToInt16(data, 0);
    }

    /// <summary>
    /// Trasforma array di bool passato in ingresso in un intero a 16 bit
    /// </summary>
    /// <param name="bitArray"></param>
    /// <returns></returns>
    public static string GetStringFromBitArray(bool[] bitArray)
    {
        string Res;
        BitArray arr = new(bitArray);

        switch (bitArray.Length)
        {
            case 16:
                {
                    byte[] data = new byte[2];
                    arr.CopyTo(data, 0);

                    Res = BitConverter.ToUInt16(data, 0).ToString();
                    break;
                }

            default:
                {
                    byte[] data = new byte[4];
                    arr.CopyTo(data, 0);

                    Res = BitConverter.ToUInt32(data, 0).ToString();
                    break;
                }
        }
        return Res;
    }

    /// <summary>
    /// Trasforma il valore intero passato in ingresso in array di bool con dimensione specificata in ingresso
    /// </summary>
    /// <param name="val">Valore integer</param>
    /// <param name="length">Required bool array length</param>
    /// <returns></returns>
    public static bool[] GetBoolArrFromString(int val, int length)
    {
        bool[] res = new bool[length];

        string binaryString = Convert.ToString(val, 2);
        int arrLen = binaryString.Length;

        for (int j = 0; j < arrLen; j++)
            res[j] = (binaryString[arrLen - j - 1] == '1');

        return res;
    }

    /// <summary>
    /// Restituisce il nome del folder a partire dalla sua path
    /// </summary>
    /// <param name="pathFolder">Path del folder</param>
    /// <returns></returns>
    public static string GetFolderName(string pathFolder) => pathFolder.Remove(0, pathFolder.LastIndexOf(Path.DirectorySeparatorChar) + 1);

    /// <summary>
    /// Crea una directory in subFolderNameDest all'interno di pathFolderDest e vi scompatta l'archivio zip pathSubFolderFrom 
    /// </summary>
    /// <param name="pathFolderDest">Path del folder di destinazione del subfolder</param>
    /// <param name="pathSubFolderFrom">Path del folder di origine del file zip</param>
    /// <param name="subFolderNameDest">Path del folder destinazione della copia</param>
    public static void UnzipToDirectory(string pathFolderDest, string pathSubFolderFrom, string subFolderNameDest)
    {
        var subFolderDestPath = Path.Combine(pathFolderDest, subFolderNameDest);

        // se esiste già una directory con questo nome, prima la elimina
        if (Directory.Exists(subFolderDestPath))
            Directory.Delete(subFolderDestPath, true);

        Directory.CreateDirectory(subFolderDestPath);
        ZipFile.ExtractToDirectory(pathSubFolderFrom, subFolderDestPath);
    }

    public static void ZipDirectory(string pathFileDest, string pathFolderOrigin)
    {
        try
        {
            if (Directory.Exists(pathFolderOrigin))
                ZipFile.CreateFromDirectory(pathFolderOrigin, pathFileDest);

        }
        catch (Exception ex)
        {
            Log.Error("File comression", "Zip File creation error: " + ex.ToString());
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pathFolderDest">Percorso cartella destinazione</param>
    /// <param name="filename">Percorso file da copiare</param>
    public static void CopiaFile(string pathFolderDest, string filename)
    {
        string pathFolderWithoutDot = filename.Substring(0, filename.Length);
        string nameDest = GetFolderName(pathFolderWithoutDot);
        File.Copy(filename, pathFolderDest + "/" + nameDest, true);
    }

    /// <summary>
    /// Copia un folder da pathSubFolderFrom in subFolderNameDest all'interno di pathFolderDest 
    /// </summary>
    /// <param name="pathFolderDest">Path del folder di destinazione del subfolder</param>
    /// <param name="pathSubFolderFrom">Path del folder di origine</param>
    /// <param name="subFolderNameDest">Path del folder destinazione della copia</param>
    public static void CopiaSubFolder(string pathFolderDest, string pathSubFolderFrom, string subFolderNameDest)
    {
        var subFolderDestPath = Path.Combine(pathFolderDest, subFolderNameDest);

        Directory.CreateDirectory(subFolderDestPath);

        //copia tutte le eventuali subdirectories
        foreach (string dirPath in Directory.GetDirectories(pathSubFolderFrom, "*",
            SearchOption.AllDirectories))
            Directory.CreateDirectory(dirPath.Replace(pathSubFolderFrom, subFolderDestPath));

        //copia tutti i file, anche delle subdirectories
        foreach (string newPath in Directory.GetFiles(pathSubFolderFrom, "*.*",
            SearchOption.AllDirectories))
            File.Copy(newPath, newPath.Replace(pathSubFolderFrom, subFolderDestPath), false);
    }

    /// <summary>
    /// Restituisce true se il file esiste alla path del folder
    /// </summary>
    /// <param name="fileName">Nome del file</param>
    /// <param name="pathFolder">Path del folder</param>
    /// <returns></returns>
    public static bool ExistsFileInFolder(string fileName, string pathFolder) => File.Exists(Path.Combine(pathFolder, fileName));


    /// <summary>
    /// Funzione che apre il pop-up conferma operatore assegnando l'alias
    /// </summary>
    /// <param name="UiSorce">Nodo dell'oggetto UI sul quale aprire il pop-up</param>    
    /// <param name="AliasNode">Il nodo dell'oggetto da passare come alias</param>
    /// <param name="DialogBox">Opzionale. NodeId della finsetra di dialogo da aprire.</param>
    public static void ConfermaUser(IUANode UiSorce, IUANode AliasNode, IUANode DialogBox = null)
    {
        DialogType DiallogConferma = DialogBox is null ? (DialogType)Project.Current.Get("UI/OggettiTemplate/Pannelli/ConfirmationDialog_R0_1") : (DialogType)DialogBox;
        DiallogConferma.SetAlias("ConfirmationDialogContext", AliasNode);    // imposto l'alias       
        _ = UICommands.OpenDialog(UiSorce, DiallogConferma);
    }

    /// <summary>
    /// Questo metodo controlla se la stringa passata in ingresso è un carattere singolo oppure no
    /// </summary>
    /// <param name="separator"></param>
    /// <returns>null se la separator è di lunghezza diversa da 1 altrimenti ritorna il carattere</returns>
    public static char? CheckCharacterSeparator(string separator)
    {
        //if (separator.Length != 1 || separator == string.Empty)
        //    return null;

        //if (char.TryParse(separator, out char result))
        //    return result;

        //return null;

        return separator == string.Empty ? null : separator == "tab" ? '\t' : char.TryParse(separator, out char result) ? result : null;
    }
}
