#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.Core;
using FTOptix.CoreBase;
using FTOptix.NetLogic;
using FTOptix.S7TiaProfinet;
#endregion

public class RuntimeNetLogic1 : FTOptix.NetLogic.BaseNetLogic
{
    [ExportMethod]
    public void SetLabelTextToHelloWord(NodeId labelNodeId)
    {
        var label = InformationModel.Get<Label>(labelNodeId);
        label.Text = "Changed label text";
    }
}
