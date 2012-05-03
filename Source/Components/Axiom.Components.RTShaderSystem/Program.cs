using System;
using System.Collections.Generic;
using Axiom.Graphics;
using Axiom.Math;

namespace Axiom.Components.RTShaderSystem
{
	public class Program : IDisposable
	{
		private readonly GpuProgramType type;
		private readonly List<UniformParameter> parameters;
		private readonly List<Function> functions;
		private readonly List<string> dependencies;

		public Program( GpuProgramType type )
		{
			this.type = type;
			EntryPointFunction = null;
			SkeletalAnimationIncluded = false;

			functions = new List<Function>();
			dependencies = new List<string>();
			parameters = new List<UniformParameter>();
		}

		/// <summary>
		///   Resolve uniform auto constant parameter with associated real data of this program
		/// </summary>
		/// <param name="autoType"> The auto type of the desired parameter </param>
		/// <param name="data"> The data to associate with the auto parameter </param>
		/// <param name="size"> number of elements in the parameter </param>
		/// <returns> parameter instance in case of that resolve operation succeeded, otherwise null </returns>
		public UniformParameter ResolveAutoParameterReal( GpuProgramParameters.AutoConstantType autoType, Real data,
		                                                  int size )
		{
			UniformParameter param = null;

			//check if parameter already exists.
			param = GetParameterByAutoType( autoType );
			if ( param != null )
			{
				if ( param.IsAutoConstantRealParameter && param.AutoConstantRealData == data )
				{
					param.Size = Axiom.Math.Utility.Max( size, param.Size );
					return param;
				}
			}

			//Create new parameter
			param = new UniformParameter( autoType, data, size );
			AddParameter( param );

			return param;
		}

		/// <summary>
		///   Resolve uniform auto constant parameter with associated real data of this program
		/// </summary>
		/// <param name="autoType"> The auto type of the desired parameter </param>
		/// <param name="type"> The desried data type of the auto parameter </param>
		/// <param name="data"> The data to associate with the auto parameter </param>
		/// <param name="size"> number of elements in the parameter </param>
		/// <returns> parameter instance in case of that resolve operation succeeded, otherwise null </returns>
		public UniformParameter ResolveAutoParameterReal( GpuProgramParameters.AutoConstantType autoType,
		                                                  Axiom.Graphics.GpuProgramParameters.GpuConstantType type,
		                                                  Real data, int size )
		{
			UniformParameter param = null;

			//check if parameter already exists
			param = GetParameterByAutoType( autoType );
			if ( param != null )
			{
				if ( param.IsAutoConstantRealParameter && param.AutoConstantRealData == data )
				{
					param.Size = Axiom.Math.Utility.Max( size, param.Size );
					return param;
				}
			}

			//Create new parameter
			param = new UniformParameter( autoType, data, size, type );
			AddParameter( param );

			return param;
		}

		public UniformParameter ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType autoType, int data )
		{
			return ResolveAutoParameterInt( autoType, data, 0 );
		}

