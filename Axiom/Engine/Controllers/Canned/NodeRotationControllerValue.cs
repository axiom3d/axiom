using System;
using Axiom.Controllers;
using Axiom.Core;
using Axiom.MathLib;

namespace Axiom.Controllers.Canned
{
	/// <summary>
	/// Summary description for NodeRotationControllerValue.
	/// </summary>
	public class NodeRotationControllerValue : IControllerValue
	{
		private float radians = 0;
		private Node node;
		private Vector3 axis;

		public NodeRotationControllerValue(Node node, Vector3 axis)
		{
			this.node = node;
			this.axis = axis;
		}

		#region IControllerValue Members

		public float Value
		{
			get { return radians; }
			set
			{
				node.Rotate(axis, value);
			}
		}

		#endregion
	}
}
