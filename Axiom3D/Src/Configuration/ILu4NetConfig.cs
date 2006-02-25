using System;
namespace DotNet3D.Configuration
{
	public interface ILu4NetConfig : IConfig
	{
		FileRequirement[] RequiredFiles { get; set; }
	}
}
