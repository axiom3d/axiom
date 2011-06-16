using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using SlimDX.Direct3D9;
using TextureTransform = SlimDX.Direct3D9.TextureTransform;

namespace Axiom.RenderSystems.DirectX9
{
    // Matrix and projection related routines
    public partial class D3DRenderSystem
    {
        #region MakeOrthoMatrix

        [OgreVersion(1, 7)]
        public override void MakeOrthoMatrix(Radian fov, Real aspectRatio, Real near, Real far, out Matrix4 dest, bool forGpuPrograms)
        {
            float thetaY = Utility.DegreesToRadians(fov / 2.0f);
            float tanThetaY = Utility.Tan(thetaY);
            float tanThetaX = tanThetaY * aspectRatio;

            float halfW = tanThetaX * near;
            float halfH = tanThetaY * near;

            var w = 1.0f / (halfW);
            var h = 1.0f / (halfH);
            var q = 0.0f;

            if (far != 0)
            {
                q = 1.0f / (far - near);
            }

            dest = Matrix4.Zero;
            dest.m00 = w;
            dest.m11 = h;
            dest.m22 = q;
            dest.m23 = -near / (far - near);
            dest.m33 = 1;

            if (forGpuPrograms)
            {
                dest.m22 = -dest.m22;
            }
        }

        #endregion

        #region ConvertProjectionMatrix

        [OgreVersion(1, 7)]
        public override void ConvertProjectionMatrix(Matrix4 mat, out Matrix4 dest, bool forGpuProgram)
        {
            dest = new Matrix4(mat.m00, mat.m01, mat.m02, mat.m03,
                                mat.m10, mat.m11, mat.m12, mat.m13,
                                mat.m20, mat.m21, mat.m22, mat.m23,
                                mat.m30, mat.m31, mat.m32, mat.m33);

            // Convert depth range from [-1,+1] to [0,1]
            dest.m20 = (dest.m20 + dest.m30) / 2.0f;
            dest.m21 = (dest.m21 + dest.m31) / 2.0f;
            dest.m22 = (dest.m22 + dest.m32) / 2.0f;
            dest.m23 = (dest.m23 + dest.m33) / 2.0f;

            if ( forGpuProgram )
                return;
            // Convert right-handed to left-handed
            dest.m02 = -dest.m02;
            dest.m12 = -dest.m12;
            dest.m22 = -dest.m22;
            dest.m32 = -dest.m32;
        }

        #endregion

        #region MakeProjectionMatrix

