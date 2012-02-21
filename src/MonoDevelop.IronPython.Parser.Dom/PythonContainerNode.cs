// 
// PythonContainerNode.cs
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
using System.Collections.Generic;

namespace MonoDevelop.IronPython.Parser.Dom
{
	[Serializable]
	public abstract class PythonContainerNode : PythonNode
	{
		List<PythonClass> m_Classes = new List<PythonClass> ();
		List<PythonFunction> m_Functions = new List<PythonFunction> ();
		HashSet<PythonAttribute> m_Attributes = new HashSet<PythonAttribute> ();
		
		// Lazy created
		List<PythonImport> m_Imports;
		List<PythonFromImport> m_FromImports;
		List<PythonComment> m_Comments;
		
		protected PythonContainerNode ()
		{
		}
		
		public IList<PythonClass> Classes {
			get { return m_Classes; }
		}
		
		public IList<PythonFunction> Functions {
			get { return m_Functions; }
		}
		
		public ISet<PythonAttribute> Attributes {
			get { return m_Attributes; }
		}

		public IList<PythonImport> Imports {
			get {
				if (!SupportsImportStatements)
					throw new NotSupportedException ("Import statements not allowed in this scope");
				
				if (m_Imports == null)
					m_Imports = new List<PythonImport> ();
				
				return m_Imports;
			}
		}
		
		public IList<PythonFromImport> FromImports {
			get {
				if (!SupportsFromStatements)
					throw new NotSupportedException ("From statements not allowed in this scope");
				
				if (m_FromImports == null)
					m_FromImports = new List<PythonFromImport> ();
				
				return m_FromImports;
			}
		}
		
		public IList<PythonComment> Comments {
			get {
				if (m_Comments == null)
					m_Comments = new List<PythonComment> ();
				
				return m_Comments;
			}
		}
		
		public virtual bool SupportsImportStatements {
			get {
				return true;
			}
		}
		
		public virtual bool SupportsFromStatements {
			get {
				return true;
			}
		}
	}
}

