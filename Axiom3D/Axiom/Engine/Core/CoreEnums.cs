#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;

namespace Axiom
{

    /// <summary>
    ///     The level of detail in which the log will go into.
    /// </summary>
    public enum LoggingLevel
    {
        Low = 1,
        Normal,
        Verbose
    }

    /// <summary>
    ///     The importance of a logged message.
    /// </summary>
    public enum LogMessageLevel
    {
        Trivial = 1,
        Normal,
        Critical
    }


    /// <summary>
    ///    The different types of scenes types that can be handled by the engine.  The various types can
    ///    be altered by plugin functionality (i.e. BSP for interior, Octree for Exterior, etc).
    /// </summary>
    public enum SceneType
    {
        Generic,
        ExteriorClose,
        ExteriorFar,
        Interior,
        Overhead
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
        /// <summary>
        ///		Objects that must be rendered first (like backgrounds).
        ///	</summary>
        Background = 0,
        /// <summary>
        ///		First queue (after backgrounds), used for skyboxes if rendered first.
        /// </summary>
        SkiesEarly = 5,
        /// <summary>All purpose queue.</summary>
        One = 10,
        /// <summary>All purpose queue.</summary>
        Two = 20,
        /// <summary>All purpose queue.</summary>
        Three = 30,
        /// <summary>All purpose queue.</summary>
        Four = 40,
        /// <summary>Default queue.</summary>
        Main = 50,
        /// <summary>All purpose queue.</summary>
        Six = 60,
        /// <summary>All purpose queue.</summary>
        Seven = 70,
        /// <summary>All purpose queue.</summary>
        Eight = 80,
        /// <summary>All purpose queue.</summary>
        Nine = 90,
        /// <summary>
        ///		Last queue before overlays, used for skyboxes if rendered last.
        /// </summary>
        SkiesLate = 95,
        /// <summary>
        ///		Use this queue for objects which must be rendered last e.g. overlays.
        ///	</summary>
        Overlay = 100
    }


    /// <summary>
    ///     Denotes the spaces which a transform can be relative to.
    /// </summary>
    public enum TransformSpace
    {
        /// <summary>
        ///     Transform is relative to the local space.
        /// </summary>
        Local,
        /// <summary>
        ///     Transform is relative to the space of the parent node.
        /// </summary>
        Parent,
        /// <summary>
        ///     Transform is relative to world space.
        /// </summary>
        World
    };

    /// <summary>
    ///    This type can be used by collaborating applications & SceneManagers to 
    ///    agree on the type of world geometry to be returned from queries. Not all
    ///    these types will be supported by all SceneManagers; once the application
    ///    has decided which SceneManager specialization to use, it is expected that 
    ///    it will know which type of world geometry abstraction is available to it.
    /// </summary>
    [Flags]
    public enum WorldFragmentType
    {
        /// <summary>
        ///    Return no world geometry hits at all.
        /// </summary>
        None = 0x01,
        /// <summary>
        ///    Return references to convex plane-bounded regions.
        /// </summary>
        PlaneBoundedRegion = 0x02,
        /// <summary>
        ///    Return a single intersection point (typically RaySceneQuery only)
        /// </summary>
        SingleIntersection = 0x04,
        /// <summary>
        ///    Custom geometry as defined by the SceneManger.
        /// </summary>
        CustomGeometry = 0x08,
        /// <summary>
        ///    General RenderOperation structure.
        /// </summary>
        RenderOperation = 0x10
    }
}
