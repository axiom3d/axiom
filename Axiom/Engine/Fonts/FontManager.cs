#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;
using System.IO;
using Axiom.Core;
using Axiom.Exceptions;
using Axiom.FileSystem;
using Axiom.Scripting;

namespace Axiom.Fonts {
    /// <summary>
    ///    Manages Font resources, parsing .fontdef files and generally organizing them.
    /// </summary>
    public class FontManager : ResourceManager {
        #region Singleton implementation

        private static FontManager instance;
        
        private FontManager() {}

        public static FontManager Instance {
            get { 
                return instance; 
            }
        }

        public static void Init() {
            if (instance != null) {
                throw new ApplicationException("FontManager initialized twice!");
            }
            instance = new FontManager();
            GarbageManager.Instance.Add(instance);
        }
        
        public override void Dispose() {
            base.Dispose();
            if (instance == this) {
                instance = null;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///    Parses all .fontdef scripts available in all resource locations.
        /// </summary>
        public void ParseAllSources() {
            string extension = ".fontdef";

            // search archives
            for(int i = 0; i < archives.Count; i++) {
                Archive archive = (Archive)archives[i];
                string[] files = archive.GetFileNamesLike("", extension);

                for(int j = 0; j < files.Length; j++) {
                    Stream data = archive.ReadFile(files[j]);

                    // parse the materials
                    ParseScript(data);
                }
            }

            // search common archives
            for(int i = 0; i < commonArchives.Count; i++) {
                Archive archive = (Archive)commonArchives[i];
                string[] files = archive.GetFileNamesLike("", extension);

                for(int j = 0; j < files.Length; j++) {
                    Stream data = archive.ReadFile(files[j]);

                    // parse the materials
                    ParseScript(data);
                }
            }
        }

        /// <summary>
        ///    Parse a .fontdef script passed in as a chunk.
        /// </summary>
        /// <param name="script"></param>
        public void ParseScript(Stream stream) {
            StreamReader script = new StreamReader(stream, System.Text.Encoding.ASCII);

            Font font = null;

            string line = "";

            // parse through the data to the end
            while((line = ParseHelper.ReadLine(script)) != null) {
                // ignore blank lines and comments
                if(line.Length == 0 || line.StartsWith("//")) {
                    continue;
                }
                else {
                    if(font == null) {
                        // first valid data should be the font name
                        font = (Font)Create(line);

                        ParseHelper.SkipToNextOpenBrace(script);
                    }
                    else {
                        // currently in a font
                        if(line == "}") {
                            // finished
                            font = null;
                        }
                        else {
                            ParseAttribute(line, font);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///    Parses an attribute of the font definitions.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="font"></param>
        private void ParseAttribute(string line, Font font) {
            string[] parms = line.Split(new char[] {' ', '\t'});
            string attrib = parms[0].ToLower();

            switch(attrib) {
                case "type":
                    if(parms.Length != 2) {
                        ParseHelper.LogParserError(attrib, font.Name, "Invalid number of params for glyph ");
                        return;
                    }
                    else {
                        if(parms[0].ToLower() == "truetype") {
                            font.Type = FontType.TrueType;
                        }
                        else {
                            font.Type = FontType.Image;
                        }
                    }
                    break;

                case "source":
                    if(parms.Length != 2) {
                        ParseHelper.LogParserError("source", font.Name, "Invalid number of params.");
                        return;
                    }

                    // set the source of the font
                    font.Source = parms[1];

                    break;

                case "glyph":
                    if(parms.Length != 6) {
                        ParseHelper.LogParserError("glyph", font.Name, "Invalid number of params.");
                        return;
                    }

                    char glyph = parms[1][0];

                    // set the texcoords for this glyph
                    font.SetGlyphTexCoords(
                        glyph, 
                        float.Parse(parms[2]),
                        float.Parse(parms[3]),
                        float.Parse(parms[4]),
                        float.Parse(parms[5]));

                    break;

                case "size":
                    if(parms.Length != 2) {
                        ParseHelper.LogParserError("size", font.Name, "Invalid number of params.");
                        return;
                    }

                    font.TrueTypeSize = int.Parse(parms[1]);

                    break;

                case "resolution":
                    if(parms.Length != 2) {
                        ParseHelper.LogParserError("resolution", font.Name, "Invalid number of params.");
                        return;
                    }

                    font.TrueTypeResolution = int.Parse(parms[1]);

                    break;

                case "antialias_colour":
                    if(parms.Length != 2) {
                        ParseHelper.LogParserError("antialias_colour", font.Name, "Invalid number of params.");
                        return;
                    }

                    font.AntialiasColor = bool.Parse(parms[1]);

                    break;
            }
        }

        #endregion Methods

        #region Implementation of ResourceManager

        public override void Load(Resource resource, int priority) {
            base.Load (resource, priority);
        }

        public override Resource Create(string name) {
            // either return an existing font if already created, or create a new one
            if(GetByName(name) != null) {
                return GetByName(name);
            }
            else {
                // create a new font and add it to the list of resources
                Font font = new Font(name);

                resourceList[name] = font;

                return font;
            }
        }

		#endregion
    }
}
