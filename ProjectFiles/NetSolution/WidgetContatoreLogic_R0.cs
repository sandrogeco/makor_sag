#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.NativeUI;
using FTOptix.NetLogic;
using FTOptix.HMIProject;
using FTOptix.UI;
using FTOptix.Alarm;
using FTOptix.S7TiaProfinet;
using FTOptix.MelsecFX3U;
using FTOptix.MelsecQ;
using FTOptix.CODESYS;
using FTOptix.EventLogger;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.OPCUAServer;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.OPCUAClient;
using FTOptix.Core;
using FTOptix.SQLiteStore;
using FTOptix.S7TCP;
using FTOptix.RAEtherNetIP;
#endregion

public class WidgetContatoreLogic_R0 : BaseNetLogic
{
    [ExportMethod]
    public void ResetCnt()
    {        
        if (Owner.GetVariable("AbilitazGestDataOraRst").Value == true)
        {
            Owner.GetVariable("DataOraRst").Value = DateTime.Now;
            Owner.GetVariable("Val_Rst").Value = Owner.GetVariable("Val_CntParziali").Value;
        }

        Owner.GetVariable("Bit_RstCntParziali").Value = true;

        if (Owner.GetVariable("AbilitazRstCntTotali").Value == true)
            Owner.GetVariable("Bit_RstCntTotali").Value = true;
    }
}
