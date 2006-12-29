using System;
using System.Collections;
using System.Collections.Specialized;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using System.Reflection;
using System.Reflection.Emit;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math.Collections;
using Chronos.Core;
using OutlookBar;

namespace LightPlugin
{
	/// <summary>
	/// Summary description for LightPlugin.
	/// </summary>
	public class LightPlugin : IEditorPlugin, IXmlWriterPlugin, IMovableObjectPlugin
	{
		private int lightCount = 0;

        #region Singleton Implementation

        private static LightPlugin _Instance;

        /// <summary>
        /// The private constructor is called from PluginManager.
        /// </summary>
        private LightPlugin() 
        {
            _Instance = this;
        }

        /// <summary>
        /// Instance allows access to the Root, SceneManager, and SceneGraph from
        /// within the plugin.
        /// </summary>
        public static LightPlugin Instance {
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
			OutlookBarCategory cat = GuiManager.Instance.CreateToolbox("Lights");
			OutlookBarButton b;

			b = GuiManager.Instance.AddButton(this, cat, "Directional Light", 0, "LightPlugin.Images.direct.png");
			b.ButtonDoubleClicked += new OutlookBar.OutlookBarButton.ButtonDoubleClickedEventHandler(b_ButtonDoubleClicked);
			
			b = GuiManager.Instance.AddButton(this, cat, "Point Light", 1, "LightPlugin.Images.point.png");
			b.ButtonDoubleClicked += new OutlookBar.OutlookBarButton.ButtonDoubleClickedEventHandler(b_ButtonDoubleClicked);

			b = GuiManager.Instance.AddButton(this, cat, "Spotlight", 2, "LightPlugin.Images.spot.png");
			b.ButtonDoubleClicked += new OutlookBar.OutlookBarButton.ButtonDoubleClickedEventHandler(b_ButtonDoubleClicked);
		}

		public void Stop() {}

		public StringCollection XmlElementHandlers 
		{ 
			get 
			{ 
				StringCollection c = new StringCollection();
				c.AddRange(new string[] { "Light" });
				return c;
			}
		}
        #endregion

        #region IXmlWriterPlugin members

		public bool Serialize(object o, XmlElement elem) 
		{
            if(o == null)
                throw new ArgumentNullException("o");
            
            if(elem == null)
                throw new ArgumentNullException("elem");

			if(o is Light) {
                WriteLightToXml(o as Light, elem);
				return true;
			}

            return false;
		}

        #endregion

        #region WriteToXml Methods

        public void WriteLightToXml(Light l, XmlElement elem)
        {
		    elem.SetAttribute("handler", "Light");

		    elem.SetAttribute("AttenuationConstant", l.AttenuationConstant.ToString());
		    elem.SetAttribute("AttenuationLinear", l.AttenuationLinear.ToString());
		    elem.SetAttribute("AttenuationQuadratic", l.AttenuationQuadratic.ToString());
		    elem.SetAttribute("AttenuationRange", l.AttenuationRange.ToString());
		    elem.SetAttribute("CastShadows", l.CastShadows.ToString());
		    elem.SetAttribute("Diffuse", String.Format("{0},{1},{2}",l.Diffuse.r,l.Diffuse.g,l.Diffuse.b,l.Diffuse.a));
		    elem.SetAttribute("Direction", String.Format("{0},{1},{2}", l.Direction.x, l.Direction.y, l.Direction.z));
		    elem.SetAttribute("Visible", l.IsVisible.ToString());
		    elem.SetAttribute("Specular", String.Format("{0},{1},{2}",l.Specular.r,l.Specular.g,l.Specular.b,l.Specular.a));
		    elem.SetAttribute("SpotlightFalloff", l.SpotlightFalloff.ToString());
		    elem.SetAttribute("SpotlightInnerAngle", l.SpotlightInnerAngle.ToString());
		    elem.SetAttribute("SpotlightOuterAngle", l.SpotlightOuterAngle.ToString());
		    elem.SetAttribute("Type", l.Type.ToString());
        }

        #endregion

		#region IMovableObjectPlugin Members

		public IPropertiesWrapper GetPropertiesObject(MovableObject obj)
		{
			if(obj is Light)
				return new LightWrapper(obj as Light);
			return null;
		}

		public IManipulator GetManipulator(Axiom.Core.MovableObject obj) {
			return new Chronos.Core.DefaultManipulator();
		}

		#endregion

		private string getLightName(LightType type) {
			return "light" + ++lightCount;
		}

		private void b_ButtonDoubleClicked(object sender, object tag) {
			Axiom.Core.SceneManager scene = Root.Instance.SceneManager;
			SceneGraph world = SceneGraph.Instance;

			string name;
			LightType lt = LightType.Directional;
			if((int)tag == 1)
				lt = LightType.Point;
			else if((int)tag == 2)
				lt = LightType.Spotlight;
			name = getLightName(lt);
			Light l = scene.CreateLight(name);
			l.Type = lt;

			EditorNode node;
			// TODO: Add non-root light adding
			node = EditorSceneManager.Instance.ActiveScene.AddObject(
				name, l, LightPlugin.Instance);

			Entity bulb = scene.CreateEntity(name + "_mesh", "Editor/bulb.mesh");
			bulb.CastShadows = false;
			node.AttachObject(bulb);
		}
	}
}
