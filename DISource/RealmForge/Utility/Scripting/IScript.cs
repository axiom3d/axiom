#region LGPL License
/*
This file is part of the RealmForge GDK.
Copyright (C) 2003-2004 Daniel L. Moorehead

The RealmForge GDK is a cross-platform game development framework and toolkit written in Mono/C# and powered by the Axiom 3D engine. It will allow for the rapid development of cutting-edge software and MMORPGs with advanced graphics, audio, and networking capabilities.

dan@xeonxstudios.com
http://xeonxstudios.com
http://sf.net/projects/realmforge

If you have or intend to contribute any significant amount of code or changes to RealmForge you must go have completed the Xeonx Studios Copyright Assignment.

RealmForge is free software; you can redistribute it and/or modify it under the terms of  the GNU Lesser General Public License as published by the Free Software Foundation; either version 2 or (at your option) any later version.

RealmForge is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the accompanying RealmForge License and GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License along with RealmForge; if not, write to the Free Software Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA.
*/
#endregion


using System;
using System.Collections;

namespace RealmForge.Scripting
{
    #region Delegates
    /// <summary>
    /// Represents the method that executes a script to handle a script event
    /// </summary>
    /// <remarks>
    /// The owner is the IScriptable object which features the ScriptEvent which this script is a handler for.
    /// The target is an optional parameter specifying what target the script is supposed to modify or a critical peice of information
    /// such as the window ID for an IWindowManager WindowCreator
    /// The args is a custom data structure which can be used to hold more options, parameters, or setting for the script.
    /// Most of the parameters for a script, should be properties for it instance however so that they can be edited when the script instance
    /// is selected in the editor.  Most often target and args are null.
    /// </remarks>
    public delegate void Script( object owner, object args );

    /// <summary>
    /// Represents a method that has no return type and no parameters and which can serve as a parameterless script
    /// </summary>
    public delegate void SimpleMethod();
    #endregion

    /// <summary>
    /// Represents a unique script object which accepts a table of parameters and can be attached to an entity via a ScriptCall object which defines what paremters should be passed
    /// There is only 1 instance of every script and they are all cached in the Scripts singleton
    /// </summary>
    /// <remarks>
    /// Script names should be of the form [Product].[Package].[Target].[Action] where Package is optionally.
    /// Examples: DemoGame.PC.MoveLeft, DemoGame.UI.Window.Hide (where UI is the package or script group), and DemoGamePlugin.Combat.Target.InstantKill
    /// Since scripts are used often, it may be a good idea to use an acronym or terse alias for the product such as RSL for RealmForgeScriptLibrary or RF for RealmForge or Demo for DemoGame
    /// The scripts used in the DemoGames, Tutorials, and provided in the RealmForge Script Library, have a product name of RSL such as in RSL.UI.CreatedWindow
    /// </remarks>
    public interface IScript
    {
        /// <summary>
        /// The method with a Script signature that executes this script
        /// </summary>
        /// <param name="invoker">The entity to which this script is attached or the invoker that called this</param>
        /// <param name="target">The target which this script is to be performed on, null if there is none</param>
        /// <param name="args">The collection of parameter values keyed to their parameter names</param>
        void Execute( object owner, object args );
    }
}
