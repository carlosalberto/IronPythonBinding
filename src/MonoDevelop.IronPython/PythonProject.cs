//
// PythonProject.cs
//  
// Author:
//       Carlos Alberto Cortez <calberto.cortez@gmail.com>
// 
// Copyright (c) 2012 Carlos Alberto Cortez
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.IO;
using System.Xml;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.IronPython
{
	public class PythonProject : Project
	{
		const string ProjectTypeName = "IronPython";
		
		public PythonProject ()
		{
		}
		
		public PythonProject (string languageName, ProjectCreateInformation info, XmlElement projectOptions)
		{
			if (!String.Equals (languageName, ProjectTypeName))
				throw new ArgumentException ("Not an IronPython project: " + languageName);
			
			if (info != null) {
				Name = info.ProjectName;
			}
		
			CreateDefaultConfigurations ();
			foreach (PythonConfiguration configuration in Configurations)
				configuration.OutputDirectory = info.ProjectBasePath;
		}
				
		public override string ProjectType {
			get { return ProjectTypeName; }
		}
		
		public static PythonProject FromSingleFile (string languageName, string fileName)
		{
			var projectInfo = new ProjectCreateInformation () {
				ProjectName = Path.GetFileNameWithoutExtension (fileName),
				SolutionPath = Path.GetDirectoryName (fileName),
				ProjectBasePath = Path.GetDirectoryName (fileName)
			};
			
			var project = new PythonProject (languageName, projectInfo, null);
			project.AddFile (new ProjectFile (fileName));
			return project;
		}
		
		public override SolutionItemConfiguration CreateConfiguration (string name)
		{
			return new PythonConfiguration (name);
		}
		
		protected override bool OnGetCanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			// We have decided to have the 'Run' menu always available,
			// and show any error (e.g. interpreter not set, module not set)
			// when it is actually invoked, providing a more precise message.
			return true;
		}
		
		protected override void DoExecute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			if (!CheckCanExecute (configuration))
				return;
	
			var config = (PythonConfiguration) GetConfiguration (configuration);
			IConsole console = config.ExternalConsole ?
				context.ExternalConsoleFactory.CreateConsole (!config.PauseConsoleOutput) :
				context.ConsoleFactory.CreateConsole (!config.PauseConsoleOutput);
			
			var aggregatedMonitor = new AggregatedOperationMonitor (monitor);
			
			try {
				var executionCommand = CreateExecutionCommand (configuration, config);
				if (!context.ExecutionHandler.CanExecute (executionCommand)) {
					monitor.ReportError (GettextCatalog.GetString ("Cannot execute application. The selected execution mode " +
						"is not supported for IronPython projects"), null);
					return;
				}
				
				var asyncOp = context.ExecutionHandler.Execute (executionCommand, console);
				aggregatedMonitor.AddOperation (asyncOp);
				asyncOp.WaitForCompleted ();
				
				monitor.Log.WriteLine ("The application exited with code: " + asyncOp.ExitCode);
				
			} catch (Exception exc) {
				monitor.ReportError (GettextCatalog.GetString ("Cannot execute \"{0}\"", config.MainModule), exc);
			} finally {
				console.Dispose ();
				aggregatedMonitor.Dispose ();
			}
		}
			
		bool CheckCanExecute (ConfigurationSelector configuration)
		{
			if (!IronManager.IsInterpreterPathValid ()) {
				MessageService.ShowError ("Interpreter not set", "A valid interpreter has not been set.");
				return false;
			}
			
			var config = (PythonConfiguration) GetConfiguration (configuration);
			if (String.IsNullOrEmpty (config.MainModule)) {
				MessageService.ShowError ("Main module not set", "Main module has not been set.");
				return false;
			}
			
			// Try to detect the actual module file.
			var module = config.MainModule.Replace ('.', System.IO.Path.DirectorySeparatorChar);
			var path = config.OutputDirectory.Combine (module).ChangeExtension ("py");
			if (!IsFileInProject (path)) {
				MonoDevelop.Ide.MessageService.ShowError ("Main module is missing", "Main module file is missing.");
				return false;
			}
			
			return true;
		}
			
		// TODO - Research on the target runtime selection (if possible/doable/recommended).
		protected virtual PythonExecutionCommand CreateExecutionCommand (ConfigurationSelector configSel, PythonConfiguration configuration)
		{
			var ironPath = (FilePath)PropertyService.Get<string> ("IronPython.InterpreterPath");
			
			var command = new PythonExecutionCommand (ironPath) {
				Arguments = IronManager.GetInterpreterArguments (configuration),
				WorkingDirectory = BaseDirectory,
				EnvironmentVariables = configuration.GetParsedEnvironmentVariables (),
				Configuration = configuration
			};
			
			if (!String.IsNullOrEmpty (configuration.ExtraPaths))
				command.EnvironmentVariables [IronManager.ExtraPathsEnvironmentVariable] = configuration.ExtraPaths;
			
			return command;
		}
		
		void CreateDefaultConfigurations ()
		{
			var debugConfig = CreateConfiguration ("Debug") as PythonConfiguration;
			debugConfig.DebugMode = true;
			debugConfig.ShowExceptionDetails = true;
			debugConfig.ShowClrExceptions = true;
			debugConfig.WarnInconsistentTabbing = true;
			Configurations.Add (debugConfig);
			
			var releaseConfig = CreateConfiguration ("Release") as PythonConfiguration;
			releaseConfig.Optimize = true;
			Configurations.Add (releaseConfig);
		}
	}
}

