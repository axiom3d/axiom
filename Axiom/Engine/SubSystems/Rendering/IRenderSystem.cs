using System;
using Axiom.Core;
using Axiom.Enumerations;

namespace Axiom.SubSystems.Rendering
{
	/// <summary>
	///		Defnines the methods and properties that should be implemented by any
	///		particular graphics API.  
	/// </summary>
	public interface IRenderSystem
	{
		#region Methods

		/// <summary>
		///		Defines what should be done at the beginning of any render loop.
		/// </summary>
		void BeginFrame();

		/// <summary>
		///		Defines what needs to be done at the end of the render loop.
		/// </summary>
		void EndFrame();

		/// <summary>
		///		Called to execute a rendering operation using the current API.
		/// </summary>
		/// <param name="renderOp">
		///		An instance of a <see cref="Axiom.SubSystems.Rendering.RenderOperation"/> that
		///		contains all information necesary for rendering a set of vertices.
		/// </param>
		void Render(RenderOperation renderOp);

		/// <summary>
		///		Will be called during engine shutdown.  Any resource that need to be disposed of
		///		should be done in this method.
		/// </summary>
		void Shutdown();

		#endregion

		#region Properties

		/// <summary>
		///		Gets/Sets the color & strength of the ambient (global directionless) light in the world.
		/// </summary>
		ColorEx AmbientLight { get; set; }

		/// <summary>
		///		Gets/Sets whether or not dynamic lighting is enabled.
		///		<p/>
		///		If true, dynamic lighting is performed on geometry with normals supplied, geometry without
		///		normals will not be displayed. If false, no lighting is applied and all geometry will be full brightness.
		/// </summary>
		bool LightingEnabled { get; set; }

		/// <summary>
		///		Gets/Sets the type of light shading required.
		/// </summary>
		Shading ShadingType { get; set; }

		/// <summary>
		///		Gets/Sets the type of texture filtering used when rendering
		///	</summary>
		///	<remarks>
		///		This method sets the kind of texture filtering applied when rendering textures onto
		///		primitives. Filtering covers how the effects of minification and magnification are
		///		disguised by resampling.
		/// </remarks>
		TextureFiltering TextureFiltering { get; set; }

		#endregion
	}
}
