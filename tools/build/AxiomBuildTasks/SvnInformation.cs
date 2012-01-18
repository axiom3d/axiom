using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axiom.Build.Tasks
{
	class SvnInformation
	{
		public long Revision
		{
			get
			{
				return MaxRevision;
			}
		}

		public long CommitRevision
		{
			get;
			set;
		}

		public DateTime Now
		{
			get;
			set;
		}

		public string Url
		{
			get;
			set;
		}

		public bool IsModified
		{
			get;
			set;
		}

		public bool MixedRevisions
		{
			get;
			set;
		}

		public long MinRevision
		{
			get;
			set;
		}

		public long MaxRevision
		{
			get;
			set;
		}

		public string RootUrl
		{
			get;
			set;
		}
		public string RevisionRange
		{
			get;
			set;
		}

		public bool IsSvnItem
		{
			get;
			set;
		}

		public bool HasModifications
		{
			get;
			set;
		}
	}
}
