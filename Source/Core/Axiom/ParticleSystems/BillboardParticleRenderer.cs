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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using Axiom.Math;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Scripting;

#endregion Namespace Declarations

namespace Axiom.ParticleSystems
{
    public class BillboardParticleRenderer : ParticleSystemRenderer
    {
        #region Fields and Properties

        private static string rendererTypeName = "billboard";
        private const string PARTICLE = "Particle";

        /// <summary>
        ///     List of available attibute parsers for script attributes.
        /// </summary>
        private readonly Dictionary<string, MethodInfo> attribParsers = new Dictionary<string, MethodInfo>();

        private BillboardSet billboardSet;

        public BillboardType BillboardType
        {
            get
            {
                return this.billboardSet.BillboardType;
            }
            set
            {
                this.billboardSet.BillboardType = value;
            }
        }

        public BillboardOrigin BillboardOrigin
        {
            get
            {
                return this.billboardSet.BillboardOrigin;
            }
            set
            {
                this.billboardSet.BillboardOrigin = value;
            }
        }

        public bool UseAccurateFacing
        {
            get
            {
                return this.billboardSet.UseAccurateFacing;
            }
            set
            {
                this.billboardSet.UseAccurateFacing = value;
            }
        }

        public BillboardRotationType BillboardRotationType
        {
            get
            {
                return this.billboardSet.BillboardRotationType;
            }
            set
            {
                this.billboardSet.BillboardRotationType = value;
            }
        }

        public Vector3 CommonDirection
        {
            get
            {
                return this.billboardSet.CommonDirection;
            }
            set
            {
                this.billboardSet.CommonDirection = value;
            }
        }

        public Vector3 CommonUpVector
        {
            get
            {
                return this.billboardSet.CommonUpVector;
            }
            set
            {
                this.billboardSet.CommonUpVector = value;
            }
        }

        //-----------------------------------------------------------------------
        //SortMode BillboardParticleRenderer::_getSortMode(void) const
        //{
        //    return mBillboardSet->_getSortMode();
        //}
        //-----------------------------------------------------------------------
        public bool PointRenderingEnabled
        {
            get
            {
                return this.billboardSet.PointRenderingEnabled;
            }
            set
            {
                this.billboardSet.PointRenderingEnabled = value;
            }
        }

        public override string Type
        {
            get
            {
                return rendererTypeName;
            }
        }

        public override Material Material
        {
            set
            {
                this.billboardSet.MaterialName = value.Name;
            }
        }

        #endregion Fields and Properties

        #region Construction and Destruction

        public BillboardParticleRenderer()
            : base()
        {
            this.billboardSet = new BillboardSet(string.Empty, 0, true);
            this.billboardSet.SetBillboardsInWorldSpace(true);
        }

        protected override void dispose(bool disposeManagedResources)
        {
            if (!IsDisposed)
            {
                if (disposeManagedResources)
                {
                    if (this.billboardSet != null)
                    {
                        if (!this.billboardSet.IsDisposed)
                        {
                            this.billboardSet.Dispose();
                        }

                        this.billboardSet = null;
                    }

                    this.attribParsers.Clear();
                }
            }

            base.dispose(disposeManagedResources);
        }

        #endregion Construction and Destruction

        #region Methods

        public override void CopyParametersTo(ParticleSystemRenderer other)
        {
            var otherBpr = (BillboardParticleRenderer)other;
            Debug.Assert(otherBpr != null);
            otherBpr.BillboardType = BillboardType;
            otherBpr.CommonUpVector = CommonUpVector;
            otherBpr.CommonDirection = CommonDirection;
        }

        /// <summary>
        ///		Parses an attribute intended for the particle system itself.
        /// </summary>
        public override bool SetParameter(string attr, string val)
        {
            if (this.attribParsers.ContainsKey(attr))
            {
                var args = new object[2];
                args[0] = val.Split(' ');
                args[1] = this;
                this.attribParsers[attr].Invoke(null, args);
                //ParticleSystemRendererAttributeParser parser =
                //        (ParticleSystemRendererAttributeParser)attribParsers[attr];

                //// call the parser method
                //parser(val.Split(' '), this);
                return true;
            }
            return false;
        }

