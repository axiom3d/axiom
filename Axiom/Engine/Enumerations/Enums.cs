#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;

namespace Axiom.Enumerations
{
	/// <summary>
	///		Covers what a billboards position means.
	/// </summary>
	public enum BillboardOrigin
	{
		TopLeft,
		TopCenter,
		TopRight,
		CenterLeft,
		Center,
		CenterRight,
		BottomLeft,
		BottomCenter,
		BottomRight
	}

	/// <summary>
	///		Type of billboard to use for a BillboardSet.
	/// </summary>
	public enum BillboardType
	{
		/// <summary>Standard point billboard (default), always faces the camera completely and is always upright</summary>
		Point,
		/// <summary>Billboards are oriented around a shared direction vector (used as Y axis) and only rotate around this to face the camera</summary>
		OrientedCommon,
		/// <summary>Billboards are oriented around their own direction vector (their own Y axis) and only rotate around this to face the camera</summary>
		OrientedSelf
	}

	/// <summary>
	///		Specifying the side of a box, used for things like skyboxes, etc.
	/// </summary>
	public enum BoxPlane
	{
		Front,
		Back,
		Left,
		Right,
		Up,
		Down
	}



	/// <summary>
	/// 
	/// </summary>
	public enum DynamicsBodyType
	{
		/// <summary></summary>
		Box,
		/// <summary></summary>
		Sphere,

	}

	/// <summary>
	/// Defines the 6 planes the make up a frustum.  
	/// </summary>
	public enum FrustumPlane
	{
		Near = 0,
		Far,
		Left,
		Right,
		Top,
		Bottom,
		/// <summary>Used for methods that require returning a value of this type but cannot return null.</summary>
		None
	}

	/// <summary>
	/// The "positive side" of the plane is the half space to which the
	/// plane normal points. The "negative side" is the other half
	/// space. The flag "no side" indicates the plane itself.
	/// </summary>
	public enum PlaneSide
	{
		None,
		Positive,
		Negative
	}

	/// <summary>
	///		Canned entities that can be created on demand.
	/// </summary>
	public enum PrefabEntity
	{
		/// <summary>A flat plane.</summary>
		Plane,
		/// <summary>The obligatory teapot.</summary>
		Teapot,
		/// <summary>Typical box.</summary>
		Box,
		/// <summary>Full cairo action.</summary>
		Pyramid
	}

	/// <summary>
	///		Priorities that can be assigned to renderable objects for sorting.
	/// </summary>
	public enum RenderQueueGroupID
	{
		/// <summary>Objects that must be rendered first (like backgrounds).</summary>
		Background = 0,
		/// <summary></summary>
		One = 10,
		/// <summary></summary>
		Two = 20,
		/// <summary></summary>
		Three = 30,
		/// <summary></summary>
		Four = 40,
		/// <summary>Default queue.</summary>
		Main = 50,
		/// <summary></summary>
		Six = 60,
		/// <summary></summary>
		Seven = 70,
		/// <summary></summary>
		Eight = 80,
		/// <summary>Last queue before overlays, used for skyboxes if rendered last.</summary>
		Nine = 90,
		/// <summary>Use this queue for objects which must be rendered last e.g. overlays</summary>
		Overlay = 100
	}

	/// <summary>
	/// The types of resources that the engine can make use of.
	/// </summary>
	public enum Resource
	{
		All,
		Textures,
		Meshes
	}

	/// <summary>
	/// The different types of scenes types that can be handled by the engine.  The various types can
	/// be altered by plugin functionality (i.e. BSP for interior, OctTree for Exterior, etc).
	/// </summary>
	public enum SceneType
	{
		Generic,
		ExteriorClose,
		ExteriorFar,
		Interior,
		Overhead
	}

}
