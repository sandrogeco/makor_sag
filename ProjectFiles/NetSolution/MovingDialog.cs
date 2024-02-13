#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.NetLogic;
using FTOptix.NativeUI;
using FTOptix.HMIProject;
using FTOptix.UI;
using FTOptix.CoreBase;
using FTOptix.Core;
#endregion

public class MovingDialog : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void Move(int verso)
    {
        switch (verso)
        {
            case 1:
                ((Dialog)Owner).TopMargin -= 5;
                ((Dialog)Owner).LeftMargin += 5;
                break;
            case 2:
                ((Dialog)Owner).LeftMargin += 5;
                break;
            case 3:
                ((Dialog)Owner).TopMargin += 5;
                ((Dialog)Owner).LeftMargin += 5;
                break;
            case 4:
                ((Dialog)Owner).TopMargin += 5;
                break;
            case 5:
                ((Dialog)Owner).TopMargin += 5;
                ((Dialog)Owner).LeftMargin -= 5;
                break;
            case 6:
                ((Dialog)Owner).LeftMargin -= 5;
                break;
            case 7:
                ((Dialog)Owner).TopMargin -= 5;
                ((Dialog)Owner).LeftMargin -= 5;
                break;
            case 8:
                ((Dialog)Owner).TopMargin -= 5;
                break;
            default:
                break;
        }
    }
}
