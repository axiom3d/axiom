#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

The rendering portion of the Axiom Engine is an adaption of the excellent 
open source engine OGRE (Object-Oriented Graphics Rendering Engine)
http://ogre.sourceforge.net.  Many thanks to the OGRE team for all of their
hard work and creating such a large community.

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
namespace Axiom.Core
{
	/// <summary>
	/// Summary description for MaterialManager.
	/// </summary>
	public class MaterialManager : ResourceManager
	{
		#region Singleton implementation

		static MaterialManager() { Init(); }
		protected MaterialManager() {}
		protected static MaterialManager instance;

		public static MaterialManager Instance
		{
			get { return instance; }
		}

		public static void Init()
		{
			instance = new MaterialManager();

			instance.Initialize();
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

		#endregion

		/// <summary>
		/// 
		/// </summary>
		public void Initialize()
		{
			// register all attribute parsers
			RegisterParsers();

			// parse material resources
			ParseAllSources(".material");
		}

		/// <summary>
		///		Registers all attribute names with their respective parser.
		/// </summary>
		/// <remarks>
		///		Methods meant to serve as attribute parsers should use a method attribute to 
		/// </remarks>
		protected void RegisterParsers()
		{
			MethodInfo[] methods = this.GetType().GetMethods();
			
			// loop through all methods and look for ones marked with attributes
			for(int i = 0; i < methods.Length; i++)
			{
				// get the current method in the loop
				MethodInfo method = methods[i];
				
				// see if the method should be used to parse one or more material attributes
				AttributeParserAttribute[] parserAtts = 
					(AttributeParserAttribute[])method.GetCustomAttributes(typeof(AttributeParserAttribute), true);

				// loop through each one we found and register its parser
				for(int j = 0; j < parserAtts.Length; j++)
				{
					AttributeParserAttribute parserAtt = parserAtts[j];

					switch(parserAtt.ParserType)
					{
							// this method should parse a material attribute
						case Parser.Material:
							attribParsers.Add(parserAtt.Name, Delegate.CreateDelegate(typeof(MaterialAttributeParser), method));
							break;

							// this method should parse a texture layer attribute
						case Parser.TextureLayer:
							layerAttribParsers.Add(parserAtt.Name, Delegate.CreateDelegate(typeof(TextureLayerAttributeParser), method));
							break;
					}

				}
				
			}
		}

		#region Implementation of ResourceManager

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public override Resource Create(string name)
		{
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
		public Material CreateDeferred(string name)
		{
			if(resourceList[name] != null)
				throw new Axiom.Exceptions.AxiomException(String.Format("Cananot create a duplicate material named '{0}'.", name));

			// create a deferred material
			Material material = new Material(name, true);

			base.Load(material, 1);

			return material;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="priority"></param>
		/// <returns></returns>
		public Material Load(string name, int priority)
		{
			Material material = null;

			// if the resource isn't cached, create it
			if(!resourceList.ContainsKey(name))
			{
				material = (Material)Create(name);
				base.Load(material, priority);
			}
			else
			{
				// get the cached version
				material = (Material)resourceList[name];
			}

			return material;
		}

		/// <summary>
		///		Look for material scripts in all known sources and parse them.
		/// </summary>
		/// <param name="extension"></param>
		public void ParseAllSources(string extension)
		{
			// search archives
			for(int i = 0; i < archives.Count; i++)
			{
				Archive archive = (Archive)archives[i];
				string[] files = archive.GetFileNamesLike("", "*" + extension);

				for(int j = 0; j < files.Length; j++)
				{
					Stream data = archive.ReadFile(files[j]);

					// parse the materials
					ParseScript(data);
				}
			}

			// search common archives
			for(int i = 0; i < commonArchives.Count; i++)
			{
				Archive archive = (Archive)commonArchives[i];
				string[] files = archive.GetFileNamesLike("", "*" + extension);

				for(int j = 0; j < files.Length; j++)
				{
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
		protected void ParseScript(Stream stream)
		{
			StreamReader script = new StreamReader(stream, System.Text.Encoding.ASCII);

			string line = "";
			Material material = null;

			// parse through the data to the end
			while((line = ReadLine(script)) != null)
			{
				// ignore blank lines and comments
				if(!(line.Length == 0 || line.StartsWith("//")))
				{
					if(material == null)
					{
						material = CreateDeferred(line);

						// read another line to skip the beginning brace of the current material
						script.ReadLine();
					}
					else if(line == "}")
					{
						// end of current material
						material = null;
					}
					else if (line == "{")
					{
						// new texture pass
						ParseNewTextureLayer(script, material);
					}
					else
					{
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
		protected void ParseNewTextureLayer(TextReader script, Material material)
		{
			string line = "";

			// create a new texture layer from the current material
			TextureLayer layer = material.AddTextureLayer("");

			while((line = ReadLine(script)) != null)
			{
				if(line.Length != 0 && !line.StartsWith("//"))
				{
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
		protected void ParseAttrib(string line, Material material)
		{
			// split attribute line by spaces
			string[] values = line.Split(' ');

			// make sure this attribute exists
			if(!attribParsers.ContainsKey(values[0]))
				System.Diagnostics.Trace.WriteLine(string.Format("Unknown material attribute: {0}", values[0]));
			else
			{
				MaterialAttributeParser parser = (MaterialAttributeParser)attribParsers[values[0]];
				parser(values, material);
			}
		}

		/// <summary>
		///		Parses an attribute string for a texture layer.
		/// </summary>
		/// <param name="line"></param>
		/// <param name="material"></param>
		/// <param name="layer"></param>
		protected void ParseLayerAttrib(string line, Material material, TextureLayer layer)
		{
			// split attribute line by spaces
			string[] values = line.Split(' ');

			// make sure this attribute exists
			if(!layerAttribParsers.ContainsKey(values[0]))
				System.Diagnostics.Trace.WriteLine(string.Format("Unknown layer attribute: {0}", values[0]));
			else
			{
				TextureLayerAttributeParser parser = (TextureLayerAttributeParser)layerAttribParsers[values[0]];

				if(values[0] != "texture" && values[0] != "cubic_texture" && 	values[0] != "anim_texture")
				{
					// lowercase all params if not a texture attrib of any sort, since texture filenams
					// can be case sensitive
					for(int i = 0; i < values.Length; i++)
						values[0] = values[0].ToLower();
				}

				parser(values, material, layer);
			}
		}

		#region Helper methods

		/// <summary>
		///		Helper method to nip/tuck the string before parsing it.
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		protected string ReadLine(TextReader reader)
		{
			string line = reader.ReadLine();

			if(line != null)
				return line.Replace("\t", "").Trim();
			else
				return null;
		}

		/// <summary>
		///		Parses an array of params and returns a color from it.
		/// </summary>
		/// <param name="values"></param>
		/// <returns></returns>
		public static ColorEx ParseColor(string[] values)
		{
			ColorEx color = new ColorEx();
			color.r = float.Parse(values[1]);
			color.g = float.Parse(values[2]);
			color.b = float.Parse(values[3]);
			color.a = (values.Length == 5) ? float.Parse(values[4]) : 1.0f;

			return color;
		}

		/// <summary>
		///		Helper method to log a formatted error when encountering problems with parsing
		///		an attribute.
		/// </summary>
		/// <param name="attribute"></param>
		/// <param name="materialName"></param>
		/// <param name="expectedParams"></param>
		public static void LogParserError(string attribute, string materialName, string reason)
		{
			string error = string.Format("Bad {0} attribute in material '{1}', wrong number of parameters. Reason: {2}", 
				attribute, materialName, reason);

			System.Diagnostics.Trace.WriteLine(error);
		}

		#endregion

		#region Material attribute parser methods

		/// <summary>
		///		Parses the 'ambient' attribute.
		/// </summary>
		[AttributeParser("ambient", Parser.Material)]
		public static void ParseAmbient(string[] values, Material material)
		{
			if(values.Length != 4 && values.Length != 5)
			{
				LogParserError(values[0], material.Name, "Expected 3-4 params");
				return;
			}
			
			material.Ambient = ParseColor(values);
		}

		/// <summary>
		///		Parses the 'depth_write' attribute.
		/// </summary>
		[AttributeParser("depth_write", Parser.Material)]
		public static void ParseDepthWrite(string[] values, Material material)
		{
			if(values.Length != 2)
			{
				LogParserError(values[0], material.Name, "Expected value 'on' or 'off'");
				return;
			}

			switch(values[1])
			{
				case "on":
					material.DepthWrite = true;
					break;
				case "off":
					material.DepthWrite = false;
					break;
				default:
					LogParserError(values[0], material.Name, "Invalid depth write value, must be 'on' or 'off'");
					return;
			}
		}

		/// <summary>
		///		Parses the 'diffuse' attribute.
		/// </summary>
		[AttributeParser("diffuse", Parser.Material)]
		public static void ParseDiffuse(string[] values, Material material)
		{
			if(values.Length != 4 && values.Length != 5)
			{
				LogParserError(values[0], material.Name, "Expected 3-4 params");
				return;
			}

			material.Diffuse = ParseColor(values);
		}

		/// <summary>
		///		Parses the 'lighting' attribute.
		/// </summary>
		[AttributeParser("lighting", Parser.Material)]
		public static void ParseLighting(string[] values, Material material)
		{
			if(values.Length != 2)
			{
				LogParserError(values[0], material.Name, "Expected param 'on' or 'off'");
				return;
			}

			switch(values[1])
			{
				case "on":
					material.Lighting = true;
					break;
				case "off":
					material.Lighting = false;
					break;
				default:
					LogParserError(values[0], material.Name, "Invalid lighting value, must be 'on' or 'off'");
					return;
			}
		}

		/// <summary>
		///		Parses the 'scene_blend' attribute.
		/// </summary>
		[AttributeParser("scene_blend", Parser.Material)]
		public static void ParseSceneBlend(string[] values, Material material)
		{
			if(values.Length != 2)
			{
				LogParserError(values[0], material.Name, "Expected 1 param.");
				return;
			}

			// lookup the real enum equivalent to the script value
			object val = ScriptEnumAttribute.Lookup(values[1], typeof(SceneBlendType));

			// if a value was found, assign it
			if(val != null)
				material.SetSceneBlending((SceneBlendType)val);
			else
				LogParserError(values[0], material.Name, "Invalid enum value");
		}

		#endregion

		#region Layer attribute parser methods

		/// <summary>
		///		Parses the 'colour_op' attribute.
		/// </summary>
		/// <param name="values"></param>
		/// <param name="material"></param>
		/// <param name="layer"></param>
		/// Note: Allows both spellings of color :-).
		[AttributeParser("color_op", Parser.TextureLayer)]
		[AttributeParser("colour_op", Parser.TextureLayer)]
		public static void ParseColorOp(string[] values, Material material, TextureLayer layer)
		{
			if(values.Length != 2)
			{
				LogParserError(values[0], material.Name, "Expected 1 param.");
				return;
			}

			// lookup the real enum equivalent to the script value
			object val = ScriptEnumAttribute.Lookup(values[1], typeof(LayerBlendOperation));

			// if a value was found, assign it
			if(val != null)
				layer.SetColorOperation((LayerBlendOperation)val);
			else
				LogParserError(values[0], material.Name, "Invalid enum value");
		}

		/// <summary>
		///		Parses the 'cubic_texture' attribute.
		/// </summary>
		/// <param name="values"></param>
		/// <param name="material"></param>
		/// <param name="layer"></param>
		[AttributeParser("cubic_texture", Parser.TextureLayer)]
		public static void ParseCubicTexture(string[] values, Material material, TextureLayer layer)
		{
			bool useUVW;
			string uvw = values[values.Length - 1].ToLower();

			switch(uvw)
			{
				case "combineduvw":
					useUVW = true;
					break;
				case "separateuv":
					useUVW = false;
					break;
				default:
					LogParserError(values[0], material.Name, "Last param must be 'combinedUVW' or 'separateUV'");
					return;
			}

			// use base name to infer the 6 texture names
			if(values.Length == 3)
				layer.SetCubicTexture(values[1], useUVW);
			else if(values.Length == 8)
			{
				// copy the array elements for the 6 tex names
				string[] names = new string[6];
				Array.Copy(values, 1, names, 0, 6);

				layer.SetCubicTexture(names, useUVW);
			}
			else
				LogParserError(values[0], material.Name, "Expected 2 or 7 params.");
			
		}		

		/// <summary>
		///		Parses the 'env_map' attribute.
		/// </summary>
		/// <param name="values"></param>
		/// <param name="material"></param>
		/// <param name="layer"></param>
		[AttributeParser("env_map", Parser.TextureLayer)]
		public static void ParseEnvMap(string[] values, Material material, TextureLayer layer)
		{
			if(values.Length != 2)
			{
				LogParserError(values[0], material.Name, "Expected 1 param.");
				return;
			}

			if(values[0] == "off")
				layer.SetEnvironmentMap(false);
			else
			{
				// lookup the real enum equivalent to the script value
				object val = ScriptEnumAttribute.Lookup(values[1], typeof(EnvironmentMap));

				// if a value was found, assign it
				if(val != null)
					layer.SetEnvironmentMap(true, (EnvironmentMap)val);
				else
					LogParserError(values[0], material.Name, "Invalid enum value");
			}
		}

		/// <summary>
		///		Parses both the 'rotate' and 'rotate_anim' attributes.
		/// </summary>
		/// <param name="values"></param>
		/// <param name="material"></param>
		/// <param name="layer"></param>
		[AttributeParser("rotate", Parser.TextureLayer)]
		[AttributeParser("rotate_anim", Parser.TextureLayer)]
		public static void ParseRotate(string[] values, Material material, TextureLayer layer)
		{
			if(values.Length != 2)
			{
				LogParserError(values[0], material.Name, "Expected 2 params.");
				return;
			}
			else if(values[0] == "rotate")
				layer.SetTextureRotate(float.Parse(values[1]));
			else // rotate_anim
			{
				layer.SetRotateAnimation(float.Parse(values[1]));
			}
		}

		/// <summary>
		///		Parses both the 'scroll' and 'scroll_anim' attributes.
		/// </summary>
		/// <param name="values"></param>
		/// <param name="material"></param>
		/// <param name="layer"></param>
		[AttributeParser("scroll", Parser.TextureLayer)]
		[AttributeParser("scroll_anim", Parser.TextureLayer)]
		public static void ParseScroll(string[] values, Material material, TextureLayer layer)
		{
			if(values.Length != 3)
			{
				LogParserError(values[0], material.Name, "Expected 3 params.");
				return;
			}
			else if(values[0] == "scroll")
				layer.SetTextureScroll(float.Parse(values[1]), float.Parse(values[2]));
			else // scroll_anim
			{
				layer.SetScrollAnimation(float.Parse(values[1]), float.Parse(values[2]));
			}
		}

		/// <summary>
		///		Parses the 'tex_address_mode' attribute.
		/// </summary>
		/// <param name="values"></param>
		/// <param name="material"></param>
		/// <param name="layer"></param>
		[AttributeParser("tex_address_mode", Parser.TextureLayer)]
		public static void ParseTexAddressMode(string[] values, Material material, TextureLayer layer)
		{
			if(values.Length != 2)
			{
				LogParserError(values[0], material.Name, "Expected 1 param.");
				return;
			}

			// lookup the real enum equivalent to the script value
			object val = ScriptEnumAttribute.Lookup(values[1], typeof(TextureAddressing));

			// if a value was found, assign it
			if(val != null)
				layer.TextureAddressing = (TextureAddressing)val;
			else
				LogParserError(values[0], material.Name, "Invalid enum value");
		}

		/// <summary>
		///		Parses the 'texture' attribute.
		/// </summary>
		/// <param name="values"></param>
		/// <param name="material"></param>
		/// <param name="layer"></param>
		[AttributeParser("texture", Parser.TextureLayer)]
		public static void ParseTexture(string[] values, Material material, TextureLayer layer)
		{
			if(values.Length != 2)
			{
				LogParserError(values[0], material.Name, "Expected texture name");
				return;
			}
			
			layer.TextureName = values[1];
		}

		/// <summary>
		///		Parses the 'wave_xform' attribute.
		/// </summary>
		/// <param name="values"></param>
		/// <param name="material"></param>
		/// <param name="layer"></param>
		[AttributeParser("wave_xform", Parser.TextureLayer)]
		public static void ParseWaveXForm(string[] values, Material material, TextureLayer layer)
		{
			if(values.Length != 7)
			{
				LogParserError(values[0], material.Name, "Expected 7 params.");
				return;
			}

			TextureTransform transType = 0;
			WaveformType waveType = 0;

			// check the transform type
			object val = ScriptEnumAttribute.Lookup(values[1], typeof(TextureTransform));

			if(val == null)
			{
				LogParserError(values[0], material.Name, "Invalid transform type enum value");
				return;
			}

			transType = (TextureTransform)val;

			// check the wavetype
			val = ScriptEnumAttribute.Lookup(values[2], typeof(WaveformType));

			if(val == null)
			{
				LogParserError(values[0], material.Name, "Invalid waveform type enum value");
				return;
			}

			waveType = (WaveformType)val;

			// set the transform animation
			layer.SetTransformAnimation(
				transType, 
				waveType, 
				float.Parse(values[3]),
				float.Parse(values[4]),
				float.Parse(values[5]),
				float.Parse(values[6]));
		}

		#endregion
	}

	#region Custom attributes

	/// <summary>
	///		Custom attribute to mark methods as handling the parsing for a material script attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public sealed class AttributeParserAttribute : Attribute
	{
		private string attributeName;
		private Parser parserType;

		public AttributeParserAttribute(string name, Parser parserType)
		{
			this.attributeName = name;
			this.parserType = parserType;
		}

		public string Name
		{
			get { return attributeName; }
		}

		public Parser ParserType
		{
			get { return parserType; }
		}
	}

	/// <summary>
	///		Types of attributes parsers used in scripts.
	/// </summary>
	public enum Parser
	{
		Material,
		TextureLayer
	}

	#endregion
}
