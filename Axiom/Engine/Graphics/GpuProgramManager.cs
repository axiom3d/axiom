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
using Axiom.Core;
using Axiom.Exceptions;
using Axiom.FileSystem;
using Axiom.Scripting;

namespace Axiom.Graphics
{
	/// <summary>
	/// 	Summary description for GpuProgramManager.
	/// </summary>
	public abstract class GpuProgramManager : ResourceManager
	{
        #region Singleton implementation

        static GpuProgramManager() { Init(); }
        protected GpuProgramManager() { instance = this; }
        protected static GpuProgramManager instance;

        public static GpuProgramManager Instance {
            get { return instance; }
        }

        public static void Init() {
            instance = null;
        }
		
        #endregion

		#region Fields
		
        /// <summary>
        ///    Collection of syntax codes that this program manager supports.
        /// </summary>
        protected StringCollection syntaxCodes = new StringCollection();

		#endregion
				
		#region Methods
		
        public override Resource Create(string name) {
            throw new AxiomException("You need to create a program using the Load or Create* methods.");
        }

        /// <summary>
        ///    Creates a new GpuProgram.
        /// </summary>
        /// <param name="name">
        ///    Name of the program to create.
        /// </param>
        /// <param name="type">
        ///    Type of the program to create, i.e. vertex or fragment.
        /// </param>
        /// <param name="syntaxCode">
        ///    Syntax of the program, i.e. vs_1_1, arbvp1, etc.
        /// </param>
        /// <returns>
        ///    A new instance of GpuProgram.
        /// </returns>
        public abstract GpuProgram Create(string name, GpuProgramType type, string syntaxCode);

        /// <summary>
        ///    Create a new, unloaded GpuProgram from a file of assembly.
        /// </summary>
        /// <remarks>
        ///    Use this method in preference to the 'load' methods if you wish to define
        ///    a GpuProgram, but not load it yet; useful for saving memory.
        /// </remarks>
        /// <param name="name">
        ///    The name of the program.
        /// </param>
        /// <param name="fileName">
        ///    The file to load.
        /// </param>
        /// <param name="syntaxCode">
        ///    Name of the syntax to use for the program, i.e. vs_1_1, arbvp1, etc.
        /// </param>
        /// <returns>
        ///    An unloaded GpuProgram instance.
        /// </returns>
        public virtual GpuProgram CreateProgram(string name, string fileName, GpuProgramType type, string syntaxCode) {
            GpuProgram program = Create(name, type, syntaxCode);
            program.SourceFile = fileName;
            resourceList[name] = program;
            return program;
        }

        /// <summary>
        ///    Create a new, unloaded GpuProgram from a string of assembly code.
        /// </summary>
        /// <remarks>
        ///    Use this method in preference to the 'load' methods if you wish to define
        ///    a GpuProgram, but not load it yet; useful for saving memory.
        /// </remarks>
        /// <param name="name">
        ///    The name of the program.
        /// </param>
        /// <param name="source">
        ///    The asm source of the program to create.
        /// </param>
        /// <param name="syntaxCode">
        ///    Name of the syntax to use for the program, i.e. vs_1_1, arbvp1, etc.
        /// </param>
        /// <returns>An unloaded GpuProgram instance.</returns>
        public virtual GpuProgram CreateProgramFromString(string name, string source, GpuProgramType type, string syntaxCode) {
            GpuProgram program = Create(name, type, syntaxCode);
            program.Source = source;
            resourceList[name] = program;
            return program;
        }

        /// <summary>
        ///    Creates a new GpuProgramParameters instance which can be used to bind parameters 
        ///    to your programs.
        /// </summary>
        /// <remarks>
        ///    Program parameters can be shared between multiple programs if you wish.
        /// </remarks>
        /// <returns></returns>
        public abstract GpuProgramParameters CreateParameters();

        /// <summary>
        ///    Returns whether a given syntax code (e.g. "ps_1_3", "fp20", "arbvp1") is supported. 
        /// </summary>
        /// <param name="syntaxCode"></param>
        /// <returns></returns>
        public bool IsSyntaxSupported(string syntaxCode) {
            return syntaxCodes.Contains(syntaxCode);
        }

        /// <summary>
        ///    Loads a GPU program from a file of assembly.
        /// </summary>
        /// <remarks>
        ///    This method creates a new program of the type specified as the second parameter.
        ///    As with all types of ResourceManager, this class will search for the file in
        ///    all resource locations it has been configured to look in.
        /// </remarks>
        /// <param name="name">
        ///    Identifying name of the program to load.
        /// </param>
        /// <param name="fileName">
        ///    The file to load.
        /// </param>
        /// <param name="type">
        ///    Type of program to create.
        /// </param>
        /// <param name="syntaxCode">
        ///    Syntax code of the program, i.e. vs_1_1, arbvp1, etc.
        /// </param>
        public virtual GpuProgram Load(string name, string fileName, GpuProgramType type, string syntaxCode) {
            GpuProgram program = Create(fileName, type, syntaxCode);
            base.Load(program, 1);
            return program;
        }

