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
using Axiom.Math;
using Axiom.Graphics;
using Axiom.Collections;
using Axiom.Animating;

namespace Axiom.Samples.CharacterSample
{
    public class SinbadCharacterController
    {
        /// <summary>
        /// number of animations the character has
        /// </summary>
        public const int NumAnims = 13;

        /// <summary>
        /// height of character's center of mass above ground
        /// </summary>
        public const int CharHeight = 5;

        /// <summary>
        /// height of camera above character's center of mass
        /// </summary>
        public const int CamHeight = 2;

        /// <summary>
        /// character running speed in units per second
        /// </summary>
        public const int RunSpeed = 17;

        /// <summary>
        /// character turning in degrees per second
        /// </summary>
        public Real TurnSpeed = 500f;

        /// <summary>
        /// animation crossfade speed in % of full weight per second
        /// </summary>
        public Real AnimFadeSpeed = 7.5;

        /// <summary>
        /// character jump acceleration in upward units per squared second
        /// </summary>
        public Real JumpAcceleration = 30f;

        /// <summary>
        /// gravity in downward units per squared second
        /// </summary>
        public Real Gravity = 90f;

        /// <summary>
        /// all the animations our character has, and a null ID
        /// some of these affect separate body parts and will be blended together
        /// </summary>
        public enum AnimationID
        {
            IdleBase,
            IdleTop,
            RunBase,
            RunTop,
            HandsClosed,
            HandsRelaxed,
            DrawSword,
            SliceVertical,
            SliceHorizontal,
            Dance,
            JumpStart,
            JumpLoop,
            JumpEnd,
            None
        }

        #region fields

        /// <summary>
        /// 
        /// </summary>
        protected Camera camera;

        /// <summary>
        /// 
        /// </summary>
        protected SceneNode bodyNode;

        /// <summary>
        /// 
        /// </summary>
        protected SceneNode cameraPivot;

        /// <summary>
        /// 
        /// </summary>
        protected SceneNode cameraGoal;

        /// <summary>
        /// 
        /// </summary>
        protected SceneNode cameraNode;

        /// <summary>
        /// 
        /// </summary>
        protected Real pivotPitch;

        /// <summary>
        /// 
        /// </summary>
        protected Entity bodyEnt;

        /// <summary>
        /// 
        /// </summary>
        protected Entity sword1;

        /// <summary>
        /// 
        /// </summary>
        protected Entity sword2;

        /// <summary>
        /// 
        /// </summary>
        protected RibbonTrail swordTrail;

        /// <summary>
        /// // master animation list
        /// </summary>
        protected AnimationState[] anims = new AnimationState[NumAnims];

        /// <summary>
        /// current base (full- or lower-body) animation
        /// </summary>
        protected AnimationID baseAnimID;

        /// <summary>
        /// current top (upper-body) animation
        /// </summary>
        protected AnimationID topAnimID;

        /// <summary>
        /// which animations are fading in
        /// </summary>
        protected bool[] fadingIn = new bool[NumAnims];

        /// <summary>
        /// which animations are fading out
        /// </summary>
        protected bool[] fadingOut = new bool[NumAnims];

        /// <summary>
        /// 
        /// </summary>
        protected bool swordsDrawn;

        /// <summary>
        /// player's local intended direction based on WASD keys
        /// </summary>
        protected Vector3 keyDirection;

        /// <summary>
        /// actual intended direction in world-space
        /// </summary>
        protected Vector3 goalDirection;

        /// <summary>
        /// for jumping
        /// </summary>
        protected Real verticalVelocity;

