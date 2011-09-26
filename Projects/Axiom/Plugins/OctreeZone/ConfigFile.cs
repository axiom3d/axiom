#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

The overall design, and a majority of the core engine and rendering code
contained within this library is a derivative of the open source Object Oriented
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/

#endregion

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id:$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System.IO;
using System.Linq;
using System.Xml;
using System.Collections;
using System.Xml.Linq;

#endregion Namespace Declarations

namespace OctreeZone
{
    public class ConfigFile
    {
        private string baseSchema;
        public ConfigFile(string baseSchemaName)
        {
            baseSchema = baseSchemaName;
        }

#if SILVERLIGHT || WINDOWS_PHONE
        private XDocument _doc = new XDocument();
        
        public bool Load(Stream stream)
        {
            var buf = new byte[stream.Length];
            stream.Read(buf, 0, buf.Length);
            var str = System.Text.Encoding.UTF8.GetString( buf, 0, buf.Length );
            _doc = XDocument.Parse(str);
			return true;
		}

		public string this[ string key ]
		{
			get
			{
                return _doc.Element(XName.Get(key,baseSchema)).Value;
			}
		}

		public IEnumerable GetEnumerator()
		{
            foreach (XElement el in _doc.Elements())
            {
                yield return new [] { el.Name.LocalName, el.Value };
            }
		}
#else
        private XmlDocument _doc = new XmlDocument();

        public bool Load(Stream stream)
        {
            _doc.Load(stream);
            return true;
        }

        public string this[string key]
        {
            get
            {
                return _doc[baseSchema][key].InnerText;
            }
        }

        public IEnumerable GetEnumerator()
        {
            foreach (XmlElement el in _doc[baseSchema])
            {
                yield return new string[] { el.Name, el.InnerText };
            }
        }
#endif

        public string getSetting(string key)
        {
            return this[key];
        }
    }
}