        /// <summary>
        ///    Loads a GPU program from a string containing the assembly source.
        /// </summary>
        /// <remarks>
        ///    This method creates a new program of the type specified as the second parameter.
        ///    As with all types of ResourceManager, this class will search for the file in
        ///    all resource locations it has been configured to look in.
        /// </remarks>
        /// <param name="name">
        ///    Name used to identify this program.
        /// </param>
        /// <param name="source">
        ///    Source code of the program to load.
        /// </param>
        /// <param name="type">
        ///    Type of program to create.
        /// </param>
        /// <param name="syntaxCode">
        ///    Syntax code of the program, i.e. vs_1_1, arbvp1, etc.
        /// </param>
        public virtual GpuProgram LoadFromString(string name, string source, GpuProgramType type, string syntaxCode) {
            GpuProgram program = Create(name, type, syntaxCode);
            program.Source = source;
            base.Load(program, 1);
            return program;
        }

        /// <summary>
        ///    Used internally to register support for a particular syntax code.
        /// </summary>
        /// <param name="code">The syntax code (i.e. vs_1_1).</param>
        public void PushSyntaxCode(string code) {
            syntaxCodes.Add(code);
        }

		#endregion

        #region Script parsing

        /// <summary>
        ///		Look for .program scripts in all known sources and parse them.
        /// </summary>
        /// <param name="extension"></param>
        public void ParseAllSources() {
            string extension = ".program";

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

        protected void ParseScript(Stream stream) {
            StreamReader script = new StreamReader(stream, System.Text.Encoding.ASCII);

            string line = "";

            // parse through the data to the end
            while((line = ParseHelper.ReadLine(script)) != null) {
                // ignore blank lines and comments
                if(line.Length == 0 || line.StartsWith("//")) {
                    continue;
                }

                // Vertex Programs
                if(line.StartsWith("vertex_program")) {
                    string[] parms = line.Split(' ');

                    if(parms.Length != 3) {
                        ParseHelper.LogParserError("vertex_program", "Top level", "Vertex program declarations must include a type and name.");
                        // skip this one
                        ParseHelper.SkipToNextCloseBrace(script);
                        continue;
                    }

                    ParseHelper.SkipToNextOpenBrace(script);
                    ParseGpuProgram(script, parms[1], parms[2], GpuProgramType.Vertex);
                }
                    // Fragment Programs
                else if(line.StartsWith("fragment_program")) {
                    string[] parms = line.Split(' ');

                    if(parms.Length != 3) {
                        ParseHelper.LogParserError("vertex_program", "Top level", "Fragment program declarations must include a type and name.");
                        // skip this one
                        ParseHelper.SkipToNextCloseBrace(script);
                        continue;
                    }

                    ParseHelper.SkipToNextOpenBrace(script);
                    ParseGpuProgram(script, parms[1], parms[2], GpuProgramType.Fragment);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="script"></param>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="programType"></param>
        protected void ParseGpuProgram(TextReader script, string name, string language, GpuProgramType programType) {

            string line = string.Empty;
            string fileName = string.Empty;
            string profiles = string.Empty;
            Hashtable parmsTable = new Hashtable();

            while((line = ParseHelper.ReadLine(script)) != null) {
                // ignore blank lines and comments
                if(line.Length == 0 || line.StartsWith("//")) {
                    continue;
                }

                if(line == "}") {
                    if(language == "asm") {
                        string profile = (string)parmsTable["syntax"];

                        GpuProgramManager.Instance.CreateProgram(name, fileName, programType, profile);
                    }
                    else {
                        // High level gpu programs
                        HighLevelGpuProgram program = HighLevelGpuProgramManager.Instance.CreateProgram(name, language, programType);
                        program.SourceFile = fileName;

                        // set all extra params
                        foreach(DictionaryEntry entry in parmsTable) {
                            program.SetParam((string)entry.Key, (string)entry.Value);
                        }
                    }

                    return;
                }
                
                string[] atts = line.Split(new char[]{' '}, 2);

                if(atts[0] == "source") {
                    fileName = atts[1];
                }
                else {
                    // store the param for parsing later
                    parmsTable.Add(atts[0], atts[1]);
                }
            }
        }

        #endregion
		
        #region Properties
		
        #endregion

        #region Implementation of ResourceManager

        public new GpuProgram GetByName(string name) {
            // look for a high level program first
            GpuProgram program = HighLevelGpuProgramManager.Instance.GetByName(name);

            // return if found
            if(program != null) {
                return program;
            }

            // return low level program
            return (GpuProgram)base.GetByName(name);
        }

        #endregion
	}
}
