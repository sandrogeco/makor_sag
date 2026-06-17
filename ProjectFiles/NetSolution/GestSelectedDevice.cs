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
using Tag = FTOptix.CommunicationDriver.Tag;
using System.IO;
using System.Collections;
using System.Xml.Linq;
using FTOptix.System;
using System.Linq;
using FTOptix.S7TiaProfinet;
#endregion

public class GestSelectedDevice : BaseNetLogic
{
    List<TagStructure> gvList = new();
    List<TagStructure> devicesList = new();
    public override void Start()
    {
        gvList.Add(Project.Current.Get<TagStructure>("CommDrivers/CODESYSDriver1/PLC_Next/Tags/PLC/GvGen"));
        gvList.Add(Project.Current.Get<TagStructure>("CommDrivers/CODESYSDriver1/PLC_Next/Tags/PLC/GvOsc"));
        gvList.Add(Project.Current.Get<TagStructure>("CommDrivers/CODESYSDriver1/PLC_Next/Tags/PLC/GvCarta"));

        string numMaster = "G0" + Owner.GetVariable("NumMaster").Value.Value;
        string[] devicesName = Owner.GetVariable("DeviceName").Value;
        List<RemoteVariable> RemoteVariable = new();
        List<string> readVariablesNames = new();

        foreach (TagStructure gvItem in gvList)
            foreach (TagStructure deviceSt in gvItem.GetNodesByType<TagStructure>())
                if (deviceSt.BrowseName[..6] == "stDiag")
                {
                    devicesList.Add(deviceSt);
                    if (deviceSt.BrowseName.EndsWith(numMaster))
                        if (deviceSt.GetVariable("byPortNum") != null)
                        {
                            readVariablesNames.Add(deviceSt.BrowseName[6..]);
                            RemoteVariable.Add(new RemoteVariable(deviceSt.GetVariable("byPortNum")));
                            continue;
                        }
                        else if (deviceSt.GetVariable("stDiagStd/byPortNum") != null)
                        {
                            readVariablesNames.Add(deviceSt.BrowseName[6..]);
                            RemoteVariable.Add(new RemoteVariable(deviceSt.GetVariable("stDiagStd/byPortNum")));
                            continue;
                        }
                }

        var readValues = InformationModel.RemoteRead(RemoteVariable).ToList();

        for (int i = 0; i < readValues.Count; i++)
        {
            devicesName[readValues[i].Value] = readVariablesNames[i];
        }

        Image img = Owner.Get<Image>("DevicesImage");
        img.Path = ResourceUri.FromProjectRelativePath("").Uri
            + Path.DirectorySeparatorChar + "Images" + Path.DirectorySeparatorChar + "SimbVari" + Path.DirectorySeparatorChar + "IO_Devices" + Path.DirectorySeparatorChar
            + Owner.GetAlias("IO_Device").GetVariable("sProductId").RemoteRead().Value + ".png";

        Project.Current.GetVariable("CommDrivers/CODESYSDriver1/PLC_Next/Tags/PLC/GvGen/bHmiReadDiag").RemoteWrite(1);
        Owner.GetVariable("DeviceName").Value = devicesName;

        //quando la variabile PortSelected cambia, viene chiamato il metodo OnPortChange
        var PortSelected = Owner.GetVariable("PortSelected");
        PortSelected.VariableChange += OnPortChange;

        //Inizializzo la pagina con il primo dispositivo
        loadDevicesDiagnostics(devicesName[0]);
    }

    public override void Stop()
    {
        Project.Current.GetVariable("CommDrivers/CODESYSDriver1/PLC_Next/Tags/PLC/GvGen/bHmiReadDiag").RemoteWrite(0);
    }

    private void loadDevicesDiagnostics(string deviceName)
    {
        PanelLoader panelLoader = Owner.Get<PanelLoader>("DeviceStatus/PanelLoader1");
        TagStructure deviceStruct = devicesList.Find(device => device.BrowseName[6..] == deviceName);

        switch (deviceStruct.BrowseName.Substring(6, 2))
        {
            case "HL":
                Standard_Devices standard_HL = Owner.Get<Standard_Devices>("DeviceStatus/Standard_Devices");
                fillObj(standard_HL);
                panelLoader.ChangePanel("Standard_Panel", standard_HL.NodeId);
                break;

            case "SP":
                Standard_Devices standard_SP = Owner.Get<Standard_Devices>("DeviceStatus/Standard_Devices");
                fillObj(standard_SP);
                panelLoader.ChangePanel("Standard_Panel", standard_SP.NodeId);
                break;

            case "YV":
                Standard_Devices standard_YV = Owner.Get<Standard_Devices>("DeviceStatus/Standard_Devices");
                fillObj(standard_YV);
                panelLoader.ChangePanel("Standard_Panel", standard_YV.NodeId);
                break;

            case "SL":
                SL_Devices sL_Devices = Owner.Get<SL_Devices>("DeviceStatus/SL_Devices");
                fillObj(sL_Devices);
                panelLoader.ChangePanel("SL_Panel", sL_Devices.NodeId);
                break;

            case "BQ":
                BQ_Devices bQ_Devices = Owner.Get<BQ_Devices>("DeviceStatus/BQ_Devices");
                fillObj(bQ_Devices);
                panelLoader.ChangePanel("BQ_Panel", bQ_Devices.NodeId);
                break;

            case "BF":
                BF_Devices bF_Devices = Owner.Get<BF_Devices>("DeviceStatus/BF_Devices");
                fillObj(bF_Devices);
                panelLoader.ChangePanel("BF_Panel", bF_Devices.NodeId);
                break;
        }

        void fillObj(IUAObject obj)
        {
            foreach (var tag in deviceStruct.GetNodesByType<IUAVariable>())
            {
                if (tag.VariableType.ToString() == "FTOptix.CommunicationDriver.TagStructureType") continue;

                obj.GetVariable(tag.BrowseName).SetDynamicLink(
                    deviceStruct.GetVariable(tag.BrowseName),
                    DynamicLinkMode.Read);
            }
            foreach (var tagStructure in deviceStruct.GetNodesByType<TagStructure>())
            {
                foreach (var tag in tagStructure.GetNodesByType<IUAVariable>())
                {
                    obj.GetVariable($"{tagStructure.BrowseName}_{tag.BrowseName}").SetDynamicLink(
                        deviceStruct.GetVariable($"{tagStructure.BrowseName}/{tag.BrowseName}"),
                        DynamicLinkMode.Read);
                }
            }
        }
    }

    public void OnPortChange(object sender, VariableChangeEventArgs e)
    {
        string[] devicesName = Owner.GetVariable("DeviceName").Value;
        loadDevicesDiagnostics(devicesName[e.NewValue]);

        Image img = Owner.Get<Image>("DevicesImage");
        img.Path = ResourceUri.FromProjectRelativePath("").Uri
            + Path.DirectorySeparatorChar + "Images" + Path.DirectorySeparatorChar + "SimbVari" + Path.DirectorySeparatorChar + "IO_Devices" + Path.DirectorySeparatorChar
            + Owner.GetAlias("IO_Device").GetVariable("sProductId").RemoteRead().Value + ".png";
    }
}
