using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Axiom.Graphics;
using MaterialPermutation = System.UInt32;
using Axiom.Core;

namespace Axiom.Demos.DeferredShadingSystem
{
    class MaterialGenerator
    {
        #region Fields and Properties

        protected string _materialBaseName;

        // Names of the bits in the MaterialPermutation bitfield
        protected readonly List<string> _bitNames = new List<string>( 32 );

        protected MaterialPermutation _psMask;
        protected MaterialPermutation _vsMask;
        protected MaterialPermutation _matMask;

        protected IMaterialGeneratorStrategy _generator;

        protected readonly Dictionary<MaterialPermutation, GpuProgram> _vsCache = new Dictionary<MaterialPermutation,GpuProgram>(),
                                                                       _psCache = new Dictionary<MaterialPermutation, GpuProgram>();

        protected readonly Dictionary<MaterialPermutation, Material> _materialCache = new Dictionary<MaterialPermutation, Material>(),
                                                                     _templateCache = new Dictionary<MaterialPermutation, Material>();

        #endregion Fields and Properties

        #region Construction and Destruction

        protected MaterialGenerator()
        {
        }

        #endregion Construction and Destruction

        public Material GetMaterial( MaterialPermutation permutation )
        {
            Material material = null;

            // Check input validity
            int totalBits = this._bitNames.Count;
            int totalPerms = 1 << totalBits;
            Debug.Assert( permutation < totalPerms );

            // Check if material/shader permutation already was generated
            if ( this._materialCache.ContainsKey( permutation ) )
            {
                material = this._materialCache[ permutation ];
            }
            else
            {
                // Create it
                Material template = this.GetTemplateMaterial( permutation );
                GpuProgram vertexShader = this.GetVertexShader( permutation );
                GpuProgram fragmentShader = this.GetPixelShader( permutation );

                // Create material name
                StringBuilder name = new StringBuilder( this._materialBaseName );
                for ( int bit = 0; bit < totalBits; bit++ )
                {
                    if ( ( permutation & ( 1 << bit ) ) != 0 )
                    {
                        name.Append( this._bitNames[ bit ] );
                    }
                }

                LogManager.Instance.Write( String.Format( "DeferredShading : Created Material {0}, VertexShader {1}, PixelShader {2}", name, vertexShader.Name, fragmentShader.Name ) );

                // Create material from template, and set shaders

                material = template.Clone( name.ToString() );
                Technique technique = material.GetTechnique( 0 );
                Pass pass = technique.GetPass( 0 );
                pass.SetFragmentProgram( fragmentShader.Name );
                pass.SetVertexProgram( vertexShader.Name );

                // And store it
                this._materialCache.Add( permutation, material );
            }

            return material;
        }

        protected GpuProgram GetPixelShader( MaterialPermutation permutation )
        {
            GpuProgram program = null;

            if ( this._psCache.ContainsKey( permutation ) )
            {
                program = this._psCache[ permutation ];
            }
            else
            {
                // Create it
                program = this._generator.GeneratePixelShader( permutation );
                this._psCache.Add( permutation, program );
            }

            return program;
        }

        protected GpuProgram GetVertexShader( MaterialPermutation permutation )
        {
            GpuProgram program = null;

            if ( this._vsCache.ContainsKey( permutation ) )
            {
                program = this._vsCache[ permutation ];
            }
            else
            {
                // Create it
                program = this._generator.GenerateVertexShader( permutation );
                this._vsCache.Add( permutation, program );
            }

            return program;
        }

        protected Material GetTemplateMaterial( MaterialPermutation permutation )
        {
            Material material = null;

            if ( this._templateCache.ContainsKey( permutation ) )
            {
                material = this._templateCache[ permutation ];
            }
            else
            {
                // Create it
                material = this._generator.GenerateTemplateMaterial( permutation );
                this._templateCache.Add( permutation, material );
            }
            
            return material;
        }

    }
}
