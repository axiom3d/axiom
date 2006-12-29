using System;
using System.Collections;
using System.Collections.Specialized;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math.Collections;
using Chronos;
using Chronos.Core;

namespace MeshPlugin
{
	/// <summary>
	/// Summary description for MeshPlugin.
	/// </summary>
	public class MeshPlugin : Chronos.Core.IEditorPlugin, Chronos.Core.IXmlWriterPlugin, Chronos.Core.IMovableObjectPlugin
	{
		public MeshForm meshForm1;
		public AnimationStates aniStates;
		private int meshCount = 0;

        #region Singleton Implementation

        private static MeshPlugin _Instance;

        /// <summary>
        /// The private constructor is called from PluginManager.
        /// </summary>
        private MeshPlugin() 
        {
            _Instance = this;
        }

        /// <summary>
        /// Instance allows access to the Root, SceneManager, and SceneGraph from
        /// within the plugin.
        /// </summary>
        public static MeshPlugin Instance {
            get {
                if(_Instance == null) {
                    string message = "Singleton instance not initialized. Please call the plugin constructor first.";
                    throw new InvalidOperationException(message);
                }
                return _Instance;
            }
        }

        #endregion

        #region EditorPluginBase members
	
		public void Start()
		{
			// aniStates = new AnimationStates();
			// GuiManager.Instance.CreateDockingWindow(aniStates);

			OutlookBar.OutlookBarCategory cat = GuiManager.Instance.CreateToolbox("Meshes");
			StringCollection files = ResourceManager.GetAllCommonNamesLike("", "*.mesh");
			foreach(string file in files) {
				OutlookBar.OutlookBarButton b = GuiManager.Instance.AddButton(this, cat, file, file, "MeshPlugin.mesh.png");
				b.ButtonDoubleClicked +=new OutlookBar.OutlookBarButton.ButtonDoubleClickedEventHandler(b_ButtonDoubleClicked);
			}
		}

		public void Stop() {}

		public StringCollection XmlElementHandlers 
		{ 
			get 
			{ 
				StringCollection c = new StringCollection();
				c.AddRange(new string[] { "Mesh" });
				return c;
			}
		}

		public IManipulator GetManipulator(Axiom.Core.MovableObject obj) {
			return new Chronos.Core.DefaultManipulator();
		}

        #endregion

        #region IXmlWriterPlugin members

		public bool Serialize(object o, XmlElement elem) 
		{
            if(o == null)
                throw new ArgumentNullException("o");
            
            if(elem == null)
                throw new ArgumentNullException("elem");

			if(o is Entity) {
                WriteEntityToXml(o as Entity, elem);
				return true;
			}

            return false;
		}

        #endregion

		#region IMovableObject members

		public IPropertiesWrapper GetPropertiesObject(Axiom.Core.MovableObject obj) 
		{
			if(obj is Entity)
				return new MeshWrapper(obj as Entity);
			return null;
		}

		#endregion

        #region WriteToXml Methods

        public void WriteEntityToXml(Entity o, XmlElement elem)
        {
		    elem.SetAttribute("handler", "Entity");

            elem.SetAttribute("CastShadows", o.CastShadows.ToString());
			elem.SetAttribute("DisplaySkeleton", o.DisplaySkeleton.ToString());
			elem.SetAttribute("Mesh", o.Mesh.Name);
			elem.SetAttribute("Name", o.Name);
			elem.SetAttribute("QueryFlags", o.QueryFlags.ToString());
			elem.SetAttribute("RenderQueueGroup", o.RenderQueueGroup.ToString());
			XmlElement subEntities = elem.OwnerDocument.CreateElement("SubEntities");
			for(int i=0; i<o.SubEntityCount; i++) 
			{
				XmlElement subEntity = elem.OwnerDocument.CreateElement("SubEntity");
				subEntity.SetAttribute("index", i.ToString());
				subEntity.SetAttribute("material",  o.GetSubEntity(i).MaterialName.ToString());
				subEntities.AppendChild(subEntity);
			}
			elem.AppendChild(subEntities);
		}

		#endregion

		private string getEntityName() {
			return "mesh" + (++meshCount).ToString();
		}

		private void addNode(string mesh) {
			string meshName = getEntityName();
			Entity ent = Root.Instance.SceneManager.CreateEntity(
				meshName, mesh);
			EditorNode n;
			n = EditorSceneManager.Instance.ActiveScene.AddObject(
				meshName + " (" + mesh + ")", ent,
				MeshPlugin.Instance);
			//n.SceneNode.AttachObject(ent);
		}

		private void b_ButtonDoubleClicked(object sender, object tag) {
			addNode(tag as string);
		}
	}
}
