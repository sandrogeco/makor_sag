#region Using directives
using UAManagedCore;
using FTOptix.NetLogic;
using FTOptix.SQLiteStore;
using FTOptix.S7TCP;
using FTOptix.RAEtherNetIP;
using FTOptix.Recipe;
using FTOptix.System;
using FTOptix.S7TiaProfinet;
#endregion

public class StrobeBtnLogic : BaseNetLogic
{
    private DelayedTask StrobeTask;
    public override void Stop()
    {    
        StrobeTask?.Dispose();
        StrobeTask = null;
    }

    [ExportMethod]
    public void Storbe()
    {
        Owner.GetVariable("StrobeVar").Value = true;
        StrobeTask = new DelayedTask(Reset, Owner.GetVariable("DurataOn_ms").Value, LogicObject);
        StrobeTask.Start();
    }

    private void Reset() => Owner.GetVariable("StrobeVar").Value = false;
}
