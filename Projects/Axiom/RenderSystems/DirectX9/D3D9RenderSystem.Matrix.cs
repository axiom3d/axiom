#region MIT/X11 License
//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.
#endregion License

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using D3D9 = SlimDX.Direct3D9;
using Plane = Axiom.Math.Plane;
using TextureTransform = SlimDX.Direct3D9.TextureTransform;
using Vector4 = Axiom.Math.Vector4;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
    // Matrix and projection related routines
    public partial class D3D9RenderSystem
    {
        #region MakeOrthoMatrix

        /// <see cref="Axiom.Graphics.RenderSystem.MakeOrthoMatrix(Radian, Real, Real, Real, out Matrix4, bool)"/>
        [OgreVersion( 1, 7, 2790 )]
        public override void MakeOrthoMatrix( Radian fovy, Real aspectRatio, Real near, Real far, out Matrix4 dest, bool forGpuPrograms )
        {
            var thetaY = fovy / 2.0f;
            var tanThetaY = Utility.Tan( thetaY );
            var tanThetaX = tanThetaY * aspectRatio;

            var half_w = tanThetaX * near;
            var half_h = tanThetaY * near;

            var iw = 1.0f / ( half_w );
            var ih = 1.0f / ( half_h );
            Real q = 0.0f;

            if ( far != 0 )
                q = 1.0 / ( far - near );

            dest = Matrix4.Zero;
            dest.m00 = iw;
            dest.m11 = ih;
            dest.m22 = q;
            dest.m23 = -near / ( far - near );
            dest.m33 = 1;

            if ( forGpuPrograms )
                dest.m22 = -dest.m22;
        }

        #endregion MakeOrthoMatrix

        #region ConvertProjectionMatrix

        /// <see cref="Axiom.Graphics.RenderSystem.ConvertProjectionMatrix(Matrix4, out Matrix4, bool)"/>
        [OgreVersion( 1, 7, 2790 )]
        public override void ConvertProjectionMatrix( Matrix4 mat, out Matrix4 dest, bool forGpuProgram )
        {
            dest = mat;

            // Convert depth range from [-1,+1] to [0,1]
            dest.m20 = ( dest.m20 + dest.m30 ) / 2;
            dest.m21 = ( dest.m21 + dest.m31 ) / 2;
            dest.m22 = ( dest.m22 + dest.m32 ) / 2;
            dest.m23 = ( dest.m23 + dest.m33 ) / 2;

            if ( forGpuProgram )
                return;
            // Convert right-handed to left-handed
            dest.m02 = -dest.m02;
            dest.m12 = -dest.m12;
            dest.m22 = -dest.m22;
            dest.m32 = -dest.m32;
        }

        #endregion ConvertProjectionMatrix

        #region MakeProjectionMatrix

        /// <see cref="Axiom.Graphics.RenderSystem.MakeProjectionMatrix(Radian, Real, Real, Real, out Matrix4, bool)"/>
        [OgreVersion( 1, 7, 2790 )]
        public override void MakeProjectionMatrix( Radian fov, Real aspectRatio, Real near, Real far, out Matrix4 dest, bool forGpuProgram )
        {
            var theta = fov * 0.5;
            var h = 1 / Utility.Tan( theta );
            var w = h / aspectRatio;
            Real q, qn;

            if ( far == 0 )
            {
                q = 1 - Frustum.InfiniteFarPlaneAdjust;
                qn = near * ( Frustum.InfiniteFarPlaneAdjust - 1 );
            }
            else
            {
                q = far / ( far - near );
                qn = -q * near;
            }

            dest = Matrix4.Zero;

            dest.m00 = w;
            dest.m11 = h;

            if ( forGpuProgram )
            {
                dest.m22 = -q;
                dest.m32 = -1.0f;
            }
            else
            {
                dest.m22 = q;
                dest.m32 = 1.0f;
            }

            dest.m23 = qn;
        }

        /// <see cref="Axiom.Graphics.RenderSystem.MakeProjectionMatrix(Real, Real, Real, Real, Real, Real, out Matrix4, bool)"/>
        [OgreVersion( 1, 7, 2790 )]
        public override void MakeProjectionMatrix( Real left, Real right, Real bottom, Real top, Real nearPlane, Real farPlane,
            out Matrix4 dest, bool forGpuProgram )
        {
            // Correct position for off-axis projection matrix
            if ( !forGpuProgram )
            {
                var offsetX = left + right;
                var offsetY = top + bottom;

                left -= offsetX;
                right -= offsetX;
                top -= offsetY;
                bottom -= offsetY;
            }

            var width = right - left;
            var height = top - bottom;
            Real q, qn;
            if ( farPlane == 0 )
            {
                q = 1 - Frustum.InfiniteFarPlaneAdjust;
                qn = nearPlane * ( Frustum.InfiniteFarPlaneAdjust - 1 );
            }
            else
            {
                q = farPlane / ( farPlane - nearPlane );
                qn = -q * nearPlane;
            }
            dest = Matrix4.Zero;
            dest.m00 = 2 * nearPlane / width;
            dest.m02 = ( right + left ) / width;
            dest.m11 = 2 * nearPlane / height;
            dest.m12 = ( top + bottom ) / height;
            if ( forGpuProgram )
            {
                dest.m22 = -q;
                dest.m32 = -1.0f;
            }
            else
            {
                dest.m22 = q;
                dest.m32 = 1.0f;
            }
            dest.m23 = qn;
        }

        #endregion MakeProjectionMatrix

        #region WorldMatrix

        private SlimDX.Matrix _dxWorldMat;

        /// <see cref="Axiom.Graphics.RenderSystem.WorldMatrix"/>
        [OgreVersion( 1, 7, 2790 )]
        public override Matrix4 WorldMatrix
        {
            get
            {
                return D3D9Helper.ConvertD3DMatrix( ref _dxWorldMat );
            }
            set
            {
                // save latest matrix
                _dxWorldMat = D3D9Helper.MakeD3DMatrix( value );
                ActiveD3D9Device.SetTransform( D3D9.TransformState.World, _dxWorldMat );
            }
        }

        #endregion WorldMatrix

        #region ViewMatrix

        /// Saved last view matrix
        [OgreVersion( 1, 7, 2790 )]
        private Matrix4 _viewMatrix = Matrix4.Identity;

        /// <see cref="Axiom.Graphics.RenderSystem.ViewMatrix"/>
        [OgreVersion( 1, 7, 2790 )]
        public override Matrix4 ViewMatrix
        {
            get
            {
                return _viewMatrix;
            }
            set
            {
                // flip the transform portion of the matrix for DX and its left-handed coord system
                // save latest view matrix
                // Axiom: Matrix4 is a struct thus passed by value, save an additional copy through
                // temporary here; Ogre passes the Matrix4 by ref
                _viewMatrix = value;
                _viewMatrix.m20 = -_viewMatrix.m20;
                _viewMatrix.m21 = -_viewMatrix.m21;
                _viewMatrix.m22 = -_viewMatrix.m22;
                _viewMatrix.m23 = -_viewMatrix.m23;

                var dxView = D3D9Helper.MakeD3DMatrix( _viewMatrix );
                ActiveD3D9Device.SetTransform( D3D9.TransformState.View, dxView );

                // also mark clip planes dirty
                if ( clipPlanes.Count != 0 )
                    clipPlanesDirty = true;
            }
        }

        #endregion ViewMatrix

        #region ProjectionMatrix

        private SlimDX.Matrix _dxProjMat;

        /// <see cref="Axiom.Graphics.RenderSystem.ProjectionMatrix"/>
        [OgreVersion( 1, 7, 2790 )]
        public override Matrix4 ProjectionMatrix
        {
            get
            {
                return D3D9Helper.ConvertD3DMatrix( ref _dxProjMat );
            }
            set
            {
                // save latest matrix
                _dxProjMat = D3D9Helper.MakeD3DMatrix( value );

                if ( activeRenderTarget.RequiresTextureFlipping )
                {
                    _dxProjMat.M12 = -_dxProjMat.M12;
                    _dxProjMat.M22 = -_dxProjMat.M22;
                    _dxProjMat.M32 = -_dxProjMat.M32;
                    _dxProjMat.M42 = -_dxProjMat.M42;
                }

                ActiveD3D9Device.SetTransform( D3D9.TransformState.Projection, _dxProjMat );

                // also mark clip planes dirty
                if ( clipPlanes.Count != 0 )
                    clipPlanesDirty = true;
            }
        }

        #endregion ProjectionMatrix

        #region SetTextureMatrix

        /// <see cref="Axiom.Graphics.RenderSystem.SetTextureMatrix(int, Matrix4)"/>
        [OgreVersion( 1, 7, 2790 )]
        public override void SetTextureMatrix( int stage, Matrix4 xform )
        {
            // the matrix we'll apply after conv. to D3D format
            var newMat = xform;

            // cache this since it's used often
            var autoTexCoordType = _texStageDesc[ stage ].AutoTexCoordType;

            // if a vertex program is bound, we mustn't set texture transforms
            if ( vertexProgramBound )
            {
                _setTextureStageState( stage, D3D9.TextureStage.TextureTransformFlags, (int)TextureTransform.Disable );
                return;
            }

            if ( autoTexCoordType == TexCoordCalcMethod.EnvironmentMap )
            {
                if ( ( _deviceManager.ActiveDevice.D3D9DeviceCaps.VertexProcessingCaps & D3D9.VertexProcessingCaps.TexGenSphereMap ) == D3D9.VertexProcessingCaps.TexGenSphereMap )
                {
                    // inverts the texture for a spheremap
                    var matEnvMap = Matrix4.Identity;
                    // set env_map values
                    matEnvMap.m11 = -1.0f;
                    // concatenate
                    newMat = newMat * matEnvMap;
                }
                else
                {
                    /* If envmap is applied, but device doesn't support spheremap,
                    then we have to use texture transform to make the camera space normal
                    reference the envmap properly. This isn't exactly the same as spheremap
                    (it looks nasty on flat areas because the camera space normals are the same)
                    but it's the best approximation we have in the absence of a proper spheremap */

                    // concatenate with the xform
                    newMat = newMat * Matrix4.ClipSpace2DToImageSpace;
                }
            }

            // If this is a cubic reflection, we need to modify using the view matrix
            if ( autoTexCoordType == TexCoordCalcMethod.EnvironmentMapReflection )
            {
                // Get transposed 3x3, ie since D3D is transposed just copy
                // We want to transpose since that will invert an orthonormal matrix ie rotation
                var viewTransposed = Matrix4.Identity;
                viewTransposed.m00 = _viewMatrix.m00;
                viewTransposed.m01 = _viewMatrix.m10;
                viewTransposed.m02 = _viewMatrix.m20;
                viewTransposed.m03 = 0.0f;

                viewTransposed.m10 = _viewMatrix.m01;
                viewTransposed.m11 = _viewMatrix.m11;
                viewTransposed.m12 = _viewMatrix.m21;
                viewTransposed.m13 = 0.0f;

                viewTransposed.m20 = _viewMatrix.m02;
                viewTransposed.m21 = _viewMatrix.m12;
                viewTransposed.m22 = _viewMatrix.m22;
                viewTransposed.m23 = 0.0f;

                viewTransposed.m30 = 0;
                viewTransposed.m31 = 0;
                viewTransposed.m32 = 0;
                viewTransposed.m33 = 1.0f;

                // concatenate
                newMat = newMat * viewTransposed;
            }

            if ( autoTexCoordType == TexCoordCalcMethod.ProjectiveTexture )
            {
                // Derive camera space to projector space transform
                // To do this, we need to undo the camera view matrix, then
                // apply the projector view & projection matrices
                newMat = _viewMatrix.Inverse();

                if ( texProjRelative )
                {
                    Matrix4 viewMatrix;
                    _texStageDesc[ stage ].Frustum.CalcViewMatrixRelative( texProjRelativeOrigin, out viewMatrix );
                    newMat = viewMatrix * newMat;
                }
                else
                {
                    newMat = _texStageDesc[ stage ].Frustum.ViewMatrix * newMat;
                }
                newMat = _texStageDesc[ stage ].Frustum.ProjectionMatrix * newMat;
                newMat = Matrix4.ClipSpace2DToImageSpace * newMat;
                newMat = xform * newMat;
            }

            // need this if texture is a cube map, to invert D3D's z coord
            if ( autoTexCoordType != TexCoordCalcMethod.None &&
                 autoTexCoordType != TexCoordCalcMethod.ProjectiveTexture )
            {
                newMat.m20 = -newMat.m20;
                newMat.m21 = -newMat.m21;
                newMat.m22 = -newMat.m22;
                newMat.m23 = -newMat.m23;
            }

            // convert our matrix to D3D format
            var d3dMat = D3D9Helper.MakeD3DMatrix( newMat );

            // set the matrix if it is not the identity
            if ( !D3D9Helper.IsIdentity( ref d3dMat ) )
            {
                //It's seems D3D automatically add a texture coordinate with value 1,
                //and fill up the remaining texture coordinates with 0 for the input
                //texture coordinates before pass to texture coordinate transformation.

                //NOTE: It's difference with D3DDECLTYPE enumerated type expand in
                //DirectX SDK documentation!

                //So we should prepare the texcoord transform, make the transformation
                //just like standardized vector expand, thus, fill w with value 1 and
                //others with 0.
                if ( autoTexCoordType == TexCoordCalcMethod.None )
                {
                    //FIXME: The actually input texture coordinate dimensions should
                    //be determine by texture coordinate vertex element. Now, just trust
                    //user supplied texture type matchs texture coordinate vertex element.
                    if ( _texStageDesc[ stage ].TexType == D3D9TextureType.Normal )
                    {
                        /* It's 2D input texture coordinate:

                        texcoord in vertex buffer     D3D expanded to     We are adjusted to
                        -->                           -->
                        (u, v)                        (u, v, 1, 0)        (u, v, 0, 1)
                        */
                        Utility.Swap( ref d3dMat.M31, ref d3dMat.M41 );
                        Utility.Swap( ref d3dMat.M32, ref d3dMat.M42 );
                        Utility.Swap( ref d3dMat.M33, ref d3dMat.M43 );
                        Utility.Swap( ref d3dMat.M34, ref d3dMat.M44 );
                    }
                }
                //else
                //{
                //    // All texgen generate 3D input texture coordinates.
                //}

                // tell D3D the dimension of tex. coord
                var texCoordDim = TextureTransform.Count2;

                if ( autoTexCoordType == TexCoordCalcMethod.ProjectiveTexture )
                {
                    //We want texcoords (u, v, w, q) always get divided by q, but D3D
                    //projected texcoords is divided by the last element (in the case of
                    //2D texcoord, is w). So we tweak the transform matrix, transform the
                    //texcoords with w and q swapped: (u, v, q, w), and then D3D will
                    //divide u, v by q. The w and q just ignored as it wasn't used by
                    //rasterizer.

                    switch ( _texStageDesc[ stage ].TexType )
                    {
                        case D3D9TextureType.Normal:
                            Utility.Swap( ref d3dMat.M13, ref d3dMat.M14 );
                            Utility.Swap( ref d3dMat.M23, ref d3dMat.M24 );
                            Utility.Swap( ref d3dMat.M33, ref d3dMat.M34 );
                            Utility.Swap( ref d3dMat.M43, ref d3dMat.M44 );

                            texCoordDim = TextureTransform.Projected | TextureTransform.Count3;
                            break;

                        case D3D9TextureType.Cube:
                        case D3D9TextureType.Volume:
                            // Yes, we support 3D projective texture.
                            texCoordDim = TextureTransform.Projected | TextureTransform.Count4;
                            break;
                    }
                }
                else
                {
                    switch ( _texStageDesc[ stage ].TexType )
                    {
                        case D3D9TextureType.Normal:
                            texCoordDim = TextureTransform.Count2;
                            break;

                        case D3D9TextureType.Cube:
                        case D3D9TextureType.Volume:
                            texCoordDim = TextureTransform.Count3;
                            break;
                    }
                }

                // note: int values of D3D.TextureTransform correspond directly with tex dimension, so direct conversion is possible
                // i.e. Count1 = 1, Count2 = 2, etc
                _setTextureStageState( stage, D3D9.TextureStage.TextureTransformFlags, (int)texCoordDim );

                // set the manually calculated texture matrix
                var d3DTransType = (D3D9.TransformState)( (int)( D3D9.TransformState.Texture0 ) + stage );
                ActiveD3D9Device.SetTransform( d3DTransType, d3dMat );
            }
            else
            {
                // disable texture transformation
                _setTextureStageState( stage, D3D9.TextureStage.TextureTransformFlags, (int)TextureTransform.Disable );

                // Needless to sets texture transform here, it's never used at all
            }
        }

        #endregion SetTextureMatrix

        #region ApplyObliqueDepthProjection

        /// <see cref="Axiom.Graphics.RenderSystem.ApplyObliqueDepthProjection"/>
        [OgreVersion( 1, 7, 2790 )]
        public override void ApplyObliqueDepthProjection( ref Matrix4 matrix, Plane plane, bool forGpuProgram )
        {
            // Thanks to Eric Lenyel for posting this calculation at www.terathon.com

            // Calculate the clip-space corner point opposite the clipping plane
            // as (sgn(clipPlane.x), sgn(clipPlane.y), 1, 1) and
            // transform it into camera space by multiplying it
            // by the inverse of the projection matrix

            /* generalised version
            Vector4 q = matrix.inverse() *
                Vector4(Math::Sign(plane.normal.x), Math::Sign(plane.normal.y), 1.0f, 1.0f);
            */
            var q = new Vector4();
            q.x = System.Math.Sign( plane.Normal.x ) / matrix.m00;
            q.y = System.Math.Sign( plane.Normal.y ) / matrix.m11;
            q.z = 1.0f;

            // flip the next bit from Lengyel since we're right-handed
            if ( forGpuProgram )
            {
                q.w = ( 1.0f - matrix.m22 ) / matrix.m23;
            }
            else
            {
                q.w = ( 1.0f + matrix.m22 ) / matrix.m23;
            }

            // Calculate the scaled plane vector
            var clipPlane4D = new Vector4( plane.Normal.x, plane.Normal.y, plane.Normal.z, plane.D );

            var c = clipPlane4D * ( 1.0f / ( clipPlane4D.Dot( q ) ) );

            // Replace the third row of the projection matrix
            matrix.m20 = c.x;
            matrix.m21 = c.y;

            // flip the next bit from Lengyel since we're right-handed
            if ( forGpuProgram )
            {
                matrix.m22 = c.z;
            }
            else
            {
                matrix.m22 = -c.z;
            }

            matrix.m23 = c.w;
        }

        #endregion ApplyObliqueDepthProjection
    };
}