        [OgreVersion(1, 7)]
        public override void MakeProjectionMatrix(Radian fov, Real aspectRatio, Real near, Real far, out Matrix4 dest, bool forGpuProgram)
        {
            float theta = Utility.DegreesToRadians((float)fov * 0.5f);
            float h = 1.0f / Utility.Tan(theta);
            float w = h / aspectRatio;
            float q, qn;

            if (far == 0)
            {
                q = 1 - Frustum.InfiniteFarPlaneAdjust;
                qn = near * (Frustum.InfiniteFarPlaneAdjust - 1);
            }
            else
            {
                q = far / (far - near);
                qn = -q * near;
            }

            dest = Matrix4.Zero;

            dest.m00 = w;
            dest.m11 = h;

            if (forGpuProgram)
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

        #region MakeProjectionMatrix

        [OgreVersion(1, 7)]
        public override void MakeProjectionMatrix(Real left, Real right, Real bottom, Real top, Real nearPlane, Real farPlane, out Matrix4 dest, bool forGpuProgram)
        {
            // Correct position for off-axis projection matrix
            if (!forGpuProgram)
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
            if (farPlane == 0)
            {
                q = 1 - Frustum.InfiniteFarPlaneAdjust;
                qn = nearPlane * (Frustum.InfiniteFarPlaneAdjust - 1);
            }
            else
            {
                q = farPlane / (farPlane - nearPlane);
                qn = -q * nearPlane;
            }
            dest = Matrix4.Zero;
            dest.m00 = 2 * nearPlane / width;
            dest.m02 = (right + left) / width;
            dest.m11 = 2 * nearPlane / height;
            dest.m12 = (top + bottom) / height;
            if (forGpuProgram)
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

        #endregion

        #region WorldMatrix

        [OgreVersion(1, 7)]
        public override Matrix4 WorldMatrix
        {
            set
            {
                device.SetTransform(TransformState.World, MakeD3DMatrix(value));
            }
        }

        #endregion

        #region ViewMatrix

        /// Saved last view matrix
        [OgreVersion(1, 7)]
        protected Matrix4 viewMatrix = Matrix4.Identity;

        [OgreVersion(1, 7)]
        public override Matrix4 ViewMatrix
        {
            set
            {
                // flip the transform portion of the matrix for DX and its left-handed coord system
                // save latest view matrix
                // Axiom: Matrix4 is a struct thus passed by value, save an additional copy through
                // temporary here; Ogre passes the Matrix4 by ref
                viewMatrix = value;
                viewMatrix.m20 = -viewMatrix.m20;
                viewMatrix.m21 = -viewMatrix.m21;
                viewMatrix.m22 = -viewMatrix.m22;
                viewMatrix.m23 = -viewMatrix.m23;

                var dxView = MakeD3DMatrix(value);
                device.SetTransform(TransformState.View, dxView);

                // also mark clip planes dirty
                if (clipPlanes.Count != 0)
                    clipPlanesDirty = true;
            }
        }

        #endregion

        #region ProjectionMatrix

        [OgreVersion(1, 7)]
        public override Matrix4 ProjectionMatrix
        {
            set
            {
                var mat = MakeD3DMatrix(value);

                if (activeRenderTarget.RequiresTextureFlipping)
                {
                    mat.M12 = -mat.M12;
                    mat.M22 = -mat.M22;
                    mat.M32 = -mat.M32;
                    mat.M42 = -mat.M42;
                }

                device.SetTransform(TransformState.Projection, mat);

                // also mark clip planes dirty
                if (clipPlanes.Count != 0)
                    clipPlanesDirty = true;
            }
        }

        #endregion

        #region SetTextureMatrix

        [OgreVersion(1, 7)]
        public override void SetTextureMatrix(int stage, Matrix4 xform)
        {
            SlimDX.Matrix d3dMat;
            var newMat = xform;

            // cache this since it's used often
            var autoTexCoordType = texStageDesc[stage].autoTexCoordType;

            // if a vertex program is bound, we mustn't set texture transforms
            if (vertexProgramBound)
            {

                device.SetTextureStageState(stage, TextureStage.TextureTransformFlags, TextureTransform.Disable);
                return;
            }

            if (autoTexCoordType == TexCoordCalcMethod.EnvironmentMap)
            {
                if ((d3dCaps.VertexProcessingCaps & VertexProcessingCaps.TexGenSphereMap) == VertexProcessingCaps.TexGenSphereMap)
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
            if (autoTexCoordType == TexCoordCalcMethod.EnvironmentMapReflection)
            {
                // Get transposed 3x3, ie since D3D is transposed just copy
                // We want to transpose since that will invert an orthonormal matrix ie rotation
                var viewTransposed = Matrix4.Identity;
                viewTransposed.m00 = viewMatrix.m00;
                viewTransposed.m01 = viewMatrix.m10;
                viewTransposed.m02 = viewMatrix.m20;
                viewTransposed.m03 = 0.0f;

                viewTransposed.m10 = viewMatrix.m01;
                viewTransposed.m11 = viewMatrix.m11;
                viewTransposed.m12 = viewMatrix.m21;
                viewTransposed.m13 = 0.0f;

                viewTransposed.m20 = viewMatrix.m02;
                viewTransposed.m21 = viewMatrix.m12;
                viewTransposed.m22 = viewMatrix.m22;
                viewTransposed.m23 = 0.0f;

                viewTransposed.m30 = 0;
                viewTransposed.m31 = 0;
                viewTransposed.m32 = 0;
                viewTransposed.m33 = 1.0f;

                // concatenate
                newMat = newMat * viewTransposed;
            }

            if (autoTexCoordType == TexCoordCalcMethod.ProjectiveTexture)
            {
                // Derive camera space to projector space transform
                // To do this, we need to undo the camera view matrix, then
                // apply the projector view & projection matrices
                newMat = viewMatrix.Inverse();

                if (texProjRelative)
                {
                    throw new NotImplementedException();
                    /*
				    Matrix4 viewMatrix;
				    mTexStageDesc[stage].frustum->calcViewMatrixRelative(mTexProjRelativeOrigin, viewMatrix);
				    newMat = viewMatrix * newMat;
                     */
                }
                //else
                {
                    newMat = texStageDesc[stage].frustum.ViewMatrix * newMat;
                }
                newMat = texStageDesc[stage].frustum.ProjectionMatrix * newMat;
                newMat = Matrix4.ClipSpace2DToImageSpace * newMat;
                newMat = xform * newMat;
            }

            // need this if texture is a cube map, to invert D3D's z coord
            if (autoTexCoordType != TexCoordCalcMethod.None &&
                 autoTexCoordType != TexCoordCalcMethod.ProjectiveTexture)
            {
                newMat.m20 = -newMat.m20;
                newMat.m21 = -newMat.m21;
                newMat.m22 = -newMat.m22;
                newMat.m23 = -newMat.m23;
            }

            var d3DTransType = (TransformState)((int)(TransformState.Texture0) + stage);

            // convert to D3D format
            d3dMat = MakeD3DMatrix(newMat);

            // set the matrix if it is not the identity
            if (!D3DHelper.IsIdentity(ref d3dMat))
            {
                //It's seems D3D automatically add a texture coordinate with value 1,
                //and fill up the remaining texture coordinates with 0 for the input
                //texture coordinates before pass to texture coordinate transformation.

                //NOTE: It's difference with D3DDECLTYPE enumerated type expand in
                //DirectX SDK documentation!

                //So we should prepare the texcoord transform, make the transformation
                //just like standardized vector expand, thus, fill w with value 1 and
                //others with 0.

                if (autoTexCoordType == TexCoordCalcMethod.None)
                {
                    //FIXME: The actually input texture coordinate dimensions should
                    //be determine by texture coordinate vertex element. Now, just trust
                    //user supplied texture type matchs texture coordinate vertex element.
                    if (texStageDesc[stage].texType == D3DTextureType.Normal)
                    {
                        /* It's 2D input texture coordinate:

                        texcoord in vertex buffer     D3D expanded to     We are adjusted to
                        -->                           -->
                        (u, v)                        (u, v, 1, 0)        (u, v, 0, 1)
                        */
                        Utility.Swap(ref d3dMat.M31, ref d3dMat.M41);
                        Utility.Swap(ref d3dMat.M32, ref d3dMat.M42);
                        Utility.Swap(ref d3dMat.M33, ref d3dMat.M43);
                        Utility.Swap(ref d3dMat.M34, ref d3dMat.M44);
                    }
                }
                //else
                //{
                //	// All texgen generate 3D input texture coordinates.
                //}

                // tell D3D the dimension of tex. coord
                var texCoordDim = TextureTransform.Count2;

                if (autoTexCoordType == TexCoordCalcMethod.ProjectiveTexture)
                {
                    //We want texcoords (u, v, w, q) always get divided by q, but D3D
                    //projected texcoords is divided by the last element (in the case of
                    //2D texcoord, is w). So we tweak the transform matrix, transform the
                    //texcoords with w and q swapped: (u, v, q, w), and then D3D will
                    //divide u, v by q. The w and q just ignored as it wasn't used by
                    //rasterizer.

                    switch (texStageDesc[stage].texType)
                    {
                        case D3DTextureType.Normal:
                            Utility.Swap(ref d3dMat.M13, ref d3dMat.M14);
                            Utility.Swap(ref d3dMat.M23, ref d3dMat.M24);
                            Utility.Swap(ref d3dMat.M33, ref d3dMat.M34);
                            Utility.Swap(ref d3dMat.M43, ref d3dMat.M44);

                            texCoordDim = TextureTransform.Projected | TextureTransform.Count3;
                            break;
                        case D3DTextureType.Cube:
                        case D3DTextureType.Volume:
                            // Yes, we support 3D projective texture.
                            texCoordDim = TextureTransform.Projected | TextureTransform.Count4;
                            break;
                    }
                }
                else
                {
                    switch (texStageDesc[stage].texType)
                    {
                        case D3DTextureType.Normal:
                            texCoordDim = TextureTransform.Count2;
                            break;
                        case D3DTextureType.Cube:
                        case D3DTextureType.Volume:
                            texCoordDim = TextureTransform.Count3;
                            break;
                    }
                }

                // note: int values of D3D.TextureTransform correspond directly with tex dimension, so direct conversion is possible
                // i.e. Count1 = 1, Count2 = 2, etc
                device.SetTextureStageState(stage, TextureStage.TextureTransformFlags, texCoordDim);

                // set the manually calculated texture matrix
                device.SetTransform(d3DTransType, d3dMat);
            }
            else
            {
                // disable texture transformation
                device.SetTextureStageState(stage, TextureStage.TextureTransformFlags, TextureTransform.Disable);
            }
        }

        #endregion

        #region ApplyObliqueDepthProjection

        [OgreVersion(1, 7)]
        public override void ApplyObliqueDepthProjection(ref Matrix4 projMatrix, Plane plane, bool forGpuProgram)
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
            q.x = System.Math.Sign(plane.Normal.x) / projMatrix.m00;
            q.y = System.Math.Sign(plane.Normal.y) / projMatrix.m11;
            q.z = 1.0f;

            // flip the next bit from Lengyel since we're right-handed
            if (forGpuProgram)
            {
                q.w = (1.0f - projMatrix.m22) / projMatrix.m23;
            }
            else
            {
                q.w = (1.0f + projMatrix.m22) / projMatrix.m23;
            }

            // Calculate the scaled plane vector
            var clipPlane4D = new Vector4(plane.Normal.x, plane.Normal.y, plane.Normal.z, plane.D);

            var c = clipPlane4D * (1.0f / (clipPlane4D.Dot(q)));

            // Replace the third row of the projection matrix
            projMatrix.m20 = c.x;
            projMatrix.m21 = c.y;

            // flip the next bit from Lengyel since we're right-handed
            if (forGpuProgram)
            {
                projMatrix.m22 = c.z;
            }
            else
            {
                projMatrix.m22 = -c.z;
            }

            projMatrix.m23 = c.w;
        }

        #endregion

        #region MakeD3DMatrix

        private SlimDX.Matrix MakeD3DMatrix(Matrix4 matrix)
        {
            var dxMat = new SlimDX.Matrix();

            // set it to a transposed matrix since DX uses row vectors
            dxMat.M11 = matrix.m00;
            dxMat.M12 = matrix.m10;
            dxMat.M13 = matrix.m20;
            dxMat.M14 = matrix.m30;
            dxMat.M21 = matrix.m01;
            dxMat.M22 = matrix.m11;
            dxMat.M23 = matrix.m21;
            dxMat.M24 = matrix.m31;
            dxMat.M31 = matrix.m02;
            dxMat.M32 = matrix.m12;
            dxMat.M33 = matrix.m22;
            dxMat.M34 = matrix.m32;
            dxMat.M41 = matrix.m03;
            dxMat.M42 = matrix.m13;
            dxMat.M43 = matrix.m23;
            dxMat.M44 = matrix.m33;

            return dxMat;
        }

        #endregion

        #region ConvertD3DMatrix

        /// <summary>
        ///		Helper method that converts a DX Matrix to our Matrix4.
        /// </summary>
        private Matrix4 ConvertD3DMatrix(ref SlimDX.Matrix d3dMat)
        {
            Matrix4 mat = Matrix4.Zero;

            mat.m00 = d3dMat.M11;
            mat.m10 = d3dMat.M12;
            mat.m20 = d3dMat.M13;
            mat.m30 = d3dMat.M14;

            mat.m01 = d3dMat.M21;
            mat.m11 = d3dMat.M22;
            mat.m21 = d3dMat.M23;
            mat.m31 = d3dMat.M24;

            mat.m02 = d3dMat.M31;
            mat.m12 = d3dMat.M32;
            mat.m22 = d3dMat.M33;
            mat.m32 = d3dMat.M34;

            mat.m03 = d3dMat.M41;
            mat.m13 = d3dMat.M42;
            mat.m23 = d3dMat.M43;
            mat.m33 = d3dMat.M44;

            return mat;
        }

        #endregion
    }
}
