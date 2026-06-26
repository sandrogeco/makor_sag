#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.WebUI;
using FTOptix.Alarm;
using FTOptix.CODESYS;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.OPCUAServer;
using FTOptix.OPCUAClient;
using FTOptix.Retentivity;
using FTOptix.EventLogger;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using System.Collections.Generic;
using FTOptix.System;
using FTOptix.S7TiaProfinet;
using FTOptix.S7TCP;
#endregion

public class ResetAllCnt : BaseNetLogic
{
    /// <summary>
    ///     Resetta tutti i contatori presenti nel progetto.
    /// </summary>
    [ExportMethod]
    public void ResetContatori()
    {
        LogicObject.GetVariable("InProgress").Value = true;

        var communicationDriver = Project.Current.GetObject("CommDrivers/CODESYSDriver1/PLC_Next/Tags");
        List<RemoteVariableValue> tags = new List<RemoteVariableValue>();

        foreach (var tagStruct in communicationDriver.GetNodesByType<TagStructure>())
        {
            FindCntTags(tagStruct);
        }

        InformationModel.RemoteWrite(tags);
        LogicObject.GetVariable("InProgress").Value = false;

        void FindCntTags(TagStructure tagStruct)
        {
            foreach (var tag in tagStruct.GetNodesByType<IUAVariable>())
            {
                if (tag.VariableType.ToString() == "FTOptix.CommunicationDriver.TagStructureType")
                {
                    continue;
                }
                if (tag.BrowseName == "bRstCnt")
                {
                    tags.Add(new RemoteVariableValue(tag, true));
                    //Log.Info("ResetAllCnt", "Found tag: " + MakeBrowsePath(tag));
                }
            }
            foreach (var childStruct in tagStruct.GetNodesByType<TagStructure>())
            {
                FindCntTags(childStruct);
            }
        }
    }


    /// <summary>
    /// 	Funzione che dato un nodo mi restituisce il percorso
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    private static string MakeBrowsePath(IUANode node)
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
