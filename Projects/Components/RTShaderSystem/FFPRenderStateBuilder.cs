using System;
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Graphics;

namespace Axiom.Components.RTShaderSystem
{
    internal class FFPRenderStateBuilder
    {
        private List<SubRenderStateFactory> ffpSubRenderStateFactoryList;
        private static FFPRenderStateBuilder _instance;


        internal bool Initialize()
        {
            SubRenderStateFactory curFactory;

            curFactory = new FFPTransformFactory();
            ShaderGenerator.Instance.AddSubRenderStateFactory( curFactory );
            ffpSubRenderStateFactoryList.Add( curFactory );

            curFactory = new FFPColorFactory();
            ShaderGenerator.Instance.AddSubRenderStateFactory( curFactory );
            ffpSubRenderStateFactoryList.Add( curFactory );

            curFactory = new FFPLightingFactory();
            ShaderGenerator.Instance.AddSubRenderStateFactory( curFactory );
            ffpSubRenderStateFactoryList.Add( curFactory );

            curFactory = new FFPTexturingFactory();
            ShaderGenerator.Instance.AddSubRenderStateFactory( curFactory );
            ffpSubRenderStateFactoryList.Add( curFactory );

            curFactory = new FFPFogFactory();
            ShaderGenerator.Instance.AddSubRenderStateFactory( curFactory );
            ffpSubRenderStateFactoryList.Add( curFactory );

            return true;
        }

        internal void Finalize()
        {
            for ( int it = 0; it < ffpSubRenderStateFactoryList.Count; it++ )
            {
                ShaderGenerator.Instance.RemoveSubRenderStateFactory( ffpSubRenderStateFactoryList[ it ] );
                ffpSubRenderStateFactoryList[ it ] = null;
            }
            ffpSubRenderStateFactoryList.Clear();
        }

        /// <summary>
        ///   build render state from the given pass that emulates the fixed function pipeline behavior
        /// </summary>
        /// <param name="sgPass"> The shader generator pass representation. Contains both source and destination pass </param>
        /// <param name="renderState"> The target render state that will hold the given pass FFP representation </param>
        public void BuildRenderState( ShaderGenerator.SGPass sgPass, TargetRenderState renderState )
        {
            renderState.Reset();

            //Build transformation sub state
            BuildFFPSubRenderState( -1, FFPTransform.FFPType, sgPass, renderState );

            //Build color sub state
            BuildFFPSubRenderState( -1, FFPTransform.FFPType, sgPass, renderState );

            //Build lighting sub state.
            BuildFFPSubRenderState( -1, FFPTransform.FFPType, sgPass, renderState );

            //Build texturing sub state
            BuildFFPSubRenderState( -1, FFPTransform.FFPType, sgPass, renderState );

            //Build fog sub state
            BuildFFPSubRenderState( -1, FFPTransform.FFPType, sgPass, renderState );

            //resolve color stage flags
            ResolveColorStageFlags( sgPass, renderState );
        }

        private void BuildFFPSubRenderState( int subRenderStateOrder, string subRenderStateType,
                                             ShaderGenerator.SGPass sgPass, TargetRenderState renderState )
        {
            if ( subRenderStateOrder == -1 )
            {
                throw new NotImplementedException( "Actual sub render type needs to be declared" );
            }

            SubRenderState subRenderState;

            subRenderState = sgPass.GetCustomFFPSubState( subRenderStateOrder );

            if ( subRenderState == null )
            {
                subRenderState = ShaderGenerator.Instance.CreateSubRenderState( subRenderStateType );
            }

            if ( subRenderState.PreAddToRenderState( renderState, sgPass.SrcPass, sgPass.DstPass ) )
            {
                renderState.AddSubRenderStateInstance( subRenderState );
            }
            else
            {
                ShaderGenerator.Instance.DestroySubRenderState( subRenderState );
            }
        }

        private void ResolveColorStageFlags( ShaderGenerator.SGPass sgPass, TargetRenderState renderState )
        {
            var subRenderStateList = renderState.TemplateSubRenderStateList;
            FFPColor colorSubState = null;

            //find the color sub state
            foreach ( var curSubRenderState in subRenderStateList )
            {
                if ( curSubRenderState.Type == FFPColor.FFPType )
                {
                    colorSubState = curSubRenderState as FFPColor;
                }
            }

            foreach ( var curSubRenderState in subRenderStateList )
            {
                //Add vertex shader specular lighting output in case of specular enabled.
                if ( curSubRenderState.Type == FFPLighting.FFPType )
                {
                    colorSubState.AddResolveStageMask( (int)FFPColor.StageFlags.VsOutputdiffuse );

                    Pass srcPass = sgPass.SrcPass;

                    if ( srcPass.Shininess > 0 && srcPass.Specular != ColorEx.Black )
                    {
                        colorSubState.AddResolveStageMask( (int)FFPColor.StageFlags.VsOutputSpecular );
                    }
                    break;
                }
            }
        }

        public static FFPRenderStateBuilder Instance
        {
            get
            {
                if ( _instance == null )
                {
                    _instance = new FFPRenderStateBuilder();
                }
                return _instance;
            }
        }
    }
}