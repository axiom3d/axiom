using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SharpSvn;

namespace Axiom.Build.Tasks
{
	public class SvnAssemblyInfo : Task
	{
		// Fields
		private ITaskItem[] assemblyInfoFileItems;

		// Methods
		public override bool Execute()
		{
			try
			{
				Log.LogMessage( "Transforming AssemblyInfo files." );
				IEnumerable<AssemblyInfoFile> releaseFiles = this.GetAssemblyInfoFiles();

				//using SharpSvn;
				SvnInformation svn = new SvnInformation();

				GatherSvnInformation( svn );
				foreach ( var file in releaseFiles )
				{
					var fullText = File.ReadAllText( file.Template, Encoding.ASCII );
					fullText = TokenFormatter.FormatToken( fullText, svn );
					File.WriteAllText( file.OutputFile, fullText, Encoding.ASCII );
				}
				return true;
			}
			catch ( Exception ex )
			{
				Log.LogError( "Unable to complete SvnAssemblyInfo Task. [{0}]: {1}\n{2}", new object[] { ex.GetType(), ex.Message, ex.StackTrace } );
				return false;
			}
		}

		private void GatherSvnInformation( SvnInformation svn )
		{
			Log.LogMessage( "Gathering SVN details from " + WorkingCopy );
			using ( var client = new SvnClient() )
			{
				var arg = new SvnStatusArgs()
				{
					Revision = new SvnRevision( SvnRevisionType.Working ),
					Depth = SvnDepth.Empty
				};
				client.Info( WorkingCopy, ( sender, e ) =>
				{
					svn.Now = DateTime.Now;
					if ( String.IsNullOrEmpty( svn.Url ) )
						svn.Url = e.Uri.AbsoluteUri;
					svn.CommitRevision = e.Revision;
				} );
				Collection<SvnStatusEventArgs> statuses;
				arg.Depth = SvnDepth.Infinity;
				arg.RetrieveAllEntries = true;
				client.GetStatus( WorkingCopy, arg, out statuses );
				foreach ( var item in statuses )
				{
					if ( string.IsNullOrEmpty( svn.RootUrl ) )
						svn.RootUrl = item.RepositoryRoot.AbsoluteUri;
					svn.MinRevision = item.Revision > 0 && ( svn.MinRevision > item.Revision || svn.MinRevision == 0 ) ? item.Revision : svn.MinRevision;
					svn.MaxRevision = item.Revision > 0 && item.Revision > svn.MaxRevision ? item.Revision : svn.MaxRevision;
					svn.IsSvnItem = false;
					switch ( item.LocalNodeStatus )
					{
						case SvnStatus.None:
						case SvnStatus.NotVersioned:
						case SvnStatus.Ignored:
							break;
						case SvnStatus.External:
						case SvnStatus.Incomplete:
						case SvnStatus.Normal:
							svn.IsSvnItem = true;
							break;
						default:
							svn.IsSvnItem = true;
							svn.HasModifications = true;
							break;
					}
					switch ( item.LocalPropertyStatus )
					{
						case SvnStatus.None:
						case SvnStatus.NotVersioned:
						case SvnStatus.Ignored:
							break;
						case SvnStatus.External:
						case SvnStatus.Incomplete:
						case SvnStatus.Normal:
							svn.IsSvnItem = true;
							break;
						default:
							svn.IsSvnItem = true;
							svn.HasModifications = true;
							break;
					}
				}

				svn.MixedRevisions = svn.MinRevision != svn.MaxRevision;
				svn.RevisionRange = String.Format( "{0}:{1}", svn.MinRevision, svn.MaxRevision );
			}
		}

		private IEnumerable<AssemblyInfoFile> GetAssemblyInfoFiles()
		{
			List<AssemblyInfoFile> fileList = new List<AssemblyInfoFile>();
			foreach ( ITaskItem item in this.assemblyInfoFileItems )
			{
				string filePath = null;
				try
				{
					if ( item.ItemSpec.Length != 0 )
					{
						filePath = item.GetMetadata( "FullPath" );
						using ( Stream fileStream = File.OpenRead( filePath ) )
						{
							AssemblyInfoFile file = new AssemblyInfoFile()
							{
								Template = item.GetMetadata( "Identity" ),
								OutputFile = item.GetMetadata( "OutputFile" )
							};
							fileList.Add( file );
							Log.LogMessage( "  \"{0}\" -> \"{1}\"", new object[] { file.Template, file.OutputFile ?? Path.GetFileName( file.Template ) } );
						}
					}
				}
				catch ( ArgumentException )
				{
					throw;
				}
				catch ( Exception )
				{
					if ( filePath != null )
					{
						Log.LogError( "Unable to open file: {0}", new object[] { filePath } );
					}
				}
			}
			return fileList;
		}

		[Required]
		public ITaskItem[] AssemblyInfoFiles
		{
			get
			{
				return this.assemblyInfoFileItems;
			}
			set
			{
				this.assemblyInfoFileItems = value;
			}
		}

		[Required]
		public string WorkingCopy
		{
			get;
			set;
		}
	}
}
