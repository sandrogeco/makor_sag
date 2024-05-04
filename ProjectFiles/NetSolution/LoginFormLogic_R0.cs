#region StandardUsing
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using System;
using UAManagedCore;
using FTOptix.Recipe;
#endregion

public class LoginFormLogic_R0 : BaseNetLogic
{
    [ExportMethod]
    public void PerformLogin(string username, string password)
    {
        var usersAlias = LogicObject.GetAlias("Users");
        if (usersAlias == null || usersAlias.NodeId == NodeId.Empty)
        {
            Log.Error("LoginForm", "Missing Users alias");
            return;
        }

        if (usersAlias.Get<User>(username) == null)
        {
            Log.Error("LoginForm", "Could not find user " + username);
            return;
        }

        bool loginResult;
        try
        {
            usersAlias.Get<User>(username).PasswordVariable.RemoteRead();

            if (!(loginResult = Session.ChangeUser(username, password)))
            {
                var LocalizedText = new LocalizedText("Password errata!");
                var translation = InformationModel.LookupTranslation(LocalizedText);
                Owner.Children.Get<Label>("LoginMsg").Text = translation.Text;
                Owner.Children.Get<Label>("LoginMsg").Visible = true;
            }
            else
            {
                ((Dialog)Owner.Owner).Close();      //E' possibile fare il cast con il tipo base                               
            }
        }
        catch (Exception e)
        {
            Log.Error("LoginForm", e.Message);
            Owner.Children.Get<Label>("LoginMsg").Text = $"Error: {e.Message}";
            Owner.Children.Get<Label>("LoginMsg").Visible = true;
        }
    }

    [ExportMethod]
    public void PerformLogout() => _ = Session.ChangeUser("Anonymous", "");
}
