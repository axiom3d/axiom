using System;

namespace Axiom.Gui
{
	/// <summary>
	/// 	Defines the interface which all components wishing to 
	/// 	supply GuiElement subclasses must implement.
	/// </summary>
	/// <remarks>
	/// 	To allow the GuiElement types available for inclusion on 
	/// 	overlays to be extended, the engine allows external apps or plugins
	/// 	to register their ability to create custom GuiElements with
	/// 	the GuiManager, using the AddGuiElementFactory method. Classes
	/// 	wanting to do this must implement this interface.
	/// 	<p/>
	/// 	Each GuiElementFactory creates a single type of GuiElement, 
	/// 	identified by a 'type name' which must be unique.
	/// </summary>
	public interface IGuiElementFactory
	{	
		#region Methods

        /// <summary>
        ///    Classes that implement this interface will return an instance of a GuiElement of their designated
        ///    type.
        /// </summary>
        /// <param name="name">Name of the element to create.</param>
        /// <returns>A new instance of a GuiElement with the specified name.</returns>
        GuiElement Create(string name);
		
		#endregion
		
		#region Properties

        /// <summary>
        ///    Classes that implement this interface should return the name of the GuiElement that it will be
        ///    responsible for creating.
        /// </summary>
        string Type {
            get;
        }
		
		#endregion

	}
}
