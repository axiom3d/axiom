using System;
using System.Collections;
using System.IO;
using Chronos.Diagnostics;
using Axiom.Graphics;
using System.Windows.Forms;
using Chronos.Core;

namespace MaterialLibraryPlugin
{
	/// <summary>
	/// Summary description for MaterialProxy.
	/// </summary>
	/// 

	public enum BlockTypes {
		root_block, material, technique, pass, texture_unit, vertex_program,
		fragment_program, vertex_program_ref, fragment_program_ref,
		default_params, invalid_block_type
	};

	public class Block {
		public BlockTypes BlockType;
		private string name;			// For the first param, the name
		public string Params;			// For additional params
		public string MaterialClass;
		public string MaterialPack;
		public ArrayList childBlocks = new ArrayList();
		public int braceCount = 0;
		public Hashtable Content = new Hashtable();

		public string FullName {
			get { 
				if(this.BlockType == BlockTypes.material) {
					return this.MaterialClass + "/" + this.MaterialPack + "/" + name;
				} else {
					return name;
				}
			}
			set {
				name = value;
			}
		}

		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}

		public override string ToString() {
			return name;
		}
	};

	public class Script {
		public Script() {}
		public Script(ArrayList blocks, string pack) {
			this.blocks = blocks;
			this.pack = pack;
		}

		public string pack = String.Empty;
		public ArrayList blocks = new ArrayList();
	}

	public class MaterialEntryManager {
		#region Singleton Implementation

		private static MaterialEntryManager _Instance;
		private Hashtable materialList = new Hashtable();
		private ArrayList scriptList = new ArrayList();
		private Hashtable classList = new Hashtable();

		/// <summary>
		/// The private constructor is called from PluginManager.
		/// </summary>
		private MaterialEntryManager() {
			_Instance = this;
		}

		/// <summary>
		/// Instance allows access to the Root, SceneManager, and SceneGraph from
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

		private string[] block_keys = new string[] {
			"material", "technique", "pass", "texture_unit", "vertex_program",
			"fragment_program", "vertex_program_ref", "fragment_program_ref",
			"default_params"
		};

		private enum TokenType {
			BLOCK_KEY, BLOCK_START, BLOCK_END, CONTENT
		};

		public void ParseScript(string filename, string matClass, string matPack) {
			Stack blockStack = new Stack();
			int lineNo = 0;
			Block root = new Block();
			blockStack.Clear();
			blockStack.Push(root);
			StreamReader fs = File.OpenText(filename);
			string line = string.Empty;
			Block newBlock;
			Block tempBlock;
			if(!classList.ContainsKey(matClass))
				classList.Add(matClass, new Hashtable());
			if(!(classList[matClass] as Hashtable).ContainsKey(matPack))
				(classList[matClass] as Hashtable).Add(matPack, new ArrayList());
			while(fs.Peek() != -1) {
				line = fs.ReadLine().Trim();
				TokenType t = getLineType(line);
				lineNo++;
				if(line.StartsWith("//")) continue;
				switch(getLineType(line)) {
					case TokenType.BLOCK_KEY:
						BlockTypes type = getBlockType(line);
						if(type == BlockTypes.invalid_block_type)
							continue;
						string[] bits = line.Split(" \t".ToCharArray());
						newBlock = new Block();
                        newBlock.BlockType = type;
						newBlock.MaterialClass = matClass;
						newBlock.MaterialPack = matPack;
						if(bits.Length > 1)
							newBlock.Name = bits[1];
						if(bits.Length > 2)
							newBlock.Params = bits[2];
						(blockStack.Peek() as Block).childBlocks.Add(newBlock);
						blockStack.Push(newBlock);
						break;
					case TokenType.BLOCK_START:
						tempBlock = (blockStack.Peek() as Block);
						if(tempBlock.braceCount == 0) {
							tempBlock.braceCount++;
						} else {
							Log.WriteError(String.Format("Error parsing {0} at line {1}: Unexpected block start.", filename, lineNo));
							MessageBox.Show(String.Format("Error parsing {0} at line {1}: Unexpected block start.", filename, lineNo),
								"Parse Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
							return;
						}
						break;
					case TokenType.BLOCK_END:
						tempBlock = (blockStack.Peek() as Block);
						tempBlock.braceCount--;
						if(tempBlock.braceCount == 0) {
							blockStack.Pop();
						} else {
							Log.WriteError(String.Format("Error parsing {0} at line {1}: Block terminator expected.", filename, lineNo));
							MessageBox.Show(String.Format("Error parsing {0} at line {1}: Block terminator expected.", filename, lineNo),
								"Parse Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
							return;
						}
						break;
					case TokenType.CONTENT:
						tempBlock = (blockStack.Peek() as Block);
						bits = line.Split(" \t".ToCharArray());
						if(bits[0] == "texture" || bits[0] == "anim_texture" || bits[0] == "cubic_texture" || bits[0] == "source") {
							for(int i=1; i<bits.Length; i++) 
								bits[i] = handleExternalResources(filename, bits[i], matClass).Replace("\\","/").Replace("//","/");
							line = String.Join(" ", bits);
						}
						bits = line.Split(" \t".ToCharArray(), 2);
						if(bits.Length == 2)
							tempBlock.Content[bits[0]] = bits[1];
						break;
				}
			}
			if(root.braceCount != 0) {
				Log.WriteError(String.Format("Error parsing {0} at line {1}: Unexpected EOF, expecting block terminator.", filename, lineNo));
				MessageBox.Show(String.Format("Error parsing {0} at line {1}: Unexpected EOF, expecting block terminator.", filename, lineNo),
					"Parse Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			foreach(Block block in root.childBlocks) {
				if(block.BlockType != BlockTypes.material) continue;
				if(this.materialList.ContainsKey(block.Name)) {
					Log.WriteError(String.Format("Error parsing {0}: Material {1} already exists. Ignoring new material.", filename, block.Name));
					MessageBox.Show(String.Format("Error parsing {0}: Material {1} already exists. Ignoring new material.", filename, block.Name),
						"Parse Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				} else {
					materialList[block.Name] = block;
				}
				((classList[matClass] as Hashtable)[matPack] as ArrayList).Add(block);
			}
			Script script = new Script(root.childBlocks, matPack);
			this.scriptList.Add(script);
			foreach(Block scriptBlock in script.blocks) {
				MemoryStream ms = assembleBlock(scriptBlock);
				FileStream tfs = new FileStream("material.temp", FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
				ms.WriteTo(tfs);
				fs.Close();
				ms.Seek(0, SeekOrigin.Begin);
				(new Axiom.Serialization.MaterialSerializer()).ParseScript(ms,filename);
			}
		}

		private MemoryStream assembleBlock(Script script) {
			MemoryStream ms = new MemoryStream();
			foreach(Block block in script.blocks) {
				assembleBlock(block, ms, 0);
			}
			ms.Seek(0, SeekOrigin.Begin);
			return ms;
		}

		private MemoryStream assembleBlock(Block block) {
			MemoryStream ms = new MemoryStream();
			assembleBlock(block, ms, 0);
			ms.Seek(0, SeekOrigin.Begin);
			return ms;
		}

		private void assembleBlock(Block block, Stream stream, int tabCount) {
			sWrite(stream, block.BlockType.ToString() + " " + block.FullName + " " + block.Params, tabCount);
			sWrite(stream, "{", tabCount);
			tabCount++;
			foreach(string key in block.Content.Keys) {
				string val = block.Content[key].ToString();
				sWrite(stream, key + " " + val, tabCount);
			}
			foreach(Block b in block.childBlocks)
				assembleBlock(b, stream, tabCount);
			tabCount--;
			sWrite(stream, "}", tabCount);
		}

		public ArrayList ScriptList {
			get { return scriptList; }
		}

		public Hashtable MaterialList {
			get { return materialList; }
		}

		public Hashtable ClassList {
			get { return classList; }
		}

		private string handleExternalResources(string originPath, string resourceName, string matClass) {
			// First try local to the matClass
			if(!Path.IsPathRooted(resourceName)) {
				string destBase = EditorResourceManager.Instance.MediaPath + Path.DirectorySeparatorChar;
				string fname = matClass + Path.DirectorySeparatorChar + resourceName;

				/*
				 *  See if the target file exists in our local cache relative to
				 *  the material class
				 **/

				if(File.Exists(destBase + fname)) {
					return fname;
				} else {
					// Didn't exist. See if it exists relative to our media dir.
					if(File.Exists(destBase + resourceName)) {
						return resourceName;
					} else {
						// Didn't exist. Try to copy it from the old path.
						string origPath = Path.GetDirectoryName(originPath);
						fname = origPath + Path.DirectorySeparatorChar + resourceName;
						if(File.Exists(fname)) {
							// Copy the existing file to our local cache.
							File.Copy(fname, destBase + matClass + Path.DirectorySeparatorChar + resourceName, false);
							return resourceName;
						}
					}
				}
				// This resource is nowhere to be found!
				throw new Exception(String.Format("Resource {0} not found while loading {1}!",
					resourceName, Path.GetFileName(originPath)));
			} else {
				return resourceName;
			}
		}

		private void sWrite(Stream stream, string str, int tabCount) {
			for(int i=0; i<tabCount; i++) stream.WriteByte((byte)'\t');
			str += "\n";
			System.Text.ASCIIEncoding encoder = new System.Text.ASCIIEncoding();
			stream.Write(encoder.GetBytes(str), 0, str.Length);
		}

		private BlockTypes getBlockType(string line) {
			string[] names = Enum.GetNames(typeof(BlockTypes));
			string[] bits = line.Split(" \t".ToCharArray(), 2);
			foreach(string name in names) {
				if(bits[0] == name)
					return (BlockTypes)Enum.Parse(typeof(BlockTypes), name, true);
			}
			return BlockTypes.invalid_block_type;
		}

		private TokenType getLineType(string line) {
			string[] bits = line.Split(" \t".ToCharArray(), 2);
			if(line.StartsWith("{")) return TokenType.BLOCK_START;
			if(line.StartsWith("}")) return TokenType.BLOCK_END;
			foreach(string s in block_keys) {
				if(bits[0].ToLower() == s.ToLower())
					return TokenType.BLOCK_KEY;
			}
			return TokenType.CONTENT;
		}
	}
}
