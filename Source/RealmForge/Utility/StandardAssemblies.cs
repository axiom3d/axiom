using System;
using System.Reflection;

namespace RealmForge
{
    /// <summary>
    /// When common assemblis are loaded they will set a reference to them here to prevent reliance upon loading by string names which may change
    /// </summary>
    /// <remarks>RealmForge plugins should register whatever factories, singletons, or plugins are needed with the Framework or other core assemblies
    /// and their availability may very depending upon installation or platform so they are not included here.</remarks>
    public class StandardAssemblies
    {
        public static Assembly Framework;
        public static Assembly Genres;
        public static Assembly RAGE;
        public static Assembly AxiomEngine;
        public static Assembly MathLib;
        public static Assembly ScriptLibrary;
        public static Assembly Utility;
        public static Assembly Cegui;
        public static Assembly CeguiThemes;

    }
}
