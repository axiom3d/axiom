using System.Collections.Generic;

using ShaderControlsContainer = System.Collections.Generic.List<Axiom.Samples.Ocean.ShaderControl>;

using Axiom.Utilities;
using Axiom.Core;

namespace Axiom.Samples.Ocean
{
	/// <summary>
	/// 
	/// </summary>
	public class MaterialControls
	{
		protected string displayName;
		protected string materialName;
		protected ShaderControlsContainer shaderControlsContainer = new ShaderControlsContainer();

		/// <summary>
		/// 
		/// </summary>
		public string DisplayName
		{
			get
			{
				return displayName;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public string MaterialName
		{
			get
			{
				return materialName;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public int ShaderControlsCount
		{
			get
			{
				return shaderControlsContainer.Count;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="displayName"></param>
		/// <param name="materialName"></param>
		public MaterialControls( string displayName, string materialName )
		{
			this.displayName = displayName;
			this.materialName = materialName;
		}

		/// <summary>
		/// Add a new control by passing a string parameter
		/// params is a string using the following format:<para></para>
		/// 'Control Name', 'Shader parameter name', 'Parameter Type', 'Min Val', 'Max Val', 'Parameter Sub Index'<para></para>
		/// 'Control Name' = is the string displayed for the control name on screen<para></para>
		/// 'Shader parameter name' = is the name of the variable in the shader<para></para>
		/// 'Parameter Type' = can be GPU_VERTEX, GPU_FRAGMENT<para></para>
		/// 'Min Val' = minimum value that parameter can be<para></para>
		/// 'Max Val' = maximum value that parameter can be<para></para>
		/// 'Parameter Sub Index' = index into the the float array of the parameter.  All GPU parameters are assumed to be float[4].<para></para>
		/// </summary>
		public void AddControl( string parameters )
		{
			// params is a string containing using the following format:
			//  "<Control Name>, <Shader parameter name>, <Parameter Type>, <Min Val>, <Max Val>, <Parameter Sub Index>"

			// break up long string into components
			string[] lineParams = parameters.Split( ',' );

			// if there are not five elements then log error and move on
			if ( lineParams.Length != 6 )
			{
				LogManager.Instance.Write( "Incorrect number of parameters passed in params string for MaterialControls.AddControl()" );
				return;
			}

			try
			{
				ShaderControl newControl = new ShaderControl();
				newControl.Name = lineParams[ 0 ].Trim();
				newControl.ParamName = lineParams[ 1 ].Trim();
				newControl.Type = lineParams[ 2 ].Trim() == "GPU_VERTEX" ? ShaderType.GpuVertex : ShaderType.GpuFragment;
				newControl.MinVal = float.Parse( lineParams[ 3 ].Trim() );
				newControl.MaxVal = float.Parse( lineParams[ 4 ].Trim() );
				newControl.ElementIndex = int.Parse( lineParams[ 5 ].Trim() );
				shaderControlsContainer.Add( newControl );
			}
			catch
			{
				LogManager.Instance.Write( "Error while parsing control params in MaterialControls.AddControl()" );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public ShaderControl GetShaderControl( int index )
		{
			Contract.Requires( index < ShaderControlsCount );
			return shaderControlsContainer[ index ];
		}
	}
}
