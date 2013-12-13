#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/

#endregion

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using Axiom.Core;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	public interface IDerivedPlaneProvider
	{
		/// <summary>
		///		Get the derived plane as transformed by its parent node.
		/// </summary>
		Plane DerivedPlane { get; }
	}

	/// <summary>
	///		Definition of a Plane that may be attached to a node, and the derived
	///		details of it retrieved simply.
	/// </summary>
	/// <remarks>
	///		This plane is not here for rendering purposes, it's to allow you to attach
	///		planes to the scene in order to have them move and follow nodes on their
	///		own, which is useful if you're using the plane for some kind of calculation,
	///		e.g. reflection.
	/// </remarks>
	public class MovablePlane : MovableObject, IDerivedPlaneProvider
	{
		#region Fields

		/// <summary>
		///		Plane as transformed by it's parent node.
		/// </summary>
		protected Plane derivedPlane = new Plane();

		/// <summary>
		///		Cached translation vector.
		/// </summary>
		protected Vector3 lastTranslate;

		/// <summary>
		///		Cached rotation.
		/// </summary>
		protected Quaternion lastRotate;

		/// <summary>
		///		Bounding box.
		/// </summary>
		protected AxisAlignedBox nullBB = AxisAlignedBox.Null;

		/// <summary>
		///		Flag for whether changes have been made to this planes position/rotation.
		/// </summary>
		protected bool isDirty;

		/// <summary>
		///		Underlying plane representation.
		/// </summary>
		/// <remarks>
		///		Ogre uses multiple inheritance for this purpose - bah! ;)
		/// </remarks>
		protected Plane containedPlane = new Plane();

		#endregion Fields

		#region Constructor

		/// <summary>
		///		Constructor.
		/// </summary>
		/// <param name="name">Name of this plane.</param>
		public MovablePlane( string name )
			: base( name )
		{
			this.lastTranslate = Vector3.Zero;
			this.lastRotate = Quaternion.Identity;
			this.isDirty = true;
		}

		#endregion Constructor

		#region Properties

		public Plane Plane
		{
			get
			{
				return this.containedPlane;
			}
		}

		/// <summary>
		///		The plane's distance from the origin.
		/// </summary>
		public float D
		{
			get
			{
				return this.containedPlane.D;
			}
			set
			{
				this.containedPlane.D = value;
			}
		}

		/// <summary>
		///		The direction the plane is facing.
		/// </summary>
		public Vector3 Normal
		{
			get
			{
				return this.containedPlane.Normal;
			}
			set
			{
				this.containedPlane.Normal = value;
			}
		}

		/// <summary>
		///		Get the derived plane as transformed by its parent node.
		/// </summary>
		public Plane DerivedPlane
		{
			get
			{
				if ( parentNode != null )
				{
					if ( this.isDirty ||
					     !( parentNode.DerivedOrientation == this.lastRotate && parentNode.DerivedPosition == this.lastTranslate ) )
					{
						// store off parent position/orientation
						this.lastRotate = parentNode.DerivedOrientation;
						this.lastTranslate = parentNode.DerivedPosition;

						// rotate normal
						this.derivedPlane.Normal = this.lastRotate*this.containedPlane.Normal;

						// d remains the same in rotation, since rotation happens first
						this.derivedPlane.D = this.containedPlane.D;

						// add on the effect of the translation (project onto new normal)
						this.derivedPlane.D -= this.derivedPlane.Normal.Dot( this.lastTranslate );

						this.isDirty = false;
					}
				}
				else
				{
					return this.containedPlane;
				}

				return this.derivedPlane;
			}
		}

		#endregion Properties

		#region SceneObject Members

		public override Axiom.Math.AxisAlignedBox BoundingBox
		{
			get
			{
				return this.nullBB;
			}
		}

		public override Real BoundingRadius
		{
			get
			{
				return Real.PositiveInfinity;
			}
		}

		public override void NotifyCurrentCamera( Camera camera )
		{
			// dont care
		}

		public override void UpdateRenderQueue( RenderQueue queue )
		{
			// do nothing
		}

		#endregion SceneObject Members
	}
}