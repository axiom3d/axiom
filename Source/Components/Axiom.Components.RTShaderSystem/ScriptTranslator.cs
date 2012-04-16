using Axiom.Graphics;
using Axiom.Scripting.Compiler.AST;

namespace Axiom.Components.RTShaderSystem
{
    public class SGScriptTranslator : Axiom.Scripting.Compiler.ScriptCompiler.Translator
    {
        private RenderState generatedRenderState;

        public override bool CheckFor( Scripting.Compiler.Keywords nodeId, Scripting.Compiler.Keywords parentId )
        {
            return true;
        }

        public static bool GetInt( AbstractNode node, out int retVal )
        {
            return Scripting.Compiler.ScriptCompiler.Translator.getInt( node, out retVal );
        }

        public static bool GetBoolean( AbstractNode node, out bool retVal )
        {
            return Scripting.Compiler.ScriptCompiler.Translator.getBoolean( node, out retVal );
        }

        public static bool GetReal( AbstractNode node, out Axiom.Math.Real retVal )
        {
            return Scripting.Compiler.ScriptCompiler.Translator.getReal( node, out retVal );
        }

        public static bool GetString( AbstractNode node, out string retVal )
        {
            return Scripting.Compiler.ScriptCompiler.Translator.getString( node, out retVal );
        }

        public SGScriptTranslator()
        {
            generatedRenderState = null;
        }

        public override void Translate( Scripting.Compiler.ScriptCompiler compiler,
                                        Scripting.Compiler.AST.AbstractNode node )
        {
            var obj = (ObjectAbstractNode)node;
            var parent = (ObjectAbstractNode)obj.Parent;

            //Translate section within a pass context
            if ( parent.Cls == "pass" )
            {
                TranslatePass( compiler, node );
            }
            if ( parent.Cls == "texture_unit" )
            {
                TranslateTextureUnit( compiler, node );
            }
        }

        public virtual SubRenderState GetGeneratedSubRenderState( string typeName )
        {
            if ( generatedRenderState != null )
            {
                //Get the list of the template sub render states composing this render state.
                var rsList = generatedRenderState.TemplateSubRenderStateList;

                foreach ( var it in rsList )
                {
                    if ( it.Type == typeName )
                    {
                        return it;
                    }
                }
            }

            return null;
        }

        private void TranslatePass( Scripting.Compiler.ScriptCompiler compiler, AbstractNode node )
        {
            var obj = (ObjectAbstractNode)node;
            var pass = (Pass)obj.Parent.Context;
            Technique technique = pass.Parent;
            Material material = technique.Parent;
            ShaderGenerator shaderGenerator = ShaderGenerator.Instance;
            string dstTechniqueSchemeName = obj.Name;
            bool techniqueCreated;

            //Make sure scheme name is valid - use default if none exists
            if ( dstTechniqueSchemeName == string.Empty )
            {
                dstTechniqueSchemeName = ShaderGenerator.DefaultSchemeName;
            }

            //Create the shader based tedchnique
            techniqueCreated = shaderGenerator.CreateShaderBasedTechnique( material.Name, material.Group,
                                                                           technique.SchemeName, dstTechniqueSchemeName,
                                                                           shaderGenerator.
                                                                               CreateShaderOverProgrammablePass );

            if ( techniqueCreated )
            {
                //Go over all render state properties
                for ( int i = 0; i < obj.Children.Count; i++ )
                {
                    if ( obj.Children[ i ] is PropertyAbstractNode )
                    {
                        var prop = obj.Children[ i ] as PropertyAbstractNode;
                        SubRenderState subRenderState;

                        //Handle light count property.
                        if ( prop.Name == "light_count" )
                        {
                            if ( prop.Values.Count != 3 )
                            {
                                //compiler.AddError(...);
                            }
                            else
                            {
                                var lightCount = new int[3];
                                if ( !SGScriptTranslator.getInts( prop.Values, 0, out lightCount, 3 ) )
                                {
                                    //compiler.addError(...);
                                }
                                else
                                {
                                    shaderGenerator.CreateScheme( dstTechniqueSchemeName );
                                    RenderState renderState = shaderGenerator.GetRenderState( dstTechniqueSchemeName,
                                                                                              material.Name,
                                                                                              material.Group,
                                                                                              (ushort)pass.Index );
                                    renderState.SetLightCount( lightCount );
                                    renderState.LightCountAutoUpdate = false;
                                }
                            }
                        }
                        else
                        {
                            subRenderState = ShaderGenerator.Instance.createSubRenderState( compiler, prop, pass, this );
                            if ( subRenderState != null )
                            {
                                AddSubRenderState( subRenderState, dstTechniqueSchemeName, material.Name, material.Group,
                                                   pass.Index );
                            }
                        }
                    }
                    else
                    {
                        processNode( compiler, obj.Children[ i ] );
                    }
                }
            }
        }

