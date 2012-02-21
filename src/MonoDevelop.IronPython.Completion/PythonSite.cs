// 
// PythonSite.cs
//  
// Author:
//       Christian Hergert <chris@dronelabs.com>
// 
// Copyright (c) 2009 Christian Hergert
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
using System.Collections.Generic;
using System.IO;
using System.Threading;

using MonoDevelop.Ide;

using MonoDevelop.IronPython.Parser;
using MonoDevelop.IronPython.Parser.Dom;

namespace MonoDevelop.IronPython.Completion
{
	public class PythonSite
	{
		PythonSiteImpl siteImpl;
		
		PythonSite ()
		{
			siteImpl = new PythonSiteImpl ("Default");
		}
		
		static PythonSite defaultSite;
		
		public static PythonSite Default {
			get {
				if (defaultSite == null)
					defaultSite = new PythonSite ();
				
				return defaultSite;
			}
		}
		
		public ParserDatabase Database {
			get {
				return siteImpl.Database;
			}
		}
		
		public string [] Paths {
			get {
				return siteImpl.Paths;
			}
		}
		
		public bool ContainsPath (string path)
		{
			return siteImpl.ContainsPath (path);
		}
		
		public void AddPath (string path)
		{
			siteImpl.AddPath (path);
		}
		
		public void RemovePath (string path)
		{
			siteImpl.RemovePath (path);
		}
	}
	
	class PythonSiteImpl
	{
		List<string> m_Paths = new List<string> ();
		PythonParser parser = new PythonParser ();
		ParserDatabase m_Database;
		uint m_saveHandle = 0;
		string m_pathsPath;
		
		public ParserDatabase Database {
			get { return m_Database; }
		}
		
		public string [] Paths {
			get { return m_Paths.ToArray (); }
		}
		
		public PythonSiteImpl (string name)
		{
			string configPath = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
			string dbPath = PathCombine (configPath, "MonoDevelop", "IronPythonBinding", "Sites", name, "completion.db");
			m_pathsPath = PathCombine (configPath, "MonoDevelop", "IronPythonBinding", "Sites", name, "paths");
			
			if (File.Exists (m_pathsPath)) {
				foreach (var line in File.ReadAllLines (m_pathsPath)) {
					string trimmed = line.Trim ();
					if (!String.IsNullOrEmpty (line))
						m_Paths.Add (trimmed);
				}
			}
			
			m_Database = new ParserDatabase (dbPath);
			m_Database.Open ();
		}
		
		public bool ContainsPath (string path)
		{
			return m_Paths.Contains (path);
		}
		
		bool OnSave ()
		{
			m_saveHandle = 0; // runs in main thread, no lock needed
			string[] paths = m_Paths.ToArray ();
			
			ThreadPool.QueueUserWorkItem (delegate {
				using (var writer = new StreamWriter (File.OpenWrite (m_pathsPath)))
				{
					foreach (var path in paths)
						writer.WriteLine (path);
				}
			});
			
			return false;
		}
		
		public void AddPath (string path)
		{
			m_Paths.Add (path);
			
			// add timeout to save the file to disk in a few seconds if not scheduled yet
			if (m_saveHandle == 0)
				m_saveHandle = GLib.Timeout.Add (5000, OnSave);
			
			if (!Directory.Exists (path))
				return;
			
			ThreadPool.QueueUserWorkItem (delegate {
				List<string> toParse = new List<string> ();
				FindAll (path, toParse);
				var count = toParse.Count;
				var i = 0;
				
				var progress = IdeApp.Workbench.ProgressMonitors.GetBackgroundProgressMonitor ("Indexing ironpython modules", Gtk.Stock.Execute);
				progress.BeginTask (String.Format ("Indexing {0}", path), count);
				
				foreach (var file in toParse) {
					progress.Log.WriteLine ("Parsing {0} of {1}: {2}", ++i, count, file);
					try {
						ParseFile (file);
					}
					catch (Exception ex) {
						progress.Log.WriteLine (ex.ToString ());
					}
					finally {
						progress.Step (1);
					}
				}
				
				progress.EndTask ();
				progress.Dispose ();
			});
		}
		
		public void RemovePath (string path)
		{
			m_Paths.Remove (path);
			
			ThreadPool.QueueUserWorkItem (delegate {
				m_Database.RemoveByFilePrefix (path);
			});
		}
		
		string PathCombine (params string[] chunks)
		{
			string retval = String.Empty;
			foreach (string chunk in chunks)
				retval = System.IO.Path.Combine (retval, chunk);
			return retval;
		}
		
		void ParseFile (string name)
		{
			var p = parser.Parse (null, name, File.ReadAllText (name));
			var compUnit = p.CompilationUnit as PythonCompilationUnit;
			if (compUnit == null || compUnit.Module == null)
				return;
			
			var pyModule = compUnit.Module;
			
			// Do all of our creation and data storage work
			// from the thread pool as it isn't critical path
			ThreadPool.QueueUserWorkItem (delegate {
				List<ParserItem> items = new List<ParserItem> ();
				
				string module = pyModule.FullName;
				if (module.EndsWith (".__init__"))
					module = module.Substring (0, module.Length - ".__init__".Length);
				
				items.Add (new ParserItem () {
					FileName = name,
					FullName = module,
					ItemType = ParserItemType.Module,
					Documentation = pyModule.Documentation,
				});
				
				if (pyModule.Classes != null) {
					foreach (var klass in pyModule.Classes) {
						string klassname = String.Format ("{0}.{1}", module, klass.Name);
						items.Add (new ParserItem () {
							FileName      = name,
							FullName      = klassname,
							ItemType      = ParserItemType.Class,
							Documentation = klass.Documentation,
						});
						
						foreach (var attr in klass.Attributes) {
							string attrname = String.Format ("{0}.{1}", klassname, attr.Name);
							items.Add (new ParserItem () {
								FileName      = name,
								FullName      = attrname,
								ItemType      = ParserItemType.Attribute,
								Documentation = attr.Documentation,
							});
						}
						
						foreach (var func in klass.Functions) {
							string funcname = String.Format ("{0}.{1}", klassname, func.Name);
							items.Add (new ParserItem () {
								FileName      = name,
								FullName      = funcname,
								ItemType      = ParserItemType.Function,
								Documentation = func.Documentation,
							});
						}
					}
					
					if (pyModule.Functions != null) {
						foreach (var func in pyModule.Functions) {
							string funcname = String.Format ("{0}.{1}", module, func.Name);
							items.Add (new ParserItem () {
								FileName      = name,
								FullName      = funcname,
								ItemType      = ParserItemType.Function,
								Documentation = func.Documentation,
							});
						}
					}
				}
				
				m_Database.AddRange (items);
			});
		}
		
		void FindAll (string name, List<string> toParse)
		{
			if (toParse == null)
				throw new ArgumentNullException ("toParse");
			
			foreach (string filename in Directory.GetFiles (name, "*.py"))
			{
				string absPath = PathCombine (name, filename);
				toParse.Insert (0, absPath);
			}
			
			foreach (string dirname in Directory.GetDirectories (name))
			{
				string absPath = PathCombine (name, dirname);
				FindAll (absPath, toParse);
			}
		}
	}
}
