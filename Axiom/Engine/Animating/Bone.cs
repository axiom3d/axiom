using System;
using Axiom.Core;

namespace Axiom.Animating
{
	/// <summary>
	/// Summary description for Bone.
	/// </summary>
	public class Bone : Node
	{
		#region Member variables

		/// <summary>Determines whether this bone is controlled at runtime.</summary>
		private bool manuallyControlled;

		#endregion

		#region Constructors

		public Bone()
		{
		}

		#endregion

		#region Methods

		protected override Node CreateChildImpl()
		{
			return null;
		}

		protected override Node CreateChildImpl(String name)
		{
			return null;
		}

		#endregion

		#region Properties

		/// <summary>
		///		Determines whether this bone is controlled at runtime.
		/// </summary>
		public bool ManuallyControlled
		{
			get { return manuallyControlled; }
			set
			{
				manuallyControlled = value;
			}
		}

		#endregion

	}

	/// <summary>
	///		Records the assignment of a single vertex to a single bone with the corresponding weight.
	///	 </summary>
	///	 <remarks>
	///		This simple struct simply holds a vertex index, bone index and weight representing the
	///		assignment of a vertex to a bone for skeletal animation. There may be many of these
	///		per vertex if blended vertex assignments are allowed.
	/// </remarks>
	public struct VertexBoneAssignment
	{
		ushort vertexIndex;
		ushort boneIndex;
		float weight;
	}
}
