using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using FTOptix.SQLiteStore;
using FTOptix.S7TCP;
using FTOptix.RAEtherNetIP;
using FTOptix.Recipe;
using FTOptix.System;
using FTOptix.S7TiaProfinet;

public class CSVFileReader : IDisposable
{
    public char FieldDelimiter { get; set; } = ',';

    public char QuoteChar { get; set; } = '"';

    public bool WrapFields { get; set; } = false;

    public bool IgnoreMalformedLines { get; set; } = false;

    public CSVFileReader(string filePath, System.Text.Encoding encoding)
    {
        streamReader = new StreamReader(filePath, encoding);
    }

    public CSVFileReader(string filePath)
    {
        streamReader = new StreamReader(filePath, System.Text.Encoding.UTF8);
    }

    public CSVFileReader(StreamReader streamReader)
    {
        this.streamReader = streamReader;
    }

    public bool EndOfFile()
    {
        return streamReader.EndOfStream;
    }

    public List<string> ReadLine()
    {
        if (EndOfFile())
            return null;

        var line = streamReader.ReadLine();

        var result = WrapFields ? ParseLineWrappingFields(line) : ParseLineWithoutWrappingFields(line);

        currentLineNumber++;
        return result;

    }

    public List<List<string>> ReadAll()
    {
        var result = new List<List<string>>();
        while (!EndOfFile())
            result.Add(ReadLine());

        return result;
    }

    private List<string> ParseLineWithoutWrappingFields(string line)
    {
        if (string.IsNullOrEmpty(line) && !IgnoreMalformedLines)
            throw new FormatException($"Error processing line {currentLineNumber}. Line cannot be empty");

        return line.Split(FieldDelimiter).ToList();
    }

    private List<string> ParseLineWrappingFields(string line)
    {
        var fields = new List<string>();
        var buffer = new StringBuilder("");
        var fieldParsing = false;

        int i = 0;
        while (i < line.Length)
        {
            if (!fieldParsing)
            {
                if (IsWhiteSpace(line, i))
                {
                    ++i;
                    continue;
                }

                // Line and column numbers must be 1-based for messages to user
                var lineErrorMessage = $"Error processing line {currentLineNumber}";
                if (i == 0)
                {
                    // A line must begin with the quotation mark
                    if (!IsQuoteChar(line, i))
                    {
                        if (IgnoreMalformedLines)
                            return null;
                        else
                            throw new FormatException($"{lineErrorMessage}. Expected quotation marks at column {i + 1}");
                    }

                    fieldParsing = true;
                }
                else
                {
                    if (IsQuoteChar(line, i))
                        fieldParsing = true;
                    else if (!IsFieldDelimiter(line, i))
                    {
                        if (IgnoreMalformedLines)
                            return null;
                        else
                            throw new FormatException($"{lineErrorMessage}. Wrong field delimiter at column {i + 1}");
                    }
                }

                ++i;
            }
            else
            {
                if (IsEscapedQuoteChar(line, i))
                {
                    i += 2;
                    buffer.Append(QuoteChar);
                }
                else if (IsQuoteChar(line, i))
                {
                    fields.Add(buffer.ToString());
                    buffer.Clear();
                    fieldParsing = false;
                    ++i;
                }
                else
                {
                    buffer.Append(line[i]);
                    ++i;
                }
            }
        }

        return fields;
    }

    private bool IsEscapedQuoteChar(string line, int i)
    {
        return line[i] == QuoteChar && i != line.Length - 1 && line[i + 1] == QuoteChar;
    }

    private bool IsQuoteChar(string line, int i)
    {
        return line[i] == QuoteChar;
    }

    private bool IsFieldDelimiter(string line, int i)
    {
        return line[i] == FieldDelimiter;
    }

    private bool IsWhiteSpace(string line, int i)
    {
        return Char.IsWhiteSpace(line[i]);
    }

    private StreamReader streamReader;
    private int currentLineNumber = 1;

    #region IDisposable support
    private bool disposed = false;
    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
            return;

        if (disposing)
            streamReader.Dispose();

        disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
    }
    #endregion
}

