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
using FTOptix.Retentivity;
using FTOptix.EventLogger;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.OPCUAClient;
using FTOptix.Core;
using FilesystemBrowser;
using System.IO;
using FTOptix.System;
using FTOptix.S7TiaProfinet;

#endregion

public class FileTextBoxRicLogic : BaseNetLogic
{
	public override void Start()
	{
		resourceUriHelper = new ResourceUriHelper(LogicObject.NodeId.NamespaceIndex);

		var filesystemBrowserNode = LogicObject.GetVariable("FilesystemBrowser");
		if (filesystemBrowserNode == null)
			throw new CoreConfigurationException("FilesystemBrowser node pointer not found");

		filesystemBrowser = LogicObject.Context.GetNode(filesystemBrowserNode.Value);
		if (filesystemBrowser == null)
			throw new CoreConfigurationException("FilesystemBrowser cannot be null");

		folderPathVariable = LogicObject.GetVariable("Path");
		if (folderPathVariable == null)
			throw new CoreConfigurationException("Path variable not found");

		fullPathVariable = filesystemBrowser.GetVariable("FullPath");
		if (fullPathVariable == null)
			throw new CoreConfigurationException("FullPath variable not found");

		accessFullFilesystemVariable = filesystemBrowser.GetVariable("AccessFullFilesystem");
		if (accessFullFilesystemVariable == null)
			throw new CoreConfigurationException("AccessFullFilesystem variable not found");

		accessNetworkDrivesVariable = filesystemBrowser.GetVariable("AccessNetworkDrives");
		if (accessNetworkDrivesVariable == null)
			throw new CoreConfigurationException("AccessNetworkDrives variable not found");

		if (accessFullFilesystemVariable.Value && !PlatformConfigurationHelper.IsFreeNavigationSupported())
			return;

		fileTextBox = (TextBox)Owner;
		fileTextBoxTextVariable = fileTextBox.GetVariable("Text");

		impButton = (Button)filesystemBrowser.GetObject("ButtonsBar").GetObject("Import");
		expButton = (Button)filesystemBrowser.GetObject("ButtonsBar").GetObject("Export");
		impButton.Enabled = false;
		expButton.Enabled = false;

		folderPathVariable.VariableChange += PathVariable_VariableChange;

		fileTextBoxTextVariable.VariableChange += FileTextBox_VariableChange;
		//fileTextBox.OnUserTextChanged += FileTextBox_UserTextChanged;

		selectedItemVariable = filesystemBrowser.GetObject("DataGrid").GetVariable("SelectedItem");
		selectedItemVariable.VariableChange += SelectedItemVariable_VariableChange;
	}

	public override void Stop()
	{
		folderPathVariable.VariableChange -= PathVariable_VariableChange;

		fileTextBoxTextVariable.VariableChange -= FileTextBox_VariableChange;
		//fileTextBox.OnUserTextChanged -= FileTextBox_UserTextChanged;

		selectedItemVariable.VariableChange -= SelectedItemVariable_VariableChange;
	}

	private void SelectedItemVariable_VariableChange(object sender, VariableChangeEventArgs e)
	{
		var nodeId = (NodeId)e.NewValue;
		if (nodeId == null || nodeId.IsEmpty)
			return;

		var entry = (FileEntry)LogicObject.Context.GetObject(nodeId);
		if (entry == null)
			return;

		var test = fileTextBox.Text;
		var test2 = impButton;
		impButton.Enabled = !string.IsNullOrEmpty(fileTextBox.Text);
		expButton.Enabled = !string.IsNullOrEmpty(fileTextBox.Text);

		if (!entry.IsDirectory)
			fileTextBox.Text = entry.FileName;
	}

