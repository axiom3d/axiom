using Axiom.Math;
using Axiom.Core;

namespace Axiom.Samples.SphereMapping
{
    /// <summary>
    /// 
    /// </summary>
    public class SphereMappingSample : SdkSample
    {
        public SphereMappingSample()
        {
            Metadata["Title"] = "Sphere Mapping";
            Metadata["Description"] = "Shows the sphere mapping feature of materials. " +
                                        "Sphere maps are not wrapped, and look the same from all directions.";
            Metadata["Thumbnail"] = "thumb_spheremap.png";
            Metadata["Category"] = "Unsorted";
        }

        protected override void SetupContent()
        {
            Viewport.BackgroundColor = ColorEx.White;
            // setup some basic lighting for our scene
            SceneManager.AmbientLight = new ColorEx(0.3f, 0.3f, 0.3f);
            SceneManager.CreateLight("SphereMappingSampleLight").Position = new Vector3(20, 80, 50);
            // set our camera to orbit around the origin and show cursor
            CameraManager.setStyle(CameraStyle.Orbit);
            TrayManager.ShowCursor();

            // create our model, give it the environment mapped material, and place it at the origin
            Entity ent = SceneManager.CreateEntity("Head", "ogrehead.mesh");
            ent.MaterialName = "Examples/SphereMappedRustySteel";
            SceneManager.RootSceneNode.AttachObject(ent);
        }
    }
}