        public override void UpdateRenderQueue(RenderQueue queue, List<Particle> currentParticles, bool cullIndividually)
        {
            this.billboardSet.CullIndividual = cullIndividually;

            // Update billboard set geometry
            this.billboardSet.BeginBillboards();
            var bb = new Billboard();
            foreach (var p in currentParticles)
            {
                bb.Position = p.Position;
                if (this.billboardSet.BillboardType == BillboardType.OrientedSelf ||
                     this.billboardSet.BillboardType == BillboardType.PerpendicularSelf)
                {
                    // Normalise direction vector
                    bb.Direction = p.Direction;
                    bb.Direction.Normalize();
                }
                bb.Color = p.Color;
                bb.rotationInRadians = p.rotationInRadians;
                bb.HasOwnDimensions = p.HasOwnDimensions;
                if (bb.HasOwnDimensions)
                {
                    bb.width = p.Width;
                    bb.height = p.Height;
                }
                this.billboardSet.InjectBillboard(bb);
            }

            this.billboardSet.EndBillboards();

            // Update the queue
            this.billboardSet.UpdateRenderQueue(queue);
        }

        public override void NotifyCurrentCamera(Camera cam)
        {
            this.billboardSet.NotifyCurrentCamera(cam);
        }

        public override void NotifyParticleRotated()
        {
            this.billboardSet.NotifyBillboardRotated();
        }

        public override void NotifyDefaultDimensions(float width, float height)
        {
            this.billboardSet.SetDefaultDimensions(width, height);
        }

        public override void NotifyParticleResized()
        {
            this.billboardSet.NotifyBillboardResized();
        }

        public override void NotifyParticleQuota(int quota)
        {
            this.billboardSet.PoolSize = quota;
        }

        public override void NotifyAttached(Node parent, bool isTagPoint)
        {
            this.billboardSet.NotifyAttached(parent, isTagPoint);
        }

        public override RenderQueueGroupID RenderQueueGroup
        {
            set
            {
                this.billboardSet.RenderQueueGroup = value;
            }
        }

        public override void SetKeepParticlesInLocalSpace(bool keepLocal)
        {
            this.billboardSet.SetBillboardsInWorldSpace(!keepLocal);
        }

        #endregion Methods

        #region Command objects

        #region BillboardTypeCommand

        [OgreVersion(1, 7, 2)]
        [ScriptableProperty("billboard_type",
            @"The type of billboard to use. 'point' means a simulated spherical particle,
                'oriented_common' means all particles in the set are oriented around common_direction,
                'oriented_self' means particles are oriented around their own direction,
                'perpendicular_common' means all particles are perpendicular to common_direction, 
                and 'perpendicular_self' means particles are perpendicular to their own direction."
            )]
        public class BillboardTypeCommand : IPropertyCommand
        {
            #region IPropertyCommand Members

            [OgreVersion(1, 7, 2)]
            public string Get(object target)
            {
                BillboardType t = ((BillboardParticleRenderer)target).BillboardType;
                return ScriptEnumAttribute.GetScriptAttribute((int)t, typeof(BillboardType));
            }

            [OgreVersion(1, 7, 2)]
            public void Set(object target, string val)
            {
                ((BillboardParticleRenderer)target).BillboardType =
                    (BillboardType)ScriptEnumAttribute.Lookup(val, typeof(BillboardType));
            }

            #endregion IPropertyCommand Members
        }

        #endregion BillboardTypeCommand

        #region BillboardOriginCommand

        [OgreVersion(1, 7, 2)]
        [ScriptableProperty("billboard_origin",
            @"This setting controls the fine tuning of where a billboard appears in relation to it's position.
                Possible value are: 'top_left', 'top_center', 'top_right', 'center_left', 'center', 'center_right',
                'bottom_left', 'bottom_center' and 'bottom_right'. Default value is 'center'."
            )]
        public class BillboardOriginCommand : IPropertyCommand
        {
            #region IPropertyCommand Members

            [OgreVersion(1, 7, 2)]
            public string Get(object target)
            {
                BillboardOrigin o = ((BillboardParticleRenderer)target).BillboardOrigin;
                return ScriptEnumAttribute.GetScriptAttribute((int)o, typeof(BillboardOrigin));
            }

            [OgreVersion(1, 7, 2)]
            public void Set(object target, string val)
            {
                ((BillboardParticleRenderer)target).BillboardOrigin =
                    (BillboardOrigin)ScriptEnumAttribute.Lookup(val, typeof(BillboardOrigin));
            }

            #endregion IPropertyCommand Members
        }

        #endregion BillboardOriginCommand

        #region BillboardRotationTypeCommand

        [OgreVersion(1, 7, 2)]
        [ScriptableProperty("billboard_rotation_type",
            @"This setting controls the billboard rotation type.
				'vertex' means rotate the billboard's vertices around their facing direction.
                'texcoord' means rotate the billboard's texture coordinates. Default value is 'texcoord'."
            )]
        public class BillboardRotationTypeCommand : IPropertyCommand
        {
            #region IPropertyCommand Members

            [OgreVersion(1, 7, 2)]
            public string Get(object target)
            {
                BillboardRotationType r = ((BillboardParticleRenderer)target).BillboardRotationType;
                return ScriptEnumAttribute.GetScriptAttribute((int)r, typeof(BillboardRotationType));
            }

