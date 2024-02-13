#region Using directives
using UAManagedCore;
using FTOptix.NetLogic;
using FTOptix.Core;
using FTOptix.UI;
using FTOptix.HMIProject;
#endregion

public class CurrentUserLogic_R1 : BaseNetLogic
{
    private PeriodicTask periodicTask;    
    private DelayedTask LogoutTask;
    private DelayedTask myTask;

    public override void Start()
    {
        if (LogicObject.GetVariable("LoginAvvio").Value)        //Se è abilitato l'apertura del pop-up di login allora creo il task ritardato per aprire il pop-up
        {
            myTask = new DelayedTask(ApriLoginForm, 1000, LogicObject);
            myTask.Start();
        }

        //Sottoscrivo l'evento di cambiamento dell'utente
        Session.UserChange += Session_UserChange;

        //se siamo sulla sessione Native allora tiro fuori il nome dell'utente attualmente loggato
        if (Session.GetVariable("IsNativeUI").Value)
        { 
            periodicTask = new PeriodicTask(GetSessionUser, 500, LogicObject);
            periodicTask.Start();
        }
    }

    public override void Stop()
    {
        myTask?.Dispose();
        myTask = null;

        periodicTask?.Dispose();
        periodicTask = null;
    }

    private void GetSessionUser() => LogicObject.GetVariable("UtenteAttuale_NativeUI").Value = Session.User.BrowseName;

    private void Session_UserChange(object sender, UserChangeEventArgs e)
    {
        if (e.newUser.BrowseName == "Makor")
        {
            LogoutTask = new DelayedTask(PerformLogout, LogicObject.GetVariable("Timeout").Value, LogicObject);
            LogoutTask.Start();
        }
    }

    private void PerformLogout()
    {
        LogoutTask?.Cancel();
        LogoutTask?.Dispose();
        LogoutTask = null;
        Session.ChangeUser("Anonymous", "");
        ApriLoginForm();
    }

    private void ApriLoginForm()
    {
        var myDialog = Project.Current.Get("UI/Panels/Prj_DialogBox").Get<DialogType>("Login");
        _ = UICommands.OpenDialog(LogicObject.Owner, myDialog, NodeId.Empty);
    }
}
