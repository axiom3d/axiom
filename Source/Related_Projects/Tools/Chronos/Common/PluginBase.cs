using System;
using System.Collections;
using System.Collections.Specialized;
using System.Windows.Forms;
using System.Xml;

namespace Chronos.Core
{
	public delegate void TabWindowHandler(Form f);

	public interface IEditorPlugin 
	{
		void Start();
		void Stop();
	}

	public interface IMovableObjectPlugin 
	{
		IPropertiesWrapper GetPropertiesObject(Axiom.Core.MovableObject obj);
		IManipulator GetManipulator(Axiom.Core.MovableObject obj);
	}

	public interface IManipulator {

		/// <summary>
		///    Returns a scene node that has a pre-constructed manipulator.
		/// </summary>
		/// <remarks>
		///    This method should create the manipulator on the given node. When the entities on
		///    the manipulator receive events, they'll be passed back to the manipulator for
		///    handling.
		///    
		///    The constructed node must be passed back to the the caller so the caller can
		///    parse it for entites to collide with.
		/// </remarks>
		/// <param name="obj">
		///		The object that this manipulator will act on. The manipulator
		///		should store this for event handling.
		/// </param>
		Axiom.Core.SceneNode GetManipulatorNode(Axiom.Core.SceneNode node);

		/// <summary>
		///    Called when the user clicks a handle supplied by the manipulator.
		/// </summary>
		/// <param name="handle">The entity (from the manipulator) that was clicked.</param>
		/// <param name="input">The Axiom input state</param>
		void ManipulatorHandleMouseDown(Axiom.Core.MovableObject handle, Axiom.Input.InputReader input);

		/// <summary>
		///    Called when the user moves the mouse cursor.
		/// </summary>
		/// <remarks>
		///    This is called regardless of the manipulator state, because you may want to 
		///    respond to mouse input even though the user hasn't otherwise interacted with the 
		///    manipulator (clicking). Examples of this may be, for example, highlighting a
		///    handle as the user moves the mouse over it.
		/// </remarks>
		/// <param name="handle">The entity (from the manipulator) that was clicked.</param>
		/// <param name="input">The Axiom input state</param>
		void ManipulatorHandleMoved(Axiom.Input.InputReader input);

		/// <summary>
		///    Called when the user releases the mouse button.
		/// </summary>
		/// <remarks>
		///		It is up to the manipulator to check whether or not it was clicked before the
		///		mouse up event. It is recommended you store some sort of state in the MouseDown
		///		handler.
		/// </remarks>
		/// <param name="handle">The entity (from the manipulator) that was clicked.</param>
		/// <param name="input">The Axiom input state</param>
		void ManipulatorHandleMouseUp(Axiom.Input.InputReader input);

		/// <summary>
		///    Called when the user presses a key in the context of the rendering window.
		/// </summary>
		/// <param name="handle">The entity (from the manipulator) that was clicked.</param>
		/// <param name="input">The Axiom input state</param>
		void ManipulatorHandleKeyPressed(Axiom.Input.InputReader input);

		void Tick(Axiom.Core.Camera camera);
	}

	public interface IXmlWriterPlugin 
	{
		bool Serialize(object o, XmlElement elem);
		StringCollection XmlElementHandlers 
		{
			get;
		}
	}

	public interface IPropertiesWrapper : IDisposable {
		TD.SandBar.ToolBar GetContextualToolBar();
	}
}