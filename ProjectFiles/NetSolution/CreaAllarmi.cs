#region Using directives
using UAManagedCore;
using FTOptix.Alarm;
using FTOptix.CoreBase;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.CommunicationDriver;
#endregion

public class CreaAllarmi : BaseNetLogic
{
    /// <summary>
    /// Questo script crea gli allarmi partendo dalla tabella delle variabili allarmi 
    /// </summary>
    [ExportMethod]
    public void CreaVarAllarmi()
    {   
        var Id_CartellaAlm = Project.Current.Get("Alarms");
        string NomeAlm;

        var Stazione = Project.Current.Get("CommDrivers/CODESYSDriver1/PLC_Next/Tags/Next");
        foreach (var TblVarAlmNode in Stazione.GetNodesByType<TagStructure>())
        {
            //if (!(TabVarAlm.BrowseName == "BehaviourStartPriority"))
            //{
                //NodeId TblAlmNode = TabVarAlm.Value;
                //var TblVarAlmNode = LogicObject.Context.GetNode(TblAlmNode);  // tiro su il nodo dove si trova la tabella delle var allarmi

                if (TblVarAlmNode is null) 
                {
                    continue;
                }

                string NomeTabVarAlm = TblVarAlmNode.BrowseName;

                if (!DesignTime_Utility.CheckNodo(Id_CartellaAlm, NomeTabVarAlm, out IUANode Nodo_TblAlm))   //controllo se la cartella Allarmi esiste sotto il Nodo "Allarmi"
                {
                    Nodo_TblAlm = DesignTime_Utility.CreaNodo(Id_CartellaAlm, NomeTabVarAlm, DesignTime_Utility.Tipo.Folder);  // Se non essite la creo  etiro su il nodo 
                }

                // leggo creo gli allarmi e li inserisco nella cartella degli allarmi 
                foreach (var Alm in TblVarAlmNode.GetNodesByType<IUAVariable>())
                {
                    NomeAlm = Alm.BrowseName;
                    string Notifica = NomeAlm.Substring(0, 4);
                    bool NotificaTrovata;
                    ushort Severità;

                    switch (Notifica)
                    {
                        case "bAlm":
                            NotificaTrovata = true;
                            Severità = 1000;
                            break;

                        case "bWrn":
                            NotificaTrovata = true;
                            Severità = 500;
                            break;

                        case "bMsg":
                            NotificaTrovata = true;
                            Severità = 1;
                            break;

                        default:
                            NotificaTrovata = false;
                            Severità = 0;
                            break;
                    }

                    if (!NotificaTrovata)
                    {
                        continue;
                    }

                    var allarme = Nodo_TblAlm.Children.Get<DigitalAlarm>(Alm.BrowseName);
                    if (allarme == null)
                    {
                        allarme = InformationModel.MakeObject<DigitalAlarm>(Alm.BrowseName);
                        Nodo_TblAlm.Add(allarme);
                    }

                    
                    //imposto le proprietà dell'allarme
                    allarme.AutoAcknowledge = true;
                    allarme.AutoConfirm = true;
                    allarme.Message = NomeAlm;                  
                                        
                    allarme.Severity = Severità;
                    allarme.InputValueVariable.SetDynamicLink(Alm, DynamicLinkMode.ReadWrite);
                }
            }
        //}
    }
}




