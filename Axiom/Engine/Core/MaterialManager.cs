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
using System.Collections;
using System.IO;
using System.Reflection;
using Axiom.Controllers;
using Axiom.FileSystem;
using Axiom.Scripting;
using Axiom.SubSystems.Rendering;

// BUG: Calling the indexer for MaterialManager with a non-existant material name, will return null and not notify of a missing material
namespace Axiom.Core {
    /// <summary>
    /// Summary description for MaterialManager.
    /// </summary>
    public class MaterialManager : ResourceManager {
        #region Singleton implementation

        static MaterialManager() { Init(); }
        protected MaterialManager() {}
        protected static MaterialManager instance;

        public static MaterialManager Instance {
            get { return instance; }
        }

        public static void Init() {
            instance = new MaterialManager();

            instance.Initialize();

            // just create the default BaseWhite material
            instance.Create("BaseWhite");
        }
		
        #endregion

        #region Delegates

        delegate void MaterialAttributeParser(string[] values, Material material);
        delegate void TextureLayerAttributeParser(string[] values, Material material, TextureLayer layer);

        #endregion

        #region Member variables

        /// <summary>Lookup table of methods that can be used to parse material attributes.</summary>
        protected Hashtable attribParsers = new Hashtable();
        protected Hashtable layerAttribParsers = new Hashtable();
		
        // constants for material section types
        const string TEX_LAYER = "TextureLayer";
        const string MATERIAL = "Material";

        #endregion

        /// <summary>
        /// 
        /// </summary>
        public void Initialize() {
            // register all attribute parsers
            RegisterParsers();
        }

        /// <summary>
        ///		Registers all attribute names with their respective parser.
        /// </summary>
        /// <remarks>
        ///		Methods meant to serve as attribute parsers should use a method attribute to 
        /// </remarks>
        protected void RegisterParsers() {
            MethodInfo[] methods = this.GetType().GetMethods();
			
            // loop through all methods and look for ones marked with attributes
            for(int i = 0; i < methods.Length; i++) {
                // get the current method in the loop
                MethodInfo method = methods[i];
				
                // see if the method should be used to parse one or more material attributes
                AttributeParserAttribute[] parserAtts = 
                    (AttributeParserAttribute[])method.GetCustomAttributes(typeof(AttributeParserAttribute), true);

                // loop through each one we found and register its parser
                for(int j = 0; j < parserAtts.Length; j++) {
                    AttributeParserAttribute parserAtt = parserAtts[j];

                    switch(parserAtt.ParserType) {
                            // this method should parse a material attribute
                        case MATERIAL:
                            attribParsers.Add(parserAtt.Name, Delegate.CreateDelegate(typeof(MaterialAttributeParser), method));
                            break;

                            // this method should parse a texture layer attribute
                        case TEX_LAYER:
                            layerAttribParsers.Add(parserAtt.Name, Delegate.CreateDelegate(typeof(TextureLayerAttributeParser), method));
                            break;
                    } // switch
                } // for
            } // for
        }

        #region Implementation of ResourceManager

