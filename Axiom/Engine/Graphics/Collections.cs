using System;
using System.Collections;

namespace Axiom.Graphics {
	/// <summary>
	///     Generics: List<VertexElement>
	/// </summary>
	public class VertexElementList : ArrayList {}

	/// <summary>
	///     Generics: List<TextureEffect>
	/// </summary>
	public class TextureEffectList : ArrayList {}

	/// <summary>
	///     Generics: List<RenderTarget>
	/// </summary>
	public class RenderTargetList : ArrayList {}

	/// <summary>
	///     Generics: List<RenderTexture>
	/// </summary>
	public class RenderTextureList : ArrayList {}

	/// <summary>
	///     Generics: List<Pass>
	/// </summary>
	public class PassList : ArrayList {}

	/// <summary>
	///     Generics: List<Technique>
	/// </summary>
	public class TechniqueList : ArrayList {}

	/// <summary>
	///     Generics: List<TextureUnitState>
	/// </summary>
	public class TextureUnitStateList : ArrayList {}

	/// <summary>
	///     Generics: List<AutoConstantEntry>
	/// </summary>
	public class AutoConstantEntryList : ArrayList {}

	/// <summary>
	///     Generics: List<AutoConstantEntry>
	/// </summary>
	public class FloatConstantEntryList : ArrayList {
		public void Resize(int size) {
			while(this.Count < size) {
				Add(new GpuProgramParameters.FloatConstantEntry());
			}
		}
	}

	/// <summary>
	///     Generics: List<AutoConstantEntry>
	/// </summary>
	public class IntConstantEntryList : ArrayList {
		public void Resize(int size) {
			while(this.Count < size) {
				Add(new GpuProgramParameters.IntConstantEntry());
			}
		}
	}

	/// <summary>
	///     Generics: List<IRenderable>
	/// </summary>
	public class RenderableList : ArrayList {}

	/// <summary>
	///     Generics: List<EdgeData.Triangle>
	/// </summary>
	public class TriangleList : ArrayList {}

	/// <summary>
	///     Generics: List<EdgeData.Edge>
	/// </summary>
	public class EdgeList : ArrayList {}

	/// <summary>
	///     Generics: List<EdgeGroup>
	/// </summary>
	public class EdgeGroupList : ArrayList {}

	/// <summary>
	///     Generics: List<VertexData>
	/// </summary>
	public class VertexDataList : ArrayList {}

	/// <summary>
	///     Generics: List<IndexData>
	/// </summary>
	public class IndexDataList : ArrayList {}

	/// <summary>
	///     Generics: List<ShadowRenderable>
	/// </summary>
	public class ShadowRenderableList : ArrayList {}
}