        /// <summary>
        /// general timer to see how long animations have been playing
        /// </summary>
        protected Real timer;

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cam"></param>
        public SinbadCharacterController(Camera cam)
        {
            SetupBody(cam.SceneManager);
            SetupCamera(cam);
            SetupAnimations();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public void InjectKeyDown(SharpInputSystem.KeyEventArgs e)
        {
            if (e.Key == SharpInputSystem.KeyCode.Key_Q &&
                 (this.topAnimID == AnimationID.IdleTop || this.topAnimID == AnimationID.RunTop))
            {
                // take swords out (or put them back, since it's the same animation but reversed)
                SetTopAnimation(AnimationID.DrawSword, true);
                this.timer = 0;
            }
            else if (e.Key == SharpInputSystem.KeyCode.Key_E && !this.swordsDrawn)
            {
                if (this.topAnimID == AnimationID.IdleTop || this.topAnimID == AnimationID.RunTop)
                {
                    // start dancing
                    SetBaseAnimation(AnimationID.Dance, true);
                    SetTopAnimation(AnimationID.None);
                    // disable hand animation because the dance controls hands
                    this.anims[(int)AnimationID.HandsRelaxed].IsEnabled = false;
                }
                else if (this.baseAnimID == AnimationID.Dance)
                {
                    // stop dancing
                    SetBaseAnimation(AnimationID.IdleBase, true);
                    SetTopAnimation(AnimationID.IdleTop);
                    // re-enable hand animation
                    this.anims[(int)AnimationID.HandsRelaxed].IsEnabled = true;
                }
            }
            // keep track of the player's intended direction
            else if (e.Key == SharpInputSystem.KeyCode.Key_W)
            {
                this.keyDirection.z = -1;
            }
            else if (e.Key == SharpInputSystem.KeyCode.Key_A)
            {
                this.keyDirection.x = -1;
            }
            else if (e.Key == SharpInputSystem.KeyCode.Key_S)
            {
                this.keyDirection.z = 1;
            }
            else if (e.Key == SharpInputSystem.KeyCode.Key_D)
            {
                this.keyDirection.x = 1;
            }

            else if (e.Key == SharpInputSystem.KeyCode.Key_SPACE &&
                      (this.topAnimID == AnimationID.IdleTop || this.topAnimID == AnimationID.RunTop))
            {
                // jump if on ground
                SetBaseAnimation(AnimationID.JumpStart, true);
                SetTopAnimation(AnimationID.None);
                this.timer = 0;
            }

            if (!this.keyDirection.IsZeroLength && this.baseAnimID == AnimationID.IdleBase)
            {
                // start running if not already moving and the player wants to move
                SetBaseAnimation(AnimationID.RunBase, true);
                if (this.topAnimID == AnimationID.IdleTop)
                {
                    SetTopAnimation(AnimationID.RunTop, true);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public void InjectKeyUp(SharpInputSystem.KeyEventArgs e)
        {
            // keep track of the player's intended direction
            if (e.Key == SharpInputSystem.KeyCode.Key_W && this.keyDirection.z == -1)
            {
                this.keyDirection.z = 0;
            }
            else if (e.Key == SharpInputSystem.KeyCode.Key_A && this.keyDirection.x == -1)
            {
                this.keyDirection.x = 0;
            }
            else if (e.Key == SharpInputSystem.KeyCode.Key_S && this.keyDirection.z == 1)
            {
                this.keyDirection.z = 0;
            }
            else if (e.Key == SharpInputSystem.KeyCode.Key_D && this.keyDirection.x == 1)
            {
                this.keyDirection.x = 0;
            }

            if (this.keyDirection.IsZeroLength && this.baseAnimID == AnimationID.RunBase)
            {
                // start running if not already moving and the player wants to move
                SetBaseAnimation(AnimationID.IdleBase, true);
                if (this.topAnimID == AnimationID.RunTop)
                {
                    SetTopAnimation(AnimationID.IdleTop, true);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public void InjectMouseMove(SharpInputSystem.MouseEventArgs e)
        {
            // update camera goal based on mouse movement
            UpdateCameraGoal(-0.05f * e.State.X.Relative, -0.05f * e.State.Y.Relative, -0.0005f * e.State.Z.Relative);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <param name="id"></param>
        public void InjectMouseDown(SharpInputSystem.MouseEventArgs e, SharpInputSystem.MouseButtonID id)
        {
            if (this.swordsDrawn && (this.topAnimID == AnimationID.IdleTop || this.topAnimID == AnimationID.RunTop))
            {
                // if swords are out, and character's not doing something weird, then SLICE!
                if (id == SharpInputSystem.MouseButtonID.Left)
                {
                    SetTopAnimation(AnimationID.SliceVertical, true);
                }
                else if (id == SharpInputSystem.MouseButtonID.Right)
                {
                    SetTopAnimation(AnimationID.SliceHorizontal, true);
                }
                this.timer = 0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deltaTime"></param>
        public void AddTime(Real deltaTime)
        {
            UpdateBody(deltaTime);
            UpdateAnimations(deltaTime);
            UpdateCamera(deltaTime);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sceneMgr"></param>
        private void SetupBody(SceneManager sceneMgr)
        {
            // create main model
            this.bodyNode = sceneMgr.RootSceneNode.CreateChildSceneNode(Vector3.UnitY * CharHeight);
            this.bodyEnt = sceneMgr.CreateEntity("SinbadBody", "Sinbad.mesh");
            this.bodyNode.AttachObject(this.bodyEnt);

            // create swords and attach to sheath
            this.sword1 = sceneMgr.CreateEntity("SinbadSword1", "Sword.mesh");
            this.sword2 = sceneMgr.CreateEntity("SinbadSword2", "Sword.mesh");
            this.bodyEnt.AttachObjectToBone("Sheath.L", this.sword1);
            this.bodyEnt.AttachObjectToBone("Sheath.R", this.sword2);

            // create a couple of ribbon trails for the swords, just for fun
            var paras = new NamedParameterList();
            paras["numberOfChains"] = "2";
            paras["maxElements"] = "80";
            this.swordTrail = (RibbonTrail)sceneMgr.CreateMovableObject("SinbadRibbon", "RibbonTrail", paras);
            this.swordTrail.MaterialName = "Examples/LightRibbonTrail";
            this.swordTrail.TrailLength = 20;
            this.swordTrail.IsVisible = false;
            sceneMgr.RootSceneNode.AttachObject(this.swordTrail);

            for (int i = 0; i < 2; i++)
            {
                this.swordTrail.SetInitialColor(i, new ColorEx(1, 0.8f, 0));
                this.swordTrail.SetColorChange(i, new ColorEx(0.75f, 0.25f, 0.25f, 0.25f));
                this.swordTrail.SetWidthChange(i, 1);
                this.swordTrail.SetInitialWidth(i, 0.5f);
            }

            this.keyDirection = Vector3.Zero;
            this.verticalVelocity = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        private void SetupAnimations()
        {
            // this is very important due to the nature of the exported animations
            this.bodyEnt.Skeleton.BlendMode = SkeletalAnimBlendMode.Cumulative;

            var animNames = new string[]
                            {
                                "IdleBase", "IdleTop", "RunBase", "RunTop", "HandsClosed", "HandsRelaxed", "DrawSwords",
                                "SliceVertical", "SliceHorizontal", "Dance", "JumpStart", "JumpLoop", "JumpEnd"
                            };

            for (int i = 0; i < NumAnims; i++)
            {
                this.anims[i] = this.bodyEnt.GetAnimationState(animNames[i]);
                this.anims[i].Loop = true;
                this.fadingIn[i] = false;
                this.fadingOut[i] = false;
            }

            // start off in the idle state (top and bottom together)
            SetBaseAnimation(AnimationID.IdleBase);
            SetTopAnimation(AnimationID.IdleTop);

            // relax the hands since we're not holding anything
            this.anims[(int)AnimationID.HandsRelaxed].IsEnabled = true;

            this.swordsDrawn = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="camera"></param>
        private void SetupCamera(Camera cam)
        {
            // create a pivot at roughly the character's shoulder
            this.cameraPivot = cam.SceneManager.RootSceneNode.CreateChildSceneNode();
            // this is where the camera should be soon, and it spins around the pivot
            this.cameraGoal = this.cameraPivot.CreateChildSceneNode(new Vector3(0, 0, 15));
            // this is where the camera actually is
            this.cameraNode = cam.SceneManager.RootSceneNode.CreateChildSceneNode();
            this.cameraNode.Position = this.cameraPivot.Position + this.cameraGoal.Position;

            this.cameraPivot.SetFixedYawAxis(true);
            this.cameraGoal.SetFixedYawAxis(true);
            this.cameraNode.SetFixedYawAxis(true);

            // our model is quite small, so reduce the clipping planes
            cam.Near = 0.1f;
            cam.Far = 100;
            this.cameraNode.AttachObject(cam);
            this.pivotPitch = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deltaTime"></param>
        private void UpdateBody(Real deltaTime)
        {
            // we will calculate this
            this.goalDirection = Vector3.Zero;

            if (this.keyDirection != Vector3.Zero && this.baseAnimID != AnimationID.Dance)
            {
                // calculate actually goal direction in world based on player's key directions
                this.goalDirection += this.keyDirection.z * this.cameraNode.Orientation.ZAxis;
                this.goalDirection += this.keyDirection.x * this.cameraNode.Orientation.XAxis;
                this.goalDirection.y = 0;
                this.goalDirection.Normalize();

                Quaternion toGoal = this.bodyNode.Orientation.ZAxis.GetRotationTo(this.goalDirection);
                // calculate how much the character has to turn to face goal direction
                Real yawToGlobal = toGoal.Yaw;
                // this is how much the character CAN turn this frame
                Real yawAtSpeed = yawToGlobal / Utility.Abs(yawToGlobal) * deltaTime * this.TurnSpeed;
                // reduce "turnability" if we're in midair
                if (this.baseAnimID == AnimationID.JumpLoop)
                {
                    yawAtSpeed *= 0.2;
                }

                // turn as much as we can, but not more than we need to
                if (yawToGlobal < 0)
                {
                    yawToGlobal = Utility.Min<Real>(yawToGlobal, yawAtSpeed);
                }
                else if (yawToGlobal > 0)
                {
                    yawToGlobal = Utility.Max<Real>(0, Utility.Min<Real>(yawToGlobal, yawAtSpeed));
                }

                this.bodyNode.Yaw(yawToGlobal);

                // move in current body direction (not the goal direction)
                this.bodyNode.Translate(new Vector3(0, 0, deltaTime * RunSpeed * this.anims[(int)this.baseAnimID].Weight),
                                         TransformSpace.Local);
            }

            if (this.baseAnimID == AnimationID.JumpLoop)
            {
                // if we're jumping, add a vertical offset too, and apply gravity
                this.bodyNode.Translate(new Vector3(0, this.verticalVelocity * deltaTime, 0), TransformSpace.Local);
                this.verticalVelocity -= this.Gravity * deltaTime;

                Vector3 pos = this.bodyNode.Position;
                if (pos.y <= CharHeight)
                {
                    // if we've hit the ground, change to landing state
                    pos.y = CharHeight;
                    this.bodyNode.Position = pos;
                    SetBaseAnimation(AnimationID.JumpEnd, true);
                    this.timer = 0;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deltaTime"></param>
        private void UpdateAnimations(Real deltaTime)
        {
            Real baseAnimSpeed = 1;
            Real topAnimSpeed = 1;

            this.timer += deltaTime;

            if (this.topAnimID == AnimationID.DrawSword)
            {
                // flip the draw swords animation if we need to put it back
                topAnimSpeed = this.swordsDrawn ? -1 : 1;

                // half-way through the animation is when the hand grasps the handles...
                if (this.timer >= this.anims[(int)this.topAnimID].Length / 2 &&
                     this.timer - deltaTime < this.anims[(int)this.topAnimID].Length / 2)
                {
                    // toggle sword trails
                    this.swordTrail.IsVisible = !this.swordsDrawn;

                    // so transfer the swords from the sheaths to the hands
                    if (this.swordsDrawn)
                    {
                        this.swordTrail.RemoveNode(this.sword1.ParentNode);
                        this.swordTrail.RemoveNode(this.sword2.ParentNode);
                    }
                    this.bodyEnt.DetachAllObjectsFromBone();
                    this.bodyEnt.AttachObjectToBone(this.swordsDrawn ? "Sheath.L" : "Handle.L", this.sword1);
                    this.bodyEnt.AttachObjectToBone(this.swordsDrawn ? "Sheath.R" : "Handle.R", this.sword2);

                    if (!this.swordsDrawn)
                    {
                        this.swordTrail.AddNode(this.sword1.ParentNode);
                        this.swordTrail.AddNode(this.sword2.ParentNode);
                    }
                    // change the hand state to grab or let go
                    this.anims[(int)AnimationID.HandsClosed].IsEnabled = !this.swordsDrawn;
                    this.anims[(int)AnimationID.HandsRelaxed].IsEnabled = this.swordsDrawn;
                } //end if

                if (this.timer >= this.anims[(int)this.topAnimID].Length)
                {
                    // animation is finished, so return to what we were doing before
                    if (this.baseAnimID == AnimationID.IdleBase)
                    {
                        SetTopAnimation(AnimationID.IdleTop);
                    }
                    else
                    {
                        SetTopAnimation(AnimationID.RunTop);
                        this.anims[(int)AnimationID.RunTop].Time = this.anims[(int)AnimationID.RunBase].Time;
                    }

                    this.swordsDrawn = !this.swordsDrawn;
                } //end if
            } //end if
            else if (this.topAnimID == AnimationID.SliceVertical || this.topAnimID == AnimationID.SliceHorizontal)
            {
                if (this.timer >= this.anims[(int)this.topAnimID].Length)
                {
                    // animation is finished, so return to what we were doing before
                    if (this.baseAnimID == AnimationID.IdleBase)
                    {
                        SetTopAnimation(AnimationID.IdleTop);
                    }
                    else
                    {
                        SetTopAnimation(AnimationID.RunTop);
                        this.anims[(int)AnimationID.RunTop].Time = this.anims[(int)AnimationID.RunBase].Time;
                    }
                }
                // don't sway hips from side to side when slicing. that's just embarrasing.
                if (this.baseAnimID == AnimationID.IdleBase)
                {
                    baseAnimSpeed = 0;
                }
            } //end else if
            else if (this.baseAnimID == AnimationID.JumpStart)
            {
                if (this.timer >= this.anims[(int)this.baseAnimID].Length)
                {
                    // takeoff animation finished, so time to leave the ground!
                    SetBaseAnimation(AnimationID.JumpLoop, true);
                    // apply a jump acceleration to the character
                    this.verticalVelocity = this.JumpAcceleration;
                }
            } //end if
            else if (this.baseAnimID == AnimationID.JumpEnd)
            {
                if (this.timer >= this.anims[(int)this.baseAnimID].Length)
                {
                    // safely landed, so go back to running or idling
                    if (this.keyDirection == Vector3.Zero)
                    {
                        SetBaseAnimation(AnimationID.IdleBase);
                        SetTopAnimation(AnimationID.IdleTop);
                    }
                    else
                    {
                        SetBaseAnimation(AnimationID.RunBase, true);
                        SetTopAnimation(AnimationID.RunTop, true);
                    }
                }
            }

            // increment the current base and top animation times
            if (this.baseAnimID != AnimationID.None)
            {
                this.anims[(int)this.baseAnimID].AddTime(deltaTime * baseAnimSpeed);
            }
            if (this.topAnimID != AnimationID.None)
            {
                this.anims[(int)this.topAnimID].AddTime(deltaTime * topAnimSpeed);
            }

            // apply smooth transitioning between our animations
            FadeAnimations(deltaTime);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deltaTime"></param>
        private void FadeAnimations(Real deltaTime)
        {
            for (int i = 0; i < NumAnims; i++)
            {
                if (this.fadingIn[i])
                {
                    // slowly fade this animation in until it has full weight
                    Real newWeight = this.anims[i].Weight + deltaTime * this.AnimFadeSpeed;
                    this.anims[i].Weight = Utility.Clamp<Real>(newWeight, 1, 0);
                    if (newWeight >= 1)
                    {
                        this.fadingIn[i] = false;
                    }
                }
                else if (this.fadingOut[i])
                {
                    // slowly fade this animation out until it has no weight, and then disable it
                    Real newWeight = this.anims[i].Weight - deltaTime * this.AnimFadeSpeed;
                    this.anims[i].Weight = Utility.Clamp<Real>(newWeight, 1, 0);
                    if (newWeight <= 0)
                    {
                        this.anims[i].IsEnabled = false;
                        this.fadingOut[i] = false;
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deltaTime"></param>
        private void UpdateCamera(Real deltaTime)
        {
            // place the camera pivot roughly at the character's shoulder
            this.cameraPivot.Position = this.bodyNode.Position + Vector3.UnitY * CamHeight;
            // move the camera smoothly to the goal
            Vector3 goalOffset = this.cameraGoal.DerivedPosition - this.cameraNode.Position;
            this.cameraNode.Translate(goalOffset * deltaTime * 9.0f);
            // always look at the pivot
            this.cameraNode.LookAt(this.cameraPivot.DerivedPosition, TransformSpace.World);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deltaYaw"></param>
        /// <param name="deltaPitch"></param>
        /// <param name="deltaZoom"></param>
        private void UpdateCameraGoal(Real deltaYaw, Real deltaPitch, Real deltaZoom)
        {
            this.cameraPivot.Yaw(deltaYaw, TransformSpace.World);

            // bound the pitch
            if (!(this.pivotPitch + deltaPitch > 25 && deltaPitch > 0) &&
                 !(this.pivotPitch + deltaPitch < -60 && deltaPitch < 0))
            {
                this.cameraPivot.Pitch(deltaPitch, TransformSpace.Local);
                this.pivotPitch += deltaPitch;
            }

            Real dist = this.cameraGoal.DerivedPosition.Distance(this.cameraPivot.DerivedPosition);
            Real distChange = deltaZoom * dist;

            // bound the zoom
            if (!(dist + distChange < 8 && distChange < 0) && !(dist + distChange > 25 && distChange > 0))
            {
                this.cameraGoal.Translate(new Vector3(0, 0, distChange), TransformSpace.Local);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        private void SetBaseAnimation(AnimationID id)
        {
            SetBaseAnimation(id, false);
        }

        /// <summary>
        /// /
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reset"></param>
        private void SetBaseAnimation(AnimationID id, bool reset)
        {
            if ((int)this.baseAnimID >= 0 && (int)this.baseAnimID < NumAnims)
            {
                // if we have an old animation, fade it out
                this.fadingIn[(int)this.baseAnimID] = false;
                this.fadingOut[(int)this.baseAnimID] = true;
            }

            this.baseAnimID = id;

            if (id != AnimationID.None)
            {
                // if we have a new animation, enable it and fade it in
                this.anims[(int)id].IsEnabled = true;
                this.anims[(int)id].Weight = 0;
                this.fadingOut[(int)id] = false;
                this.fadingIn[(int)id] = true;
                if (reset)
                {
                    this.anims[(int)id].Time = 0;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        private void SetTopAnimation(AnimationID id)
        {
            SetTopAnimation(id, false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reset"></param>
        private void SetTopAnimation(AnimationID id, bool reset)
        {
            if ((int)this.topAnimID >= 0 && (int)this.topAnimID < NumAnims)
            {
                // if we have an old animation, fade it out
                this.fadingIn[(int)this.topAnimID] = false;
                this.fadingOut[(int)this.topAnimID] = true;
            }

            this.topAnimID = id;

            if (id != AnimationID.None)
            {
                // if we have a new animation, enable it and fade it in
                this.anims[(int)id].IsEnabled = true;
                this.anims[(int)id].Weight = 0;
                this.fadingOut[(int)id] = false;
                this.fadingIn[(int)id] = true;
                if (reset)
                {
                    this.anims[(int)id].Time = 0;
                }
            }
        }
    }
}