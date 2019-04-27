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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.IO;
using Axiom.Collections;
using Axiom.Core;
using Axiom.Scripting;

#endregion Namespace Declarations

namespace Axiom.Fonts
{
    /// <summary>
    ///    Manages Font resources, parsing .fontdef files and generally organizing them.
    /// </summary>
    /// 
    /// <ogre name="FontManager">
    ///     <file name="OgreFontManager.h"   revision="1.10" lastUpdated="6/19/2006" lastUpdatedBy="Borrillis" />
    ///     <file name="OgreFontManager.cpp" revision="1.14" lastUpdated="6/19/2006" lastUpdatedBy="Borrillis" />
    /// </ogre> 
    /// 
    public class FontManager : ResourceManager, ISingleton<FontManager>
    {
        #region ISingleton<FontManager> Implementation

        /// <summary>
        ///     Gets the singleton instance of this class.
        /// </summary>
        public static FontManager Instance
        {
            get
            {
                return Singleton<FontManager>.Instance;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool Initialize(params object[] args)
        {
            return true;
        }

        #endregion ISingleton<FontManager> Implementation

        #region Constructors and Destructor

        /// <summary>
        ///     Internal constructor.  This class cannot be instantiated externally.
        /// </summary>
        public FontManager()
            : base()
        {
            // Loading order
            LoadingOrder = 200.0f;

            // Scripting is supported by this manager
            ScriptPatterns.Add("*.fontdef");
            // Register scripting with resource group manager
            ResourceGroupManager.Instance.RegisterScriptLoader(this);

            // Resource type
            ResourceType = "Font";

            // Register with resource group manager
            ResourceGroupManager.Instance.RegisterResourceManager(ResourceType, this);
        }

        #endregion Constructors and Destructor

        #region Methods

        /// <summary>
        ///    Parses an attribute of the font definitions.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="font"></param>
        protected void parseAttribute(string line, Font font)
        {
            var parms = line.Split(new char[]
                                    {
                                        ' ', '\t'
                                    });
            var attrib = parms[0].ToLower();

            switch (attrib)
            {
                case "type":
                    if (parms.Length != 2)
                    {
                        ParseHelper.LogParserError(attrib, font.Name, "Invalid number of params for glyph ");
                        return;
                    }
                    else
                    {
                        if (parms[1].ToLower() == "truetype")
                        {
                            font.Type = FontType.TrueType;
                        }
                        else
                        {
                            font.Type = FontType.Image;
                        }
                    }
                    break;

                case "source":
                    if (parms.Length != 2)
                    {
                        ParseHelper.LogParserError("source", font.Name, "Invalid number of params.");
                        return;
                    }

                    // set the source of the font
                    font.Source = parms[1];

                    break;

                case "glyph":
                    if (parms.Length != 6)
                    {
                        ParseHelper.LogParserError("glyph", font.Name, "Invalid number of params.");
                        return;
                    }

                    var glyph = parms[1][0];

                    // set the texcoords for this glyph
                    font.SetGlyphTexCoords(glyph, StringConverter.ParseFloat(parms[2]), StringConverter.ParseFloat(parms[3]),
                                            StringConverter.ParseFloat(parms[4]), StringConverter.ParseFloat(parms[5]));

                    break;

                case "size":
                    if (parms.Length != 2)
                    {
                        ParseHelper.LogParserError("size", font.Name, "Invalid number of params.");
                        return;
                    }

                    font.TrueTypeSize = int.Parse(parms[1]);

                    break;

                case "resolution":
                    if (parms.Length != 2)
                    {
                        ParseHelper.LogParserError("resolution", font.Name, "Invalid number of params.");
                        return;
                    }

                    font.TrueTypeResolution = int.Parse(parms[1]);

                    break;

                case "antialias_colour":
                    if (parms.Length != 2)
                    {
                        ParseHelper.LogParserError("antialias_colour", font.Name, "Invalid number of params.");
                        return;
                    }

                    font.AntialiasColor = bool.Parse(parms[1]);

                    break;
            }
        }

        #endregion Methods

        #region ResourceManager Implementation

        protected override Resource _create(string name, ulong handle, string group, bool isManual,
                                             IManualResourceLoader loader, NameValuePairList createParams)
        {
            return new Font(this, name, handle, group, isManual, loader);
        }

        #endregion ResourceManager Implementation

        #region IScriptLoader Implementation

        /// <summary>
        ///    Parse a .fontdef script passed in as a chunk.
        /// </summary>
        public override void ParseScript(Stream stream, string groupName, string fileName)
        {
            var script = new StreamReader(stream, System.Text.Encoding.UTF8);

            Font font = null;

            string line;

            // parse through the data to the end
            while ((line = ParseHelper.ReadLine(script)) != null)
            {
                // ignore blank lines and comments
                if (line.Length == 0 || line.StartsWith("//"))
                {
                    continue;
                }
                else
                {
                    if (font == null)
                    {
                        // No current font
                        // So first valid data should be font name
                        if (line.StartsWith("font "))
                        {
                            // chop off the 'particle_system ' needed by new compilers
                            line = line.Substring(5);
                        }
                        font = (Font)Create(line, groupName);

                        ParseHelper.SkipToNextOpenBrace(script);
                    }
                    else
                    {
                        // currently in a font
                        if (line == "}")
                        {
                            // finished
                            font = null;
                            // NB font isn't loaded until required
                        }
                        else
                        {
                            parseAttribute(line, font);
                        }
                    }
                }
            }
        }

        #endregion IScriptLoader Implementation

        #region IDisposable Implementation

        protected override void dispose(bool disposeManagedResources)
        {
            if (!IsDisposed)
            {
                if (disposeManagedResources)
                {
                    // Unregister with resource group manager
                    ResourceGroupManager.Instance.UnregisterResourceManager(ResourceType);
                    // Unegister scripting with resource group manager
                    ResourceGroupManager.Instance.UnregisterScriptLoader(this);
                    Singleton<FontManager>.Destroy();
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            base.dispose(disposeManagedResources);
        }

        #endregion IDisposable Implementation
    }
}