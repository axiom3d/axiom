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
using Axiom.Core;

/// <summary>
///    Holds all the data associated with a Binary Space Parition
///    (BSP) based indoor level.
/// </summary>
/// <remarks>
///    The data used here is populated by loading level files via
///    the BspLevelManager.Load method, although application users
///    are more likely to call SceneManager.SetWorldGeometry which will
///    automatically arrange the loading of the level. Note that this assumes
///    that you have asked for an indoor-specialized SceneManager (specify
///    SceneType.Indoor when calling Engine.GetSceneManager).</p>
///    We currently only support loading from Quake3 Arena level files,
///    although any source that can be converted into this classes structure
///    could also be used. The Quake3 level load process is in a different
///    class called Quake3Level to keep the specifics separate.</p>
/// </remarks>
public class BspLevel : Resource {
}