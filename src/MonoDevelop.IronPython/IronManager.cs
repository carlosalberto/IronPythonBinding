//
// IronManager.cs
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
using System.Diagnostics;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.IronPython
{
	public static class IronManager
	{
		public const string ExtraPathsEnvironmentVariable = "IRONPYTHONPATH";
		
		public static string GetInterpreterPath ()
		{
			return PropertyService.Get<string> ("IronPython.InterpreterPath");
		}
		
		public static void SetInterpreterPath (string interpreterPath)
		{
			PropertyService.Set ("IronPython.InterpreterPath", interpreterPath);
		}
		
		static readonly string [] commonRuntimeFiles = {
			"ipy.exe",
			"ipyw.exe",
			"IronPython.dll",
			"Microsoft.Dynamic.dll",
			"Microsoft.Scripting.dll"
		};
		
		public static bool IsInterpreterPathValid ()
		{
			return IsInterpreterPathValid (GetInterpreterPath ());
		}
		
		public static bool IsInterpreterPathValid (string interpreterPath)
		{
			if (String.IsNullOrEmpty (interpreterPath) || !File.Exists (interpreterPath))
				return false;
			
			var dir = Path.GetDirectoryName (interpreterPath);
			foreach (var commonFile in commonRuntimeFiles)
				if (!File.Exists (Path.Combine (dir, commonFile)))
				    return false;
			
			return true;
		}
			
		public static bool TryGetRuntimeVersion (string interpreterPath,
				out string version)
		{
			version = null;
			
			if (!IsInterpreterPathValid (interpreterPath))
				return false;

			Process p = null;
			try {
				p = Runtime.SystemAssemblyService.CurrentRuntime.ExecuteAssembly (new ProcessStartInfo () {
					FileName = interpreterPath,
					Arguments = "-V",
					RedirectStandardOutput = true,
					UseShellExecute = false
				});
				
				var output = p.StandardOutput.ReadToEnd ();
				if (output.StartsWith ("PythonContext") || output.StartsWith ("IronPython")) {
					version = output;
					return true;
				}
				
			} catch (Exception ex) {
				LoggingService.LogError ("The specified IronPython setup seems to be broken", ex);
			}
			
			return false;
		}
			
		public static string GetInterpreterArguments (PythonConfiguration config)
		{
			if (config == null)
				throw new ArgumentNullException ("configuration");
			
			var args = new ProcessArgumentBuilder ();
			args.Add ("-m");
			args.AddQuoted (config.MainModule);
			args.Add ("-u");
			
			if (config.Optimize)
				args.Add ("-O");

			if (config.LangVersion == LangVersion.Python30)
				args.Add ("-X:Python30");
			
			if (config.ShowExceptionDetails)
				args.Add ("-X:ExceptionDetail");
			
			if (config.ShowClrExceptions)
				args.Add ("-X:ShowClrExceptions");
			
			if (!String.IsNullOrEmpty (config.InterpreterArguments))
				args.Add (config.InterpreterArguments);
			
			return args.ToString ();
		}
	}
}

