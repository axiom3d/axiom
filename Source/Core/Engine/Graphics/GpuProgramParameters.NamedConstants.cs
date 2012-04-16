#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System.Diagnostics;

using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	public partial class GpuProgramParameters
	{
		/// <summary>
		/// Find a constant definition for a named parameter.
		/// <remarks>
		/// This method returns null if the named parameter did not exist, unlike
		/// <see cref="GetConstantDefinition" /> which is more strict; unless you set the 
		/// last parameter to true.
		/// </remarks>
		/// </summary>
		/// <param name="name">The name to look up</param>
		/// <param name="throwExceptionIfNotFound"> If set to true, failure to find an entry
		/// will throw an exception.</param>
		[OgreVersion( 1, 7, 2790 )]
#if NET_40
		public GpuConstantDefinition FindNamedConstantDefinition( string name, bool throwExceptionIfNotFound = false )
#else
		public GpuConstantDefinition FindNamedConstantDefinition( string name, bool throwExceptionIfNotFound )
#endif
		{
			if ( _namedConstants == null )
			{
				if ( throwExceptionIfNotFound )
				{
					throw new AxiomException( "Named constants have not been initialized, perhaps a compile error." );
				}

				return null;
			}

			GpuConstantDefinition def;
			if ( !_namedConstants.Map.TryGetValue( name, out def ) )
			{
				if ( throwExceptionIfNotFound )
				{
					throw new AxiomException( "Parameter called {0} does not exist. ", name );
				}

				return null;
			}

			return def;
		}

#if !NET_40
		/// <see cref="FindNamedConstantDefinition(string, bool)"/>
		public GpuConstantDefinition FindNamedConstantDefinition( string name )
		{
			return FindNamedConstantDefinition( name, false );
		}
#endif
	};
}
