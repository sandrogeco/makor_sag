#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Alarm;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.WebUI;
using FTOptix.CODESYS;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.OPCUAServer;
using FTOptix.OPCUAClient;
using FTOptix.Retentivity;
using FTOptix.EventLogger;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using FTOptix.System;
using FTOptix.S7TiaProfinet;
using FTOptix.S7TCP;

#endregion

public class GestPDF_Viewer : BaseNetLogic
{
	override
	public void Start()
	{
		PDFSelected(1);
	}

	/// <summary>
	///		Seleziona il pdf da visualizzare.
	///		Npdf:
	///			1->hmi
	///			2->mec
	///			3->ele
	///			4->pne
	/// </summary>
	/// <param name="Npdf"></param>
	[ExportMethod]
	public void PDFSelected(int Npdf)
	{
		PdfViewer viewer = (PdfViewer)LogicObject.Owner.GetObject("PDFViewer1");
		var path = getPath();

		if (File.Exists(getPDF(Npdf, path)))
		{
			viewer.Path = getPDF(Npdf, path);
		}
		else
		{
			viewer.Path = getPDF(Npdf, "C:\\Makor\\Manuali\\Inglese");
		}

	}

	private string getPath()
	{
		LocalizedText traslationKey = new LocalizedText(LogicObject.NodeId.NamespaceIndex, Session.ActualLanguage);
		var lingua = InformationModel.LookupTranslation(traslationKey, new List<string>() { "it-IT" });

		return "C:\\Makor\\Manuali\\" + lingua.Text;
	}

	private string getPDF(int Npdf, string path){

		return Npdf switch
		{
			1 => path + "\\hmi.pdf",
			2 => path + "\\mec.pdf",
			3 => path + "\\ele.pdf",
			4 => path + "\\pne.pdf",
			_ => ""
		};
	}
}
