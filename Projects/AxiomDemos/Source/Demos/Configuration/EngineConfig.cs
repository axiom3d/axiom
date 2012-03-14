namespace Axiom.Demos.Configuration
{
	public partial class EngineConfig
	{
		#region Nested type: ConfigOptionDataTable

		public partial class ConfigOptionDataTable
		{
			public ConfigOptionRow FindByNameRenderSystem( string Name, string RenderSystem )
			{
				foreach ( ConfigOptionRow row in Rows )
				{
					if ( row.Name == Name && row.RenderSystem == RenderSystem )
					{
						return row;
					}
				}
				return null;
			}
		}

		#endregion
	}
}
