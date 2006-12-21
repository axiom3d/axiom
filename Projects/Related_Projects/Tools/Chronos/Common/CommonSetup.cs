using System;
using Chronos.Core;

namespace Chronos.Core
{
	/// <summary>
	/// Summary description for CommonSetup.
	/// </summary>
	public class CommonSetup
	{
		public static void Setup()
		{
			SceneGraph.Init();
			EditorSceneManager.Init();
			EditorResourceManager.Init();
			ResourceManagerForm.Init();
			//ProjectBrowser.Init();
			SceneGraph.Instance.Setup();
			GuiManager.Instance.CreateDockingWindow(SceneGraph.Instance);
			GuiManager.Instance.CreateDockingWindow(SceneGraph.Instance.propform);
			//GuiManager.Instance.CreateDockingWindow(ProjectBrowser.Instance);
		}
	}
}
