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
	public class SvnInfo : Task
	{
		// Methods
		public override bool Execute()
		{
			try
			{
				//using SharpSvn;
				SvnInformation svn = new SvnInformation();

			    Log.LogMessage( "Gathering SVN details from " + WorkingCopy );
				GatherSvnInformation( svn );
				SvnRepository = svn.RootUrl;
				SvnRevision = svn.Revision;
				return true;
			}
			catch ( Exception ex )
			{
				Log.LogError( "Unable to complete SvnInfo Task. [{0}]: {1}\n{2}", new object[] { ex.GetType(), ex.Message, ex.StackTrace } );
				return false;
			}
		}

		private void GatherSvnInformation( SvnInformation svn )
		{
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
					svn.MinRevision = item.Revision > 0 && ( item.Revision < svn.MinRevision || svn.MinRevision == 0 ) ? item.Revision : svn.MinRevision;
					svn.MaxRevision = item.Revision > 0 && ( item.Revision > svn.MaxRevision || svn.MaxRevision == 0 ) ? item.Revision : svn.MaxRevision;
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

		[Output]
		public string SvnRepository { get; set; }
		
		[Output]
		public long SvnRevision { get; set; }
		
		[Required]
		public string WorkingCopy
		{
			get;
			set;
		}
	}
}
