using System;
using System.Drawing;
using System.Collections;
using System.IO;
using Crownwood.Magic.Docking;
using TD.SandBar;
using System.Windows.Forms;
using Axiom.Core;

namespace Chronos.Core
{

	public struct DocumentEventArgs {
		public bool Cancel;
	}

	public class DocumentEventHook {
		public delegate void DocumentEventArgsDelegate(object sender, ref DocumentEventArgs e);
		public event DocumentEventArgsDelegate Closing;
		public DocumentEventHook() {}
		public bool FireClose(object sender) {
			if(Closing != null) {
				DocumentEventArgs e = new DocumentEventArgs();
				e.Cancel = false;
				Closing(this, ref e);
				return e.Cancel;
			}
			return true;
		}
	}

	public class ClosableDocument : DocumentManager.Document  {
		// The reason we use a document event hook is so that each plugin author doesn't have
		// to have a reference to the DocumentManager dll in his project.
		public DocumentEventHook docHook = new DocumentEventHook();
		public ClosableDocument(Control c, string text) : base(c, text) {}
		public void Close(object sender) {
			if (!this.docHook.FireClose(sender))
				this.TabStrip.Documents.Remove(this);
		}
	}
	/// <summary>
	/// Summary description for GuiManager.
	/// </summary>

	public class GuiManager
	{
		private ArrayList customToolBars = new ArrayList();
		private DockingManager manager;
		private OutlookBar.OutlookBar toolbox; 
		private DocumentManager.DocumentManager documentManager;
		private ToolBarContainer toolBars;
		private Form mainForm;

		#region Singleton Implementation

		private static GuiManager instance;

		/// <summary>
		/// The private constructor is called from PluginManager.
		/// </summary>
		private GuiManager(Form main, DockingManager manager, OutlookBar.OutlookBar toolbox, DocumentManager.DocumentManager docMan, ToolBarContainer tb) 
		{
			GuiManager.instance = this;
			this.manager = manager;
			this.documentManager = docMan;
			this.toolbox = toolbox;
			this.toolBars = tb;
			this.mainForm = main;
		}

		public static void Init(Form main, DockingManager manager, OutlookBar.OutlookBar toolbox, DocumentManager.DocumentManager docMan, ToolBarContainer tb) 
		{
			if(instance == null) 
			{
				instance = new GuiManager(main, manager, toolbox, docMan, tb);
			}
		}

		public static GuiManager Instance 
		{
			get 
			{
				if(GuiManager.instance == null) 
				{
					string message = "Singleton instance not initialized. Please call the plugin constructor first.";
					throw new InvalidOperationException(message);
				}
				return GuiManager.instance;
			}
		}
		#endregion

		public void CreateDockingWindow(Form f) 
		{
			Content cA;
			WindowContent wc;

			cA = manager.Contents.Add(f, f.Text);
			cA.Icon = f.Icon;
			wc = manager.AddContentWithState(cA, State.DockRight) as WindowContent;
		}

		public DocumentEventHook CreateDocument(Control control, string text) 
		{
			ClosableDocument doc = new ClosableDocument(control, text);
			documentManager.AddDocument(doc);
			documentManager.TabStrips[0].SelectedDocument = doc;
			return doc.docHook;
		}

		public OutlookBar.OutlookBarCategory CreateToolbox(string name) {
			OutlookBar.OutlookBarCategory cat = new OutlookBar.OutlookBarCategory();
			cat.Text = name;
			cat.ButtonSpacing = 2;
			cat.HighlightType = OutlookBar.ButtonHighlightType.ImageAndText;
			cat.LayoutType = OutlookBar.CategoryLayoutType.TextRight;
			toolbox.Categories.Add(cat);
			return cat;
		}

		public OutlookBar.OutlookBarButton AddButton(OutlookBar.OutlookBarCategory category, string text, object tag) {
			if(category == null) return null;
			if(text == null) return null;
			OutlookBar.OutlookBarButton button = new OutlookBar.OutlookBarButton();
			button.Text = text;
			button.Tag = tag;
			category.Buttons.Add(button);
			return button as OutlookBar.OutlookBarButton;
		}

		public OutlookBar.OutlookBarButton AddButton(object sender, OutlookBar.OutlookBarCategory category, string text, object tag, string imageName) {
			OutlookBar.OutlookBarButton b = AddButton(category, text, tag);
			Stream s = GetResource(sender, imageName);
			if(s != null)
				b.Image = Image.FromStream(s);
			return b;
		}

		private Stream GetResource(object owner, string name) {
			return owner.GetType().Assembly.GetManifestResourceStream(name);
		}

		public void WireUpPostPluginEvents() {
			SceneGraph.Instance.SelectedObjectChanged += new Chronos.Core.SceneGraph.SelectedObjectChangedDelegate(Instance_SelectedObjectChanged);
		}

		public void AdoptForm(Form form) {
			form.Owner = mainForm;
		}

		private void Instance_SelectedObjectChanged(object sender, EditorNode node) {
			foreach(MenuBar b in customToolBars) {
				(b.Parent as ToolBarContainer).Controls.Remove(b);
			}
			customToolBars.Clear();

			IMovableObjectPlugin plugin = node.GetOwner();
			if(plugin != null) {
				foreach(MovableObject obj in node.AttachedObjects) {
					IPropertiesWrapper wrapper = plugin.GetPropertiesObject(obj);
					if(wrapper != null) {
						TD.SandBar.ToolBar b = wrapper.GetContextualToolBar();
						if(b != null) {
							customToolBars.Add(b);
							toolBars.Controls.Add(b);
						}
					}
				}
			}
		}
	}
}
