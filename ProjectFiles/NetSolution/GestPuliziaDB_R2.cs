#region Using directives
using FTOptix.NetLogic;
using FTOptix.Store;
using System;
using UAManagedCore;
using FTOptix.Recipe;
using FTOptix.System;
using FTOptix.S7TiaProfinet;
using FTOptix.S7TCP;
#endregion

public class GestPuliziaDB_R2 : BaseNetLogic
{
    public override void Start()
    {
        if (LogicObject.GetVariable("StoricoAlmNumGiorni").Value > 0)
        {
            PulisciStoricoAllarmi();
            //if (LogicObject.GetVariable("AbilitaGestStastiche").Value)
            //    PulisciCntProduz();
        }
    }

    /// <summary>
    /// Esegue la pulizia in base alla MARCA TEMPORALE. Farlo per numero liimte di record sarebbe stato molto dispendioso in termini di Query.
    /// </summary>
    private void PulisciStoricoAllarmi()
    {
        var Giorno = DateTime.Now.AddDays(-LogicObject.GetVariable("StoricoAlmNumGiorni").Value).Date.ToString("yyyy-MM-dd HH:mm:ss.fff");
        ((Store)Owner).Query($"DELETE FROM \"AlarmLogger\" WHERE Time < '{Giorno}'", out _, out object[,] resultSet);       //Le var datetime devono essere incapsulate tra apici

        // Check if the resultSet is a bidimensional array
        if (resultSet.Rank != 2)
            return;
    }


    private void PulisciCntProduz()
    {
        var Giorno = DateTime.Now.AddDays(-365).Date.ToString("yyyy-MM-dd HH:mm:ss.fff");
        ((Store)Owner).Query($"DELETE FROM \"CntProduzione\" WHERE LoginTime < '{Giorno}'", out _, out object[,] resultSet);       //Le var datetime devono essere incapsulate tra apici

        // Check if the resultSet is a bidimensional array
        if (resultSet.Rank != 2)
            return;
    }
}
