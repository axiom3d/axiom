using System;
using System.Collections;
using System.Xml;
using System.IO;
using ICSharpCode.TextEditor.Document;
using System.Collections.Generic;

namespace MaterialLibraryPlugin
{
	public class FileReadHighlighter : ISyntaxModeFileProvider 
	{
		#region ISyntaxModeFileProvider Members

        ICollection<SyntaxMode> ISyntaxModeFileProvider.SyntaxModes
        {
			get 
			{
				List<SyntaxMode> list = new List<SyntaxMode>();

				// we could read this from a file too to include .particle, etc, but screw it for now ;)
				list.Add(new SyntaxMode("Material.xshd", "Ogre/Material", ".material"));

				return list;
			}
		}

		public System.Xml.XmlTextReader GetSyntaxModeFile(SyntaxMode syntaxMode) 
		{
			if(syntaxMode.FileName == "Material.xshd") 
			{
				XmlTextReader reader = new XmlTextReader(File.OpenRead(syntaxMode.FileName));

				return reader;
			}

			return null;
		}

        public void UpdateSyntaxModeList()
        {
            //throw new NotImplementedException();
        }

        #endregion

    } 
}
