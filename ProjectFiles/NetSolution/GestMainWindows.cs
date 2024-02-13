#region Using directives
using System;
using FTOptix.Core;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.CoreBase;
using FTOptix.Store;
using FTOptix.HMIProject;
using FTOptix.SQLiteStore;
using FTOptix.OPCUAServer;
using FTOptix.NativeUI;
using FTOptix.Alarm;
using FTOptix.ODBCStore;
#endregion

public class GestMainWindows : BaseNetLogic
{
    public override void Start()
    {
        //Controllo se eseiste la variabile che identifica il tipo di sessione
        var isNativeUI = Session.GetVariable("IsNativeUI");
        //if (isNativeUI == null)        
        //    Session.Add(InformationModel.MakeVariable("IsNativeUI", OpcUa.DataTypes.Boolean));        

        var presentationEngine = FindPresentationEngine();
        if (presentationEngine != null)
            isNativeUI.Value = presentationEngine.IsInstanceOf(FTOptix.NativeUI.ObjectTypes.NativeUIPresentationEngine);
    }

    IUAObject FindPresentationEngine()
    {
        IUANode currentNode = Session;
        while (true)
        {
            if (currentNode == null)
                return null;

            var currentObject = (IUAObject)currentNode;
            if (currentObject != null && currentObject.IsInstanceOf(FTOptix.UI.ObjectTypes.PresentationEngine))
                return currentObject;

            currentNode = currentNode.Owner;
        }
    }
}
