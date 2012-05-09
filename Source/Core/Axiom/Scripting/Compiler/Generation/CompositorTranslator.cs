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

using Axiom.Graphics;
using Axiom.Scripting.Compiler.AST;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{
	public partial class ScriptCompiler
	{
		public class CompositorTranslator : Translator
		{
			protected Compositor _Compositor;

			public CompositorTranslator()
				: base()
			{
				this._Compositor = null;
			}

			#region Translator Implementation

			public override bool CheckFor( Keywords nodeId, Keywords parentId )
			{
				return nodeId == Keywords.ID_COMPOSITOR;
			}

			/// <see cref="Translator.Translate"/>
			public override void Translate( ScriptCompiler compiler, AbstractNode node )
			{
				var obj = (ObjectAbstractNode)node;

				if ( obj != null )
				{
					if ( string.IsNullOrEmpty( obj.Name ) )
					{
						compiler.AddError( CompileErrorCode.ObjectNameExpected, obj.File, obj.Line );
						return;
					}
				}
				else
				{
					compiler.AddError( CompileErrorCode.ObjectNameExpected, obj.File, obj.Line );
					return;
				}

				// Create the compositor
				object compObject;
				ScriptCompilerEvent evt = new CreateCompositorScriptCompilerEvent( obj.File, obj.Name, compiler.ResourceGroup );
				var processed = compiler._fireEvent( ref evt, out compObject );

				if ( !processed )
				{
					//TODO
					// The original translated implementation of this code block was simply the following:
					// _Compositor = (Compositor)CompositorManager.Instance.Create( obj.Name, compiler.ResourceGroup );
					// but sometimes it generates an excepiton due to a duplicate resource.
					// In order to avoid the above mentioned exception, the implementation was changed, but
					// it need to be checked when ResourceManager._add will be updated to the lastest version

					var checkForExistingComp = (Compositor)CompositorManager.Instance.GetByName( obj.Name );

					if ( checkForExistingComp == null )
					{
						this._Compositor = (Compositor)CompositorManager.Instance.Create( obj.Name, compiler.ResourceGroup );
					}
					else
					{
						this._Compositor = checkForExistingComp;
					}
				}
				else
				{
					this._Compositor = (Compositor)compObject;
				}

				if ( this._Compositor == null )
				{
					compiler.AddError( CompileErrorCode.ObjectAllocationError, obj.File, obj.Line );
					return;
				}

				// Prepare the compositor
				this._Compositor.RemoveAllTechniques();
				this._Compositor.Origin = obj.File;
				obj.Context = this._Compositor;

				foreach ( var i in obj.Children )
				{
					if ( i is ObjectAbstractNode )
					{
						processNode( compiler, i );
					}
					else
					{
						compiler.AddError( CompileErrorCode.UnexpectedToken, i.File, i.Line, "token not recognized" );
					}
				}
			}

			#endregion Translator Implementation
		}
	}
}