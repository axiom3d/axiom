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

using System;
using Axiom.Graphics;
using Axiom.Scripting.Compiler.AST;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{
    public partial class ScriptCompiler
    {
        public class SharedParametersTranslator : Translator
        {
            #region Translator Implementation

            internal override bool CheckFor( Keywords nodeId, Keywords parentId )
            {
                return nodeId == Keywords.ID_SHARED_PARAMS;
            }

            /// <see cref="Translator.Translate"/>
            [OgreVersion( 1, 7, 2 )]
            public override void Translate( ScriptCompiler compiler, AbstractNode node )
            {
                var obj = (ObjectAbstractNode)node;

                // Must have a name
                if ( string.IsNullOrEmpty( obj.Name ) )
                {
                    compiler.AddError( CompileErrorCode.ObjectNameExpected, obj.File, obj.Line, "shared_params must be given a name" );
                    return;
                }

                object paramsObj;
                GpuProgramParameters.GpuSharedParameters sharedParams;
                ScriptCompilerEvent evt = new CreateGpuSharedParametersScriptCompilerEvent( obj.File, obj.Name, compiler.ResourceGroup );
                bool processed = compiler._fireEvent( ref evt, out paramsObj );

                if ( !processed )
                    sharedParams = GpuProgramManager.Instance.CreateSharedParameters( obj.Name );
                else
                    sharedParams = (GpuProgramParameters.GpuSharedParameters)paramsObj;

                if ( sharedParams == null )
                {
                    compiler.AddError( CompileErrorCode.ObjectAllocationError, obj.File, obj.Line );
                    return;
                }

                foreach ( var i in obj.Children )
                {
                    if ( !( i is PropertyAbstractNode ) )
                        continue;
                    var prop = (PropertyAbstractNode)i;

                    switch ( (Keywords)prop.Id )
                    {
                        #region ID_SHARED_PARAM_NAMED
                        case Keywords.ID_SHARED_PARAM_NAMED:
                            {
                                if ( prop.Values.Count < 2 )
                                {
                                    compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line, "shared_param_named - expected 2 or more arguments" );
                                    continue;
                                }

                                var i0 = getNodeAt( prop.Values, 0 );
                                var i1 = getNodeAt( prop.Values, 1 );

                                if ( !( i0 is AtomAbstractNode ) || !( i1 is AtomAbstractNode ) )
                                {
                                    compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line, "name and parameter type expected" );
                                    continue;
                                }

                                var atom0 = (AtomAbstractNode)i0;
                                var pName = atom0.Value;
                                GpuProgramParameters.GpuConstantType constType;
                                var arraySz = 1;
                                if ( !getConstantType( i1, out constType ) )
                                {
                                    compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line, "invalid parameter type" );
                                    continue;
                                }

                                var isFloat = GpuProgramParameters.GpuConstantDefinition.IsFloatConst( constType );

                                var mFloats = new GpuProgramParameters.FloatConstantList();
                                var mInts = new GpuProgramParameters.IntConstantList();

                                for ( var otherValsi = 2; otherValsi < prop.Values.Count; ++otherValsi )
                                {
                                    if ( !( prop.Values[ otherValsi ] is AtomAbstractNode ) )
                                        continue;

                                    var atom = (AtomAbstractNode)prop.Values[ otherValsi ];

                                    if ( atom.Value[ 0 ] == '[' && atom.Value[ atom.Value.Length - 1 ] == ']' )
                                    {
                                        var arrayStr = atom.Value.Substring( 1, atom.Value.Length - 2 );
                                        if ( !int.TryParse( arrayStr, out arraySz ) )
                                        {
                                            compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line, "invalid array size" );
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        var floatVal = 0.0f;
                                        var intVal = 0;
                                        var parseRes = false;

                                        if ( isFloat )
                                            parseRes = float.TryParse( atom.Value, out floatVal );
                                        else
                                            parseRes = int.TryParse( atom.Value, out intVal );

                                        if ( !parseRes )
                                        {
                                            compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line,
                                                atom.Value + " invalid - extra parameters to shared_param_named must be numbers" );
                                            continue;
                                        }
                                        if ( isFloat )
                                            mFloats.Add( floatVal );
                                        else
                                            mInts.Add( intVal );
                                    }

                                } // each extra param

                                // define constant entry
                                try
                                {
                                    sharedParams.AddConstantDefinition( pName, constType, arraySz );
                                }
                                catch ( Exception e )
                                {
                                    compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line, e.Message );
                                    continue;
                                }

                                // initial values
                                var elemsExpected = GpuProgramParameters.GpuConstantDefinition.GetElementSize( constType, false ) * arraySz;
                                var elemsFound = isFloat ? mFloats.Count : mInts.Count;
                                if ( elemsFound > 0 )
                                {
                                    if ( elemsExpected != elemsFound )
                                    {
                                        compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
                                            "Wrong number of values supplied for parameter type" );
                                        continue;
                                    }

                                    if ( isFloat )
                                        sharedParams.SetNamedConstant( pName, mFloats.Data );
                                    else
                                        sharedParams.SetNamedConstant( pName, mInts.Data );
                                }
                            }
                            break;
                        #endregion ID_SHARED_PARAM_NAMED

                        default:
                            break;
                    }
                }
            }

            #endregion Translator Implementation
        }
    };
}
