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
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using Axiom.Controllers;
using Axiom.Core;
using Axiom.FileSystem;
using Axiom.MathLib;
using Axiom.Scripting;

namespace Axiom.Graphics {
    /// <summary>
    /// Summary description for MaterialManager.
    /// </summary>
    // TODO: Switch delegates to use a context struct, to make all delegates have the same sig
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
            Material baseWhite = (Material)instance.Create("BaseWhite");
            baseWhite.CreateTechnique().CreatePass();
            baseWhite.Lighting = false;

            instance.defaultTextureFiltering = TextureFiltering.Bilinear;
            instance.defaultAnisotropy = 1;
        }
		
        #endregion

        #region Delegates

        delegate void PassAttributeParser(string[] values, Pass pass);
        delegate void TextureUnitAttributeParser(string[] values, TextureUnitState texUnit);

        #endregion

        #region Member variables

        /// <summary>Lookup table of methods that can be used to parse material attributes.</summary>
        protected Hashtable passAttribParsers = new Hashtable();
        protected Hashtable texUnitAttribParsers = new Hashtable();

        protected TextureFiltering defaultTextureFiltering;
        protected int defaultAnisotropy;
		
        // constants for material section types
        const string GpuProgram = "GpuProgram";
        const string GpuProgramDef = "GpuProgramDef";
        const string TextureUnit = "TextureUnit";
        const string Pass = "Pass";

        #endregion

        #region Properties

        /// <summary>
        ///    Sets the default anisotropy level to be used for loaded textures, for when textures are
        ///    loaded automatically (e.g. by Material class) or when 'Load' is called with the default
        ///    parameters by the application.
        /// </summary>
        public int DefaultAnisotropy {
            get {
                return defaultAnisotropy;
            }
            set {
                defaultAnisotropy = value;

                // TODO: Fix me dammit, need aniso on material
                // reset for all current textures
                foreach(Material material in resourceList.Values) {
                    material.TextureFiltering = defaultTextureFiltering;
                }
            }
        }

        /// <summary>
        ///    Sets the default texture filtering to use for all textures in the engine.
        /// </summary>
        public TextureFiltering DefaultTextureFiltering {
            get {
                return defaultTextureFiltering;
            }
            set {
                defaultTextureFiltering = value;

                // reset for all current textures
                foreach(Material material in resourceList.Values) {
                    material.TextureFiltering = defaultTextureFiltering;
                }
            }
        }

        #endregion Properties

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
                        case Pass:
                            passAttribParsers.Add(parserAtt.Name, Delegate.CreateDelegate(typeof(PassAttributeParser), method));
                            break;

                            // this method should parse a texture layer attribute
                        case TextureUnit:
                            texUnitAttribParsers.Add(parserAtt.Name, Delegate.CreateDelegate(typeof(TextureUnitAttributeParser), method));
                            break;
                    } // switch
                } // for
            } // for
        }


        #region Implementation of ResourceManager

        public new Material GetByName(string name) {
            return (Material)base.GetByName(name);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override Resource Create(string name) {
            if(resourceList[name] != null)
                throw new Axiom.Exceptions.AxiomException(string.Format("Cananot create a duplicate material named '{0}'.", name));

            // create a material
            Material material = new Material(name);

            resourceList[name] = material;
				
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

            // parse gpu programs first
            GpuProgramManager.Instance.ParseAllSources();

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

        #endregion

        /// <summary>
        ///		
        /// </summary>
        protected void ParseScript(Stream stream) {
            StreamReader script = new StreamReader(stream, System.Text.Encoding.ASCII);

            string line = "";

            // parse through the data to the end
            while((line = ParseHelper.ReadLine(script)) != null) {
                // ignore blank lines and comments
                if(line.Length == 0 || line.StartsWith("//")) {
                    continue;
                }

                if(line.StartsWith("material")) {
                    string[] parms = line.Split(new char[] {' '}, 2);

                    if(parms.Length != 2) {
                        ParseHelper.LogParserError("material", "Top level", "Materials must have a name.");
                        // skip this one
                        ParseHelper.SkipToNextCloseBrace(script);
                        continue;
                    }
                    
                    ParseHelper.SkipToNextOpenBrace(script);

                    ParseMaterial(script, parms[1]);
                }
            }
        }


        protected void ParseMaterial(TextReader script, string name) {
            // create a new material
            Material material = (Material)Create(name);

            string line = "";

            while((line = ParseHelper.ReadLine(script)) != null) {
                // ignore blank lines and comments
                if(line.Length == 0 || line.StartsWith("//")) {
                    continue;
                }

                if(line == "}") {
                    // compile the material
                    material.Compile();

                    return;
                }

                if(line == "technique") {
                    ParseHelper.SkipToNextOpenBrace(script);
                    ParseTechnique(script, material);
                }
                else {
                    ParseHelper.LogParserError("material", "material", "Only techniques can be child blocks for a material block.");
                }
            }
        }

        protected void ParseTechnique(TextReader script, Material material) {
            // create a new technique
            Technique technique = material.CreateTechnique();

            string line = "";

            while((line = ParseHelper.ReadLine(script)) != null) {
                // ignore blank lines and comments
                if(line.Length == 0 || line.StartsWith("//")) {
                    continue;
                }

                if(line == "}") {
                    return;
                }

                if(line == "pass") {
                    ParseHelper.SkipToNextOpenBrace(script);
                    ParsePass(script, technique);
                }
                else {
                    ParseHelper.LogParserError("technique", "technique", "Only passes can be child blocks for a technique block.");
                }
            }
        }

        /// <summary>
        ///		Parses a texture layer in a material script.
        /// </summary>
        /// <param name="script"></param>
        /// <param name="material"></param>
        protected void ParseTextureUnit(TextReader script, Pass pass) {
            TextureUnitState texUnit = pass.CreateTextureUnitState("");

            string line = "";

            while((line = ParseHelper.ReadLine(script)) != null) {
                // ignore blank lines and comments
                if(line.Length == 0 || line.StartsWith("//")) {
                    continue;
                }

                // have we reached the end of the layer
                if(line == "}")
                    return;
                else {
                    // split attribute line by spaces
                    string[] values = line.Split(' ');

                    // make sure this attribute exists
                    if(!texUnitAttribParsers.ContainsKey(values[0]))
                        System.Diagnostics.Trace.WriteLine(string.Format("Unknown layer attribute: {0}", values[0]));
                    else {
                        TextureUnitAttributeParser parser = (TextureUnitAttributeParser)texUnitAttribParsers[values[0]];

                        if(values[0] != "texture" && values[0] != "cubic_texture" && 	values[0] != "anim_texture") {
                            // lowercase all params if not a texture attrib of any sort, since texture filenames
                            // can be case sensitive
                            for(int i = 0; i < values.Length; i++)
                                values[0] = values[0].ToLower();
                        }

                        parser(ParseHelper.GetParams(values), texUnit);
                    }
                }
            }
        }

        /// <summary>
        ///		Parses an attribute line for the current pass.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="technique"></param>
        protected void ParsePass(TextReader script, Technique technique) {

            Pass pass = technique.CreatePass();

            string line = "";

            while((line = ParseHelper.ReadLine(script)) != null) {
                if(line.Length == 0 || line.StartsWith("//")) {
                    continue;
                }

                if(line == "}") {
                    return;
                }

                if(line == "texture_unit") {
                    ParseHelper.SkipToNextOpenBrace(script);
                    ParseTextureUnit(script, pass);
                }
                else if(line.StartsWith("vertex_program_ref")) {
                    string[] parms = line.Split(' ');

                    if(parms.Length != 2) {
                        ParseHelper.LogParserError("vertex_program_ref", pass.Parent.Parent.Name, "A name must be specified for a vertex program reference.");
                        ParseHelper.SkipToNextCloseBrace(script);
                        return;
                    }

                    ParseHelper.SkipToNextOpenBrace(script);
                    ParseGpuProgramRef(script, parms[1], pass, GpuProgramType.Vertex);
                }
                else if(line.StartsWith("fragment_program_ref")) {
                    string[] parms = line.Split(' ');

                    if(parms.Length != 2) {
                        ParseHelper.LogParserError("fragment_program_ref", pass.Parent.Parent.Name, "A name must be specified for a fragment program reference.");
                        ParseHelper.SkipToNextCloseBrace(script);
                        return;
                    }
                    
                    ParseHelper.SkipToNextOpenBrace(script);
                    ParseGpuProgramRef(script, parms[1], pass, GpuProgramType.Fragment);
                }
                else {
                    // split attribute line by spaces
                    string[] values = line.Split(' ');

                    // make sure this attribute exists
                    if(!passAttribParsers.ContainsKey(values[0]))
                        System.Diagnostics.Trace.WriteLine(string.Format("Unknown pass attribute: {0}", values[0]));
                    else {
                        PassAttributeParser parser = (PassAttributeParser)passAttribParsers[values[0]];
                        parser(ParseHelper.GetParams(values), pass);
                    }
                }
            }
        }

        protected void ParseGpuProgramRef(TextReader script, string name, Pass pass, GpuProgramType type) {

            string line = "";
            GpuProgramParameters programParams = null;

            switch(type) {
                case GpuProgramType.Vertex:
                    pass.VertexProgramName = name;

                    if(!pass.VertexProgram.IsSupported) {
                        ParseHelper.SkipToNextCloseBrace(script);
                        return;
                    }

                    programParams = pass.VertexProgramParameters;
                    break;

                case GpuProgramType.Fragment:
                    pass.FragmentProgramName = name;

                    if(!pass.FragmentProgram.IsSupported) {
                        ParseHelper.SkipToNextCloseBrace(script);
                        return;
                    }

                    programParams = pass.FragmentProgramParameters;
                    break;
            }

            while((line = ParseHelper.ReadLine(script)) != null) {
                if(line.Length == 0 || line.StartsWith("//")) {
                    continue;
                }

                if(line == "}") {
                    return;
                }

                string[] parms = line.Split(' ');
                int index = 0;

                switch(parms[0]) {
                    case "param_indexed":
                        index = int.Parse(parms[1]);
                        string dataType = parms[2];

                        if(dataType == "float4") {
                            if(parms.Length != 7) {
                                ParseHelper.LogParserError("param_indexed", pass.Parent.Parent.Name, "Float4 gpu program params must have 4 components specified.");
                                ParseHelper.SkipToNextCloseBrace(script);
                                return;
                            }

                            Vector4 vec = new Vector4(float.Parse(parms[3]), float.Parse(parms[4]), float.Parse(parms[5]), float.Parse(parms[6]));
                            programParams.SetConstant(index, vec);
                        }
                        // TODO: more types
                        break;

                    case "param_indexed_auto":
                        index = int.Parse(parms[1]);
                        string constant = parms[2];
                        int extraInfo = (parms.Length == 4) ? int.Parse(parms[3]) : 0;

                        object val = ScriptEnumAttribute.Lookup(constant, typeof(AutoConstants));

                        if(val != null) {
                            AutoConstants autoConstant = (AutoConstants)val;
                            programParams.SetAutoConstant(index, autoConstant, extraInfo);
                        }
                        else {
                            ParseHelper.LogParserError("vertex_program_ref", pass.Parent.Parent.Name, string.Format("Unrecognized auto contant type '{0}'", constant));
                        }

                        break;

                    case "param_named":
                        name = parms[1];
                        dataType = parms[2];

                        if(dataType == "float4") {
                            if(parms.Length != 7) {
                                ParseHelper.LogParserError("param_named", pass.Parent.Parent.Name, "Float4 gpu program params must have 4 components specified.");
                                ParseHelper.SkipToNextCloseBrace(script);
                                return;
                            }

                            Vector4 vec = new Vector4(float.Parse(parms[3]), float.Parse(parms[4]), float.Parse(parms[5]), float.Parse(parms[6]));
                            programParams.SetNamedConstant(name, vec);
                        }
                        // TODO: more types
                        break;

                    case "param_named_auto":

                        string paramName = parms[1];
                        constant = parms[2];
                        extraInfo = 0;

                        // time is a special case here
                        if(constant == "time") {
                            float factor = 1.0f;
                            
                            if(parms.Length == 4) {
                                factor = float.Parse(parms[3]);

                                programParams.SetNamedConstantFromTime(paramName, factor);
                            }

                            continue;
                        }
                        else {
                            extraInfo = (parms.Length == 4) ? int.Parse(parms[3]) : 0;
                        }

                        val = ScriptEnumAttribute.Lookup(constant, typeof(AutoConstants));

                        if(val != null) {
                            AutoConstants autoConstant = (AutoConstants)val;
                            programParams.SetNamedAutoConstant(paramName, autoConstant, extraInfo);
                        }
                        else {
                            ParseHelper.LogParserError("vertex_program_ref", pass.Parent.Parent.Name, string.Format("Unrecognized auto contant type '{0}'", constant));
                        }

                        break;

                    default:
                        ParseHelper.LogParserError("vertex_program_ref", pass.Parent.Parent.Name, "Unknown vertex program ref param");
                        break;
                }
            }
        }

        #region Material attribute parser methods

        [AttributeParser("ambient", Pass)]
        public static void ParseAmbient(string[] values, Pass pass) {
            if(values.Length != 3 && values.Length != 4) {
                ParseHelper.LogParserError("ambient", pass.Parent.Name, "Expected 3-4 params");
                return;
            }
			
            pass.Ambient = ParseHelper.ParseColor(values);
        }

        [AttributeParser("colour_write", Pass)]
        [AttributeParser("color_write", Pass)]
        public static void ParseColorWrite(string[] values, Pass pass) {
            if(values.Length != 1) {
                ParseHelper.LogParserError("color_write", pass.Parent.Name, "Expected value 'on' or 'off'");
                return;
            }

            switch(values[0]) {
                case "on":
                    pass.ColorWrite = true;
                    break;
                case "off":
                    pass.ColorWrite = false;
                    break;
                default:
                    ParseHelper.LogParserError("color_write", pass.Parent.Name, "Invalid depth write value, must be 'on' or 'off'");
                    return;
            }
        }

        [AttributeParser("depth_write", Pass)]
        public static void ParseDepthWrite(string[] values, Pass pass) {
            if(values.Length != 1) {
                ParseHelper.LogParserError("depth_write", pass.Parent.Name, "Expected value 'on' or 'off'");
                return;
            }

            switch(values[0]) {
                case "on":
                    pass.DepthWrite = true;
                    break;
                case "off":
                    pass.DepthWrite = false;
                    break;
                default:
                    ParseHelper.LogParserError("depth_write", pass.Parent.Name, "Invalid depth write value, must be 'on' or 'off'");
                    return;
            }
        }

        [AttributeParser("diffuse", Pass)]
        public static void ParseDiffuse(string[] values, Pass pass) {
            if(values.Length != 3 && values.Length != 4) {
                ParseHelper.LogParserError("diffuse", pass.Parent.Name, "Expected 3-4 params");
                return;
            }

            pass.Diffuse = ParseHelper.ParseColor(values);
        }

        [AttributeParser("shininess", Pass)]
        public static void ParseShininess(string[] values, Pass pass) {
            if(values.Length != 1) {
                ParseHelper.LogParserError("shininess", pass.Parent.Name, "Bad shininess attribute, expected 1 param.");
                return;
            }

            pass.Shininess = float.Parse(values[0]);
        }

        [AttributeParser("specular", Pass)]
        public static void ParseSpecular(string[] values, Pass pass) {
            if(values.Length != 3 && values.Length != 4) {
                ParseHelper.LogParserError("emissive", pass.Parent.Name, "Bad specular attribute, expected 4 or 5 params");
                return;
            }

            pass.Specular = ParseHelper.ParseColor(values);
        }

        [AttributeParser("emissive", Pass)]
        public static void ParseEmissive(string[] values, Pass pass) {
            if(values.Length != 3 && values.Length != 4) {
                ParseHelper.LogParserError("emissive", pass.Parent.Name, "Expected 3-4 params");
                return;
            }

            pass.Emissive = ParseHelper.ParseColor(values);
        }

        [AttributeParser("depth_check", Pass)]
        public static void ParseDepthCheck(string[] values, Pass pass) {
            if(values.Length != 1) {
                ParseHelper.LogParserError("depth_check", pass.Parent.Name, "Expected param 'on' or 'off'");
                return;
            }

            switch(values[0]) {
                case "on":
                    pass.DepthCheck = true;
                    break;
                case "off":
                    pass.DepthCheck = false;
                    break;
                default:
                    ParseHelper.LogParserError("depth_check", pass.Parent.Name, "Invalid depth_check value, must be 'on' or 'off'");
                    return;
            }
        }

        [AttributeParser("iteration", Pass)]
        public static void ParseIteration(string[] values, Pass pass) {
            if(values.Length < 1 || values.Length > 2) {
                ParseHelper.LogParserError("iteration", pass.Parent.Name, "Expected 1 or 2 param values.'");
                return;
            }

            if(values[0] == "once") {
                pass.SetRunOncePerLight(false);
            }
            else if(values[0] == "once_per_light") {
                if(values.Length == 2) {
                    // parse light type

                    // lookup the real enum equivalent to the script value
                    object val = ScriptEnumAttribute.Lookup(values[1], typeof(LightType));

                    // if a value was found, assign it
                    if(val != null) {
                        pass.SetRunOncePerLight(true, true, (LightType)val);
                    }
                    else {
                        ParseHelper.LogParserError("iteration", pass.Parent.Name, "Invalid enum value");
                    }
                }
                else {
                    pass.SetRunOncePerLight(true, false);
                }
            }
            else {
                ParseHelper.LogParserError("iteration", pass.Parent.Name, "Invalid iteration value");
            }
        }

        [AttributeParser("lighting", Pass)]
        public static void ParseLighting(string[] values, Pass pass) {
            if(values.Length != 1) {
                ParseHelper.LogParserError("lighting", pass.Parent.Name, "Expected param 'on' or 'off'");
                return;
            }

            switch(values[0]) {
                case "on":
                    pass.LightingEnabled = true;
                    break;
                case "off":
                    pass.LightingEnabled = false;
                    break;
                default:
                    ParseHelper.LogParserError("lighting", pass.Parent.Name, "Invalid lighting value, must be 'on' or 'off'");
                    return;
            }
        }


        [AttributeParser("max_lights", Pass)]
        public static void ParseMaxLights(string[] values, Pass pass) {
            if(values.Length != 1) {
                ParseHelper.LogParserError("max_lights", pass.Parent.Name, "Expected 1 param value.'");
                return;
            }

            pass.MaxLights = int.Parse(values[0]);
        }

        [AttributeParser("scene_blend", Pass)] 
        public static void ParseSceneBlend(string[] values, Pass pass) {           
            switch (values.Length) { 
                case 1: 
                    // e.g. scene_blend add 
                    // lookup the real enum equivalent to the script value 
                    object val = ScriptEnumAttribute.Lookup(values[0], typeof(SceneBlendType)); 
          
                    // if a value was found, assign it 
                    if(val != null) 
                        pass.SetSceneBlending((SceneBlendType)val); 
                    else 
                        ParseHelper.LogParserError("scene_blend", pass.Parent.Parent.Name, "Invalid enum value"); 
                    break; 
                case 2: 
                    // e.g. scene_blend source_alpha one_minus_source_alpha  
                    // lookup the real enums equivalent to the script values 
                    object srcVal = ScriptEnumAttribute.Lookup(values[0], typeof(SceneBlendFactor)); 
                    object destVal = ScriptEnumAttribute.Lookup(values[1], typeof(SceneBlendFactor)); 
       
                    // if both values were found, assign them 
                    if(srcVal != null && destVal != null) {
                        pass.SetSceneBlending((SceneBlendFactor)srcVal, (SceneBlendFactor)destVal); 
                    }
                    else {
                        if (srcVal == null) {
                            ParseHelper.LogParserError("scene_blend", pass.Parent.Parent.Name, "Invalid enum value: " + values[0].ToString()); 
                        }
                        if (destVal == null) {
                            ParseHelper.LogParserError("scene_blend", pass.Parent.Parent.Name, "Invalid enum value: " + values[1].ToString()); 
                        }
                    }
                    break; 
                default: 
                    pass.SetSceneBlending(SceneBlendFactor.Zero,SceneBlendFactor.Zero); 
                    ParseHelper.LogParserError("scene_blend", pass.Parent.Parent.Name, "Expected 1 or 2 params."); 
                    return; 
            } 
        } 

        [AttributeParser("cull_hardware", Pass)]
        public static void ParseCullHardware(string[] values, Pass pass) {
            if(values.Length != 1) {
                ParseHelper.LogParserError("cull_hardware", pass.Parent.Name, "Expected 2 params.");
                return;
            }

            // lookup the real enum equivalent to the script value
            object val = ScriptEnumAttribute.Lookup(values[0], typeof(CullingMode));

            // if a value was found, assign it
            if(val != null)
                pass.CullMode = (CullingMode)val;
            else
                ParseHelper.LogParserError("cull_hardware", pass.Parent.Name, "Invalid enum value");
        }

        [AttributeParser("cull_software", Pass)]
        public static void ParseCullSoftware(string[] values, Pass pass) {
            if(values.Length != 1) {
                ParseHelper.LogParserError("cull_software", pass.Parent.Name, "Invalid enum value");
                return;
            }

            // lookup the real enum equivalent to the script value
            object val = ScriptEnumAttribute.Lookup(values[0], typeof(ManualCullingMode));

            // if a value was found, assign it
            if(val != null)
                pass.ManualCullMode = (ManualCullingMode)val;
            else
                ParseHelper.LogParserError("cull_software", pass.Parent.Name, "Invalid enum value");
        }
        #endregion

        #region Texture unit attribute parser methods

        [AttributeParser("anim_texture", TextureUnit)]
        public static void ParseAnimTexture(string[] values, TextureUnitState layer) {
            if(values.Length < 3) {
                ParseHelper.LogParserError("anim_texture", layer.Parent.Parent.Parent.Name, "Must have at least 3 params");
                return;
            }

            if(values.Length == 3 && int.Parse(values[1]) != 0) {
                // first form using the base name and number of frames
                layer.SetAnimatedTextureName(values[0], int.Parse(values[1]), float.Parse(values[2]));
            }
            else {
                // second form using individual names
                layer.SetAnimatedTextureName(values, values.Length - 1, float.Parse(values[values.Length - 1]));
            }
        }

        /// Note: Allows both spellings of color :-).
        [AttributeParser("color_op", TextureUnit)]
        [AttributeParser("colour_op", TextureUnit)]
        public static void ParseColorOp(string[] values, TextureUnitState layer) {
            if(values.Length != 1) {
                ParseHelper.LogParserError("color_op", layer.Parent.Parent.Name, "Expected 1 param.");
                return;
            }

            // lookup the real enum equivalent to the script value
            object val = ScriptEnumAttribute.Lookup(values[0], typeof(LayerBlendOperation));

            // if a value was found, assign it
            if(val != null)
                layer.SetColorOperation((LayerBlendOperation)val);
            else
                ParseHelper.LogParserError("color_op", layer.Parent.Parent.Name, "Invalid enum value");
        }

        /// Note: Allows both spellings of color :-).
        [AttributeParser("colour_op_multipass_fallback", TextureUnit)]
        [AttributeParser("color_op_multipass_fallback", TextureUnit)]
        public static void ParseColorOpFallback(string[] values, TextureUnitState layer) {
            // lookup the real enums equivalent to the script values 
            object srcVal = ScriptEnumAttribute.Lookup(values[0], typeof(SceneBlendFactor)); 
            object destVal = ScriptEnumAttribute.Lookup(values[1], typeof(SceneBlendFactor)); 
       
            // if both values were found, assign them 
            if(srcVal != null && destVal != null) {
                layer.SetColorOpMultipassFallback((SceneBlendFactor)srcVal, (SceneBlendFactor)destVal); 
            }
            else {
                if (srcVal == null) {
                    ParseHelper.LogParserError("color_op_multipass_fallback", layer.Parent.Parent.Name, "Invalid enum value: " + values[0].ToString()); 
                }
                if (destVal == null) {
                    ParseHelper.LogParserError("color_op_multipass_fallback", layer.Parent.Parent.Name, "Invalid enum value: " + values[1].ToString()); 
                }
            }
        }

        /// Note: Allows both spellings of color :-).
        [AttributeParser("color_op_ex", TextureUnit)]
        [AttributeParser("colour_op_ex", TextureUnit)]
        public static void ParseColorOpEx(string[] values, TextureUnitState layer) {
            if(values.Length < 3 || values.Length > 12) {
                ParseHelper.LogParserError("color_op_ex", layer.Parent.Parent.Name, "Expected either 3 or 10 params.");
                return;
            }

            LayerBlendOperationEx op = 0;
            LayerBlendSource src1 = 0;
            LayerBlendSource src2 = 0;
            float manual = 0.0f;
            ColorEx colSrc1 = ColorEx.White;
            ColorEx colSrc2 = ColorEx.White;

            try {
                op = (LayerBlendOperationEx)ScriptEnumAttribute.Lookup(values[0], typeof(LayerBlendOperationEx));
                src1 = (LayerBlendSource)ScriptEnumAttribute.Lookup(values[1], typeof(LayerBlendSource));
                src2 = (LayerBlendSource)ScriptEnumAttribute.Lookup(values[2], typeof(LayerBlendSource));

                if(op == LayerBlendOperationEx.BlendManual) {
                    if(values.Length < 4) {
                        ParseHelper.LogParserError("color_op_ex", layer.Parent.Parent.Name, "Expected 4 params for manual blending.");
                        return;
                    }

                    manual = int.Parse(values[3]);
                }

                if(src1 == LayerBlendSource.Manual) {
                    int paramIndex = 3;
                    if(op == LayerBlendOperationEx.BlendManual) {
                        paramIndex++;
                    }

                    if(values.Length < paramIndex + 2) {
                        ParseHelper.LogParserError("color_op_ex", layer.Parent.Parent.Name, "Wrong number of params.");
                        return;
                    }

                    colSrc1.r = float.Parse(values[paramIndex++]);
                    colSrc1.g = float.Parse(values[paramIndex++]);
                    colSrc1.b = float.Parse(values[paramIndex]);
                }

                if(src2 == LayerBlendSource.Manual) {
                    int paramIndex = 3;

                    if(op == LayerBlendOperationEx.BlendManual) {
                        paramIndex++;
                    }

                    if(values.Length < paramIndex + 2) {
                        ParseHelper.LogParserError("color_op_ex", layer.Parent.Parent.Name, "Wrong number of params.");
                        return;
                    }

                    colSrc2.r = float.Parse(values[paramIndex++]);
                    colSrc2.g = float.Parse(values[paramIndex++]);
                    colSrc2.b = float.Parse(values[paramIndex]);
                }
            }
            catch(Exception ex) {
                ParseHelper.LogParserError("color_op_ex", layer.Parent.Parent.Name, ex.Message);
            }

            layer.SetColorOperationEx(op, src1, src2, colSrc1, colSrc2, manual);
        }

        [AttributeParser("cubic_texture", TextureUnit)]
        public static void ParseCubicTexture(string[] values, TextureUnitState layer) {
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
                    ParseHelper.LogParserError("cubic_texture", layer.Parent.Parent.Name, "Last param must be 'combinedUVW' or 'separateUV'");
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
                ParseHelper.LogParserError("cubic_texture", layer.Parent.Parent.Name, "Expected 2 or 7 params.");
			
        }		

        [AttributeParser("env_map", TextureUnit)]
        public static void ParseEnvMap(string[] values, TextureUnitState layer) {
            if(values.Length != 1) {
                ParseHelper.LogParserError("env_map", layer.Parent.Parent.Name, "Expected 1 param.");
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
                    ParseHelper.LogParserError("env_map", layer.Parent.Parent.Name, "Invalid enum value");
            }
        }

        [AttributeParser("filtering", TextureUnit)]
        public static void ParseLayerFiltering(string[] values, TextureUnitState unitState) {
            if(values.Length == 1) {
                // lookup the real enum equivalent to the script value
                object val = ScriptEnumAttribute.Lookup(values[0], typeof(TextureFiltering));

                // if a value was found, assign it
                if(val != null)
                    unitState.SetTextureFiltering((TextureFiltering)val);
                else
                    ParseHelper.LogParserError("filtering", unitState.Parent.Parent.Name, "Invalid enum value");
            }
            else if(values.Length == 3) {
                // complex format
                object val1 = ScriptEnumAttribute.Lookup(values[0], typeof(FilterOptions));
                object val2 = ScriptEnumAttribute.Lookup(values[1], typeof(FilterOptions));
                object val3 = ScriptEnumAttribute.Lookup(values[2], typeof(FilterOptions));

                if(val1 == null || val2 == null || val3 == null) {
                    ParseHelper.LogParserError("filtering", unitState.Parent.Parent.Parent.Name, "Invalid enum value.");
                }
                else {
                    unitState.SetTextureFiltering((FilterOptions)val1, (FilterOptions)val2, (FilterOptions)val3);
                }
            }
            else {
                ParseHelper.LogParserError("filtering", unitState.Parent.Parent.Name, "Expected 1 param.");
                return;
            }
        }

        [AttributeParser("rotate", TextureUnit)]
        public static void ParseRotate(string[] values, TextureUnitState layer) {
            if(values.Length != 1) {
                ParseHelper.LogParserError("rotate", layer.Parent.Parent.Name, "Expected 1 param.");
                return;
            }
			
            layer.SetTextureRotate(float.Parse(values[0]));
        }

        [AttributeParser("rotate_anim", TextureUnit)]
        public static void ParseRotateAnim(string[] values, TextureUnitState layer) {
            if(values.Length != 1) {
                ParseHelper.LogParserError("rotate_anim", layer.Parent.Parent.Name, "Expected 1 param.");
                return;
            }

            layer.SetRotateAnimation(float.Parse(values[0]));
        }

        [AttributeParser("scale", TextureUnit)]
        public static void ParseScale(string[] values, TextureUnitState layer) {
            if(values.Length != 2) {
                ParseHelper.LogParserError("scale", layer.Parent.Parent.Name, "Expected 2 params.");
                return;
            }
			
            layer.SetTextureScale(float.Parse(values[0]), float.Parse(values[1]));
        }

        [AttributeParser("scroll", TextureUnit)]
        public static void ParseScroll(string[] values, TextureUnitState layer) {
            if(values.Length != 2) {
                ParseHelper.LogParserError("scroll", layer.Parent.Parent.Name, "Expected 2 params.");
                return;
            }
			
            layer.SetTextureScroll(float.Parse(values[0]), float.Parse(values[1]));
        }

        [AttributeParser("scroll_anim", TextureUnit)]
        public static void ParseScrollAnim(string[] values, TextureUnitState layer) {
            if(values.Length != 2) {
                ParseHelper.LogParserError("scroll_anim", layer.Parent.Parent.Name, "Expected 2 params.");
                return;
            }

            layer.SetScrollAnimation(float.Parse(values[0]), float.Parse(values[1]));
        }

        [AttributeParser("tex_address_mode", TextureUnit)]
        public static void ParseTexAddressMode(string[] values, TextureUnitState layer) {
            if(values.Length != 1) {
                ParseHelper.LogParserError("tex_address_mode", layer.Parent.Parent.Name, "Expected 1 param.");
                return;
            }

            // lookup the real enum equivalent to the script value
            object val = ScriptEnumAttribute.Lookup(values[0], typeof(TextureAddressing));

            // if a value was found, assign it
            if(val != null)
                layer.TextureAddressing = (TextureAddressing)val;
            else
                ParseHelper.LogParserError("tex_address_mode", layer.Parent.Parent.Name, "Invalid enum value");
        }

        [AttributeParser("tex_coord_set", TextureUnit)]
        public static void ParseTexCoordSet(string[] values, TextureUnitState layer) {
            if(values.Length != 1) {
                ParseHelper.LogParserError("tex_coord_set", layer.Parent.Parent.Name, "Expected texture name");
                return;
            }
			
            layer.TextureCoordSet = int.Parse(values[0]);
        }

        [AttributeParser("texture", TextureUnit)]
        public static void ParseTexture(string[] values, TextureUnitState layer) {
            if(values.Length < 1 || values.Length > 2) {
                ParseHelper.LogParserError("texture", layer.Parent.Parent.Name, "Expected syntax 'texture <name> [type]'");
                return;
            }

            TextureType texType = TextureType.TwoD;

            if(values.Length == 2) {
                // check the transform type
                object val = ScriptEnumAttribute.Lookup(values[1], typeof(TextureType));

                if(val == null) {
                    ParseHelper.LogParserError("texture", layer.Parent.Parent.Parent.Name, "Invalid texture type enum value");
                    return;
                }

                texType = (TextureType)val;
            }
			
            layer.SetTextureName(values[0], texType);
        }

        [AttributeParser("wave_xform", TextureUnit)]
        public static void ParseWaveXForm(string[] values, TextureUnitState layer) {
            if(values.Length != 6) {
                ParseHelper.LogParserError("wave_xform", layer.Parent.Parent.Name, "Expected 6 params.");
                return;
            }

            TextureTransform transType = 0;
            WaveformType waveType = 0;

            // check the transform type
            object val = ScriptEnumAttribute.Lookup(values[0], typeof(TextureTransform));

            if(val == null) {
                ParseHelper.LogParserError("wave_xform", layer.Parent.Parent.Name, "Invalid transform type enum value");
                return;
            }

            transType = (TextureTransform)val;

            // check the wavetype
            val = ScriptEnumAttribute.Lookup(values[1], typeof(WaveformType));

            if(val == null) {
                ParseHelper.LogParserError("wave_xform", layer.Parent.Parent.Name, "Invalid waveform type enum value");
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

        #endregion Texture unit attribute parsing methods
    }
}
