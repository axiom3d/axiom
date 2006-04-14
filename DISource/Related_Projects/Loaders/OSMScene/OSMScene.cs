#region License
/*
 This port of the Ofusion Scene Loader is distributed as Freeware
 */
#endregion

#region Using
using System;
using System.Data;
using System.Collections.Generic;
using System.Text;
using Axiom;
using Axiom.Core;
using Axiom.MathLib;
using RealmForge;
#endregion

namespace Ofusion.Net
{
    public partial class OSMScene
    {
        #region Private variables
        private SceneManager mScene;
        private RenderWindow mWindow;
        private string mSceneFile;
        private DataSet mSceneLayout = new DataSet("SceneLayout");
        private string section = "";
        #endregion

        #region Properties

        private object GetProperty(string section, int index, string property)
        {
            return SceneSetup[section].Rows[index][property];
        }

        private DataRow GetPropertyRow(string section, int index)
        {
            return SceneSetup[section].Rows[index];
        }

        private int SceneManager
        {
            get
            {
                if (SceneSetup.Contains("sceneManager"))
                    return int.Parse(GetProperty("sceneManager", 0, "type").ToString());
                else
                    return -1;
            }
        }

        private int BackgroundRed
        {
            get { return int.Parse(GetProperty("bkgcolor", 0, "r").ToString()); }
        }

        private int BackgroundGreen
        {
            get { return int.Parse(GetProperty("bkgcolor", 0, "g").ToString()); }
        }

        private int BackgroundBlue
        {
            get { return int.Parse(GetProperty("bkgcolor", 0, "b").ToString()); }
        }

        private int TechniqueType
        {
            get
            {
                if (SceneSetup.Contains("shadowTechnique"))
                    return int.Parse(TechniqueTypeRow["type"].ToString());
                else
                    return -1;
            }
        }

        private DataRow TechniqueTypeRow
        {
            get { return GetPropertyRow("shadowTechnique", 0); }
        }

        private int SkyTechnique
        {
            get
            {
                if (SceneSetup.Contains("skyTechnique"))
                    return int.Parse(SkyTechniqueRow["type"].ToString());
                else
                    return -1;
            }
        }

        private DataRow SkyTechniqueRow
        {
            get { return GetPropertyRow("skyTechnique", 0); }
        }

        private DataTableCollection SceneSetup
        {
            get { return mSceneLayout.Tables; }
        }

        private DataTable Entities
        {
            get { return mSceneLayout.Tables["entity"]; }
        }

        private DataTable Cameras
        {
            get { return mSceneLayout.Tables["camera"]; }
        }

        private DataTable Lights
        {
            get { return mSceneLayout.Tables["light"]; }
        }

        #endregion

        #region Public functions
        public OSMScene(SceneManager Scene, RenderWindow Window)
        {
            this.mScene = Scene;
            this.mWindow = Window;
        }

        public bool Initialize(string SceneFile)
        {
            try
            {
                this.mSceneFile = SceneFile;
                mSceneLayout.ReadXml(SceneFile);

                SceneNode pMain = null;
                CreateScene(pMain);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("OSMScene Initialize: " + ex.Message);
            }
        }

        #endregion

        #region Scene Load and create

        private void CreateScene(SceneNode pParent)
        {
            if (pParent == null)
                pParent = mScene.RootSceneNode.CreateChildSceneNode();

            LoadSceneManager();
            LoadEntities(pParent);
            LoadLights(pParent);
            LoadCameras(pParent);
        }

