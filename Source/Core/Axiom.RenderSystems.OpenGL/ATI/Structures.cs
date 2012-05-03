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
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL.ATI
{
	/// <summary>
	///     Structure used to build rule paths.
	/// </summary>
	public struct TokenRule
	{
		public OperationType operation;
		public Symbol tokenID;
		public string symbol;
		public int errorID;

		public TokenRule( OperationType op )
		{
			operation = op;
			tokenID = 0;
			symbol = "";
			errorID = 0;
		}

		public TokenRule( OperationType op, Symbol tokenID )
		{
			operation = op;
			this.tokenID = tokenID;
			symbol = "";
			errorID = 0;
		}

		public TokenRule( OperationType op, Symbol tokenID, string symbol )
		{
			operation = op;
			this.tokenID = tokenID;
			this.symbol = symbol;
			errorID = 0;
		}
	}

	/// <summary>
	///     Structure used to build Symbol Type library.
	/// </summary>
	public struct SymbolDef
	{
		/// <summary>
		///     Token ID which is the index into the Token Type library.
		/// </summary>
		public Symbol ID;

		/// <summary>
		///     Data used by pass 2 to build native instructions.
		/// </summary>
		public int pass2Data;

		/// <summary>
		///     Context key to fit the Active Context.
		/// </summary>
		public uint contextKey;

		/// <summary>
		///     New pattern to set for Active Context bits.
		/// </summary>
		public uint contextPatternSet;

		/// <summary>
		///     Contexts bits to clear Active Context bits.
		/// </summary>
		public uint contextPatternClear;

		/// <summary>
		///     Index into text table for default name : set at runtime.
		/// </summary>
		public int defTextID;

		/// <summary>
		///     Index into Rule database for non-terminal toke rulepath.
		///     Note: If RuleID is zero the token is terminal.
		/// </summary>
		public int ruleID;

		public SymbolDef( Symbol symbol, int glEnum, ContextKeyPattern ckp )
		{
			ID = symbol;
			pass2Data = glEnum;
			contextKey = (uint)ckp;
			contextPatternSet = 0;
			contextPatternClear = 0;
			defTextID = 0;
			ruleID = 0;
		}

		public SymbolDef( Symbol symbol, int glEnum, ContextKeyPattern ckp, uint cps )
		{
			ID = symbol;
			pass2Data = glEnum;
			contextKey = (uint)ckp;
			contextPatternSet = cps;
			contextPatternClear = 0;
			defTextID = 0;
			ruleID = 0;
		}

		public SymbolDef( Symbol symbol, int glEnum, ContextKeyPattern ckp, ContextKeyPattern cps )
		{
			ID = symbol;
			pass2Data = glEnum;
			contextKey = (uint)ckp;
			contextPatternSet = (uint)cps;
			contextPatternClear = 0;
			defTextID = 0;
			ruleID = 0;
		}
	}

	/// <summary>
	///     Structure for Token instructions.
	/// </summary>
	public struct TokenInstruction
	{
		/// <summary>
		///     Non-Terminal Token Rule ID that generated Token.
		/// </summary>
		public Symbol NTTRuleID;

		/// <summary>
		///     Token ID.
		/// </summary>
		public Symbol ID;

		/// <summary>
		///     Line number in source code where Token was found
		/// </summary>
		public int line;

		/// <summary>
		///     Character position in source where Token was found
		/// </summary>
		public int pos;

		public TokenInstruction( Symbol symbol, Symbol ID )
		{
			NTTRuleID = symbol;
			this.ID = ID;
			line = 0;
			pos = 0;
		}
	}

	public struct TokenInstType
	{
		public string Name;
		public int ID;
	}

	public struct RegisterUsage
	{
		public bool Phase1Write;
		public bool Phase2Write;
	}

	/// <summary>
	///     Structure used to keep track of arguments and instruction parameters.
	/// </summary>
	internal struct OpParam
	{
		public int Arg; // type of argument
		public bool Filled; // has it been filled yet
		public uint MaskRep; // Mask/Replicator flags
		public int Mod; // argument modifier
	}

	internal struct RegModOffset
	{
		public int MacroOffset;
		public int RegisterBase;
		public int OpParamsIndex;

		public RegModOffset( int offset, Symbol regBase, int index )
		{
			MacroOffset = offset;
			RegisterBase = (int)regBase;
			OpParamsIndex = index;
		}
	}

	internal struct MacroRegModify
	{
		public TokenInstruction[] Macro;
		public int MacroSize;
		public RegModOffset[] RegMods;
		public int RegModSize;

		public MacroRegModify( TokenInstruction[] tokens, RegModOffset[] offsets )
		{
			Macro = tokens;
			MacroSize = tokens.Length;
			RegMods = offsets;
			RegModSize = offsets.Length;
		}
	}
}