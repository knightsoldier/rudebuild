using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using Extensibility;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.CommandBars;
using RudeBuildVSShared;

namespace RudeBuildVSAddIn
{
	public class Connect : IDTExtensibility2, IDTCommandTarget, ICommandRegistrar
	{
        private DTE2 _application;
        private AddIn _addInInstance;
        private CommandManager _commandManager;
        private OutputPane _outputPane;
        private Builder _builder;

		#region ICommandRegistrar implementation

		public const string VSAddInCommandPrefix = "RudeBuildVSAddIn.Connect.";

		public EnvDTE.Command GetCommand(DTE2 application, string name)
		{
			Commands2 vsCommands = (Commands2)application.Commands;
			var vsCommand = from Command command in vsCommands
							where command.Name == VSAddInCommandPrefix + name
							select command;
			return vsCommand.SingleOrDefault();
		}

		public EnvDTE.Command RegisterCommand(DTE2 application, int id, string name, string caption, string toolTip, string icon, ICommand command)
		{
			EnvDTE.Command vsCommand = GetCommand(application, name);
			if (vsCommand != null)
				return vsCommand;

			Commands2 vsCommands = (Commands2)application.Commands;
			return vsCommands.AddNamedCommand2(_addInInstance, name, caption, toolTip, false, icon);
		}
		
		#endregion

		private void RegisterCommands()
        {
            _commandManager.RegisterCommand(0, "BuildSolution", "&Build Solution", "RudeBuild: Build Solution", "3", new BuildSolutionCommand(_builder, BuildCommandBase.Mode.Build));
            _commandManager.RegisterCommand(0, "RebuildSolution", "&Rebuild Solution", "RudeBuild: Rebuild Solution", null, new BuildSolutionCommand(_builder, BuildCommandBase.Mode.Rebuild));
            _commandManager.RegisterCommand(0, "CleanSolution", "&Clean Solution", "RudeBuild: Clean Solution", null, new BuildSolutionCommand(_builder, BuildCommandBase.Mode.Clean));
            _commandManager.RegisterCommand(0, "CleanCache", "C&lean Cache", "RudeBuild: Clean RudeBuild Solution Cache", null, new CleanCacheCommand(_builder));
            _commandManager.RegisterCommand(0, "BuildProject", "B&uild Project", "RudeBuild: Build Project", "2", new BuildProjectCommand(_builder, BuildCommandBase.Mode.Build));
            _commandManager.RegisterCommand(0, "RebuildProject", "R&ebuild Project", "RudeBuild: Rebuild Project", null, new BuildProjectCommand(_builder, BuildCommandBase.Mode.Rebuild));
            _commandManager.RegisterCommand(0, "CleanProject", "Clea&n Project", "RudeBuild: Clean Project", null, new BuildProjectCommand(_builder, BuildCommandBase.Mode.Clean));
            _commandManager.RegisterCommand(0, "StopBuild", "&Stop Build", "RudeBuild: Stop Build", "5", new StopBuildCommand(_builder));
            _commandManager.RegisterCommand(0, "GlobalSettings", "&Global Settings...", "Opens the RudeBuild Global Settings Dialog", "4", new GlobalSettingsCommand(_builder));
            _commandManager.RegisterCommand(0, "SolutionSettings", "S&olution Settings...", "Opens the RudeBuild Solution Settings Dialog", "4", new SolutionSettingsCommand(_builder, _outputPane));
            _commandManager.RegisterCommand(0, "About", "&About", "About RudeBuild", null, new AboutCommand());
        }

        private void UnregisterCommands()
        {
            _commandManager.UnregisterCommand("BuildSolution");
            _commandManager.UnregisterCommand("RebuildSolution");
            _commandManager.UnregisterCommand("CleanSolution");
            _commandManager.UnregisterCommand("CleanCache");
            _commandManager.UnregisterCommand("BuildProject");
            _commandManager.UnregisterCommand("RebuildProject");
            _commandManager.UnregisterCommand("CleanProject");
            _commandManager.UnregisterCommand("StopBuild");
            _commandManager.UnregisterCommand("GlobalSettings");
            _commandManager.UnregisterCommand("SolutionSettings");
            _commandManager.UnregisterCommand("About");
        }

