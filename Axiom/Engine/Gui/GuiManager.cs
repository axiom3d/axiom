#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/
#endregion

using System;
using System.Collections;
using System.Diagnostics;

namespace Axiom.Gui {
    /// <summary>
    ///    This class acts as a repository and regitrar of overlay components.
    /// </summary>
    /// <remarks>
    ///    GuiManager's job is to manage the lifecycle of GuiElement (subclass)
    ///    instances, and also to register plugin suppliers of new components.
    /// </remarks>
    public class GuiManager {

        #region Singleton implementation

        static GuiManager() { Init(); }
        protected GuiManager() {}
        protected static GuiManager instance;

        public static GuiManager Instance {
            get { return instance; }
        }

        public static void Init() {
            instance = new GuiManager();
        }
		
        #endregion

        #region Member variables

        private Hashtable factories = new Hashtable();
        private Hashtable instances = new Hashtable();
        private Hashtable templates = new Hashtable();

        #endregion

        #region Methods

        /// <summary>
        ///     Registers a new GuiElementFactory with this manager.
        /// </summary>
        /// <remarks>
        ///    Should be used by plugins or other apps wishing to provide
        ///    a new GuiElement subclass.
        /// </remarks>
        /// <param name="factory"></param>
        public void AddElementFactory(IGuiElementFactory factory) {
            factories.Add(factory.Type, factory);

            Trace.WriteLine(string.Format("GuiElementFactory for type '{0}' registered.", factory.Type));
        }

        /// <summary>
        ///    Creates a new GuiElement of the type requested.
        /// </summary>
        /// <param name="typeName">The type of element to create is passed in as a string because this
        ///    allows plugins to register new types of component.</param>
        /// <param name="instanceName">The type of element to create.</param>
        /// <returns></returns>
        public GuiElement CreateElement(string typeName, string instanceName) {
            return CreateElement(typeName, instanceName, false);
        }

        /// <summary>
        ///    Creates a new GuiElement of the type requested.
        /// </summary>
        /// <param name="typeName">The type of element to create is passed in as a string because this
        ///    allows plugins to register new types of component.</param>
        /// <param name="instanceName">The type of element to create.</param>
        /// <param name="isTemplate"></param>
        /// <returns></returns>
        public GuiElement CreateElement(string typeName, string instanceName, bool isTemplate) {
            Hashtable elements = GetElementTable(isTemplate);

            if(elements.ContainsKey(instanceName)) {
                throw new Exception(string.Format("GuiElement with the name '{0}' already exists.")); 
            }

            GuiElement element = CreateElementFromFactory(typeName, instanceName);
            element.Initialize();

            // register
            elements.Add(instanceName, element);
    
            return element;        
        }

        /// <summary>
        ///    Creates an element of the specified type, with the specified name
        /// </summary>
        /// <remarks>
        ///    A factory must be available to handle the requested type, or an exception will be thrown.
        /// </remarks>
        /// <param name="typeName"></param>
        /// <param name="instanceName"></param>
        /// <returns></returns>
        public GuiElement CreateElementFromFactory(string typeName, string instanceName) {
            if(!factories.ContainsKey(typeName)) {
                throw new Exception(string.Format("Cannot locate factory for element type '{0}'", typeName));
            }

            // create the element
            return ((IGuiElementFactory)factories[typeName]).Create(instanceName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="templateName"></param>
        /// <param name="typeName"></param>
        /// <param name="instanceName"></param>
        /// <param name="isTemplate"></param>
        /// <returns></returns>
        public GuiElement CreateElementFromTemplate(string templateName, string typeName, string instanceName, bool isTemplate) {
            GuiElement element = null;

            if(templateName.Length == 0) {
                element = CreateElement(typeName, instanceName, isTemplate);
            }
            else {
                GuiElement template = GetElement(templateName, true);

                string typeToCreate = "";
                if(typeName.Length == 0) {
                    typeToCreate = template.Type;
                }
                else {
                    typeToCreate = typeName;
                }

                element = CreateElement(typeToCreate, instanceName, isTemplate);

                // Copy settings from template
                ((GuiContainer)element).CopyFromTemplate(template);
            }

            return element;
        }

        /// <summary>
        ///    Gets a reference to an existing element.
        /// </summary>
        /// <param name="name">Name of the element to retrieve.</param>
        /// <param name="isTemplate"></param>
        /// <returns></returns>
        public GuiElement GetElement(string name) {
            Hashtable elements = GetElementTable(false);

            Debug.Assert(elements.ContainsKey(name), string.Format("GuiElement with the name'{0}' was not found.", name));

            return (GuiElement)elements[name];
        }

        /// <summary>
        ///    Gets a reference to an existing element.
        /// </summary>
        /// <param name="name">Name of the element to retrieve.</param>
        /// <param name="isTemplate"></param>
        /// <returns></returns>
        public GuiElement GetElement(string name, bool isTemplate) {
            Hashtable elements = GetElementTable(isTemplate);

            Debug.Assert(elements.ContainsKey(name), string.Format("GuiElement with the name'{0}' was not found.", name));

            return (GuiElement)elements[name];
        }

        /// <summary>
        ///    Quick helper method to return the lookup table for the right element type.
        /// </summary>
        /// <param name="isTemplate"></param>
        /// <returns></returns>
        private Hashtable GetElementTable(bool isTemplate) {
            return isTemplate ? templates : instances;
        }

        #endregion Methods
    }
}
