#region Using directives
using System;
using FTOptix.Core;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.S7TiaProfinet;
using FTOptix.CoreBase;
using FTOptix.Alarm;
using FTOptix.UI;
using FTOptix.CommunicationDriver;
using FTOptix.NetLogic;
using FTOptix.EventLogger;
using FTOptix.ODBCStore;
using FTOptix.Store;
using FTOptix.OPCUAClient;
using FTOptix.Retentivity;
using FTOptix.MelsecQ;
using FTOptix.OPCUAServer;
using FTOptix.HMIProject;
using FTOptix.NativeUI;
using FTOptix.MelsecFX3U;
using FTOptix.CODESYS;
using FTOptix.SQLiteStore;
using FTOptix.S7TCP;
using FTOptix.RAEtherNetIP;
using FTOptix.Recipe;
using FTOptix.System;
#endregion

public class SpostaOggetti_Logic : BaseNetLogic
{
    [ExportMethod] 
    public void SpostaSu()
    {
        NodeId NodoDaSpostare = LogicObject.GetVariable("NodoDaSpostare").Value;
        var NodoDaSpostareSu = LogicObject.Context.GetNode(NodoDaSpostare);
        NodoDaSpostareSu.MoveUp();
        Log.Info("Nodo: " + NodoDaSpostareSu.BrowseName + " spostato di una posizione");
    }

    [ExportMethod]
    public void SpostaGiu()
    {
        NodeId NodoDaSpostare = LogicObject.GetVariable("NodoDaSpostare").Value;
        var NodoDaSpostareGiu = LogicObject.Context.GetNode(NodoDaSpostare);
        NodoDaSpostareGiu.MoveDown();
        Log.Info("Nodo: " + NodoDaSpostareGiu.BrowseName + " spostato di una posizione");
    }
}