        private void AddToolbarToUI()
        {
            CommandBar commandBar = _commandManager.AddCommandBar("RudeBuild", MsoBarPosition.msoBarTop);
            int insertIndex = 1;
            _commandManager.AddCommandToCommandBar(commandBar, "BuildProject", insertIndex++, style: MsoButtonStyle.msoButtonIcon);
            _commandManager.AddCommandToCommandBar(commandBar, "BuildSolution", insertIndex++, style: MsoButtonStyle.msoButtonIcon);
            _commandManager.AddCommandToCommandBar(commandBar, "StopBuild", insertIndex++, beginGroup:true, style: MsoButtonStyle.msoButtonIcon);
        }

        private void AddMenuToUI()
        {
            CommandBar commandBar = _commandManager.AddPopupCommandBar("MenuBar", "RudeBuild", "R&udeBuild", GetMainMenuBarInsertIndex());
            int insertIndex = 1;
            _commandManager.AddCommandToCommandBar(commandBar, "BuildSolution", insertIndex++);
            _commandManager.AddCommandToCommandBar(commandBar, "RebuildSolution", insertIndex++);
            _commandManager.AddCommandToCommandBar(commandBar, "CleanSolution", insertIndex++);
            _commandManager.AddCommandToCommandBar(commandBar, "CleanCache", insertIndex++);
            _commandManager.AddCommandToCommandBar(commandBar, "BuildProject", insertIndex++, beginGroup: true);
            _commandManager.AddCommandToCommandBar(commandBar, "RebuildProject", insertIndex++);
            _commandManager.AddCommandToCommandBar(commandBar, "CleanProject", insertIndex++);

            _commandManager.AddCommandToCommandBar(commandBar, "StopBuild", insertIndex++, beginGroup: true);

            _commandManager.AddCommandToCommandBar(commandBar, "GlobalSettings", insertIndex++, beginGroup: true);
            _commandManager.AddCommandToCommandBar(commandBar, "SolutionSettings", insertIndex++);

            _commandManager.AddCommandToCommandBar(commandBar, "About", insertIndex++, beginGroup: true);
        }

        private void AddProjectRightClickMenuToUI()
        {
            IList<CommandBar> parentCommandBars = _commandManager.FindCommandBars("Project");
            foreach (CommandBar parentCommandBar in parentCommandBars)
            {
                CommandBar commandBar = _commandManager.AddPopupCommandBar(parentCommandBar, "RudeBuild", "R&udeBuild", GetPopupMenuBarInsertIndex(parentCommandBar), beginGroup: true);
                int insertIndex = 1;
                _commandManager.AddCommandToCommandBar(commandBar, "BuildProject", insertIndex++);
                _commandManager.AddCommandToCommandBar(commandBar, "RebuildProject", insertIndex++);
                _commandManager.AddCommandToCommandBar(commandBar, "CleanProject", insertIndex++);
            }
        }

        private void AddSolutionRightClickMenuToUI()
        {
            IList<CommandBar> parentCommandBars = _commandManager.FindCommandBars("Solution");
            foreach (CommandBar parentCommandBar in parentCommandBars)
            {
                CommandBar commandBar = _commandManager.AddPopupCommandBar(parentCommandBar, "RudeBuild", "R&udeBuild", GetPopupMenuBarInsertIndex(parentCommandBar), beginGroup: true);
                int insertIndex = 1;
                _commandManager.AddCommandToCommandBar(commandBar, "BuildSolution", insertIndex++);
                _commandManager.AddCommandToCommandBar(commandBar, "RebuildSolution", insertIndex++);
                _commandManager.AddCommandToCommandBar(commandBar, "CleanSolution", insertIndex++);
            }
        }

        private void AddCommandsToUI()
        {
            AddToolbarToUI();
            AddMenuToUI();
            AddProjectRightClickMenuToUI();
            AddSolutionRightClickMenuToUI();
        }

        private void SafeDeleteCommandBar(CommandBar commandBar)
        {
            if (null != commandBar)
            {
                try
                {
                    commandBar.Delete();
                }
                catch
                {
                    // Just ignore any errors.
                }
            }
        }

        private void RemoveCommandsFromUI()
        {
            CommandBar toolCommandBar = _commandManager.FindCommandBar("RudeBuild");
            SafeDeleteCommandBar(toolCommandBar);
            CommandBar menuCommandBar = _commandManager.FindPopupCommandBar("MenuBar", "RudeBuild");
            SafeDeleteCommandBar(menuCommandBar);
            
            IList<CommandBar> parentCommandBars = _commandManager.FindCommandBars("Project");
            foreach (CommandBar parentCommandBar in parentCommandBars)
            {
                CommandBar projectCommandBar = _commandManager.FindPopupCommandBar(parentCommandBar, "RudeBuild");
                SafeDeleteCommandBar(projectCommandBar);
            }

            parentCommandBars = _commandManager.FindCommandBars("Solution");
            foreach (CommandBar parentCommandBar in parentCommandBars)
            {
                CommandBar solutionCommandBar = _commandManager.FindPopupCommandBar(parentCommandBar, "RudeBuild");
                SafeDeleteCommandBar(solutionCommandBar);
            }
        }

