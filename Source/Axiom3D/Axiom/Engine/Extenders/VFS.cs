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

#region Namespace Declarations
using System;
using System.Collections.Generic;
using System.Text;
#endregion

namespace Axiom
{
    /// <summary>
    /// Virtual filesystem 
    /// </summary>
    public class Vfs
    {
        #region Singleton implementation
        private Vfs()
        {
        }

        private static Vfs _instance = null;
        public static Vfs Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Vfs();

                return _instance;
            }
        }

        #endregion

        public void Initialize()
        {
            LogManager.Instance.Write("*-*-* Virtual File System initialized");
        }

        /// <summary>
        /// VFS contents
        /// </summary>
        protected HierarchicalRegistry<INamespaceExtender>
            fileSystem = new HierarchicalRegistry<INamespaceExtender>();

        /// <summary>
        /// Registers a new namespace with the VFS
        /// </summary>
        /// <param name="namespaceExtender">reference to the namespace extender</param>
        public virtual void RegisterNamespace(INamespaceExtender namespaceExtender)
        {
            fileSystem.RegisterNamespace(namespaceExtender);
            fileSystem.Add(namespaceExtender.Namespace, namespaceExtender);

            LogManager.Instance.Write("Registered new VFS namespace {0} to be processed by {1}",
                namespaceExtender.Namespace, namespaceExtender.GetType().FullName);
        }

        private INamespaceExtender recursiveLookupExtender(ref string namespaceName)
        {
            // finished recursing?
            if (namespaceName.IndexOf("/") == -1)
                return null; 

            namespaceName =
                namespaceName.Substring(0, namespaceName.LastIndexOf("/"));

            if (fileSystem.ContainsKey(namespaceName + "/"))
                return fileSystem[namespaceName + "/"];
            else
                return recursiveLookupExtender(ref namespaceName);
        }

        /// <summary>
        /// Returns an object from the VFS
        /// </summary>
        /// <param name="path">object path</param>
        /// <returns>If the path is a namespace (i.e. ends with an "/"), 
        /// <see cref="INamespaceExteder"/> instance is returned, otherwise Vfs tries
        /// to locate the object by extracting its namespace information and querying
        /// the respective <see cref="INamespaceExteder"/> for the object</returns>
        public object this[string path]
        {
            get
            {
                if (path.EndsWith("/"))
                    return fileSystem[path];
                else
                {
                    if (path.LastIndexOf("/") == -1)
                        return null;

                    string namespaceName =
                        path.Substring(0, path.LastIndexOf("/") + 1);
                    INamespaceExtender extender =
                        recursiveLookupExtender(ref namespaceName);

                    if (extender == null)
                        return null;

                    string objectName =
                        path.Substring(namespaceName.Length + 1);
                        
                    return (extender).GetObject<object>(objectName);
                }
            }
        }
    }

    ///// <summary>
    ///// Axiom VFS singleton class
    ///// </summary>
    //// TODO: find a better name/implementation for it
    //public class Vfs : Singleton<Vfs>
    //{
    //    public override bool Initialize()
    //    {
    //        return true;
    //    }
    //}
}
