using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Axiom.Components.RTShaderSystem
{
    class TextureAtlasAttib
    {
        public IndexPositionMode positionMode = IndexPositionMode.Relative;
        public int positionOffset = 1;
        public bool autoBorderAdjust = true;

        public TextureAtlasAttib(IndexPositionMode _posMode, int posOffset, bool _autoBorderAdjust)
        {
            this.positionMode = _posMode;
            this.positionOffset = posOffset;
            this.autoBorderAdjust = _autoBorderAdjust;
        }
    }

    public enum IndexPositionMode
    {
        Relative,
        Absolute
    }
    public class TextureAtlasSamplerFactory : SubRenderStateFactory
    {
        

        private Dictionary<string, List<TextureAtlasRecord>> atlases;
        TextureAtlasAttib defaultAtlasAttrib;
        List<TextureAtlasRecord> blankAtlasTable = new List<TextureAtlasRecord>();
        string RTAtlasKey = "RTAtlas"; 

        static TextureAtlasSamplerFactory _instance;

        public TextureAtlasSamplerFactory()
        { }

        public static TextureAtlasSamplerFactory Instance
        {
            get { return _instance; }
        }

        public override string Type
        {
            get { return TextureAtlasSampler.SGXType; }
        }
        internal override SubRenderState CreateInstance(Scripting.Compiler.ScriptCompiler compiler, Scripting.Compiler.AST.PropertyAbstractNode prop, Graphics.Pass pass, SGScriptTranslator stranslator)
        {
            return null;
        }
        public override void WriteInstance(Serialization.MaterialSerializer ser, SubRenderState subRenderState, Graphics.Pass srcPass, Graphics.Pass dstPass)
        {
        }
        protected override SubRenderState CreateInstanceImpl()
        {
            return new TextureAtlasSampler();
        }


        public bool AddTextureAtlasDefinition(string fileName)
        {
           return this.AddTextureAtlasDefinition(fileName, new List<TextureAtlasRecord>());
        }
        public bool AddTextureAtlasDefinition(string fileName, List<TextureAtlasRecord> textureAtlasTable)
        {
            try
            {
                StreamReader inp = new StreamReader(fileName);
                return AddTextureAtlasDefinition(inp, textureAtlasTable);
            }
            catch
            {
                throw;
            }
        }
        public bool AddTextureAtlasDefinition(StreamReader stream)
        {
           return this.AddTextureAtlasDefinition(stream, new List<TextureAtlasRecord>());
        }
        public bool AddTextureAtlasDefinition(StreamReader stream, List<TextureAtlasRecord> textureAtlasTable)
        {

            Dictionary<string, List<TextureAtlasRecord>> tmpMap = new Dictionary<string, List<TextureAtlasRecord>>();
            bool isSuccess = false;
            while (stream.EndOfStream == false)
            {
                string line = stream.ReadLine();
                int indexWithNotWhiteSpace = -1;
                for (int i = 0; i < line.Length; i++)
                {
                    char cur = line[i];
                    if (char.IsWhiteSpace(cur) == false)
                    {
                        indexWithNotWhiteSpace = i;
                        break;
                    }

                }
                //check if this is a line with information
                if (indexWithNotWhiteSpace != -1 && line[indexWithNotWhiteSpace] != '#')
                {
                    //parse the line
                    var strings = line.Split(new string[] { ",\t" }, StringSplitOptions.None);

                    if (strings.Length > 8)
                    {
                        string textureName = strings[1];

                        if (tmpMap.ContainsKey(textureName) == false)
                        {
                            tmpMap.Add(textureName, new List<TextureAtlasRecord>());
                        }

                        //file line format: <original texture filename>/t/t<atlas filename>, <atlas idx>, <atlas type>, <woffset>, <hoffset>, <depth offset>, <width>, <height>
                        //                                                       1                  2            3           4         5             6           7         8
                        TextureAtlasRecord newRecord = new TextureAtlasRecord(strings[0], strings[1], float.Parse(strings[4]), float.Parse(strings[5]), float.Parse(strings[7]), float.Parse(strings[8]), tmpMap[textureName].Count);

                        tmpMap[textureName].Add(newRecord);
                        if (textureAtlasTable != null)
                        {
                            textureAtlasTable.Add(newRecord);
                        }
                        isSuccess = true;
                    }
                }
            }
            //place the information in the main texture
            int maxTextureCount = 0;

            foreach (var key in tmpMap.Keys)
            {
                SetTextureAtlasTable(key, tmpMap[key]);
                if (maxTextureCount >= tmpMap[key].Count)
                {
                    maxTextureCount = tmpMap[key].Count;
                }

                if (maxTextureCount > TextureAtlasSampler.MaxSafeAtlasedTextuers)
                {
                    Axiom.Core.LogManager.Instance.Write("Warning : atlas texture has too many internally defined textures. Shader may fail to compile.");

                }
            }

            return isSuccess;

        }
        

        public void SetTextureAtlasTable(string textureName, List<TextureAtlasRecord> atlasData)
        {
            this.SetTextureAtlasTable(textureName, atlasData, true);
        }
        public void SetTextureAtlasTable(string textureName, List<TextureAtlasRecord> atlasData, bool autoBorderAdjust)
        {
            if ((atlasData == null || atlasData.Count == 0))
                RemoveTextureAtlasTable(textureName);
            else
                atlases.Add(textureName, atlasData);
        }

        public void RemoveTextureAtlasTable(string textureName)
        {
            atlases.Remove(textureName);
        }

        public void RemoveAllTextureAtlasTables()
        {
            atlases.Clear();
        }

        public List<TextureAtlasRecord> GetTextureAtlasTable(string textureName)
        {
            if (atlases.ContainsKey(textureName))
            {
                return atlases[textureName];
            }
            else
            {
                return blankAtlasTable;
            }
        }

        public void SetDefaultAtlasingAttributes(IndexPositionMode mode, int offset, bool autoAdjustBorders)
        {
            defaultAtlasAttrib = new TextureAtlasAttib(mode, offset, autoAdjustBorders);
        }

        public void SetMaterialAtlasingAttributes(Axiom.Graphics.Material material, IndexPositionMode mode, int offset, bool autoAdjustBorders)
        {
            if((material != null) && (material.TechniqueCount != 0))
            {
                //TODO
                //var anyAttrib = material.GetTechnique(0).GetUserObjectBinding.GetUserAny(RTAtlasKey);
            }
        }
        public bool HasMaterialAtlasingAttributes(Axiom.Graphics.Material material, out TextureAtlasAttib attrib)
        {
            attrib = null;
            bool isMaterialSpecific = false;
            if (material != null && material.TechniqueCount > 0)
            {
                //TODO
                //var anyAttrib = material.GetTechnique(0).GetUserObjectBindings().GetUserAny(RTAtlasKey);
                //if (anyAttrib == null)
                 //   isMaterialSpecific = true;
                if (isMaterialSpecific && attrib != null)
                {
                    attrib = null;
                    //attrib = anyAttrib;
                }
                
            }

            return isMaterialSpecific;
        }
        public TextureAtlasAttib DefaultAtlasingAttributes
        {
            get { return defaultAtlasAttrib; }
        }
    }
}
