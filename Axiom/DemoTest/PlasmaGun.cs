﻿#region LGPL License
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
using Axiom.Core;
using Axiom.Scripting;
using Axiom.Physics;
using Axiom.MathLib;

namespace Demos {
    /// <summary>
    /// 
    /// </summary>
    public class PlasmaGun : GameObject {
        static public int nextNum = 0;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sceneManager"></param>
        public PlasmaGun(SceneManager sceneManager): base(sceneManager) {
            entity = sceneMgr.CreateEntity("Plasma" + nextNum++, "plasma.xmf");
            node = (SceneNode)sceneMgr.RootSceneNode.CreateChild("PlasmaEntNode" + nextNum++);
            node.AttachObject(entity);
        }
    }

    public class RailGun : GameObject {
        static public int nextNum = 0;

        public RailGun(SceneManager sceneManager) : base(sceneManager) {
            entity = sceneMgr.CreateEntity("RailGun" + nextNum++, "railgun.xmf");
            node = (SceneNode)sceneMgr.RootSceneNode.CreateChild("RailgunEntNode" + nextNum++);
            node.AttachObject(entity);
        }
    }
}
