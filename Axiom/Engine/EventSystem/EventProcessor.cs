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
using System.Collections;
using Axiom.Core;
using Axiom.Graphics;

namespace Axiom.EventSystem {
    /// <summary>
    /// Summary description for EventProcessor.
    /// </summary>
    public class EventProcessor {
        #region Fields

        /// <summary>
        ///    Holds queued events in a FIFO manner.
        /// </summary>
        Queue eventQueue = new Queue();

        #endregion Fields

        public EventProcessor() {
        }

        public void RegisterKeyTarget(IKeyTarget target) {
        }

        public void RegisterMouseTarget(IMouseTarget target) {
        }

        public void Initialize(RenderWindow window) {
        }

        public void Start() {
            // add a frame listener so that we can process events each frame
            Engine.Instance.FrameStarted += new FrameEvent(RenderSystem_FrameStarted);
        }

        public void Stop() {
            // add a frame listener so that we can process events each frame
            Engine.Instance.FrameStarted -= new FrameEvent(RenderSystem_FrameStarted);
        }

        private bool RenderSystem_FrameStarted(object source, FrameEventArgs e) {
            while(eventQueue.Count > 0) {
                // loop through and process each event
				
            }

            return true;
        }
    }
}
