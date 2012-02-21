//
// PythonConfiguration.cs
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

using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;

namespace MonoDevelop.IronPython
{
	public enum LangVersion {
		Python27,
		Python30,
	}
	
	public class PythonConfiguration : ProjectConfiguration
	{
		[ItemProperty ("MainModule", DefaultValue = "main")]
		string mainModule = "main";
		
		[ItemProperty ("InterpreterArgs")]
		string interpreterArgs = String.Empty;
		
		[ItemProperty ("ExtraPaths")]
		string extraPaths = String.Empty;
		
		[ItemProperty ("LangVersion", DefaultValue = LangVersion.Python27)]
		LangVersion langVersion = LangVersion.Python27;
		
		[ItemProperty ("ShowExceptionDetails")]
		bool showExceptionDetails;
		
		[ItemProperty ("ShowClrExceptions")]
		bool showClrExceptions;
			
		[ItemProperty ("Optimize")]
		bool optimize;
		
		[ItemProperty ("WarnInconsistentTabbing")]
		bool warnInconsistentTabbing;
			
		public PythonConfiguration ()
		{
		}
		
		public PythonConfiguration (string name)
		{
			Name = name;
		}
		
		public string MainModule {
			get { return mainModule; }
			set { mainModule = value ?? String.Empty; }
		}
		
		public string InterpreterArguments {
			get { return interpreterArgs; }
			set { interpreterArgs = value ?? String.Empty; }
		}
		
		public LangVersion LangVersion {
			get { return langVersion; }
			set { langVersion = value; }
		}
		
		public string ExtraPaths {
			get { return extraPaths; }
			set { extraPaths = value ?? String.Empty; }
		}
		
		public bool ShowExceptionDetails {
			get { return showExceptionDetails; }
			set { showExceptionDetails = value; }
		}
		
		public bool ShowClrExceptions {
			get { return showClrExceptions; }
			set { showClrExceptions = value; }
		}
		
		public bool Optimize {
			get { return optimize; }
			set { optimize = value; }
		}
		
		public bool WarnInconsistentTabbing {
			get { return warnInconsistentTabbing; }
			set { warnInconsistentTabbing = value; }
		}
		
		public override void CopyFrom (ItemConfiguration config)
		{
			var pyConfig = config as PythonConfiguration;
			if (pyConfig == null)
				throw new ArgumentException ("Not a python configuration", "item");
			
			base.CopyFrom (config);
			
			mainModule = pyConfig.mainModule;
			extraPaths = pyConfig.ExtraPaths;
			interpreterArgs = pyConfig.interpreterArgs;
			langVersion = pyConfig.langVersion;
			optimize = pyConfig.optimize;
			showExceptionDetails = pyConfig.showExceptionDetails;
			showClrExceptions = pyConfig.showClrExceptions;
			warnInconsistentTabbing = pyConfig.warnInconsistentTabbing;
		}
	}
}