		/// <summary>
		///   Resolve uniform auto constant parameter with associated int data of this program
		/// </summary>
		/// <param name="autoType"> The auto type of the desried parameter </param>
		/// <param name="data"> The data to associate with the auto parameter </param>
		/// <param name="size"> number of elements in the parameter </param>
		/// <returns> </returns>
		public UniformParameter ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType autoType, int data,
		                                                 int size )
		{
			UniformParameter param = null;

			//check if parameter already exits.
			param = GetParameterByAutoType( autoType );
			if ( param != null )
			{
				if ( param.IsAutoConstantIntParameter && param.AutoConstantIntData == data )
				{
					param.Size = Axiom.Math.Utility.Max( size, param.Size );
					return param;
				}
			}
			//Create new parameter
			param = new UniformParameter( autoType, data, size );
			AddParameter( param );

			return param;
		}

		/// <summary>
		///   Resolve uniform auto constant parameter with associated int data of this program
		/// </summary>
		/// <param name="autoType"> The auto type of the desried parameter </param>
		/// <param name="type"> The desired data type of the auto parameter </param>
		/// <param name="data"> The data to associate with the auto parameter </param>
		/// <param name="size"> number of elements in the parameter </param>
		/// <returns> </returns>
		public UniformParameter ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType autoType,
		                                                 Axiom.Graphics.GpuProgramParameters.GpuConstantType type,
		                                                 int data, int size )
		{
			UniformParameter param;

			//check if parameter already exists.
			param = GetParameterByAutoType( autoType );
			if ( param != null )
			{
				if ( param.IsAutoConstantIntParameter && param.AutoConstantIntData == data )
				{
					param.Size = Axiom.Math.Utility.Max( size, param.Size );
					return param;
				}
			}

			//Create new parameter
			param = new UniformParameter( autoType, data, size, type );
			AddParameter( param );

			return param;
		}

		public UniformParameter ResolveParameter( Axiom.Graphics.GpuProgramParameters.GpuConstantType type, int index,
		                                          GpuProgramParameters.GpuParamVariability variability,
		                                          string suggestedName )
		{
			return ResolveParameter( type, index, variability, suggestedName, 0 );
		}

		/// <summary>
		/// </summary>
		/// <param name="type"> </param>
		/// <param name="index"> </param>
		/// <param name="variability"> default is .All </param>
		/// <param name="suggestedName"> </param>
		/// <param name="size"> Default is 0 </param>
		/// <returns> </returns>
		public UniformParameter ResolveParameter( Axiom.Graphics.GpuProgramParameters.GpuConstantType type, int index,
		                                          GpuProgramParameters.GpuParamVariability variability,
		                                          string suggestedName, int size )
		{
			UniformParameter param = null;

			if ( index == -1 )
			{
				index = 0;

				//find the next availalbe index of the target type
				for ( int i = 0; i < parameters.Count; i++ )
				{
					if ( parameters[ i ].Type == type && parameters[ i ].IsAutoConstantParameter == false )
					{
						index++;
					}
				}
			}
			else
			{
				//Check if parameter already exits
				param = GetParameterByType( type, index );
				if ( param != null )
				{
					return param;
				}
			}
			//Create new parameter.
			param = ParameterFactory.CreateUniform( type, index, (int)variability, suggestedName, size );
			AddParameter( param );

			return param;
		}

		public UniformParameter GetParameterByName( string name )
		{
			foreach ( var param in parameters )
			{
				if ( param.Name == name )
				{
					return param;
				}
			}

			return null;
		}

		public UniformParameter GetParameterByType( Axiom.Graphics.GpuProgramParameters.GpuConstantType type, int index )
		{
			foreach ( var param in parameters )
			{
				if ( param.Type == type && param.Index == index )
				{
					return param;
				}
			}


			return null;
		}

		public UniformParameter GetParameterByAutoType( GpuProgramParameters.AutoConstantType autoType )
		{
			foreach ( var param in parameters )
			{
				if ( param.IsAutoConstantParameter && param.AutoConstantType == autoType )
				{
					return param;
				}
			}
			return null;
		}

		public Function GetFunctionByName( string name )
		{
			foreach ( var func in functions )
			{
				if ( func.Name == name )
				{
					return func;
				}
			}

			return null;
		}

		/// <summary>
		///   Creates a new function in this program.
		/// </summary>
		/// <param name="name"> The name of the function to create </param>
		/// <param name="desc"> The description of the function </param>
		/// <param name="functionType"> </param>
		/// <returns> The newly created function instance </returns>
		public Function CreateFunction( string name, string desc, Function.FunctionType functionType )
		{
			Function shaderFunction = GetFunctionByName( name );

			if ( shaderFunction != null )
			{
				throw new Axiom.Core.AxiomException( "Function " + name + " already declared in program." );
			}

			shaderFunction = new Function( name, desc, functionType );
			functions.Add( shaderFunction );

			return shaderFunction;
		}

		/// <summary>
		///   Add dependency for this program. Basically a filename that will be included in this program and provide predefined shader functions code One should verify that the given library file he provides can be reached by the resource manager This step can be achieved using the ResourceGroupManager.AddResourceLocation method.
		/// </summary>
		/// <param name="libFileName"> </param>
		public void AddDependency( string libFileName )
		{
			for ( int i = 0; i < dependencies.Count; i++ )
			{
				if ( dependencies[ i ] == libFileName )
				{
					return;
				}
			}
			dependencies.Add( libFileName );
		}

		/// <summary>
		///   Get the library name of the given index dependency
		/// </summary>
		/// <param name="index"> </param>
		public string GetDependency( int index )
		{
			return dependencies[ index ];
		}

		private void DestroyParameters()
		{
			parameters.Clear();
		}

		private void DestroyFunctions()
		{
			for ( int i = 0; i < functions.Count; i++ )
			{
				if ( functions[ i ] != null )
				{
					functions[ i ].Dispose();
					functions[ i ] = null;
				}
			}
			functions.Clear();
		}

		private void AddParameter( UniformParameter parameter )
		{
			if ( GetParameterByName( parameter.Name ) != null )
			{
				throw new Axiom.Core.AxiomException( "Parameter <" + parameter.Name + "> already declared in program." );
			}

			parameters.Add( parameter );
		}

		private void RemoveParameter( UniformParameter parameter )
		{
			for ( int i = 0; i < parameters.Count; i++ )
			{
				if ( parameters[ i ] == parameter )
				{
					parameters[ i ] = null;
					parameters.RemoveAt( i );
					break;
				}
			}
		}

		public int DependencyCount
		{
			get
			{
				return dependencies.Count;
			}
		}

		public GpuProgramType Type
		{
			get
			{
				return type;
			}
		}

		public List<Function> Functions
		{
			get
			{
				return functions;
			}
		}

		public bool SkeletalAnimationIncluded { get; set; }

		public List<UniformParameter> Parameters
		{
			get
			{
				return parameters;
			}
		}

		public Function EntryPointFunction { get; set; }


		public void Dispose()
		{
			DestroyParameters();
			DestroyFunctions();
		}
	}
}