        private int GetMainMenuBarInsertIndex()
        {
            CommandBarControl commandBarControl = _commandManager.FindCommandBarControlByCaption("MenuBar", "IncrediBuild");
            if (null != commandBarControl)
                return commandBarControl.Index;

            commandBarControl = _commandManager.FindCommandBarControlByCaption("MenuBar", "Build");
            if (null != commandBarControl)
                return commandBarControl.Index + 1;

            return 5;
        }

        private int GetPopupMenuBarInsertIndex(CommandBar parentCommandBar)
        {
            CommandBarControl commandBarControl = _commandManager.FindCommandBarControlByCaption(parentCommandBar, "IncrediBuild");
            if (null != commandBarControl)
                return commandBarControl.Index;

            commandBarControl = _commandManager.FindCommandBarControlByCaption(parentCommandBar, "Project Dependencies...");
            if (null != commandBarControl)
                return commandBarControl.Index;

            return 5;
        }

		public void OnConnection(object application, ext_ConnectMode connectMode, object addInInstance, ref Array custom)
		{
            // Uncomment the following line and rebuild to allow debugging this addin.
            //MessageBox.Show("Start debugging!", "RudeBuild", MessageBoxButton.OK);

            if (connectMode != ext_ConnectMode.ext_cm_AfterStartup && connectMode != ext_ConnectMode.ext_cm_Startup)
                return;

            _application = (DTE2)application;
            _addInInstance = (AddIn)addInInstance;

            try
            {
                if (null == _commandManager)
                    _commandManager = new CommandManager(_application, this);
                if (null == _outputPane)
                    _outputPane = new OutputPane(_application, "RudeBuild");
                if (null == _builder)
                    _builder = new Builder(_outputPane);

                RegisterCommands();
                AddCommandsToUI();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("RudeBuild initialization error!\n" + ex.Message, "RudeBuild", MessageBoxButton.OK, MessageBoxImage.Error);
            }
		}

		public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
		{
        }

		public void OnAddInsUpdate(ref Array custom)
		{
		}

		public void OnStartupComplete(ref Array custom)
		{
		}

		public void OnBeginShutdown(ref Array custom)
		{
            if (null != _builder)
                _builder.Stop();
		}

        public void OnUninstall(DTE2 application)
        {
            _application = (DTE2)application;

            if (null == _commandManager)
                _commandManager = new CommandManager(_application, this);

            RemoveCommandsFromUI();
            UnregisterCommands();
        }

        private static string GetShortCommandName(string longCommandName)
        {
            int dotIndex = longCommandName.LastIndexOf('.');
            return dotIndex != -1 ? longCommandName.Substring(dotIndex + 1) : longCommandName;
        }

	    public void QueryStatus(string longCommandName, vsCommandStatusTextWanted neededText, ref vsCommandStatus status, ref object commandText)
	    {
	        if (neededText != vsCommandStatusTextWanted.vsCommandStatusTextWantedNone)
                return;

	        try
	        {
	            string commandName = GetShortCommandName(longCommandName);
	            if (_commandManager.IsCommandEnabled(commandName))
	                status = vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
	            else
	                status = vsCommandStatus.vsCommandStatusSupported;
	        }
	        catch (Exception ex)
	        {
	            MessageBox.Show("An internal RudeBuild exception occurred!\n" + ex.Message, "RudeBuild", MessageBoxButton.OK, MessageBoxImage.Error);
	        }
	    }

	    public void Exec(string longCommandName, vsCommandExecOption executeOption, ref object varIn, ref object varOut, ref bool handled)
		{
			handled = false;
			if (executeOption == vsCommandExecOption.vsCommandExecOptionDoDefault)
			{
                try
                {
                    string commandName = GetShortCommandName(longCommandName);
                    _commandManager.ExecuteCommand(commandName);
                    handled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An internal RudeBuild exception occurred!\n" + ex.Message, "RudeBuild", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
		}
	}
}
