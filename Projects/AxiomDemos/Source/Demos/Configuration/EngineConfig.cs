namespace Axiom.Demos.Configuration
{
	public partial class EngineConfig
	{
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
	}
}
