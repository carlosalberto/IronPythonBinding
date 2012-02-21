//
// MainPanel.cs
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
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.IronPython.Gui
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class MainPanel : Gtk.Bin
	{
		public MainPanel ()
		{
			this.Build ();
			
			ironFileEntry.Path = IronManager.GetInterpreterPath ();
			ironFileEntry.PathChanged += (o, e) => UpdateStatus (ironFileEntry.Path);
			
			UpdateStatus (ironFileEntry.Path);
		}
		
		public string InterpreterPath {
			get { return ironFileEntry.Path; }
			set { ironFileEntry.Path = value; }
		}
		
		void UpdateStatus (string path)
		{
			string version;
			if (IronManager.TryGetRuntimeVersion (path, out version)) {
				fileEntryImage.SetFromStock (Gtk.Stock.Ok, Gtk.IconSize.Button);
				informationLabel.LabelProp = version;
			} else {
				fileEntryImage.SetFromStock (Stock.Error, Gtk.IconSize.Button);
				informationLabel.LabelProp = "Specify a valid interpreter first.";
			}
		}
	}
	
	public class MainPanelBinding : ItemOptionsPanel
	{
		MainPanel panel;
		
		public override Gtk.Widget CreatePanelWidget ()
		{
			panel = new MainPanel ();
			panel.InterpreterPath = IronManager.GetInterpreterPath ();
			
			return panel;
		}
		
		public override void ApplyChanges ()
		{
			IronManager.SetInterpreterPath (panel.InterpreterPath);
		}
	}
}