            [OgreVersion(1, 7, 2)]
            public void Set(object target, string val)
            {
                ((BillboardParticleRenderer)target).BillboardRotationType =
                    (BillboardRotationType)ScriptEnumAttribute.Lookup(val, typeof(BillboardRotationType));
            }

            #endregion IPropertyCommand Members
        }

        #endregion BillboardRotationTypeCommand

        #region CommonDirectionCommand

        [OgreVersion(1, 7, 2)]
        [ScriptableProperty("common_direction",
            @"Only useful when billboard_type is oriented_common or perpendicular_common.
				When billboard_type is oriented_common, this parameter sets the common orientation for
				all particles in the set (e.g. raindrops may all be oriented downwards).
				When billboard_type is perpendicular_common, this parameter sets the perpendicular vector for
				all particles in the set (e.g. an aureola around the player and parallel to the ground)."
            )]
        public class CommonDirectionCommand : IPropertyCommand
        {
            #region IPropertyCommand Members

            [OgreVersion(1, 7, 2)]
            public string Get(object target)
            {
                return ((BillboardParticleRenderer)target).CommonDirection.ToString();
            }

            [OgreVersion(1, 7, 2)]
            public void Set(object target, string val)
            {
                ((BillboardParticleRenderer)target).CommonDirection = StringConverter.ParseVector3(val);
            }

            #endregion IPropertyCommand Members
        }

        #endregion CommonDirectionCommand

        #region CommonUpVectorCommand

        [OgreVersion(1, 7, 2)]
        [ScriptableProperty("common_up_vector",
            @"Only useful when billboard_type is perpendicular_self or perpendicular_common. This
				parameter sets the common up-vector for all particles in the set (e.g. an aureola around
				the player and parallel to the ground)."
            )]
        public class CommonUpVectorCommand : IPropertyCommand
        {
            #region IPropertyCommand Members

            [OgreVersion(1, 7, 2)]
            public string Get(object target)
            {
                return ((BillboardParticleRenderer)target).CommonUpVector.ToString();
            }

            [OgreVersion(1, 7, 2)]
            public void Set(object target, string val)
            {
                ((BillboardParticleRenderer)target).CommonUpVector = StringConverter.ParseVector3(val);
            }

            #endregion IPropertyCommand Members
        }

        #endregion CommonUpVectorCommand

        #region PointRenderingCommand

        [OgreVersion(1, 7, 2)]
        [ScriptableProperty("point_rendering",
            @"Set whether or not particles will use point rendering
				rather than manually generated quads. This allows for faster
				rendering of point-oriented particles although introduces some
				limitations too such as requiring a common particle size.
				Possible values are 'true' or 'false'."
            )]
        public class PointRenderingCommand : IPropertyCommand
        {
            #region IPropertyCommand Members

            [OgreVersion(1, 7, 2)]
            public string Get(object target)
            {
                return ((BillboardParticleRenderer)target).PointRenderingEnabled.ToString();
            }

            [OgreVersion(1, 7, 2)]
            public void Set(object target, string val)
            {
                ((BillboardParticleRenderer)target).PointRenderingEnabled = StringConverter.ParseBool(val);
            }

            #endregion IPropertyCommand Members
        }

        #endregion PointRenderingCommand

        #region AccurateFacingCommand

        [OgreVersion(1, 7, 2)]
        [ScriptableProperty("accurate_facing",
            @"Set whether or not particles will be oriented to the camera
				based on the relative position to the camera rather than just
				the camera direction. This is more accurate but less optimal.
				Cannot be combined with point rendering."
            )]
        public class AccurateFacingCommand : IPropertyCommand
        {
            #region IPropertyCommand Members

            [OgreVersion(1, 7, 2)]
            public string Get(object target)
            {
                return ((BillboardParticleRenderer)target).UseAccurateFacing.ToString();
            }

            [OgreVersion(1, 7, 2)]
            public void Set(object target, string val)
            {
                ((BillboardParticleRenderer)target).UseAccurateFacing = StringConverter.ParseBool(val);
            }

            #endregion IPropertyCommand Members
        }

        #endregion AccurateFacingCommand

        #endregion Command objects
    }

    /** Factory class for BillboardParticleRenderer */

    public class BillboardParticleRendererFactory : ParticleSystemRendererFactory
    {
        private const string rendererTypeName = "billboard";

        #region IParticleSystemRendererFactory Members

        public override string Type
        {
            get
            {
                return rendererTypeName;
            }
        }

        /// @copydoc FactoryObj::createInstance
        public override ParticleSystemRenderer CreateInstance(string name)
        {
            return new BillboardParticleRenderer();
        }

        #endregion IParticleSystemRendererFactory Members
    };
}