        #region Scene Manager
        private void LoadSceneManager()
        {
            if (mScene == null)
            {
                if (SceneManager >= 0)
                    setSceneManager();
                else
                    mScene = Root.Instance.SceneManagers.GetSceneManager(SceneType.Generic);
            }

            // Set ambient lighting
            mScene.AmbientLight.r = float.Parse(GetProperty("lightcolor", 0, "r").ToString());
            mScene.AmbientLight.g = float.Parse(GetProperty("lightcolor", 0, "g").ToString());
            mScene.AmbientLight.b = float.Parse(GetProperty("lightcolor", 0, "b").ToString());
        }
        private void setSceneManager()
        {
            mScene = Root.Instance.SceneManagers.GetSceneManager(GetSMType(SceneManager));

            mWindow.GetViewport(0).BackgroundColor =
                new ColorEx(BackgroundRed, BackgroundGreen, BackgroundBlue);

            if (TechniqueType >= 0)
            {
                mScene.ShadowTechnique = GetTechniqueType(TechniqueType);
                mScene.SetShadowTextureSettings(
                        ushort.Parse(TechniqueTypeRow["tex_size"].ToString()),
                        ushort.Parse(TechniqueTypeRow["tex_count"].ToString()));

                //TODO: Add the shadow color into the scene
                //mScene.ShadowColor = ;
            }

            if (SkyTechnique >= 0)
            {
                Plane plane = new Plane();
                Quaternion quat = new Quaternion();

                float dist = float.Parse(SkyTechniqueRow["dist"].ToString());
                string material = SkyTechniqueRow["material"].ToString();
                float scale = float.Parse(SkyTechniqueRow["scale"].ToString());
                float tiling = float.Parse(SkyTechniqueRow["tiling"].ToString());
                bool drawFirst = Boolean.Parse(SkyTechniqueRow["drawFirst"].ToString());
                float bow = float.Parse(SkyTechniqueRow["bow"].ToString());
                int xSegments = int.Parse(SkyTechniqueRow["xSegments"].ToString());
                int ySegments = int.Parse(SkyTechniqueRow["ySegments"].ToString());

                quat = Axiom.MathLib.Quaternion.Identity;

                plane.D = dist;
                plane.Normal = -(Vector3.UnitY);

                mScene.SetSkyPlane(false, plane, "");
                mScene.SetSkyBox(false, "", 0);
                mScene.SetSkyDome(false, "", 0, 0);

                switch (SkyTechnique)
                {
                    case 1:
                        mScene.SetSkyPlane(true, plane, material, scale, tiling, drawFirst, bow,
                            xSegments, ySegments);
                        break;

                    case 2:
                        mScene.SetSkyBox(true, material, dist, drawFirst, quat);
                        break;

                    case 3:
                        mScene.SetSkyDome(true, material, bow, tiling, dist, drawFirst, quat);
                        break;
                }
            }
        }
        private ShadowTechnique GetTechniqueType(int type)
        {
            return (ShadowTechnique)Enum.Parse(typeof(ShadowTechnique), Enum.GetName(typeof(ShadowTechnique), type));
        }
        private SceneType GetSMType(int type)
        {
            return (SceneType)Enum.Parse(typeof(SceneType), Enum.GetName(typeof(SceneType), type));
        }
        #endregion

        #region Entities

        private void LogStatus(LoggingLevel logLevel)
        {
            LogManager.Instance.LogDetail = logLevel;
        }

        private void LoadEntities(SceneNode pParent)
        {
            section = "entity";
            foreach (DataRow sEntity in Entities.Rows)
            {
                Entity pEntity = mScene.CreateEntity(
                        sEntity["name"].ToString(),
                        sEntity["filename"].ToString());

                pEntity.CastShadows = sEntity["CastShadows"].ToString() == "no" ? false : true;

                SceneNode pNode = CreateNode(sEntity, pParent);
                pNode.AttachObject(pEntity);
            }
        }

        private DataRowView GetView(string table, int EntityId)
        {
            return GetCompleteView(table, EntityId)[0];
        }

        private DataView GetCompleteView(string table, int EntityId)
        {
            DataView dv = new DataView(SceneSetup[table]);
            dv.RowFilter = string.Format("entity_Id = {1}", table, EntityId);
            return dv;
        }

        private DataView GetCompleteView(string table, string Filter)
        {
            DataView dv = new DataView(SceneSetup[table]);
            dv.RowFilter = Filter;
            return dv;
        }

