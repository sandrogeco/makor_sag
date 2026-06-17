#region Using directives
using System;
using UAManagedCore;
using FTOptix.NetLogic;
using System.Net;
using FTOptix.Core;
using System.Net.Sockets;
using FTOptix.S7TCP;
using FTOptix.RAEtherNetIP;
using FTOptix.Recipe;
using FTOptix.System;
using FTOptix.S7TiaProfinet;
#endregion

public class SystemVariablesLogic : BaseNetLogic
{
    private PeriodicTask periodicTask;
    private int numCycle;

    public override void Start()
    {
        CheckOS();
        numCycle = 0;
        periodicTask = new PeriodicTask(GetSystemVariables, 125, LogicObject);        //Il task viene eseguito ogni 125ms
        periodicTask.Start();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
        periodicTask?.Dispose();
        periodicTask = null;
    }

    private void CheckOS()
    {
        LogicObject.GetVariable("Windows").Value = System.Runtime.InteropServices.RuntimeInformation
                                               .IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
        LogicObject.GetVariable("Linux").Value = System.Runtime.InteropServices.RuntimeInformation
                                               .IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);
    }

    public void GetSystemVariables()
    {
        numCycle++;
        int ms = numCycle * 125;
        //if (ms % 125 == 0)
        //    LogicObject.Children.Get<IUAVariable>("Blink_500ms").Value = !LogicObject.Children.Get<IUAVariable>("Blink_500ms").Value;
        //if (ms % 250 == 0)
        //    LogicObject.Children.Get<IUAVariable>("Blink_500ms").Value = !LogicObject.Children.Get<IUAVariable>("Blink_500ms").Value;
        if (ms % 500 == 0)
            LogicObject.GetVariable("Blink_500ms").Value = !LogicObject.GetVariable("Blink_500ms").Value;
        if (ms % 1000 == 0)
        {
            LogicObject.GetVariable("Blink_1s").Value = !LogicObject.GetVariable("Blink_1s").Value;
            OneSecondSystemVariables();
        }

        if (ms % 2000 == 0)
            LogicObject.GetVariable("Blink_2s").Value = !LogicObject.GetVariable("Blink_2s").Value;
        if (ms % 5000 == 0)
            LogicObject.GetVariable("Blink_5s").Value = !LogicObject.GetVariable("Blink_5s").Value;
        if (ms % 10000 == 0)
        {
            LogicObject.GetVariable("Blink_10s").Value = !LogicObject.GetVariable("Blink_10s").Value;
            numCycle = 0;
        }
    }

    private void OneSecondSystemVariables()
    {
        //Check Device USB
        var pathUsbResourceUri = new ResourceUri("%USB1%");
        try
        {
            var uri = pathUsbResourceUri.Uri;
            LogicObject.GetVariable("UsbPresent").Value = true;
        }
        catch (Exception)
        {
            LogicObject.GetVariable("UsbPresent").Value = false;
        }

        //Network Parameters (Only for Windows O.S.)
        int count = 0;
        if (LogicObject.GetVariable("Windows").Value)
        {
            string hostName = Dns.GetHostName();
            var ips = System.Net.Dns.GetHostEntry(hostName).AddressList;
            LogicObject.GetVariable("WindowsIP1").Value = ips[0].MapToIPv4().ToString();
            LogicObject.GetVariable("WindowsIP2").Value = Dns.GetHostEntry(hostName).AddressList[1].ToString();
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    switch (count)
                    {
                        case 0:
                            LogicObject.GetVariable("WindowsIP1").Value = ip.ToString();
                            break;
                        case 1:
                            LogicObject.GetVariable("WindowsIP2").Value = ip.ToString();
                            break;
                        default:
                            break;
                    }
                    count++;
                }
            }
        }
    }    
}
