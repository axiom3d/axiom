using System.Collections.Generic;
using Axiom.Graphics;
using Axiom.Scripting.Compiler;
using Axiom.Scripting.Compiler.AST;
using Axiom.Serialization;

namespace Axiom.Components.RTShaderSystem
{
	public abstract class SubRenderState : RenderState
	{
		public SubRenderState()
		{
		}

		public abstract string Type { get; }

		public abstract int ExecutionOrder { get; }

		public virtual void CopyFrom( SubRenderState other )
		{
		}

		internal virtual bool CreateCpuSubPrograms( ProgramSet programSet )
		{
			bool result;

			//resolve params
			result = ResolveParameters( programSet );
			if ( result == false )
			{
				return false;
			}

			//resolve dependencis
			result = ResolveDependencies( programSet );
			if ( result == false )
			{
				return false;
			}

			//add fuction invocations
			result = AddFunctionInvocations( programSet );
			if ( result == false )
			{
				return false;
			}

			return true;
		}

		public virtual void UpdateGpuProgramsParams( Graphics.IRenderable rend, Graphics.Pass pass,
		                                             Graphics.AutoParamDataSource source,
		                                             Core.Collections.LightList lightList )
		{
		}

		public virtual bool PreAddToRenderState( TargetRenderState targetRenderState, Graphics.Pass srcPass,
		                                         Graphics.Pass dstPass )
		{
			return true;
		}

		protected virtual bool ResolveParameters( ProgramSet programSet )
		{
			return true;
		}

		protected virtual bool ResolveDependencies( ProgramSet programSet )
		{
			return true;
		}

		protected virtual bool AddFunctionInvocations( ProgramSet programSet )
		{
			return true;
		}
	}

	public abstract class SubRenderStateFactory
	{
		protected List<SubRenderState> subRenderStateList = new List<SubRenderState>();
		public abstract string Type { get; }

		public virtual SubRenderState CreateInstance()
		{
			var subRenderState = CreateInstanceImpl();
			this.subRenderStateList.Add( subRenderState );
			return subRenderState;
		}


		public virtual SubRenderState CreateInstance( ScriptCompiler compiler, PropertyAbstractNode prop, Pass pass,
		                                              ScriptTranslator stranslator )
		{
			return null;
		}

		public virtual SubRenderState CreateInstance( ScriptCompiler compiler, PropertyAbstractNode prop,
		                                              TextureUnitState texState, ScriptTranslator translator )
		{
			return null;
		}

		internal virtual SubRenderState CreateOrRetrieveInstance( ScriptTranslator translator )
		{
			//check if we already creaet a srs
			SubRenderState subRenderState = translator.GetGeneratedSubRenderState( Type );
			if ( subRenderState == null )
			{
				//create a new sub render state
				subRenderState = CreateInstance();
			}
			return subRenderState;
		}

		public virtual void DestroyInstance( SubRenderState subRenderState )
		{
			for ( int i = 0; i < this.subRenderStateList.Count; i++ )
			{
				SubRenderState it = this.subRenderStateList[ i ];

				if ( it == subRenderState )
				{
					it.Dispose();
					this.subRenderStateList.Remove( it );
					break;
				}
			}
		}

		public virtual void DestroyAllInstances()
		{
			for ( int i = 0; i < this.subRenderStateList.Count; i++ )
			{
				this.subRenderStateList[ i ].Dispose();
				this.subRenderStateList[ i ] = null;
			}
			this.subRenderStateList.Clear();
		}

		public virtual void WriteInstance( MaterialSerializer ser, SubRenderState subRenderState, Pass srcPass,
		                                   Pass dstPass )
		{
		}

		public virtual void WriteInstance( MaterialSerializer ser, SubRenderState subRenderState,
		                                   TextureUnitState srcTextureUnit, TextureUnitState dstTextureUnit )
		{
		}

		protected abstract SubRenderState CreateInstanceImpl();
	}
}