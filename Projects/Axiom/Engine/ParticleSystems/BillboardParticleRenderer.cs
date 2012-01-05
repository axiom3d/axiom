#region LGPL License

/*
Axiom Graphics Engine Library
Copyright � 2003-2011 Axiom Project Team

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
		private static string rendererTypeName = "billboard";
		private const string PARTICLE = "Particle";

		/// <summary>
		///     List of available attibute parsers for script attributes.
		/// </summary>
		private Dictionary<string, MethodInfo> attribParsers =
			new Dictionary<string, MethodInfo>();

		private BillboardSet billboardSet;

		public BillboardParticleRenderer()
			: base()
		{
			billboardSet = new BillboardSet( "", 0, true );
			billboardSet.SetBillboardsInWorldSpace( true );

			// TODO: Is this the right way to do this?
			RegisterParsers();
		}

		/// <summary>
		/// Class level dispose method
		/// </summary>
		/// <remarks>
		/// When implementing this method in an inherited class the following template should be used;
		/// protected override void dispose( bool disposeManagedResources )
		/// {
		/// 	if ( !isDisposed )
		/// 	{
		/// 		if ( disposeManagedResources )
		/// 		{
		/// 			// Dispose managed resources.
		/// 		}
		///
		/// 		// There are no unmanaged resources to release, but
		/// 		// if we add them, they need to be released here.
		/// 	}
		///
		/// 	// If it is available, make the call to the
		/// 	// base class's Dispose(Boolean) method
		/// 	base.dispose( disposeManagedResources );
		/// }
		/// </remarks>
		/// <param name="disposeManagedResources">True if Unmanaged resources should be released.</param>
		protected override void dispose( bool disposeManagedResources )
		{
			if( !this.IsDisposed )
			{
				if( disposeManagedResources )
				{
					// Dispose managed resources.
					if( this.billboardSet != null )
					{
						if( !this.billboardSet.IsDisposed )
						{
							this.billboardSet.Dispose();
						}

						this.billboardSet = null;
					}

					this.attribParsers.Clear();
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			base.dispose( disposeManagedResources );
		}

		#region Attribute Parsers

		[ParserCommand( "billboard_type", PARTICLE )]
		public static void ParseBillboardType( string[] values, ParticleSystemRenderer _renderer )
		{
			if( values.Length != 1 )
			{
				ParseHelper.LogParserError( "billboard_type", _renderer.Type, "Wrong number of parameters." );
				return;
			}

			// lookup the real enum equivalent to the script value
			object val = ScriptEnumAttribute.Lookup( values[ 0 ], typeof( BillboardType ) );

			BillboardParticleRenderer renderer = (BillboardParticleRenderer)_renderer;
			// if a value was found, assign it
			if( val != null )
			{
				renderer.BillboardType = (BillboardType)val;
			}
			else
			{
				ParseHelper.LogParserError( "billboard_type", _renderer.Type, "Invalid enum value" );
			}
		}

		[ParserCommand( "billboard_origin", PARTICLE )]
		public static void ParseBillboardOrigin( string[] values, ParticleSystemRenderer _renderer )
		{
			if( values.Length != 1 )
			{
				ParseHelper.LogParserError( "billboard_origin", _renderer.Type, "Wrong number of parameters." );
				return;
			}

			// lookup the real enum equivalent to the script value
			object val = ScriptEnumAttribute.Lookup( values[ 0 ], typeof( BillboardOrigin ) );

			BillboardParticleRenderer renderer = (BillboardParticleRenderer)_renderer;
			// if a value was found, assign it
			if( val != null )
			{
				renderer.BillboardOrigin = (BillboardOrigin)val;
			}
			else
			{
				ParseHelper.LogParserError( "billboard_origin", _renderer.Type, "Invalid enum value" );
			}
		}

		[ParserCommand( "billboard_rotation_type", PARTICLE )]
		public static void ParseBillboardRotationType( string[] values, ParticleSystemRenderer _renderer )
		{
			if( values.Length != 1 )
			{
				ParseHelper.LogParserError( "billboard_rotation_type", _renderer.Type, "Wrong number of parameters." );
				return;
			}

			// lookup the real enum equivalent to the script value
			object val = ScriptEnumAttribute.Lookup( values[ 0 ], typeof( BillboardRotationType ) );

			BillboardParticleRenderer renderer = (BillboardParticleRenderer)_renderer;
			// if a value was found, assign it
			if( val != null )
			{
				renderer.BillboardRotationType = (BillboardRotationType)val;
			}
			else
			{
				ParseHelper.LogParserError( "billboard_rotation_type", _renderer.Type, "Invalid enum value" );
			}
		}

		[ParserCommand( "common_direction", PARTICLE )]
		public static void ParseCommonDirection( string[] values, ParticleSystemRenderer _renderer )
		{
			if( values.Length != 3 )
			{
				ParseHelper.LogParserError( "common_direction", _renderer.Type, "Wrong number of parameters." );
				return;
			}
			BillboardParticleRenderer renderer = (BillboardParticleRenderer)_renderer;
			renderer.CommonDirection = StringConverter.ParseVector3( values );
		}

		[ParserCommand( "common_up_vector", PARTICLE )]
		public static void ParseCommonUpDirection( string[] values, ParticleSystemRenderer _renderer )
		{
			if( values.Length != 3 )
			{
				ParseHelper.LogParserError( "common_up_vector", _renderer.Type, "Wrong number of parameters." );
				return;
			}
			BillboardParticleRenderer renderer = (BillboardParticleRenderer)_renderer;
			renderer.CommonUpVector = StringConverter.ParseVector3( values );
		}

		[ParserCommand( "point_rendering", PARTICLE )]
		public static void ParsePointRendering( string[] values, ParticleSystemRenderer _renderer )
		{
			if( values.Length != 1 )
			{
				ParseHelper.LogParserError( "point_rendering", _renderer.Type, "Wrong number of parameters." );
				return;
			}

			BillboardParticleRenderer renderer = (BillboardParticleRenderer)_renderer;
			renderer.PointRenderingEnabled = StringConverter.ParseBool( values[ 0 ] );
		}

		[ParserCommand( "accurate_facing", PARTICLE )]
		public static void ParseAccurateFacing( string[] values, ParticleSystemRenderer _renderer )
		{
			if( values.Length != 1 )
			{
				ParseHelper.LogParserError( "accurate_facing", _renderer.Type, "Wrong number of parameters." );
				return;
			}

			BillboardParticleRenderer renderer = (BillboardParticleRenderer)_renderer;
			renderer.UseAccurateFacing = StringConverter.ParseBool( values[ 0 ] );
		}

		/// <summary>
		///		Registers all attribute names with their respective parser.
		/// </summary>
		/// <remarks>
		///		Methods meant to serve as attribute parsers should use a method attribute to 
		/// </remarks>
		private void RegisterParsers()
		{
			MethodInfo[] methods = this.GetType().GetMethods();

			// loop through all methods and look for ones marked with attributes
			for( int i = 0; i < methods.Length; i++ )
			{
				// get the current method in the loop
				MethodInfo method = methods[ i ];

				// see if the method should be used to parse one or more material attributes
				ParserCommandAttribute[] parserAtts =
					(ParserCommandAttribute[])method.GetCustomAttributes( typeof( ParserCommandAttribute ), true );

				// loop through each one we found and register its parser
				for( int j = 0; j < parserAtts.Length; j++ )
				{
					ParserCommandAttribute parserAtt = parserAtts[ j ];

					switch( parserAtt.ParserType )
					{
							// this method should parse a material attribute
						case PARTICLE:
							// attribParsers.Add(parserAtt.Name, Delegate.CreateDelegate(typeof(ParticleSystemRendererAttributeParser), method));
							attribParsers[ parserAtt.Name ] = method;
							break;
					} // switch
				} // for
			} // for
		}

		public override void CopyParametersTo( ParticleSystemRenderer other )
		{
			BillboardParticleRenderer otherBpr = (BillboardParticleRenderer)other;
			Debug.Assert( otherBpr != null );
			otherBpr.BillboardType = this.BillboardType;
			otherBpr.CommonUpVector = this.CommonUpVector;
			otherBpr.CommonDirection = this.CommonDirection;
		}

		#endregion

		/// <summary>
		///		Parses an attribute intended for the particle system itself.
		/// </summary>
		/// <param name="line"></param>
		/// <param name="system"></param>
		public override bool SetParameter( string attr, string val )
		{
			if( attribParsers.ContainsKey( attr ) )
			{
				object[] args = new object[2];
				args[ 0 ] = val.Split( ' ' );
				args[ 1 ] = this;
				attribParsers[ attr ].Invoke( null, args );
				//ParticleSystemRendererAttributeParser parser =
				//        (ParticleSystemRendererAttributeParser)attribParsers[attr];

				//// call the parser method
				//parser(val.Split(' '), this);
				return true;
			}
			return false;
		}

		public override void UpdateRenderQueue( RenderQueue queue,
		                                        List<Particle> currentParticles,
		                                        bool cullIndividually )
		{
			billboardSet.CullIndividual = cullIndividually;

			// Update billboard set geometry
			billboardSet.BeginBillboards();
			Billboard bb = new Billboard();
			foreach( Particle p in currentParticles )
			{
				bb.Position = p.Position;
				if( billboardSet.BillboardType == BillboardType.OrientedSelf ||
				    billboardSet.BillboardType == BillboardType.PerpendicularSelf )
				{
					// Normalise direction vector
					bb.Direction = p.Direction;
					bb.Direction.Normalize();
				}
				bb.Color = p.Color;
				bb.rotationInRadians = p.rotationInRadians;
				bb.HasOwnDimensions = p.HasOwnDimensions;
				if( bb.HasOwnDimensions )
				{
					bb.width = p.Width;
					bb.height = p.Height;
				}
				billboardSet.InjectBillboard( bb );
			}

			billboardSet.EndBillboards();

			// Update the queue
			billboardSet.UpdateRenderQueue( queue );
		}

		//-----------------------------------------------------------------------
		public override Material Material { set { billboardSet.MaterialName = value.Name; } }

		//-----------------------------------------------------------------------
		public override void NotifyCurrentCamera( Camera cam )
		{
			billboardSet.NotifyCurrentCamera( cam );
		}

		//-----------------------------------------------------------------------
		public override void NotifyParticleRotated()
		{
			billboardSet.NotifyBillboardRotated();
		}

		//-----------------------------------------------------------------------
		public override void NotifyDefaultDimensions( float width, float height )
		{
			billboardSet.SetDefaultDimensions( width, height );
		}

		//-----------------------------------------------------------------------
		public override void NotifyParticleResized()
		{
			billboardSet.NotifyBillboardResized();
		}

		//-----------------------------------------------------------------------
		public override void NotifyParticleQuota( int quota )
		{
			billboardSet.PoolSize = quota;
		}

		//-----------------------------------------------------------------------
		public override void NotifyAttached( Node parent, bool isTagPoint )
		{
			billboardSet.NotifyAttached( parent, isTagPoint );
		}

		//-----------------------------------------------------------------------
		public override RenderQueueGroupID RenderQueueGroup { set { billboardSet.RenderQueueGroup = value; } }
		//-----------------------------------------------------------------------
		public override void SetKeepParticlesInLocalSpace( bool keepLocal )
		{
			billboardSet.SetBillboardsInWorldSpace( !keepLocal );
		}

		//-----------------------------------------------------------------------
		public BillboardType BillboardType { get { return billboardSet.BillboardType; } set { billboardSet.BillboardType = value; } }

		public BillboardOrigin BillboardOrigin { get { return billboardSet.BillboardOrigin; } set { billboardSet.BillboardOrigin = value; } }

		//-----------------------------------------------------------------------
		public bool UseAccurateFacing { get { return billboardSet.UseAccurateFacing; } set { billboardSet.UseAccurateFacing = value; } }

		public BillboardRotationType BillboardRotationType { get { return billboardSet.BillboardRotationType; } set { billboardSet.BillboardRotationType = value; } }

		public Vector3 CommonDirection { get { return billboardSet.CommonDirection; } set { billboardSet.CommonDirection = value; } }

		public Vector3 CommonUpVector { get { return billboardSet.CommonUpVector; } set { billboardSet.CommonUpVector = value; } }

		//-----------------------------------------------------------------------
		//SortMode BillboardParticleRenderer::_getSortMode(void) const
		//{
		//    return mBillboardSet->_getSortMode();
		//}
		//-----------------------------------------------------------------------
		public bool PointRenderingEnabled { get { return billboardSet.PointRenderingEnabled; } set { billboardSet.PointRenderingEnabled = value; } }

		public override string Type { get { return rendererTypeName; } }
	}

	/** Factory class for BillboardParticleRenderer */

	public class BillboardParticleRendererFactory : ParticleSystemRendererFactory
	{
		private const string rendererTypeName = "billboard";

		#region IParticleSystemRendererFactory Members

		public override string Type { get { return rendererTypeName; } }

		/// @copydoc FactoryObj::createInstance
		public override ParticleSystemRenderer CreateInstance( string name )
		{
			return new BillboardParticleRenderer();
		}

		/// @copydoc FactoryObj::destroyInstance
		public override void DestroyInstance( ref ParticleSystemRenderer inst ) {}

		#endregion IParticleSystemRendererFactory Members
	};
}
