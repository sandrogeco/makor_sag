#region Using directives
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.NetLogic;
using FTOptix.HMIProject;
using FTOptix.UI;
using FTOptix.CODESYS;
using FTOptix.SQLiteStore;
using FTOptix.S7TCP;
using FTOptix.RAEtherNetIP;
using FTOptix.Recipe;
using FTOptix.System;
using FTOptix.S7TiaProfinet;

#endregion

public class LocaleComboBoxLogic : BaseNetLogic
{
    public override void Start()
    {
        var modelLocales = InformationModel.MakeObject("Locales");
        modelLocales.Children.Clear();

        var projectLocales = Project.Current.Localization.Locales;
        foreach (var locale in projectLocales)
        {
            var language = InformationModel.MakeVariable(locale, OpcUa.DataTypes.String);
            language.Value = locale;
            language.DisplayName = InformationModel.LookupTranslation(new LocalizedText(locale));
            modelLocales.Add(language);
        }

        LogicObject.Add(modelLocales);
        ((ComboBox)Owner).Model = modelLocales.NodeId;
    }
}
