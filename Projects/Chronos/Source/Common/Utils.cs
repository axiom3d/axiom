#region LGPL License
/*
Chronos World Editor
Copyright (C) 2004 Chris "Antiarc" Heald [antiarc@antiarc.net]

This application is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This application is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/
#endregion

using System;
using System.Collections;
using System.IO;
using System.Windows.Forms;
using System.Text;

using Axiom.Scripting;
using Axiom.Core;
using Axiom.Math;
using Axiom.Configuration;
using Axiom.Graphics;

namespace Chronos.Core
{
	/// <summary>
	/// Summary description for Utils.
	/// </summary>
	public class Utils
	{
		public delegate void ParseHandler(Stream stream, TreeNode node);

		public Utils()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		static public ArrayList getFileList(string root, string filter, bool recurse) 
		{
			ArrayList r = new ArrayList();
			string[] bits;
			if(recurse) 
			{
				string[] dirs = System.IO.Directory.GetDirectories(root);
				for(int i=0; i<dirs.Length;i++) 
				{
					ArrayList fileList = getFileList(dirs[i], filter, recurse);
					foreach(string f in fileList) 
					{
						bits = f.Split('/');
						bits = bits[bits.Length - 1].Split('\\');
						r.Add(bits[bits.Length - 1]);
					}
				}
			}
			string[] files = System.IO.Directory.GetFiles(root, filter);
			
			for(int i=0; i<files.Length;i++) 
			{
				bits = files[i].Split('/');
				bits = bits[bits.Length - 1].Split('\\');
				r.Add(bits[bits.Length - 1]);
			}
			return r;
		}

		static public void ParseMaterial(Stream stream, TreeNode node, bool useSubBits) 
		{
			StreamReader script = new StreamReader(stream, System.Text.Encoding.ASCII);
			string line = "";
			while((line = ParseHelper.ReadLine(script)) != null) 
			{
				// ignore blank lines and comments
				if(line.Length == 0 || line.StartsWith("//")) 
					continue;
				if(line.StartsWith("material")) 
				{
					string[] parms = line.Split(new char[] {' '}, 2);
					if(parms.Length == 2) 
					{
						TreeNode n = new TreeNode(parms[1]);
						n.ImageIndex = 0;
						n.SelectedImageIndex = 0;
						Material m = MaterialManager.Instance.GetByName(parms[1]);
						n.Tag = m;
						node.Nodes.Add (n);
					}
				}
			}
		}

		static public void ParseParticle(Stream stream, TreeNode node) 
		{
			StreamReader script = new StreamReader(stream, System.Text.Encoding.ASCII);
			string line = "";
			while((line = ParseHelper.ReadLine(script)) != null) 
			{
				// ignore blank lines and comments
				if(line.Trim().Length == 0 || line.StartsWith("//")) 
					continue;
				node.Nodes.Add ( new TreeNode(line) );
				SkipBlock(script);
			}
		}

		static public void SkipBlock(TextReader r) 
		{
			string line = "";
			int braces=0;
			while((line = ParseHelper.ReadLine(r)) != null)  
			{
				if(line.Trim() == "{") 
				{
					braces++;
				} 
				else if(line.Trim() == "}") 
				{
					braces--;
					if(braces == 0) return;
				}
			}
		}

		static public ArrayList getMediaDirectories() 
		{
			ArrayList directories = new ArrayList();

			EngineConfig config = new EngineConfig();

			// load the config file
			// relative from the location of debug and releases executables
			config.ReadXml(Path.Combine(Application.StartupPath, "EngineConfig.xml"));

			// interrogate the available resource paths
			foreach( EngineConfig.FilePathRow row in config.FilePath) 
			{
				string fullPath = Path.Combine(Application.StartupPath, row.src);

				switch(row.type) 
				{
					case "Folder":
						directories.Add(fullPath);
						break;
				}
			}
			return directories;
		}

		public static void parseForScripts(string dir, string extension, ParseHandler p, TreeNodeCollection col) 
		{
			string[] dirs = System.IO.Directory.GetDirectories(dir);
			foreach(string s in dirs) 
			{
				parseForScripts(s, extension, p, col);
			}

			ArrayList r = Utils.getFileList(dir, extension, false);
			foreach(string s in r) 
			{
				TreeNode n = new TreeNode(s);
				bool noAdd = false;
				foreach(TreeNode ns in col) 
				{
					if(ns.Text == s)
						noAdd = true;
				}
				if(!noAdd) 
				{
					FileStream st = new FileStream(dir + "/" + s,  FileMode.Open, FileAccess.Read, FileShare.Read);
					try 
					{
						p(st, n);
						//Utils.ParseMaterial(st, n);
						col.Add ( n );
					} 
					finally 
					{
						st.Close();
					}
				}
			}
		}

		static public string FormatException(Exception e, string format, params object[] args)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat(format, args);

			if(e != null) {
				sb.AppendFormat("An exception of type, {0}, was thrown from, {1}.", e.GetType().FullName, e.TargetSite);
				sb.AppendFormat("\nTrace:\n {0}", e.StackTrace);
				sb.Append("\n\nReason:");
				do {
					sb.AppendFormat("\n -{0}", e.Message);
					e = e.InnerException;
				} while(e != null);
			}

			return sb.ToString();
		}
	}
}
