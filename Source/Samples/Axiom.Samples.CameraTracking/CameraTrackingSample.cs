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

using Axiom.Core;
using Axiom.Animating;
using Axiom.Math;

namespace Axiom.Samples.CameraTrack
{
    /// <summary>
    /// 
    /// </summary>
    public class CameraTrackingSample : SdkSample
    {
        /// <summary>
        /// 
        /// </summary>
        protected AnimationState animState;

        /// <summary>
        /// 
        /// </summary>
        public CameraTrackingSample()
        {
            Metadata["Title"] = "Camera Tracking";
            Metadata["Description"] = "An example of using AnimationTracks to make a node smoothly follow " +
                                        "a predefined path with spline interpolation. Also uses the auto-tracking feature of the camera.";
            Metadata["Thumbnail"] = "thumb_camtrack.png";
            Metadata["Category"] = "Unsorted";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        /// <returns></returns>
        public override bool FrameRenderingQueued(FrameEventArgs evt)
        {
            this.animState.AddTime(evt.TimeSinceLastFrame);
            return base.FrameRenderingQueued(evt);
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void SetupContent()
        {
            // setup some basic lighting for our scene
            SceneManager.AmbientLight = new ColorEx(0.3f, 0.3f, 0.3f);
            SceneManager.CreateLight("CameraTrackLight").Position = new Vector3(20, 80, 50);

            SceneManager.SetSkyBox(true, "Examples/MorningSkyBox", 5000);
            // create an ogre head entity and attach it to a node
            Entity head = SceneManager.CreateEntity("Head", "ogrehead.mesh");
            SceneNode headNode = SceneManager.RootSceneNode.CreateChildSceneNode();
            headNode.AttachObject(head);
            CameraManager.setStyle(CameraStyle.Manual);
            // we will be controlling the camera ourselves, so disable the camera man
            Camera.SetAutoTracking(true, headNode); // make the camera face the head


            // create a camera node and attach camera to it
            SceneNode camNode = SceneManager.RootSceneNode.CreateChildSceneNode();
            camNode.AttachObject(Camera);

            // set up a 10 second animation for our camera, using spline interpolation for nice curves
            Animation anim = SceneManager.CreateAnimation("CameraTrack", 10);
            anim.InterpolationMode = InterpolationMode.Spline;
            // create a track to animate the camera's node
            NodeAnimationTrack track = anim.CreateNodeTrack(0, camNode);

            // create keyframes for our track
            track.CreateNodeKeyFrame(0).Translate = new Vector3(200, 0, 0);
            track.CreateNodeKeyFrame(2.5f).Translate = new Vector3(0, -50, 100);
            track.CreateNodeKeyFrame(5).Translate = new Vector3(-500, 100, 0);
            track.CreateNodeKeyFrame(7.5f).Translate = new Vector3(0, 200, -300);
            track.CreateNodeKeyFrame(10).Translate = new Vector3(200, 0, 0);

            // create a new animation state to track this
            this.animState = SceneManager.CreateAnimationState("CameraTrack");
            this.animState.IsEnabled = true;
            base.SetupContent();
        }
    }
}