        private void TranslateTextureUnit( Scripting.Compiler.ScriptCompiler compiler, AbstractNode node )
        {
            var obj = (ObjectAbstractNode)node;
            var texState = (TextureUnitState)obj.Parent.Context;
            Pass pass = texState.Parent;
            Technique technique = pass.Parent;
            Material material = technique.Parent;
            ShaderGenerator shaderGenerator = ShaderGenerator.Instance;
            string dstTechniqueSchemeName = obj.Name;
            bool techniqueCreated;

            //Make sure teh scheme is valid - use default if none exists
            if ( dstTechniqueSchemeName == string.Empty )
            {
                dstTechniqueSchemeName = ShaderGenerator.DefaultSchemeName;
            }

            //check if technique already created
            techniqueCreated = shaderGenerator.HasShaderBasedTechnique( material.Name, material.Group,
                                                                        technique.SchemeName, dstTechniqueSchemeName );

            if ( techniqueCreated == false )
            {
                //Create the shader based techniqe
                techniqueCreated = shaderGenerator.CreateShaderBasedTechnique( material.Name, material.Group,
                                                                               technique.SchemeName,
                                                                               dstTechniqueSchemeName,
                                                                               shaderGenerator.
                                                                                   CreateShaderOverProgrammablePass );
            }

            if ( techniqueCreated )
            {
                //Attempt to get the render state which might have been created by the pass parsing
                generatedRenderState = shaderGenerator.GetRenderState( dstTechniqueSchemeName, material.Name,
                                                                       material.Group, (ushort)pass.Index );

                //Go over all the render state properties
                for ( int i = 0; i < obj.Children.Count; i++ )
                {
                    if ( obj.Children[ i ] is PropertyAbstractNode )
                    {
                        var prop = obj.Children[ i ] as PropertyAbstractNode;
                        SubRenderState subRenderState = ShaderGenerator.Instance.createSubRenderState( compiler, prop,
                                                                                                       texState, this );

                        if ( subRenderState != null )
                        {
                            AddSubRenderState( subRenderState, dstTechniqueSchemeName, material.Name, material.Group,
                                               pass.Index );
                        }
                    }
                    else
                    {
                        processNode( compiler, obj.Children[ i ] );
                    }
                }
            }
        }

        private void AddSubRenderState( SubRenderState newSubRenderState, string dstTechniqueSchemeName,
                                        string materialNem,
                                        string groupName, int passIndex )
        {
            //Check if a different sub render state of the same type already exists
            ShaderGenerator shaderGenerator = ShaderGenerator.Instance;

            //Create a new scheme if needed
            shaderGenerator.CreateScheme( dstTechniqueSchemeName );

            //Update the active render state
            generatedRenderState = shaderGenerator.GetRenderState( dstTechniqueSchemeName, materialNem,
                                                                   (ushort)passIndex );

            //add the new sub render state
            generatedRenderState.AddTemplateSubRenderState( newSubRenderState );
        }
    }
}