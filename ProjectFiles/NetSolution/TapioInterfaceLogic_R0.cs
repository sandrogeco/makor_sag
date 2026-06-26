using UAManagedCore;
using FTOptix.Recipe;
using FTOptix.System;
using FTOptix.S7TiaProfinet;
using FTOptix.S7TCP;

public class TapioInterfaceLogic_R0 : FTOptix.NetLogic.BaseNetLogic
{
    public override void Start()
    {
        // Registrazione del task periodico
        if (Owner.GetVariable("EnbTapioInterface").Value)
        {
            periodicTask1 = new PeriodicTask(MachState, 1000, LogicObject);
            periodicTask1.Start();
        }        
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
        if(Owner.GetVariable("EnbTapioInterface").Value)
        {
            periodicTask1.Dispose();
            periodicTask1 = null;
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    private void MachState()
    {
        if (Owner.Children.Get<IUAVariable>("CycOn").Value)
        {
            Owner.Children.Get<IUAVariable>("MachStatus").Value = "s_mainusage";
            Owner.Children.Get<IUAVariable>("MachStatusDescription").Value = "Machine Cycle On";
        }

        if (Owner.Children.Get<IUAVariable>("CycOff").Value)
        {
            Owner.Children.Get<IUAVariable>("MachStatus").Value = "s_idletime";
            Owner.Children.Get<IUAVariable>("MachStatusDescription").Value = "Machine Idle Time";
        }

        if (Owner.Children.Get<IUAVariable>("MachAlm").Value)
        {
            Owner.Children.Get<IUAVariable>("MachStatus").Value = "s_malfunction";
            Owner.Children.Get<IUAVariable>("MachStatusDescription").Value = "Machine Failure";
        }

        if (Owner.Children.Get<IUAVariable>("MachChangeOver").Value)
        {
            Owner.Children.Get<IUAVariable>("MachStatus").Value = "s_secondaryusage";
            Owner.Children.Get<IUAVariable>("MachStatusDescription").Value = "Machine Changeover In Progress";
        }
    }

    private PeriodicTask periodicTask1;
}
