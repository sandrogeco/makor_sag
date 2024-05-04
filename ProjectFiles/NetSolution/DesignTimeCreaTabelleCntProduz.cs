#region Using directives
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Store;
using System.Linq;
using UAManagedCore;
using FTOptix.Recipe;
#endregion

public class DesignTimeCreaTabelleCntProduz : BaseNetLogic
{
    [ExportMethod]
    public void CreaColonne()
    {
        NodeId NodeCntProduzPlc = LogicObject.GetVariable("CntProduzPlc").Value;
        var CntProduzPlc = LogicObject.Context.GetNode(NodeCntProduzPlc);

        //Eseguo un ciclo for sulle tabelle del database ed estraggo la tabella dei contatori.
        foreach (var (Tbl, MyCol) in from Tbl in ((Store)Owner).Tables
                                     where Tbl.BrowseName == "CntProduzione"
                                     from Cnt in CntProduzPlc.GetNodesByType<IUAVariable>()
                                     where Tbl.Columns.Get(Cnt.BrowseName) is null
                                     let MyCol = InformationModel.MakeVariable<StoreColumn>(Cnt.BrowseName, ((UAVariable)Cnt).DataType)
                                     select (Tbl, MyCol))
        {
            Tbl.Columns.Add(MyCol);
        }
    }
}
