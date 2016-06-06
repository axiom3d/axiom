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
using System.Diagnostics;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	///     Records the use of temporary blend buffers.
	/// </summary>
	public class TempBlendedBufferInfo : IHardwareBufferLicensee
	{
		#region Fields

		/// <summary>
		///     Pre-blended position buffer.
		/// </summary>
		public HardwareVertexBuffer srcPositionBuffer;

		/// <summary>
		///     Pre-blended normal buffer.
		/// </summary>
		public HardwareVertexBuffer srcNormalBuffer;

		/// <summary>
		///     Pre-blended tangent buffer.
		/// </summary>
		public HardwareVertexBuffer srcTangentBuffer;

		/// <summary>
		///     Pre-blended binormal buffer.
		/// </summary>
		public HardwareVertexBuffer srcBinormalBuffer;

		/// <summary>
		///     Post-blended position buffer.
		/// </summary>
		public HardwareVertexBuffer destPositionBuffer;

		/// <summary>
		///     Post-blended normal buffer.
		/// </summary>
		public HardwareVertexBuffer destNormalBuffer;

		/// <summary>
		///     Post-blended tangent buffer.
		/// </summary>
		public HardwareVertexBuffer destTangentBuffer;

		/// <summary>
		///     Post-blended binormal buffer.
		/// </summary>
		public HardwareVertexBuffer destBinormalBuffer;

		/// <summary>
		///     Both positions and normals are contained in the same buffer
		/// </summary>
		public bool posNormalShareBuffer;

		/// <summary>
		///     Index at which the positions are bound in the buffer.
		/// </summary>
		public short posBindIndex;

		/// <summary>
		///     Index at which the normals are bound in the buffer.
		/// </summary>
		public short normBindIndex;

		/// <summary>
		///     Index at which the tangents are bound in the buffer.
		/// </summary>
		public short tanBindIndex;

		/// <summary>
		///     Index at which the binormals are bound in the buffer.
		/// </summary>
		public short binormBindIndex;

		/// <summary>
		///		Should we bind the position buffer
		/// </summary>
		public bool bindPositions;

		/// <summary>
		///		Should we bind the normals buffer
		/// </summary>
		public bool bindNormals;

		/// <summary>
		///		Should we bind the tangents buffer
		/// </summary>
		public bool bindTangents;

		/// <summary>
		///		Should we bind the binormals buffer
		/// </summary>
		public bool bindBinormals;

		#endregion Fields

		#region Methods

		/// <summary>
		///		Utility method, extract info from the given VertexData
		/// </summary>
		public void ExtractFrom( VertexData sourceData )
		{
			// Release old buffer copies first
			var mgr = HardwareBufferManager.Instance;
			if ( this.destPositionBuffer != null )
			{
				mgr.ReleaseVertexBufferCopy( this.destPositionBuffer );
				Debug.Assert( this.destPositionBuffer == null );
			}
			if ( this.destNormalBuffer != null )
			{
				mgr.ReleaseVertexBufferCopy( this.destNormalBuffer );
				Debug.Assert( this.destNormalBuffer == null );
			}

			var decl = sourceData.vertexDeclaration;
			var bind = sourceData.vertexBufferBinding;
			var posElem = decl.FindElementBySemantic( VertexElementSemantic.Position );
			var normElem = decl.FindElementBySemantic( VertexElementSemantic.Normal );
			var tanElem = decl.FindElementBySemantic( VertexElementSemantic.Tangent );
			var binormElem = decl.FindElementBySemantic( VertexElementSemantic.Binormal );

			Debug.Assert( posElem != null, "Positions are required" );

			this.posBindIndex = posElem.Source;
			this.srcPositionBuffer = bind.GetBuffer( this.posBindIndex );

			if ( normElem == null )
			{
				this.posNormalShareBuffer = false;
				this.srcNormalBuffer = null;
			}
			else
			{
				this.normBindIndex = normElem.Source;
				if ( this.normBindIndex == this.posBindIndex )
				{
					this.posNormalShareBuffer = true;
					this.srcNormalBuffer = null;
				}
				else
				{
					this.posNormalShareBuffer = false;
					this.srcNormalBuffer = bind.GetBuffer( this.normBindIndex );
				}
			}
			if ( tanElem == null )
			{
				this.srcTangentBuffer = null;
			}
			else
			{
				this.tanBindIndex = tanElem.Source;
				this.srcTangentBuffer = bind.GetBuffer( this.tanBindIndex );
			}

			if ( binormElem == null )
			{
				this.srcBinormalBuffer = null;
			}
			else
			{
				this.binormBindIndex = binormElem.Source;
				this.srcBinormalBuffer = bind.GetBuffer( this.binormBindIndex );
			}
		}

		/// <summary>
		///     Utility method, checks out temporary copies of src into dest.
		/// </summary>
		public void CheckoutTempCopies( bool positions, bool normals, bool tangents, bool binormals )
		{
			this.bindPositions = positions;
			this.bindNormals = normals;
			this.bindTangents = tangents;
			this.bindBinormals = binormals;

			if ( this.bindPositions && this.destPositionBuffer == null )
			{
				this.destPositionBuffer = HardwareBufferManager.Instance.AllocateVertexBufferCopy( this.srcPositionBuffer,
				                                                                                   BufferLicenseRelease.Automatic,
				                                                                                   this );
			}

			if ( this.bindNormals && !this.posNormalShareBuffer && this.srcNormalBuffer != null && this.destNormalBuffer == null )
			{
				this.destNormalBuffer = HardwareBufferManager.Instance.AllocateVertexBufferCopy( this.srcNormalBuffer,
				                                                                                 BufferLicenseRelease.Automatic,
				                                                                                 this );
			}

			if ( this.bindTangents && this.srcTangentBuffer != null )
			{
				if ( this.tanBindIndex != this.posBindIndex && this.tanBindIndex != this.normBindIndex )
				{
					this.destTangentBuffer = HardwareBufferManager.Instance.AllocateVertexBufferCopy( this.srcTangentBuffer,
					                                                                                  BufferLicenseRelease.Automatic,
					                                                                                  this );
				}
			}

			if ( this.bindNormals && this.srcBinormalBuffer != null )
			{
				if ( this.binormBindIndex != this.posBindIndex && this.binormBindIndex != this.normBindIndex &&
				     this.binormBindIndex != this.tanBindIndex )
				{
					this.destBinormalBuffer = HardwareBufferManager.Instance.AllocateVertexBufferCopy( this.srcBinormalBuffer,
					                                                                                   BufferLicenseRelease.Automatic,
					                                                                                   this );
				}
			}
		}

		public void CheckoutTempCopies()
		{
			CheckoutTempCopies( true, true, true, true );
		}

		/// <summary>
		///     Detect currently have buffer copies checked out and touch it
		/// </summary>
		public bool BuffersCheckedOut( bool positions, bool normals )
		{
			if ( positions || ( normals && this.posNormalShareBuffer ) )
			{
				if ( this.destPositionBuffer == null )
				{
					return false;
				}
				HardwareBufferManager.Instance.TouchVertexBufferCopy( this.destPositionBuffer );
			}
			if ( normals && !this.posNormalShareBuffer )
			{
				if ( this.destNormalBuffer == null )
				{
					return false;
				}
				HardwareBufferManager.Instance.TouchVertexBufferCopy( this.destNormalBuffer );
			}
			return true;
		}

		/// <summary>
		///     Utility method, binds dest copies into a given VertexData.
		/// </summary>
		/// <param name="targetData">VertexData object to bind the temp buffers into.</param>
		/// <param name="suppressHardwareUpload"></param>
		public void BindTempCopies( VertexData targetData, bool suppressHardwareUpload )
		{
			this.destPositionBuffer.SuppressHardwareUpdate( suppressHardwareUpload );
			targetData.vertexBufferBinding.SetBinding( this.posBindIndex, this.destPositionBuffer );

			if ( this.bindNormals && this.destNormalBuffer != null )
			{
				if ( this.normBindIndex != this.posBindIndex )
				{
					this.destNormalBuffer.SuppressHardwareUpdate( suppressHardwareUpload );
					targetData.vertexBufferBinding.SetBinding( this.normBindIndex, this.destNormalBuffer );
				}
			}
			if ( this.bindTangents && this.destTangentBuffer != null )
			{
				if ( this.tanBindIndex != this.posBindIndex && this.tanBindIndex != this.normBindIndex )
				{
					this.destTangentBuffer.SuppressHardwareUpdate( suppressHardwareUpload );
					targetData.vertexBufferBinding.SetBinding( this.tanBindIndex, this.destTangentBuffer );
				}
			}
			if ( this.bindBinormals && this.destBinormalBuffer != null )
			{
				if ( this.binormBindIndex != this.posBindIndex && this.binormBindIndex != this.normBindIndex &&
				     this.binormBindIndex != this.tanBindIndex )
				{
					this.destBinormalBuffer.SuppressHardwareUpdate( suppressHardwareUpload );
					targetData.vertexBufferBinding.SetBinding( this.binormBindIndex, this.destBinormalBuffer );
				}
			}
		}

		#endregion Methods

		#region IHardwareBufferLicensee Members

		/// <summary>
		///     Implementation of LicenseExpired.
		/// </summary>
		/// <param name="buffer"></param>
		public void LicenseExpired( HardwareBuffer buffer )
		{
			if ( buffer == this.destPositionBuffer )
			{
				this.destPositionBuffer = null;
			}
			if ( buffer == this.destNormalBuffer )
			{
				this.destNormalBuffer = null;
			}
		}

		#endregion
	}
}