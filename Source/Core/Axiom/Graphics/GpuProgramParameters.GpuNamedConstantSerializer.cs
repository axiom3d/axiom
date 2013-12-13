using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
	//using System.Security.AccessControl;
using System.Text;
using Axiom.Serialization;

namespace Axiom.Graphics
{
	public partial class GpuProgramParameters
	{
		/// <summary>
		/// Simple class for loading / saving GpuNamedConstants
		/// </summary>
		public class GpuNamedConstantsSerializer : Serializer
		{
			[OgreVersion( 1, 7, 2790 )]
			public GpuNamedConstantsSerializer()
			{
				version = "[v1.0]";
			}

			[OgreVersion( 1, 7, 2790 )]
			public void ExportNamedConstants( GpuNamedConstants pConsts, string filename )
			{
				ExportNamedConstants( pConsts, filename, Endian.Native );
			}

			[OgreVersion( 1, 7, 2790 )]
			public void ExportNamedConstants( GpuNamedConstants pConsts, string filename, Endian endianMode )
			{
				using ( var f = new FileStream( filename, FileMode.CreateNew, FileAccess.Write ) )
				{
					ExportNamedConstants( pConsts, f, endianMode );
				}
			}

			private void ExportNamedConstants( GpuNamedConstants pConsts, Stream stream, Endian endianMode )
			{
				using ( var w = new BinaryWriter( stream ) )
				{
				}

				throw new NotImplementedException();
			}


			public void ImportNamedConstants( Stream stream, GpuNamedConstants pDest )
			{
				throw new NotImplementedException();
			}
		}
	}
}