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
#endregion LGPL License

#region Using Directives

using System;
using System.Collections;

using Axiom.Core;
using Axiom.Math;
using Axiom.Collections;
using Axiom.Media;
using Axiom.Graphics;

using Axiom.SceneManagers.PagingLandscape.Collections;
using Axiom.SceneManagers.PagingLandscape.Tile;

#endregion Using Directives


namespace Axiom.SceneManagers.PagingLandscape.Texture
{
	/// <summary>
	/// Summary description for Texture.
	/// </summary>
	public abstract class Texture
	{
		#region Fields
		protected bool isLoaded;

		protected Material material;

		protected long dataX, dataZ;
	    protected bool mPositiveShadow;

	    #endregion Fields

		public Texture()
		{
			isLoaded = false;
			dataX = 0;
			dataZ = 0;
			material = null;
		}


		public virtual void Load(long mX, long mZ)
		{
			dataX = mX;
			dataZ = mZ;
			loadMaterial();
			isLoaded = true;
		}

		public virtual void Unload()
		{
			isLoaded = false;
			unloadMaterial();
		}

		public bool IsLoaded
		{
			get
			{
				return isLoaded;
			}
		}

		public Material Material
		{
			get
			{
				return material;
			}
		}
/*
        protected virtual void loadMaterial()
        {
            string commonName = dataZ.ToString() + "." + dataX.ToString();
            string matname = "Image." + commonName;

            if (null == material)
            {
                if (Options.Instance.MaterialPerPage)
                {
                    // JEFF - all material settings configured through material script
                    material = (Material) MaterialManager.Instance.GetByName(matname);

                    if (null == material)
                    {
                        material = (Material) MaterialManager.Instance.Load(matname, Core.ResourceGroupManager.AutoDetectResourceGroupName);

                        if (null == material)
                        {
                            LogManager.Instance.DefaultLog.Write(LogMessageLevel.Critical, false,
                                                                      "PLSM2 : Cannot find material named " + matname);
                            return;
                        }
                    }
                    {
                        // This whole block to be pointless...
                        for (int i = 0; i < material.TechniqueCount; i++)
                        {
                            Technique t = material.GetTechnique(i);

                            for (int j = 0; j < t.PassCount; j++)
                            {
                                Pass p = t.GetPass(j);

                                for (int k = 0; k < p.TextureUnitStageCount; k++)
                                {
                                    TextureUnitState tu = p.GetTextureUnitState(k);

                                    // TODO: Check with borrillis about marking "shadow" textures
                                    if (!string.IsNullOrEmpty(tu.Name))
                                    {

                                    }
                                }
                            }
                        }
                        // ...end of pointless block...
                    }
                }
                else
                {
                    string filename = Options.Instance.Landscape_Filename;
                    bool compressed = Options.Instance.VertexCompression;

                    string MatClassName = compressed
                                              ?
                                                  matname + "Decompress"
                                              :
                                                  matname;

                    matname = MatClassName + "."
                              + filename;

                    material = (Material) MaterialManager.Instance.GetByName(matname);
                    if (null == material)
                    {
                        material = (Material)MaterialManager.Instance.Load(MatClassName, Options.Instance.GroupName);
                        System.Diagnostics.Debug.Assert(null != material,
                                                        MatClassName + " Must exists in the " + Options.Instance.GroupName +
                                                        " group");
                        material = material.Clone(matname);

                        string extName = Options.Instance.TextureExtension;
                        string beginName = filename + ".";
                        string endName = "." + commonName + ".";
                        bool deformable;
                        string texName = string.Empty, finalTexName = string.Empty;

                        uint channel = 0;
                        int splat = 0;

                        uint alphachannel = 0;
                        uint coveragechannel = 0;

                        for (int i = 0; i < material.TechniqueCount; i++)
                        {
                            splat = 0;
                            channel = 0;
                            coveragechannel = 0;
                            alphachannel = 0;
                            Technique t = material.GetTechnique(i);
                            for (int j = 0; j < t.PassCount; j++)
                            {
                                Pass p = t.GetPass(j);
                                for (int k = 0; k < p.TextureUnitStageCount; k++)
                                {
                                    TextureUnitState tu = p.GetTextureUnitState(k);
                                    string texType = tu.Name;
                                    if (!texType.Contains("."))
                                    {
                                        // This Texture Name is A keyword,
                                        // meaning we have to dynamically replace it
                                        deformable = false;
                                        // check by what texture to replace keyword
                                        if (texType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
                                        {
                                            texName = Options.Instance.Image_Filename + endName;
                                            deformable = true;
                                        }
                                        else if (texType.StartsWith("splatting", StringComparison.OrdinalIgnoreCase))
                                        {
                                            texName =
                                                Options.Instance.SplatDetailMapNames[
                                                    splat%Options.Instance.NumMatHeightSplat];
                                            splat++;
                                        }
                                        else if (texType.StartsWith("base", StringComparison.OrdinalIgnoreCase))
                                        {
                                            texName = beginName + texType + endName;
                                            channel++;
                                            deformable = true;
                                        }
                                        else if (texType.StartsWith("normal", StringComparison.OrdinalIgnoreCase))
                                        {
                                            texName = beginName + texType + endName;
                                            channel++;
                                            deformable = true;
                                        }
                                        else if (texType.StartsWith("normalmap", StringComparison.OrdinalIgnoreCase))
                                        {
                                            texName = beginName + texType + endName;
                                            channel++;
                                            deformable = true;
                                        }
                                        //else if (StringUtil::startsWith (texType, "normalmap", true))
                                        //{
                                        //    String outBase, OutPath;
                                        //    StringUtil::splitFilename (beginName, outBase, OutPath);
                                        //    texName = OutPath + texType + endName;
                                        //    channel++;
                                        //    deformable = true;
                                        //}
                                        else if (texType.StartsWith("normalheightmap", StringComparison.OrdinalIgnoreCase))
                                        {
                                            texName = beginName + texType + endName;
                                            channel++;
                                            deformable = true;
                                        }
                                        else if (texType.StartsWith("normalheightmap",
                                                                    StringComparison.OrdinalIgnoreCase))
                                        {
                                            texName = beginName + texType + "." + alphachannel + endName;
                                            deformable = true;
                                            alphachannel++;
                                            channel++;
                                        }
                                        else if (texType.StartsWith("coveragemap",
                                                                    StringComparison.OrdinalIgnoreCase))
                                        {
                                            //texName = beginName + texType + nameSep;
                                            string OutPath;
                                            OutPath = System.IO.Path.GetFullPath(beginName);
                                            //StringUtil::splitFilename (beginName, outBase, OutPath);
                                            texName = OutPath + texType + ".";
                                            texName += (coveragechannel*3)%
                                                       Options.Instance.NumMatHeightSplat + endName;
                                            deformable = true;
                                            channel++;
                                            coveragechannel++;
                                        }
                                        else if (texType.StartsWith("coverage",
                                                                    StringComparison.OrdinalIgnoreCase))
                                        {
                                            texName = beginName + texType + "." +
                                                      (coveragechannel*4)%
                                                      Options.Instance.NumMatHeightSplat + endName;
                                            deformable = true;
                                            channel++;
                                            coveragechannel++;
                                        }
                                        else if (texType.StartsWith("light",
                                                                    StringComparison.OrdinalIgnoreCase))
                                        {
                                            texName = beginName + texType + endName + extName;
                                        }
                                        else if (texType.StartsWith("horizon",
                                                                    StringComparison.
                                                                        OrdinalIgnoreCase))
                                        {
                                            texName = beginName + "HSP" + endName + extName;
                                            mPositiveShadow = true;
                                        }
                                        if (deformable)
                                        {
                                            if (Options.Instance.Deformable &&
                                                ResourceGroupManager.Instance.ResourceExists(Options.Instance.GroupName,
                                                                                             texName + "modif." +
                                                                                             extName))
                                            {
                                                finalTexName = texName + "modif." + extName;
                                            }
                                            else
                                            {
                                                finalTexName = texName + extName;
                                            }
                                        }
                                        else
                                        {
                                            finalTexName = texName;
                                        }
                                        tu.SetTextureName(finalTexName);

                                        material.Load();
                                        material.Lighting = Options.Instance.Lit;

                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
*/

	    protected abstract void loadMaterial();
		protected abstract void unloadMaterial();

	}
}