        private SceneNode CreateNode(DataRow sEntity, SceneNode pParent)
        {
            SceneNode pNode = null;
            string Name = sEntity["name"].ToString();
            string Parent = sEntity.Table.Columns.Contains("parent") ? sEntity["parent"].ToString() : null;

            LogStatus(LoggingLevel.Low);

            if (Parent == null)
            {
                try
                {
                    pNode = mScene.GetSceneNode(Name);
                }
                catch
                {
                    pNode = pParent.CreateChildSceneNode(Name);
                }
            }
            else
            {
                SceneNode pParentNode;

                try 
	            {
                    pParentNode = mScene.GetSceneNode(Parent);
	            }
	            catch
	            {
                    pParentNode = pParent.CreateChildSceneNode(Parent);
	            }

                try
                {
                    pNode = mScene.GetSceneNode(Name);
                    SceneNode pOldParent = (SceneNode)pNode.Parent;
                    pOldParent.RemoveChild(pNode);
                    pParentNode.AddChild(pNode);
                }
                catch
                {
                    pNode = pParentNode.CreateChildSceneNode(Name);
                }
            }

            LogStatus(LoggingLevel.Normal);

            // Position
            DataRowView view = GetView("position", int.Parse(sEntity[string.Format("{0}_Id", section)].ToString()));
            if (view != null)
            {
                pNode.Position = new Vector3(
                    float.Parse(view["x"].ToString()),
                    float.Parse(view["y"].ToString()),
                    float.Parse(view["z"].ToString()));
            }

            // Rotation
            view = GetView("rotation", int.Parse(sEntity[string.Format("{0}_Id", section)].ToString()));
            if (view != null)
            {
                pNode.Orientation = new Quaternion(
                    float.Parse(view["w"].ToString()),
                    float.Parse(view["x"].ToString()),
                    float.Parse(view["y"].ToString()),
                    float.Parse(view["z"].ToString()));
            }

            // Scale
            view = GetView("scale", int.Parse(sEntity[string.Format("{0}_Id", section)].ToString()));
            if (view != null)
            {
                pNode.Scale(new Vector3(
                    float.Parse(view["x"].ToString()),
                    float.Parse(view["y"].ToString()),
                    float.Parse(view["z"].ToString())));
            }

            // Animation Portion
            DataView animationsList = GetCompleteView("animations", int.Parse(sEntity[string.Format("{0}_Id", section)].ToString()));

            foreach(DataRowView animCol in animationsList)
            {
                DataView animSets = GetCompleteView("animation", 
                    string.Format("animations_Id = {0}", int.Parse(animCol["animations_Id"].ToString())));

                foreach (DataRowView anim in animSets)
                {
                    Animation pAnim = null;

                    try
                    {
                        pAnim = mScene.GetAnimation(anim["name"].ToString());
                    }
                    catch
                    {
                    }

                    if (pAnim == null)
                    {
                        float length = float.Parse(anim["length"].ToString());
                        pAnim = mScene.CreateAnimation(anim["name"].ToString(), length);
                        pAnim.InterpolationMode = InterpolationMode.Linear;
                    }

                    AnimationTrack pTrack = pAnim.CreateTrack((short)(pAnim.Tracks.Count + 1), pNode);

                    DataView keyFrames = GetCompleteView("keyframe",
                        string.Format("animation_Id = {0}", int.Parse(anim["animation_Id"].ToString())));

                    foreach (DataRowView keyFrame in keyFrames)
                    {
                        KeyFrame pKeyFrame = pTrack.CreateKeyFrame(float.Parse(keyFrame["time"].ToString()));

                        DataView keyFrameView = GetCompleteView( "position",
                            string.Format("keyframe_Id = {0}", int.Parse(keyFrame["keyframe_Id"].ToString())));

                        if (keyFrameView != null)
                        {
                            pKeyFrame.Translate = new Vector3(
                                float.Parse(keyFrameView[0]["x"].ToString()),
                                float.Parse(keyFrameView[0]["y"].ToString()),
                                float.Parse(keyFrameView[0]["z"].ToString())
                                );
                        }

                        keyFrameView = GetCompleteView("rotation",
                            string.Format("keyframe_Id = {0}", int.Parse(keyFrame["keyframe_Id"].ToString())));

                        if (keyFrameView != null)
                        {
                            pKeyFrame.Rotation = new Quaternion(
                                float.Parse(keyFrameView[0]["w"].ToString()),
                                float.Parse(keyFrameView[0]["x"].ToString()),
                                float.Parse(keyFrameView[0]["y"].ToString()),
                                float.Parse(keyFrameView[0]["z"].ToString())
                                );
                        }

                        keyFrameView = GetCompleteView("scale",
                            string.Format("keyframe_Id = {0}", int.Parse(keyFrame["keyframe_Id"].ToString())));

                        if (keyFrameView != null)
                        {
                            pKeyFrame.Scale = new Vector3(
                                float.Parse(keyFrameView[0]["x"].ToString()),
                                float.Parse(keyFrameView[0]["y"].ToString()),
                                float.Parse(keyFrameView[0]["z"].ToString())
                                );
                        }
                    }
                }
            }

            return pNode;
        }

