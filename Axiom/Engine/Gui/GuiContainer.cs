using System;
using System.Collections;
using System.Diagnostics;
using Axiom.Core;

namespace Axiom.Gui
{
	/// <summary>
	/// 	A 2D element which contains other GuiElement instances.
	/// </summary>
	/// <remarks>
	/// 	This is a specialization of GuiElement for 2D elements that contain other
	/// 	elements. These are also the smallest elements that can be attached directly
	/// 	to an Overlay.
	/// 	<p/>
	/// 	GuiContainers should be managed using GuiManager. This class is responsible for
	/// 	instantiating elements, and also for accepting new types of element
	/// 	from plugins etc.
	/// </remarks>
	public abstract class GuiContainer : GuiElement
	{
		#region Member variables
		
        protected Hashtable children = new Hashtable();
        protected ArrayList childList = new ArrayList();
        protected Hashtable childContainers = new Hashtable();

		#endregion
		
		#region Constructors
		
        /// <summary>
        ///    Don't use directly, create through GuiManager.CreateElement.
        /// </summary>
        /// <param name="name"></param>
		protected internal GuiContainer(string name) : base(name) {
		}
		
		#endregion
		
		#region Methods

        /// <summary>
        ///    Adds another GuiElement to this container.
        /// </summary>
        /// <param name="element"></param>
        public virtual void AddChild(GuiElement element) {
            if(element.IsContainer) {
                AddChildImpl((GuiContainer)element);
            }
            else {
                AddChildImpl(element);
            }
        }

        /// <summary>
        ///    Adds another GuiElement to this container.
        /// </summary>
        /// <param name="element"></param>
        public virtual void AddChildImpl(GuiElement element) {
            Debug.Assert(!children.ContainsKey(element.Name), string.Format("Child with name '{0}' already defined.", element.Name));

            // add to lookup table and list
            children.Add(element.Name, element);
            childList.Add(element);

            // inform this child about his/her parent and zorder
            element.NotifyParent(this, overlay);
            element.NotifyZOrder(zOrder + 1);
        }
        
        /// <summary>
        ///    Add a nested container to this container.
        /// </summary>
        /// <param name="container"></param>
        public virtual void AddChildImpl(GuiContainer container) {
            // add this container to the main child list first
            GuiElement element = container;
            AddChildImpl(element);
            element.NotifyParent(this, overlay);
            element.NotifyZOrder(zOrder + 1);

            // inform container children of the current overlay
            // *gasp* it's a foreach!  this isn't a time critical method anyway
            foreach(GuiElement child in container.children) {
                child.NotifyParent(container, overlay);
                child.NotifyZOrder(container.ZOrder + 1);
            }

            // now add the container to the container collection
            childContainers.Add(container.Name, container);
        }
           
        /// <summary>
        ///    Gets the named child of this container.
        /// </summary>
        /// <param name="name"></param>
        public virtual GuiElement GetChild(string name) {
            Debug.Assert(children.ContainsKey(name), "children.ContainsKey(name)");

            return (GuiElement)children[name];
        }

        /// <summary>
        ///    Tell the object and its children to recalculate their positions.
        /// </summary>
        public override void PositionsOutOfDate() {
            // call baseclass method
            base.PositionsOutOfDate();

            for(int i = 0; i < childList.Count; i++) {
                ((GuiElement)childList[i]).PositionsOutOfDate();
            }
        }

        public override void Update() {
            // call base class method
            base.Update ();

            for(int i = 0; i < childList.Count; i++) {
                ((GuiElement)childList[i]).Update();
            }
        }

        public override void NotifyZOrder(int zOrder) {
            // call base class method
            base.NotifyZOrder (zOrder);

            for(int i = 0; i < childList.Count; i++) {
                ((GuiElement)childList[i]).NotifyZOrder(zOrder);
            }
        }

        public override void NotifyParent(GuiContainer parent, Overlay overlay) {
            // call the base class method
            base.NotifyParent (parent, overlay);

            for(int i = 0; i < childList.Count; i++) {
                ((GuiElement)childList[i]).NotifyParent(this, overlay);
            }
        }

        public override void UpdateRenderQueue(Axiom.SubSystems.Rendering.RenderQueue queue) {
            if(isVisible) {
                // call base class method
                base.UpdateRenderQueue(queue);

                for(int i = 0; i < childList.Count; i++) {
                    ((GuiElement)childList[i]).UpdateRenderQueue(queue);
                }
            }
        }

		#endregion
		
		#region Properties
		
        /// <summary>
        ///    This is most certainly a container.
        /// </summary>
        public override bool IsContainer {
            get {
                return true;
            }
        }

        public override String Type {
            get {
                return "GuiContainer";
            }
        }

		#endregion

	}
}
