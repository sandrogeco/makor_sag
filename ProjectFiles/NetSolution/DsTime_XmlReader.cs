#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.NativeUI;
using FTOptix.HMIProject;
using FTOptix.UI;
using FTOptix.Core;
using FTOptix.CoreBase;
using FTOptix.NetLogic;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using FTOptix.SQLiteStore;
using FTOptix.S7TCP;
using FTOptix.RAEtherNetIP;
using FTOptix.Recipe;
#endregion

public class DsTime_XmlReader : BaseNetLogic
{
    [ExportMethod]
    public void ParseXmlFile()
    {
        //recupero il file XML da leggere 
        var csvPathVariable = LogicObject.GetVariable("Path");
        string myString = new ResourceUri(csvPathVariable.Value).Uri;
        Log.Info("Parsing Xml file: " + myString);
        //chiamo il metodo per la lettura del file XML
        ReadAllNodes(myString);
    }

    private void ReadAllNodes(string filePath)
    {
        XmlDocument xmlDocument = new();
        xmlDocument.Load(filePath);

        //xmlDocument.DocumentElement.ChildNodes ritorna l'elenco dei nodi figli della radice 
        //per ognuno dei nodi figli chiamo il metodo ReadChildNodes che legge i dati realitiv al nodo
        //con cui chiamo il metodo e ricorsivamente esegue la stessa operazione su tutti i suoi eventuali figli
        foreach (var item in from XmlNode item in xmlDocument.DocumentElement["NodeList"].FirstChild.ChildNodes
                             where item.Attributes["name"].InnerText.Contains("All")
                             select item)
        {
            ReadChildNodes(item, "");
        }
    }

    private void ReadChildNodes(XmlNode xmlNode, string strPath)
    {
        if (xmlNode.Name != "#text")
        {
            strPath = xmlNode.Attributes["name"] == null
                ? strPath + " - Commento: " + xmlNode.ChildNodes[0].InnerText
                : strPath + " - Node: " + xmlNode.Name + " name: " + xmlNode.Attributes["name"].InnerText;

            Log.Info(strPath);

            foreach (XmlNode item in xmlNode.ChildNodes)
                ReadChildNodes(item, strPath);            
        }
    }
}
