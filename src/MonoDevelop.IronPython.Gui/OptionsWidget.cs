//
// OptionsWidget.cs
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
using System.Text;
using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.IronPython.Gui
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class OptionsWidget : Gtk.Bin
	{
		ListStore versionsStore;
		ListStore pathsStore;
		
		public OptionsWidget ()
		{
			this.Build ();
			
			versionsStore = new ListStore (typeof (string), typeof (LangVersion));
			versionsStore.AppendValues ("2.7", LangVersion.Python27);
			versionsStore.AppendValues ("3.0", LangVersion.Python30);
			versionComboBox.Model = versionsStore;
			
			pathsStore = new ListStore (typeof (string));
			pathsTreeview.Model = pathsStore;
			var column = new TreeViewColumn ();
			var cell = new CellRendererText ();
			column.PackStart (cell, true);
			column.AddAttribute (cell, "text", 0);
			pathsTreeview.AppendColumn (column);
			pathsTreeview.Selection.Changed += delegate {
				pathsRemoveButton.Sensitive = pathsTreeview.Selection.CountSelectedRows () > 0;
			};
			
			Visible = true;
		}
		
		public string DefaultModule {
			get { return moduleEntry.Text ?? String.Empty; }
			set { moduleEntry.Text = value ?? String.Empty; }
		}
		
		public LangVersion LangVersion {
			get {
				switch (versionComboBox.Active) {
				case 0: return LangVersion.Python27;
				case 1: return LangVersion.Python30;
				}
				
				return LangVersion.Python27; // fallback
			}
			set {
				switch (value) {
				case LangVersion.Python27:
					versionComboBox.Active = 0;
					break;
				case LangVersion.Python30:
					versionComboBox.Active = 1;
					break;
				}
			}
		}
		
		public bool Optimize {
			get { return optimizeCheck.Active; }
			set { optimizeCheck.Active = value; }
		}
		
		public bool ShowClrExceptions {
			get { return showClrExcCheck.Active; }
			set { showClrExcCheck.Active = value; }
		}
		
		public bool ShowExceptionDetail {
			get { return showExcDetailCheck.Active; }
			set { showExcDetailCheck.Active = value; }
		}
		
		public bool WarnInconsistentTabbing {
			get { return warnInconsistentTabCheck.Active; }
			set { warnInconsistentTabCheck.Active = value; }
		}
		
		public string ExtraPaths {
			get {
				TreeIter iter;
				if (!pathsStore.GetIterFirst (out iter))
					return String.Empty;
				
				var sb = new StringBuilder ();
				do {
					if (sb.Length > 0)
						sb.Append (System.IO.Path.PathSeparator);
					
					sb.Append ((string)pathsStore.GetValue (iter, 0));
				} while (pathsStore.IterNext (ref iter));
				
				return sb.ToString ();
			}
			set {
				pathsStore.Clear ();
				if (String.IsNullOrEmpty (value))
					return;
				
				var paths = value.Split (System.IO.Path.PathSeparator);
				foreach (string path in paths)
					pathsStore.AppendValues (path);
			}
		}
		
		void pathsAddButtonClickHandler (object o, EventArgs args)
		{
			var dialog = new FileChooserDialog ("Add path",
					IdeApp.Workbench.RootWindow,
					FileChooserAction.SelectFolder,
					Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
					Gtk.Stock.Open, Gtk.ResponseType.Ok);

			if (dialog.Run () == (int)Gtk.ResponseType.Ok)
				pathsStore.AppendValues (dialog.Filename);

			dialog.Destroy ();
		}
		
		void pathsRemoveButtonClickHandler (object o, EventArgs args)
		{
			TreeModel model;
			TreeIter iter;

			if (pathsTreeview.Selection.GetSelected (out model, out iter))
				((ListStore)model).Remove (ref iter);
		}
	}
	
	public class OptionsPanel : MultiConfigItemOptionsPanel
	{
		OptionsWidget widget;
		
		public override Gtk.Widget CreatePanelWidget ()
		{
			widget = new OptionsWidget ();
			return widget;
		}
				
		public override void LoadConfigData ()
		{
			var config = CurrentConfiguration as PythonConfiguration;
			
			widget.DefaultModule = config.MainModule;
			widget.ExtraPaths = config.ExtraPaths;
			widget.LangVersion = config.LangVersion;
			widget.Optimize = config.Optimize;
			widget.ShowClrExceptions = config.ShowClrExceptions;
			widget.ShowExceptionDetail = config.ShowExceptionDetails;
			widget.WarnInconsistentTabbing = config.WarnInconsistentTabbing;
		}
		
		public override void ApplyChanges ()
		{
			var config = CurrentConfiguration as PythonConfiguration;
			
			config.MainModule = widget.DefaultModule;
			config.ExtraPaths = widget.ExtraPaths;
			config.LangVersion = widget.LangVersion;
			config.Optimize = widget.Optimize;
			config.ShowClrExceptions = widget.ShowClrExceptions;
			config.ShowExceptionDetails = widget.ShowExceptionDetail;
			config.WarnInconsistentTabbing = widget.WarnInconsistentTabbing;
		}
	}
}