        #endregion

        #region Lights

        private void LoadLights(SceneNode pParent)
        {
            section = "light";

            foreach (DataRow lightV in Lights.Rows)
            {
                Light light = mScene.CreateLight(lightV["name"].ToString());

                switch (lightV["type"].ToString())
                {
                    case "omni":
                        light.Type = LightType.Point;
                        break;

                    case "spot":
                        light.Type = LightType.Spotlight;
                        light.SetSpotlightRange(float.Parse(lightV["hotspot"].ToString()),
                                                float.Parse(lightV["falloff"].ToString()));
                        break;

                    case "directional":
                        light.Type = LightType.Directional;
                        break;
                }

                light.IsVisible = (lightV["on"].ToString() == "true" ? true : false);
                light.CastShadows = (lightV["CastShadows"].ToString() == "yes" ? true : false);

                DataView view = GetCompleteView("color", string.Format("light_Id = {0}", lightV["light_Id"].ToString()));
                if (view.Table != null)
                {
                    light.Diffuse = new ColorEx(
                        float.Parse(view[0]["r"].ToString()),
                        float.Parse(view[0]["g"].ToString()),
                        float.Parse(view[0]["b"].ToString()));
                }

                view = GetCompleteView("specular", string.Format("light_Id = {0}", lightV["light_Id"].ToString()));
                if (view.Table != null)
                {
                    light.Specular = new ColorEx(
                        float.Parse(view[0]["r"].ToString()),
                        float.Parse(view[0]["g"].ToString()),
                        float.Parse(view[0]["b"].ToString()));
                }

                view = GetCompleteView("attenuation", string.Format("light_Id = {0}", lightV["light_Id"].ToString()));
                if (view.Table != null)
                {
                    light.SetAttenuation(
                        float.Parse(view[0]["range"].ToString()),
                        float.Parse(view[0]["constant"].ToString()),
                        float.Parse(view[0]["linear"].ToString()),
                        float.Parse(view[0]["quadratic"].ToString()));
                }

                SceneNode pLightNode = CreateNode(lightV, pParent);
                pLightNode.AttachObject(light);

                view = GetCompleteView("target", string.Format("light_Id = {0}", lightV["light_Id"].ToString()));
                if (view.Table != null)
                {
                    //SceneNode pTarget = CreateNode((DataRow)view, pParent);
                    //pLightNode.SetAutoTracking(true, pTarget); 
                }
            }
        }

        #endregion

        #region Cameras

        private void LoadCameras(SceneNode pParent)
        {
            section = "camera";
            //TODO: Insert the camera create code here
        }

        #endregion

        #endregion
    }
}

