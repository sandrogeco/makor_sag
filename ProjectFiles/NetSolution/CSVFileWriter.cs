using System;
using System.Text;
using System.IO;
using FTOptix.SQLiteStore;
using FTOptix.S7TCP;
using FTOptix.RAEtherNetIP;
using FTOptix.Recipe;

public class CSVFileWriter : IDisposable
{
    public char FieldDelimiter { get; set; } = ',';

    public char QuoteChar { get; set; } = '"';

    public bool WrapFields { get; set; } = false;

    public CSVFileWriter(string filePath)
    {
        streamWriter = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);
    }

    public CSVFileWriter(string filePath, System.Text.Encoding encoding)
    {
        streamWriter = new StreamWriter(filePath, false, encoding);
    }

    public CSVFileWriter(StreamWriter streamWriter)
    {
        this.streamWriter = streamWriter;
    }

    public void WriteLine(string[] fields)
    {
        var stringBuilder = new StringBuilder();

        for (var i = 0; i < fields.Length; ++i)
        {
            if (WrapFields)
                stringBuilder.AppendFormat("{0}{1}{0}", QuoteChar, EscapeField(fields[i]));
            else
                stringBuilder.AppendFormat("{0}", fields[i]);

            if (i != fields.Length - 1)
                stringBuilder.Append(FieldDelimiter);
        }

        streamWriter.WriteLine(stringBuilder.ToString());
        streamWriter.Flush();
    }

    private string EscapeField(string field)
    {
        var quoteCharString = QuoteChar.ToString();
        return field.Replace(quoteCharString, quoteCharString + quoteCharString);
    }

    private StreamWriter streamWriter;

    #region IDisposable Support
    private bool disposed = false;
    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
            return;

        if (disposing)
            streamWriter.Dispose();

        disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
    }
    #endregion
}

