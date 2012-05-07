using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using GLenum = OpenTK.Graphics.ES20.All;

namespace Axiom.RenderSystems.OpenGLES2.GLSLES
{
	internal class GLSLESLinkProgramManager : GLSLESProgramManagerCommon
	{
		private Dictionary<long, GLSLESLinkProgram> linkPrograms;

		private GLSLESLinkProgram activeLinkProgram;
		private Dictionary<string, GLenum> typeEnumMap;
		private static GLSLESLinkProgramManager _instance = null;

		public GLSLESLinkProgramManager()
		{
			this.activeLinkProgram = null;
		}

		~GLSLESLinkProgramManager()
		{
			foreach ( var key in this.linkPrograms.Keys )
			{
				this.linkPrograms[ key ] = null;
			}
		}

		public GLSLESLinkProgram ActiveLinkProgram
		{
			get
			{
				if ( this.activeLinkProgram != null )
				{
					return this.activeLinkProgram;
				}

				long activeKey = 0;

				if ( activeVertexGpuProgram != null )
				{
					activeKey = activeVertexGpuProgram.ProgramID << 32;
				}
				if ( activeFragmentGpuProgram != null )
				{
					activeKey += activeFragmentGpuProgram.ProgramID;
				}

				//Only return a link program object if a vertex or fragment program exist
				if ( activeKey > 0 )
				{
					if ( !this.linkPrograms.ContainsKey( activeKey ) )
					{
						this.activeLinkProgram = new GLSLESLinkProgram( activeVertexGpuProgram, activeFragmentGpuProgram );
						this.linkPrograms.Add( activeKey, this.activeLinkProgram );
					}
					else
					{
						this.activeLinkProgram = this.linkPrograms[ activeKey ];
					}
				}
				//Make the program object active
				if ( this.activeLinkProgram != null )
				{
					this.activeLinkProgram.Activate();
				}

				return this.activeLinkProgram;
			}
		}

		public GLSLESGpuProgram ActiveFragmentShader
		{
			set
			{
				if ( value != activeFragmentGpuProgram )
				{
					activeFragmentGpuProgram = value;
					this.activeLinkProgram = null;
				}
			}
		}

		public GLSLESGpuProgram ActiveVertexShader
		{
			set
			{
				if ( value != activeVertexGpuProgram )
				{
					activeVertexGpuProgram = value;
					this.activeLinkProgram = null;
				}
			}
		}

		public static GLSLESLinkProgramManager Instance
		{
			get
			{
				if ( _instance == null )
				{
					_instance = new GLSLESLinkProgramManager();
				}
				return _instance;
			}
		}
	}
}
