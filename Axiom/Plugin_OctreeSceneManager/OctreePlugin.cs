using System; 
using Axiom.Core; 

namespace Axiom.SceneManagers.Octree {
	public class OctreePlugin : IPlugin { 
		public void Start() { 
			Engine.Instance.SceneManagers[SceneType.Generic] = new OctreeSceneManager(); 
		} 

		public void Stop() { 
		} 
	} 
}
 
 
 
