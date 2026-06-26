#region Using directives
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Store;
using System;
using UAManagedCore;
using FTOptix.System;
using FTOptix.S7TiaProfinet;
using FTOptix.S7TCP;
#endregion

public class FrameDatiProduzLogic : BaseNetLogic
{
    public enum TipoFiltroGestStat
    {
        SettimanaCorrente,
        SettimanaPrec,
        Oggi,
        Ieri,
        MeseCorrente,
        MeseScorso,
        PerData
    }

    public override void Start()
    {
        var FilterObject = Owner.GetObject("FilterObject");
        FilterObject.GetVariable("AnnoStart").Value = DateTime.Now.Year;
        FilterObject.GetVariable("AnnoStop").Value = DateTime.Now.Year;
        FilterObject.GetVariable("MeseStart").Value = DateTime.Now.Month;
        FilterObject.GetVariable("MeseStop").Value = DateTime.Now.Month;
        FilterObject.GetVariable("GiornoStart").Value = DateTime.Now.Day;
        FilterObject.GetVariable("GiornoStop").Value = DateTime.Now.Day;
    }


    [ExportMethod]
    public void ResetCnt()
    {
        NodeId DataBaseNode = Owner.GetVariable("FilterObject/DataBase").Value;
        var DataBase = InformationModel.Get<Store>(DataBaseNode);

        DataBase.Query("DELETE FROM CntProduzione", out _, out _);
    }
}
