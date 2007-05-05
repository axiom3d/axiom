using System;

using Axiom.Core;
using Axiom.Animating;
using Axiom.Collections;

namespace Chess.Main
{
	/// <summary>
	/// Summary description for AnimationBlender.
	/// </summary>
	public class AnimationBlender
	{
		#region enums
		public enum BlendingTransition 
		{ 
			BlendSwitch,         // stop source and start dest 
			BlendWhileAnimating, // cross fade, Blend source animation out while blending destination animation in 
			BlendThenAnimate     // Blend source to first frame of dest, when done, start dest anim 
		}; 
		#endregion

		#region Fields
		private Entity entity; 
		private AnimationState sourceAnimationState; 
		private AnimationState targetAnimationState; 
		private float timeRemaining;
		private float duration; 
		private BlendingTransition blendingTransition; 
		#endregion

		#region constructors
		public AnimationBlender(Entity entity)
		{
			this.entity = entity;
		}
		public AnimationBlender(AnimationBlender copy)
		{
			entity = copy.entity; 
			sourceAnimationState = copy.sourceAnimationState; 
			targetAnimationState = copy.targetAnimationState; 
			timeRemaining = copy.timeRemaining;
			duration = copy.duration; 
			blendingTransition = copy.blendingTransition;    
		}
		#endregion

		#region Properties

		public AnimationState SourceAnimationState
		{
			get{return this.sourceAnimationState;}
		}
		public AnimationState TargetAnimationState
		{
			get{return this.targetAnimationState;}
		}
		#endregion


		#region Methods
		public float GetProgress() 
		{
			return (timeRemaining/duration);
		}   
		public void Delete()
		{

		}
		public void Init (string animation)
		{
			this.Init(animation,false);
		}
		public void Init (string animation, bool loop)
		{
			AnimationStateSet animationStateSet = entity.GetAllAnimationStates();
			foreach ( AnimationState anim in animationStateSet.Values )
			{
				anim.IsEnabled = false; 
				anim.Weight = 0; 
				anim.Time = 0; 
			}

			if (animationStateSet.AllAnimationStates.ContainsKey(animation))
			{
				sourceAnimationState = entity.GetAnimationState( animation ); 
				sourceAnimationState.IsEnabled = (true); 
				sourceAnimationState.Weight =(1); 
				//			sourceAnimationState.setLoop(loop);
				timeRemaining = 0; 
				duration = 1; 
				targetAnimationState = null;   
			}
			else
			{
				//System.Diagnostics.Debug.Assert(false,entity.Name + " does not contain the animationState " + animation);
			}
		}
		public void Blend (string animation, BlendingTransition transition, float duration)
		{
			this.Blend(animation,transition,duration,false);
		}
		public void Blend (string animation, BlendingTransition transition, float duration, bool loop)
		{
			if( transition == BlendingTransition.BlendSwitch ) 
			{ 
				if( sourceAnimationState != null ) 
				{
					sourceAnimationState.Weight = (0f);
					sourceAnimationState.IsEnabled = (false);           
				}
				sourceAnimationState = entity.GetAnimationState( animation ); 
				sourceAnimationState.IsEnabled=(true); 
				sourceAnimationState.Weight =(1f); 
				sourceAnimationState.Time =(0f); 
				//sourceAnimationState.Loop = (loop);
				timeRemaining = 0f; 
			} 
			else 
			{ 
				AnimationState newTarget = entity.GetAnimationState( animation ); 

				if( timeRemaining > 0 ) 
				{ 
					// oops, weren't finished yet 
					if( newTarget == targetAnimationState ) 
					{ 
						// nothing to do! (ignoring duration here) 
					} 
					else if( newTarget == sourceAnimationState ) 
					{ 
						// going back to the source state, so let's switch 
						sourceAnimationState = targetAnimationState; 
						targetAnimationState = newTarget; 
						timeRemaining = duration - timeRemaining; // i'm ignoring the new duration here 
					} 
					else 
					{ 
						// ok, newTarget is really new, so either we simply replace the target with this one, or 
						// we make the target the new source 
						if( timeRemaining < duration * 0.5f ) 
						{ 
							// simply replace the target with this one 
							targetAnimationState.IsEnabled =(false); 
							targetAnimationState.Weight =(0);               
						} 
						else 
						{ 
							// old target becomes new source 
							sourceAnimationState.IsEnabled=(false); 
							sourceAnimationState.Weight=(0); 
							sourceAnimationState = targetAnimationState; 
						} 
						targetAnimationState = newTarget; 
						targetAnimationState.IsEnabled=(true); 
//						targetAnimationState.setLoop(loop);
						targetAnimationState.Weight=( 1.0f - timeRemaining / duration ); 
						targetAnimationState.Time =0f; 
					} 
				} 
				else 
				{                 
					blendingTransition = transition; 
					timeRemaining -= duration; 
					targetAnimationState = newTarget; 
					targetAnimationState.IsEnabled = (true); 
//					targetAnimationState.setLoop(loop);
					targetAnimationState.Weight = (0f); 
					targetAnimationState.Time = (0f);          
				} 
			} 
		}
		public void AddTime (float time)
		{
			if( sourceAnimationState != null ) 
			{ 
				if( timeRemaining > 0 ) 
				{ 
					timeRemaining -= time; 
					if( timeRemaining < 0 ) 
					{ 
						// finish blending 
						sourceAnimationState.IsEnabled = false; 
						sourceAnimationState.Weight = 0; 
						sourceAnimationState = targetAnimationState; 
						sourceAnimationState.IsEnabled = true; 
						sourceAnimationState.Weight =1; 
						targetAnimationState = null; 
					} 
					else 
					{ 
						// still blending, advance weights 
						sourceAnimationState.Weight = ( timeRemaining / duration ); 
						targetAnimationState.Weight = ( 1.0f - timeRemaining / duration ); 

						if( blendingTransition == BlendingTransition.BlendWhileAnimating ) 
						{
							//targetAnimationState.AddTime( time ); 
							targetAnimationState.Time += time;
						}
					} 
				} 
				//sourceAnimationState.AddTime( time ); 
				sourceAnimationState.Time += time;
			} 
		}


		#endregion
	}
}
