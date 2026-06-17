#region Using directives
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using System.Linq;
using UAManagedCore;
using FTOptix.System;
using FTOptix.S7TiaProfinet;
#endregion

public class DesignTimeCreaTabelleCntProduz : BaseNetLogic
{
    [ExportMethod]
    public void CreaColonne()
    {
        NodeId NodeCntProduzPlc = LogicObject.GetVariable("CntProduzPlc").Value;
        var CntProduzPlc = LogicObject.Context.GetNode(NodeCntProduzPlc);
        System.Collections.Generic.IEnumerable<(Table Tbl, StoreColumn MyCol)> enumerable()
        {
            foreach (var Tbl in ((SQLiteStoreType)Owner).Tables)
            {
                if (Tbl.BrowseName == "CntProduzione")
                    foreach (var Cnt in CntProduzPlc.GetNodesByType<IUAVariable>())
                    {
                        if (Tbl.Columns.Get(Cnt.BrowseName) is null)
                        {
                            var MyCol = InformationModel.MakeVariable<StoreColumn>(Cnt.BrowseName, ((UAVariable)Cnt).DataType);
                            yield return (Tbl, MyCol);
                        }
                    }
            }
        }

        //Eseguo un ciclo for sulle tabelle del database ed estraggo la tabella dei contatori.
        foreach (var (Tbl, MyCol) in enumerable())
        {
            Tbl.Columns.Add(MyCol);
        }
    }
}
