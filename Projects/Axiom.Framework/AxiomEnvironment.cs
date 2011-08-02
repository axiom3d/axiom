#region MIT/X11 License
//Copyright © 2003-2011 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.
#endregion License

#region Namespace Declarations
using System;
using System.Linq;
using Axiom.Core;
using Axiom.Framework.Configuration;
using Axiom.Graphics;
using Vector3 = Axiom.Math.Vector3;
using Axiom.Animating;

#endregion Namespace Declarations


namespace Axiom.Framework
{
	public class AxiomEnvironment
	{
		private Root _root;
		private LogManager _logManager;
		private Log _log;
		public Log Log
		{
			get
			{
				return _log;
			}
		}

		private ResourceGroupManager _resourceGroupManager;
		private MeshManager _meshManager;
		private LodStrategyManager _lodStrategyMgr;
		private MaterialManager _materialMgr;
		private SkeletonManager _skeletonMgr;
		//StatefulMeshSerializer _meshSerializer;
		//public StatefulMeshSerializer MeshSerializer { get { return _meshSerializer; } }
		//StatefulSkeletonSerializer _skeletonSerializer;
		//public StatefulSkeletonSerializer SkeletonSerializer { get { return _skeletonSerializer; } }
		private DefaultHardwareBufferManager _bufferManager;
		private bool _isStandalone;
		public bool IsStandalone
		{
			get
			{
				return _isStandalone;
			}
		}

		public AxiomEnvironment()
		{
		}

		~AxiomEnvironment()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="standalone"></param>
		/// <param name="log"></param>
		public void Initialize( bool standalone = true, Log log = null )
		{
			if ( standalone )
			{
				this._logManager = LogManager.Instance;
				this._log = this._logManager.CreateLog( "meshmagick.log", true, false );
				//this._root = new Root();
				this._resourceGroupManager = new ResourceGroupManager();
				this._meshManager = new MeshManager();
				this._meshManager.BoundsPaddingFactor = 0.0f;
				this._lodStrategyMgr = LodStrategyManager.Instance;
				this._materialMgr = new MaterialManager();
				this._materialMgr.Initialize();
				this._skeletonMgr = new SkeletonManager();
				this._bufferManager = new DefaultHardwareBufferManager();
				this._isStandalone = true;
			}
			else
			{
				this._log = log;
				this._isStandalone = false;
			}

			//this._meshSerializer = new StatefulMeshSerializer();
			//this._skeletonSerializer = new StatefulSkeletonSerializer();

		}

	}
}
