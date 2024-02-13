#region Using directives
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using System.Collections.Generic;
using System.Linq;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
#endregion

public class DesignTime_Utility : BaseNetLogic
{
    public enum Tipo { Folder, variabile, Object, ObjectType };

    public static bool CheckNodo(IUANode NodoPadre, string BrowseName, out IUANode Nodo)
    {
        bool res = false;
        Nodo = NodoPadre.Get(BrowseName);

        if (Nodo != null) { res = true; }

        return res;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="NodoPadre">IUANode dell'item sotto il quale cercare/creare il nuovo elemento</param>
    /// <param name="BrowseName">Browsename del nuovo elemento</param>
    /// <param name="Tipolog">Tipologia di elemento da creare</param>
    /// <param name="TypeNodeId">Opzionale. NodeId del tipo di elemento da creare. Esempio 'OpcUa.DataTypes.Boolean' oppure CustomTypeObject.NodeId  </param>
    /// <returns></returns>
    public static IUANode CreaNodo(IUANode NodoPadre, string BrowseName, Tipo Tipolog, NodeId TypeNodeId = null)
    {
        var NodeID_MyObj = NodoPadre.Children.Get(BrowseName);

        if (NodeID_MyObj == null)        // se non presente creo l'ogetto        
        {
            switch (Tipolog)
            {
                case Tipo.Folder:
                    var MyFolder = InformationModel.MakeObject<FTOptix.Core.Folder>(BrowseName);
                    NodoPadre.Add(MyFolder);
                    break;

                case Tipo.Object:
                    var MyObj = InformationModel.MakeObject(BrowseName);
                    NodoPadre.Add(MyObj);
                    break;

                case Tipo.ObjectType:
                    var MyObjType = InformationModel.MakeObject(BrowseName, TypeNodeId);
                    NodoPadre.Add(MyObjType);
                    break;

                case Tipo.variabile:
                    var MyVar = InformationModel.MakeVariable(BrowseName, TypeNodeId.Equals(null) ? OpcUa.DataTypes.Int32 : TypeNodeId);
                    NodoPadre.Add(MyVar);
                    break;
            }

            NodeID_MyObj = NodoPadre.Children.Get(BrowseName);
        }

        return NodeID_MyObj;
    }

    public static NodeId GetOpcUaDataType(string TipoTag)
    {
        NodeId TagType;
        switch (TipoTag)
        {
            case "Boolean":
                TagType = OpcUa.DataTypes.Boolean;
                break;
            case "SByte":
                TagType = OpcUa.DataTypes.SByte;
                break;
            case "Byte":
                TagType = OpcUa.DataTypes.Byte;
                break;
            case "Int16":
                TagType = OpcUa.DataTypes.Int16;
                break;
            case "UInt16":
                TagType = OpcUa.DataTypes.UInt16;
                break;
            case "Int32":
                TagType = OpcUa.DataTypes.Int32;
                break;
            case "UInt32":
                TagType = OpcUa.DataTypes.UInt32;
                break;
            case "Int64":
                TagType = OpcUa.DataTypes.Int64;
                break;
            case "UInt64":
                TagType = OpcUa.DataTypes.UInt64;
                break;
            case "Double":
                TagType = OpcUa.DataTypes.Double;
                break;
            case "Float":
                TagType = OpcUa.DataTypes.Float;
                break;
            case "String":
                TagType = OpcUa.DataTypes.String;
                break;
            default:
                TagType = OpcUa.DataTypes.Int32;
                break;
        }
        return TagType;
    }

    /// <summary>
    /// Controlla se nella LocalizationDictionary indicata esistono traduzioni per la ChiaveTesto. Se la traduzione esiste allora viene viene restituito il LocalizedText altrimenti viene creata la traduz e vien restituito il localizedText 
    /// NOTA : la chiave viene creata con traduzione solo in italiano e la traduzione è uguale al testo della chiave
    /// </summary>
    /// <param name="ChiaveTesto">La chiave da associare alle traduzioni</param>
    /// <param name="LocalizationDictionary">Tabella delle traduzioni</param>
    /// <returns></returns>
    public static LocalizedText GetTranslation(string ChiaveTesto, IUAVariable LocalizationDictionary)
    {
        var myLocalizedText = new LocalizedText(ChiaveTesto);

        //Controllo se la traduzione è presente nel progetto 
        if (string.IsNullOrWhiteSpace(InformationModel.LookupTranslation(myLocalizedText).Text))
        {
            myLocalizedText = new LocalizedText(ChiaveTesto, "it-IT");
            InformationModel.AddTranslation(myLocalizedText, LocalizationDictionary);
            return myLocalizedText;
        }
        else
            return myLocalizedText;
    }

    /// <summary>
    /// Controlla se nella LocalizationDictionary indicata esistono traduzioni per la ChiaveTesto. Se la traduzione esiste allora viene viene restituito il LocalizedText altrimenti viene creata la traduz e vien restituito il localizedText 
    /// NOTA : se non vengono passate la lista dei localeId e la lista delle traduz allora la chiave che viene creata con traduzione solo in italiano e la traduzione è uguale al testo della chiave
    /// </summary>
    /// <param name="ChiaveTesto">La chiave da associare alle traduzioni</param>
    /// <param name="LocalizationDictionary">Tabella delle traduzioni</param>
    /// <param name="ListaLocaleId"></param>
    /// <param name="ListaTraduz"></param>
    /// <returns></returns>
    public static LocalizedText GetAllTranslation(string ChiaveTesto, IUAVariable LocalizationDictionary, List<string> ListaLocaleId = null, List<string> ListaTraduz = null)
    {
        var myLocalizedText = new LocalizedText(ChiaveTesto, ChiaveTesto, "it-IT");

        //Controllo se la traduzione è presente nel progetto 
        if (string.IsNullOrWhiteSpace(InformationModel.LookupTranslation(myLocalizedText, new List<string>() { "it-IT" }).Text))
        {
            InformationModel.AddTranslation(myLocalizedText, LocalizationDictionary);
        }

        if (!(ListaLocaleId is null))
        {
            int i = 0;
            foreach (var myText in from Item in ListaLocaleId
                                   let myText = new LocalizedText(ChiaveTesto, ListaTraduz[i], Item)
                                   select myText)
            {
                InformationModel.SetTranslation(myText);
                i++;
            }
        }

        return myLocalizedText;
    }
}
