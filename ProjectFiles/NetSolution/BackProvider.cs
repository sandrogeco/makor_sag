#region StandardUsing
using System;
using FTOptix.Core;
using System.Collections.Generic;
using FTOptix.CoreBase;
using FTOptix.HMIProject;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.OPCUAServer;
using FTOptix.OPCUAClient;
using FTOptix.ODBCStore;
using FTOptix.MelsecFX3U;
using FTOptix.S7TCP;
using FTOptix.S7TiaProfinet;
using FTOptix.MelsecQ;
using FTOptix.CODESYS;
using FTOptix.SQLiteStore;
using FTOptix.RAEtherNetIP;
using FTOptix.Recipe;
using FTOptix.System;
#endregion

public class BackProvider : FTOptix.NetLogic.BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        oldPanelStack = new Stack<NodeId>();

        var panelLoader = (PanelLoader)Owner;
        if (panelLoader == null)
            Log.Error("Panel loader not found");
        panelLoader.PanelVariable.VariableChange += PanelVariable_VariableChange;
    }

    private void PanelVariable_VariableChange(object sender, VariableChangeEventArgs e)
    {
        var oldPanel = LogicObject.Context.GetNode(e.OldValue);
        NodeId oldPanelNodeId = e.OldValue;
        oldPanelStack.Push(oldPanelNodeId);
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
        var panelLoader = (PanelLoader)Owner;
        if (panelLoader == null)
            Log.Error("Panel loader not found");

        panelLoader.PanelVariable.VariableChange -= PanelVariable_VariableChange;
    }

    [ExportMethod]
    public void Back()
    {
        var panelLoader = (PanelLoader)Owner;
        if (panelLoader == null)
            Log.Error("Panel loader not found");

        if (oldPanelStack.Count == 0)
            return;

        var panelNodeId = oldPanelStack.Pop();
        panelLoader.PanelVariable.VariableChange -= PanelVariable_VariableChange;
        panelLoader.ChangePanel(panelNodeId, NodeId.Empty);
        panelLoader.PanelVariable.VariableChange += PanelVariable_VariableChange;
    }

    private Stack<NodeId> oldPanelStack;
}
