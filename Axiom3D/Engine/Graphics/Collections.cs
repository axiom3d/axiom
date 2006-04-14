using System;
using System.Collections;
using System.Collections.Generic;

namespace Axiom
{
    /// <summary>
    ///     Generics: List<VertexElement>
    /// </summary>
    public class VertexElementList : ArrayList
    {
    }

    /// <summary>
    ///     Generics: List<TextureEffect>
    /// </summary>
    public class TextureEffectList : List<TextureEffect>
    {
    }

    /// <summary>
    ///     Generics: List<RenderTarget>
    /// </summary>
    public class RenderTargetList : List<RenderTarget>
    {
    }

    /// <summary>
    ///     Generics: List<RenderTexture>
    /// </summary>
    public class RenderTextureList : List<RenderTexture>
    {
    }

    /// <summary>
    ///     Generics: List<Pass>
    /// </summary>
    public class PassList : List<Pass>
    {
    }

    /// <summary>
    ///     Generics: List<Technique>
    /// </summary>
    public class TechniqueList : List<Technique>
    {
    }

    /// <summary>
    ///     Generics: List<TextureUnitState>
    /// </summary>
    public class TextureUnitStateList : ArrayList
    {
    }

    /// <summary>
    ///     Generics: List<AutoConstantEntry>
    /// </summary>
    public class AutoConstantEntryList : List<Axiom.GpuProgramParameters.AutoConstantEntry>
    {
    }

    /// <summary>
    ///     Generics: List<AutoConstantEntry>
    /// </summary>
    public class FloatConstantEntryList : List<Axiom.GpuProgramParameters.FloatConstantEntry>
    {
        public void Resize( int size )
        {
            while ( this.Count < size )
            {
                Add( new GpuProgramParameters.FloatConstantEntry() );
            }
        }
    }

    /// <summary>
    ///     Generics: List<AutoConstantEntry>
    /// </summary>
    public class IntConstantEntryList : ArrayList
    {
        public void Resize( int size )
        {
            while ( this.Count < size )
            {
                Add( new GpuProgramParameters.IntConstantEntry() );
            }
        }
       
    }

    /// <summary>
    ///     Generics: List<IRenderable>
    /// </summary>
    public class RenderableList : List<IRenderable>
    {
    }

    /// <summary>
    ///     Generics: List<EdgeData.Triangle>
    /// </summary>
    public class TriangleList : List<EdgeData.Triangle>
    {
    }

    /// <summary>
    ///     Generics: List<EdgeData.Edge>
    /// </summary>
    public class EdgeList : List<EdgeData.Edge>
    {
    }

    /// <summary>
    ///     Generics: List<EdgeGroup>
    /// </summary>
    public class EdgeGroupList : List<EdgeData.EdgeGroup>
    {
    }

    /// <summary>
    ///     Generics: List<VertexData>
    /// </summary>
    public class VertexDataList : List<VertexData>
    {
    }

    /// <summary>
    ///     Generics: List<IndexData>
    /// </summary>
    public class IndexDataList : List<IndexData>
    {
    }

    /// <summary>
    ///     Generics: List<ShadowRenderable>
    /// </summary>
    public class ShadowRenderableList : List<ShadowRenderable>
    {
    }

    /// <summary>
    ///		Generics: List<RenderOperation>
    /// </summary>
    public class OperationTypeList : List<OperationType>
    {
        //public void Add( OperationType type )
        //{
        //    base.Add( type );
        //}

        //public new OperationType this[int index]
        //{
        //    get
        //    {
        //        return (OperationType)base[index];
        //    }
        //    set
        //    {
        //        base[index] = value;
        //    }
        //}
    }
}
