using System;
using System.Collections;
using Axiom.Enumerations;

namespace Axiom.SubSystems.Rendering
{
	/// <summary>
	///		Contains all the information required to render a set of vertices.  This includes
	///		a list of VertexBuffers. 
	/// </summary>
	/// <remarks>
	///		This class contains
	/// </remarks>
	public class RenderOperation
	{
		#region Member variables

		/// <summary>
		///		Type of operation to perform.
		/// </summary>
		/// TODO: Rename the enum to OperationType
		public RenderMode operationType;

		/// <summary>
		///		Contains a list of hardware vertex buffers for this complete render operation.
		/// </summary>
		public VertexData vertexData;

		/// <summary>
		///		When <code>useIndices</code> is set to true, this must hold a reference to an index
		///		buffer containing indices into the vertices stored here. 
		/// </summary>
		public IndexData indexData;

		/// <summary>
		///		Specifies whether or not a list of indices should be used when rendering the vertices in
		///		the buffers.
		/// </summary>
		public bool useIndices;

		#endregion

		#region Constructors

		/// <summary>
		///		Default constructor.
		/// </summary>
		public RenderOperation()
		{
		}

		#endregion
	}
}
