using System;
using System.Collections;
using System.IO;
using Chronos.Diagnostics;
using Axiom.Graphics;

namespace MaterialLibraryPlugin
{
	/// <summary>
	/// Summary description for MaterialProxy.
	/// </summary>
	/// 

	public enum FirstClassBlockTypes {
		VertexProgram, FragmentProgram, Material
	};

	public struct MaterialEntry {
		public FirstClassBlockTypes BlockType;
		public string Name;				// For the first param, the name
		public string Params;			// For additional params
		public string MaterialClass;
		public string MaterialPack;
	};

	public class MaterialEntryManager {
		#region Singleton Implementation

		private static MaterialEntryManager _Instance;

		/// <summary>
		/// The private constructor is called from PluginManager.
		/// </summary>
		private MaterialEntryManager() {
			_Instance = this;
		}

		/// <summary>
		/// Instance allows access to the Engine, SceneManager, and SceneGraph from
		/// within the plugin.
		/// </summary>
		public static MaterialEntryManager Instance {
			get {
				if(_Instance == null) {
					string message = "Singleton instance not initialized. Please call the plugin constructor first.";
					throw new InvalidOperationException(message);
				}
				return _Instance;
			}
		}

		public static void Init() {
			if(_Instance != null) {
				string message = "Attempting to initialize MaterialEntryManager twice. Please use the existing instance.";
				throw new InvalidOperationException(message);
			}
			_Instance = new MaterialEntryManager();
		}

		#endregion

		Hashtable materials = new Hashtable();
		Hashtable shaders = new Hashtable();

		private const string[] block_keys = {
			"material", "technique", "pass", "texture_unit", "vertex_program",
			"fragment_program", "vertex_program_ref", "fragment_program_ref",
			"default_params"
		};

		private enum TokenType {
			BLOCK_KEY, BLOCK_START, BLOCK_END, CONTENT
		};

		public void Parse(string filename, string matClass, string matPack) {
			StreamReader fs = File.OpenText(filename);
			string line = string.Empty;
			while(fs.Peek() != -1) {
				TokenType t = getLineType(line);
				switch(getLineType(line)) {
					case TokenType.BLOCK_KEY:
						break;
					case TokenType.BLOCK_START:
						break;
					case TokenType.BLOCK_END:
						break;
					case TokenType.CONTENT:
				}
			}
		}

		private TokenType getLineType(string line) {
			string[] bits = line.Split(" \t".ToCharArray(), 2);
			if(line.StartsWith("{")) return TokenType.BLOCK_START;
			if(line.StartsWith("}")) return TokenType.BLOCK_END;
			foreach(string s in block_keys) {
				if(line.StartsWith(s + " "))
					return TokenType.BLOCK_KEY;
			}
			return TokenType.CONTENT;
		}

		public void ParseScript(string filename, string matClass, string matPack) {
			StreamReader fs = File.OpenText(filename);
			string line = String.Empty;
			line = fs.ReadLine().Trim();
			while(line != null) {
				if(line.ToLower().StartsWith("material ")) {
					string[] bits = line.Split(" ".ToCharArray(), 2);
					if(bits.Length == 2) {
						if(materials.ContainsKey(matClass + "/" + matPack + "/" + bits[1])) {
							Log.WriteWarning(
								String.Format(
									"Material {0} already exists in class {1}, pack {2}",
									bits[1], matClass, matPack
								)
							);
							continue;
						}
						MaterialEntry e = new MaterialEntry();
						e.BlockType = FirstClassBlockTypes.Material;
						e.MaterialClass = matClass;
						e.MaterialPack = matPack;
						e.Name = bits[1];
						materials[matClass + "/" + matPack + "/" + e.Name] = e;
					}
					skipBlock(fs);
 				} else if(line.ToLower().StartsWith("vertex_program ") || 
						line.ToLower().StartsWith("fragment_program ")) {
					string[] bits = line.Split(" ".ToCharArray(), 3);
					if(bits.Length >= 2) {
						if(shaders.ContainsKey(matClass + "/" + matPack + "/" + bits[1])) {
							Log.WriteWarning(
								String.Format(
								"Shader {0} already exists in class {1}, pack {2}",
								bits[1], matClass, matPack
								)
							);
							continue;
						}
						
						MaterialEntry e = new MaterialEntry();
						if(line.ToLower().StartsWith("vertex"))
							e.BlockType = FirstClassBlockTypes.VertexProgram;
						else
							e.BlockType = FirstClassBlockTypes.FragmentProgram;
						e.MaterialClass = matClass;
						e.MaterialPack = matPack;
						e.Name = bits[1];
						if(bits.Length == 3)
							e.Params = bits[2];
						shaders[matClass + "/" + matPack + "/" + e.Name] = e;
					}
					while(true) {
						line = fs.ReadLine().Trim();
						if(line.StartsWith("source")) {
							bits = line.Split(" ".ToCharArray(), 2);
							if(bits.Length == 2) {
								Stream s = MaterialManager.FindCommonResourceData(bits[1]);
								if(s == null) {
									// Doesn't exist! Oops!

								} else {
									s.Close();
								}
							}
						}
					}
					
					skipBlock(fs);
				}
				line = fs.ReadLine();
			}
		}

		private void skipBlock(TextReader script) {
			int braceCount = 0;
			bool openedBrace = false;
			string line = script.ReadLine().Trim();
			while(line != null && (openedBrace == false || braceCount > 0)) {
				if(line.StartsWith("{")) {
					braceCount++;
					openedBrace = true;
				} else if(line.StartsWith("}")) {
					braceCount--;
				}
				line = script.ReadLine().Trim();
			}
		}
	}
}
