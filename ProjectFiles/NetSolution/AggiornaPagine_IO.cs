#region Using directives
using System;
using FTOptix.Core;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.S7TiaProfinet;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.EventLogger;
using FTOptix.Alarm;
using FTOptix.Store;
using FTOptix.OPCUAServer;
using FTOptix.HMIProject;
using FTOptix.ODBCStore;
using FTOptix.Retentivity;
using FTOptix.NativeUI;
using FTOptix.MelsecQ;
using FTOptix.OPCUAClient;
using FTOptix.MelsecFX3U;
using FTOptix.CODESYS;
using FTOptix.SQLiteStore;
using FTOptix.S7TCP;
using FTOptix.RAEtherNetIP;
using FTOptix.Recipe;
#endregion

public class AggiornaPagine_IO : BaseNetLogic
{
    private int NumOggettiDispVert = 10;
    private int NumDispVertPag = 3;

    [ExportMethod]
    public void AggiornaPagInput()
    {
        NodeId Source = LogicObject.Children.Get<IUAVariable>("NodoSource_In").Value;
        var NodoSource = LogicObject.Context.GetNode(Source);  // tiro su il nodo dove si trovano le variabili IO

        int i = 1;  // indice degli Oggetti della singola dispozione verticale
        int j = 1;  // indice delle disposizioni verticali della pagina 
        int Pag = 1;

        IUANode VertDisp = Owner.Children.Get("Pag_Ingressi_1").Get("DisposizioneVerticale1"); // prendo il node ID della prima pagina e del primo vertical display

        foreach (var Child in NodoSource.GetNodesByType<IUAVariable>())
        {
            if(i> NumOggettiDispVert)
            {
                j++;
                i = 1;
                if (j> NumDispVertPag)   
                {
                    Pag++;
                    j = 1;                    
                }

                VertDisp = Owner.Children.Get("Pag_Ingressi_"+ Pag).Get("DisposizioneVerticale" + j);                  
            }

            var Label = VertDisp.Children.Get<Label>("Label" + i);
            var Led = VertDisp.Children.Get("Led" + i).Get<IUAVariable>("Attivo");

            Label.Text = Child.BrowseName;
            Led.SetDynamicLink(Child, DynamicLinkMode.Read);

            i++;           
        }
    }

    [ExportMethod]
    public void AggiornaPagOutput()
    {
        NodeId Source = LogicObject.Children.Get<IUAVariable>("NodoSource_Out").Value;
        var NodoSource = LogicObject.Context.GetNode(Source);  // tiro su il nodo dove si trovano le variabili IO

        int i = 1;  // indice degli Oggetti della singola dispozione verticale
        int j = 1;  // indice delle disposizioni verticali della pagina 
        int Pag = 1;

        IUANode VertDisp = Owner.Children.Get("Pag_Uscite_1").Get("DisposizioneVerticale1"); // prendo il node ID della prima pagina e del primo vertical display

        foreach (var Child in NodoSource.GetNodesByType<IUAVariable>())
        {
            if (i > NumOggettiDispVert)
            {
                j++;
                i = 1;
                if (j > NumDispVertPag)
                {
                    Pag++;
                    j = 1;
                }

                VertDisp = Owner.Children.Get("Pag_Uscite_" + Pag).Get("DisposizioneVerticale" + j);
            }

            var Label = VertDisp.Children.Get<Label>("Label" + i);
            var Led = VertDisp.Children.Get("Led" + i).Get<IUAVariable>("Attivo");

            Label.Text = Child.BrowseName;
            Led.SetDynamicLink(Child, DynamicLinkMode.Read);

            i++;

        }
    }

    [ExportMethod]
    public void AggiornaPagSafeInput()
    {
        NodeId Source = LogicObject.Children.Get<IUAVariable>("NodoSource_SafeIn").Value;
        var NodoSource = LogicObject.Context.GetNode(Source);  // tiro su il nodo dove si trovano le variabili IO

        int i = 1;  // indice degli Oggetti della singola dispozione verticale
        int j = 1;  // indice delle disposizioni verticali della pagina 
        int Pag = 1;

        IUANode VertDisp = Owner.Children.Get("Pag_IngressiSafe_1").Get("DisposizioneVerticale1"); // prendo il node ID della prima pagina e del primo vertical display

        foreach (var Child in NodoSource.GetNodesByType<IUAVariable>())
        {
            if (i > NumOggettiDispVert)
            {
                j++;
                i = 1;
                if (j > NumDispVertPag)
                {
                    Pag++;
                    j = 1;
                }

                VertDisp = Owner.Children.Get("Pag_IngressiSafe_" + Pag).Get("DisposizioneVerticale" + j);
            }

            var Label = VertDisp.Children.Get<Label>("Label" + i);
            var Led = VertDisp.Children.Get("Led" + i).Get<IUAVariable>("Attivo");

            Label.Text = Child.BrowseName;
            Led.SetDynamicLink(Child, DynamicLinkMode.Read);

            i++;

        }
    }

    [ExportMethod]
    public void AggiornaPagSafeOutput()
    {
        NodeId Source = LogicObject.Children.Get<IUAVariable>("NodoSource_SafeOut").Value;
        var NodoSource = LogicObject.Context.GetNode(Source);  // tiro su il nodo dove si trovano le variabili IO

        int i = 1;  // indice degli Oggetti della singola dispozione verticale
        int j = 1;  // indice delle disposizioni verticali della pagina 
        int Pag = 1;

        IUANode VertDisp = Owner.Children.Get("Pag_UsciteSafe_1").Get("DisposizioneVerticale1"); // prendo il node ID della prima pagina e del primo vertical display

        foreach (var Child in NodoSource.GetNodesByType<IUAVariable>())
        {
            if (i > NumOggettiDispVert)
            {
                j++;
                i = 1;
                if (j > NumDispVertPag)
                {
                    Pag++;
                    j = 1;
                }

                VertDisp = Owner.Children.Get("Pag_UsciteSafe_" + Pag).Get("DisposizioneVerticale" + j);
            }

            var Label = VertDisp.Children.Get<Label>("Label" + i);
            var Led = VertDisp.Children.Get("Led" + i).Get<IUAVariable>("Attivo");

            Label.Text = Child.BrowseName;
            Led.SetDynamicLink(Child, DynamicLinkMode.Read);

            i++;

        }
    }
}
