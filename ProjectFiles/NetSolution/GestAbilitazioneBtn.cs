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
using FTOptix.System;
using FTOptix.S7TiaProfinet;
#endregion

public class GestAbilitazioneBtn : BaseNetLogic
{
    DataGrid dataGridItem;
    [ExportMethod]
    public void EnbBtnImport(NodeId dataGrid)
    {
        dataGridItem = InformationModel.Get<DataGrid>(dataGrid);

        // Get the UI Selected Item variable (an object)
        var selectedRow = InformationModel.Get(dataGridItem.UISelectedItem);
        if (selectedRow == null)
        {
            Owner.Get<Button>("ButtonsBar/Import").Enabled = false;
            return;
        }

        // Get the value at the column called "ClientID" (string)
        var selectedRowFileName = selectedRow.Children.GetVariable("FileName");
        if (selectedRowFileName.Value.Value.ToString().Contains(".sqlite"))
        {
            Owner.Get<Button>("ButtonsBar/Import").Enabled = true;
            return;
        }

        Owner.Get<Button>("ButtonsBar/Import").Enabled = false;
    }
}