        /// <summary>
        ///    Indexer that gets a material by name.
        /// </summary>
        public new Material this[string name] {
            get {
                return (Material)base[name];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override Resource Create(string name) {
            if(resourceList[name] != null)
                throw new Axiom.Exceptions.AxiomException(String.Format("Cananot create a duplicate material named '{0}'.", name));

            // create a material
            Material material = new Material(name);

            base.Load(material, 1);
				
            return material;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Material CreateDeferred(string name) {
            if(resourceList[name] != null)
                throw new Axiom.Exceptions.AxiomException(String.Format("Cananot create a duplicate material named '{0}'.", name));

            // create a deferred material
            Material material = new Material(name, true);

            // add to the internal resource list
            resourceList.Add(name, material);

            // defer load until first use

            return material;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public Material Load(string name, int priority) {
            Material material = null;

            // if the resource isn't cached, create it
            if(!resourceList.ContainsKey(name)) {
                material = (Material)Create(name);
                base.Load(material, priority);
            }
            else {
                // get the cached version
                material = (Material)resourceList[name];
            }

            return material;
        }

        /// <summary>
        ///		Look for material scripts in all known sources and parse them.
        /// </summary>
        /// <param name="extension"></param>
        public void ParseAllSources() {
            string extension = ".material";

            // search archives
            for(int i = 0; i < archives.Count; i++) {
                Archive archive = (Archive)archives[i];
                string[] files = archive.GetFileNamesLike("", "*" + extension);

                for(int j = 0; j < files.Length; j++) {
                    Stream data = archive.ReadFile(files[j]);

                    // parse the materials
                    ParseScript(data);
                }
            }

            // search common archives
            for(int i = 0; i < commonArchives.Count; i++) {
                Archive archive = (Archive)commonArchives[i];
                string[] files = archive.GetFileNamesLike("", "*" + extension);

                for(int j = 0; j < files.Length; j++) {
                    Stream data = archive.ReadFile(files[j]);

                    // parse the materials
                    ParseScript(data);
                }
            }
        }

        #endregion

        /// <summary>
        ///		
        /// </summary>
        protected void ParseScript(Stream stream) {
            StreamReader script = new StreamReader(stream, System.Text.Encoding.ASCII);

            string line = "";
            Material material = null;

            // parse through the data to the end
            while((line = ParseHelper.ReadLine(script)) != null) {
                // ignore blank lines and comments
                if(!(line.Length == 0 || line.StartsWith("//"))) {
                    if(material == null) {
                        material = CreateDeferred(line);

                        // read another line to skip the beginning brace of the current material
                        script.ReadLine();
                    }
                    else if(line == "}") {
                        // end of current material
                        material = null;
                    }
                    else if (line == "{") {
                        // new texture pass
                        ParseNewTextureLayer(script, material);
                    }
                    else {
                        // attribute line
                        ParseAttrib(line.ToLower(), material);
                    }
                }
            }
        }

        /// <summary>
        ///		Parses a texture layer in a material script.
        /// </summary>
        /// <param name="script"></param>
        /// <param name="material"></param>
        protected void ParseNewTextureLayer(TextReader script, Material material) {
            string line = "";

            // create a new texture layer from the current material
            TextureLayer layer = material.AddTextureLayer("");

            while((line = ParseHelper.ReadLine(script)) != null) {
                if(line.Length != 0 && !line.StartsWith("//")) {
                    // have we reached the end of the layer
                    if(line == "}")
                        return;
                    else
                        ParseLayerAttrib(line, material, layer);
                }
            }
        }

        /// <summary>
        ///		Parses an attribute line for the current material.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="material"></param>
        protected void ParseAttrib(string line, Material material) {
            // split attribute line by spaces
            string[] values = line.Split(' ');

            // make sure this attribute exists
            if(!attribParsers.ContainsKey(values[0]))
                System.Diagnostics.Trace.WriteLine(string.Format("Unknown material attribute: {0}", values[0]));
            else {
                MaterialAttributeParser parser = (MaterialAttributeParser)attribParsers[values[0]];
                parser(ParseHelper.GetParams(values), material);
            }
        }

        /// <summary>
        ///		Parses an attribute string for a texture layer.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="material"></param>
        /// <param name="layer"></param>
        protected void ParseLayerAttrib(string line, Material material, TextureLayer layer) {
            // split attribute line by spaces
            string[] values = line.Split(' ');

            // make sure this attribute exists
            if(!layerAttribParsers.ContainsKey(values[0]))
                System.Diagnostics.Trace.WriteLine(string.Format("Unknown layer attribute: {0}", values[0]));
            else {
                TextureLayerAttributeParser parser = (TextureLayerAttributeParser)layerAttribParsers[values[0]];

                if(values[0] != "texture" && values[0] != "cubic_texture" && 	values[0] != "anim_texture") {
                    // lowercase all params if not a texture attrib of any sort, since texture filenames
                    // can be case sensitive
                    for(int i = 0; i < values.Length; i++)
                        values[0] = values[0].ToLower();
                }

                parser(ParseHelper.GetParams(values), material, layer);
            }
        }

        #region Material attribute parser methods

        [AttributeParser("ambient", MATERIAL)]
        public static void ParseAmbient(string[] values, Material material) {
            if(values.Length != 3 && values.Length != 4) {
                ParseHelper.LogParserError("ambient", material.Name, "Expected 3-4 params");
                return;
            }
			
            material.Ambient = ParseHelper.ParseColor(values);
        }

        [AttributeParser("depth_write", MATERIAL)]
        public static void ParseDepthWrite(string[] values, Material material) {
            if(values.Length != 1) {
                ParseHelper.LogParserError("depth_write", material.Name, "Expected value 'on' or 'off'");
                return;
            }

            switch(values[0]) {
                case "on":
                    material.DepthWrite = true;
                    break;
                case "off":
                    material.DepthWrite = false;
                    break;
                default:
                    ParseHelper.LogParserError("depth_write", material.Name, "Invalid depth write value, must be 'on' or 'off'");
                    return;
            }
        }

        [AttributeParser("diffuse", MATERIAL)]
        public static void ParseDiffuse(string[] values, Material material) {
            if(values.Length != 3 && values.Length != 4) {
                ParseHelper.LogParserError("diffuse", material.Name, "Expected 3-4 params");
                return;
            }

            material.Diffuse = ParseHelper.ParseColor(values);
        }

        [AttributeParser("lighting", MATERIAL)]
        public static void ParseLighting(string[] values, Material material) {
            if(values.Length != 1) {
                ParseHelper.LogParserError("lighting", material.Name, "Expected param 'on' or 'off'");
                return;
            }

            switch(values[0]) {
                case "on":
                    material.Lighting = true;
                    break;
                case "off":
                    material.Lighting = false;
                    break;
                default:
                    ParseHelper.LogParserError("lighting", material.Name, "Invalid lighting value, must be 'on' or 'off'");
                    return;
            }
        }

        [AttributeParser("scene_blend", MATERIAL)]
        public static void ParseSceneBlend(string[] values, Material material) {
            if(values.Length != 1) {
                ParseHelper.LogParserError("scene_blend", material.Name, "Expected 1 param.");
                return;
            }

            // lookup the real enum equivalent to the script value
            object val = ScriptEnumAttribute.Lookup(values[0], typeof(SceneBlendType));

            // if a value was found, assign it
            if(val != null)
                material.SetSceneBlending((SceneBlendType)val);
            else
                ParseHelper.LogParserError("scene_blend", material.Name, "Invalid enum value");
        }

        #endregion

        #region Layer attribute parser methods

        /// Note: Allows both spellings of color :-).
        [AttributeParser("color_op", TEX_LAYER)]
        [AttributeParser("colour_op", TEX_LAYER)]
        public static void ParseColorOp(string[] values, Material material, TextureLayer layer) {
            if(values.Length != 1) {
                ParseHelper.LogParserError("color_op", material.Name, "Expected 1 param.");
                return;
            }

            // lookup the real enum equivalent to the script value
            object val = ScriptEnumAttribute.Lookup(values[0], typeof(LayerBlendOperation));

            // if a value was found, assign it
            if(val != null)
                layer.SetColorOperation((LayerBlendOperation)val);
            else
                ParseHelper.LogParserError("color_op", material.Name, "Invalid enum value");
        }

        [AttributeParser("cubic_texture", TEX_LAYER)]
        public static void ParseCubicTexture(string[] values, Material material, TextureLayer layer) {
            bool useUVW;
            string uvw = values[values.Length - 1].ToLower();

            switch(uvw) {
                case "combineduvw":
                    useUVW = true;
                    break;
                case "separateuv":
                    useUVW = false;
                    break;
                default:
                    ParseHelper.LogParserError("cubic_texture", material.Name, "Last param must be 'combinedUVW' or 'separateUV'");
                    return;
            }

            // use base name to infer the 6 texture names
            if(values.Length == 2)
                layer.SetCubicTexture(values[0], useUVW);
            else if(values.Length == 7) {
                // copy the array elements for the 6 tex names
                string[] names = new string[6];
                Array.Copy(values, 0, names, 0, 6);

                layer.SetCubicTexture(names, useUVW);
            }
            else
                ParseHelper.LogParserError("cubic_texture", material.Name, "Expected 2 or 7 params.");
			
        }		

        [AttributeParser("env_map", TEX_LAYER)]
        public static void ParseEnvMap(string[] values, Material material, TextureLayer layer) {
            if(values.Length != 1) {
                ParseHelper.LogParserError("env_map", material.Name, "Expected 1 param.");
                return;
            }

            if(values[0] == "off")
                layer.SetEnvironmentMap(false);
            else {
                // lookup the real enum equivalent to the script value
                object val = ScriptEnumAttribute.Lookup(values[0], typeof(EnvironmentMap));

                // if a value was found, assign it
                if(val != null)
                    layer.SetEnvironmentMap(true, (EnvironmentMap)val);
                else
                    ParseHelper.LogParserError("env_map", material.Name, "Invalid enum value");
            }
        }

        [AttributeParser("rotate", TEX_LAYER)]
        public static void ParseRotate(string[] values, Material material, TextureLayer layer) {
            if(values.Length != 1) {
                ParseHelper.LogParserError("rotate", material.Name, "Expected 1 param.");
                return;
            }
			
            layer.SetTextureRotate(float.Parse(values[0]));
        }

        [AttributeParser("rotate_anim", TEX_LAYER)]
        public static void ParseRotateAnim(string[] values, Material material, TextureLayer layer) {
            if(values.Length != 1) {
                ParseHelper.LogParserError("rotate_anim", material.Name, "Expected 1 param.");
                return;
            }

            layer.SetRotateAnimation(float.Parse(values[0]));
        }

        [AttributeParser("scroll", TEX_LAYER)]
        public static void ParseScroll(string[] values, Material material, TextureLayer layer) {
            if(values.Length != 2) {
                ParseHelper.LogParserError("scroll", material.Name, "Expected 2 params.");
                return;
            }
			
            layer.SetTextureScroll(float.Parse(values[0]), float.Parse(values[1]));
        }

        [AttributeParser("scroll_anim", TEX_LAYER)]
        public static void ParseScrollAnim(string[] values, Material material, TextureLayer layer) {
            if(values.Length != 2) {
                ParseHelper.LogParserError("scroll_anim", material.Name, "Expected 2 params.");
                return;
            }

            layer.SetScrollAnimation(float.Parse(values[0]), float.Parse(values[1]));
        }

        [AttributeParser("tex_address_mode", TEX_LAYER)]
        public static void ParseTexAddressMode(string[] values, Material material, TextureLayer layer) {
            if(values.Length != 1) {
                ParseHelper.LogParserError("tex_address_mode", material.Name, "Expected 1 param.");
                return;
            }

            // lookup the real enum equivalent to the script value
            object val = ScriptEnumAttribute.Lookup(values[0], typeof(TextureAddressing));

            // if a value was found, assign it
            if(val != null)
                layer.TextureAddressing = (TextureAddressing)val;
            else
                ParseHelper.LogParserError("tex_address_mode", material.Name, "Invalid enum value");
        }

        [AttributeParser("texture", TEX_LAYER)]
        public static void ParseTexture(string[] values, Material material, TextureLayer layer) {
            if(values.Length != 1) {
                ParseHelper.LogParserError("texture", material.Name, "Expected texture name");
                return;
            }
			
            layer.TextureName = values[0];
        }

        [AttributeParser("wave_xform", TEX_LAYER)]
        public static void ParseWaveXForm(string[] values, Material material, TextureLayer layer) {
            if(values.Length != 6) {
                ParseHelper.LogParserError("wave_xform", material.Name, "Expected 6 params.");
                return;
            }

            TextureTransform transType = 0;
            WaveformType waveType = 0;

            // check the transform type
            object val = ScriptEnumAttribute.Lookup(values[0], typeof(TextureTransform));

            if(val == null) {
                ParseHelper.LogParserError("wave_xform", material.Name, "Invalid transform type enum value");
                return;
            }

            transType = (TextureTransform)val;

            // check the wavetype
            val = ScriptEnumAttribute.Lookup(values[1], typeof(WaveformType));

            if(val == null) {
                ParseHelper.LogParserError("wave_xform", material.Name, "Invalid waveform type enum value");
                return;
            }

            waveType = (WaveformType)val;

            // set the transform animation
            layer.SetTransformAnimation(
                transType, 
                waveType, 
                float.Parse(values[2]),
                float.Parse(values[3]),
                float.Parse(values[4]),
                float.Parse(values[5]));
        }

        #endregion
    }
}