	private void PathVariable_VariableChange(object sender, VariableChangeEventArgs e)
	{
		var updatedPathResourceUri = new ResourceUri(e.NewValue);
		if (!resourceUriHelper.IsFolderPathAllowed(updatedPathResourceUri, accessFullFilesystemVariable.Value, accessNetworkDrivesVariable.Value))
		{
			Log.Error("FileTextBoxLogic", $"Cannot browse to {updatedPathResourceUri} since this path is not allowed in current configuration");
			return;
		}

		var insertedFileText = fileTextBox.Text;
		if (string.IsNullOrEmpty(insertedFileText))
		{
			impButton.Enabled = false;
			expButton.Enabled = false;
			return;
		}

		// Set the FullPath output parameter
		impButton.Enabled = SetFullPathVariable(updatedPathResourceUri.Uri, insertedFileText);
		expButton.Enabled = SetFullPathVariable(updatedPathResourceUri.Uri, insertedFileText);
	}

	// In case an item has been selected from the datagrid it is necessary to press the confirmation button to start the configured action.
	private void FileTextBox_VariableChange(object sender, VariableChangeEventArgs e)
	{
		var insertedFileText = ((LocalizedText)e.NewValue.Value).Text;
		if (string.IsNullOrEmpty(insertedFileText))
		{
			impButton.Enabled = false;
			expButton.Enabled = false;
			return;
		}

		// Addition of namespace prefix information is necessary when folder path is configured via
		// QStudio placeholders like %APPLICATIONDIR%\, %PROJECTDIR%\.
		// This can happen at the start of the project
		var folderPath = resourceUriHelper.AddNamespacePrefixToQRuntimeFolder(folderPathVariable.Value);
		string updatedFolderPathSystemPath = new ResourceUri(folderPath).Uri;

		impButton.Enabled = SetFullPathVariable(updatedFolderPathSystemPath, insertedFileText);
		expButton.Enabled = SetFullPathVariable(updatedFolderPathSystemPath, insertedFileText);
	}

	// If the file name is inserted by the user the action is started when Enter is pressed.
	private void FileTextBox_UserTextChanged(object sender, UserTextChangedEvent e)
	{
		var insertedFileText = e.NewText.Text;
		if (string.IsNullOrEmpty(insertedFileText))
		{
			impButton.Enabled = false;
			expButton.Enabled = false;
			return;
		}

		// Addition of namespace prefix information is necessary when folder path is configured via
		// QStudio placeholders like %APPLICATIONDIR%\, %PROJECTDIR%\.
		// This can happen at the start of the project
		var folderPath = resourceUriHelper.AddNamespacePrefixToQRuntimeFolder(folderPathVariable.Value);
		string updatedFolderPathSystemPath = new ResourceUri(folderPath).Uri;
		bool isInputValid = SetFullPathVariable(updatedFolderPathSystemPath, insertedFileText);

		impButton.Enabled = isInputValid;
		expButton.Enabled = isInputValid;
		if (!isInputValid)
			return;

		// Start the configured action in FileSelectedCallback
		var fileSelectorDialog = filesystemBrowser.Owner.Owner;
		try
		{
			var methodInvocation = (MethodInvocation)fileSelectorDialog.GetObject("FileSelectedCallback");
			if (string.IsNullOrEmpty(methodInvocation.Method))
				Log.Warning("FileTextBoxLogic", "FileSelectedCallback is not configured");

			methodInvocation.Invoke();
		}
		catch (Exception exception)
		{
			Log.Error("FileTextBoxLogic", $"Unable to execute specified action: {exception.Message}");
		}

		((Dialog)fileSelectorDialog).Close();
	}

	private bool SetFullPathVariable(string folderPath, string selectedFileName)
	{
		// Folder paths must be rejected
		if (Path.IsPathRooted(selectedFileName))
		{
			Log.Error("FileTextBoxLogic", $"{selectedFileName} is a full path, but only a file name is allowed");
			return false;
		}

		fullPathVariable.Value = ResourceUri.FromAbsoluteFilePath(Path.Combine(folderPath, selectedFileName));

		return true;
	}

	private IUANode filesystemBrowser;
	private TextBox fileTextBox;
	private IUAVariable fileTextBoxTextVariable;
	private IUAVariable folderPathVariable;
	private IUAVariable fullPathVariable;
	private IUAVariable selectedItemVariable;
	private IUAVariable accessFullFilesystemVariable;
	private IUAVariable accessNetworkDrivesVariable;
	private Button impButton;
	private Button expButton;

	private ResourceUriHelper resourceUriHelper;
}
