using System;
using System.Collections.Generic;
using Axiom.Collections;


/*
Axiom wraps this in ScriptableObject
ParamCommands are defined like this:

[ScriptableProperty("includes_skeletal_animation")]
private class IncludesSkeletalAnimationPropertyCommand : Scripting.IPropertyCommand
{
    public string Get(object target)
    {
        return ((GpuProgram)target).IsSkeletalAnimationIncluded.ToString();
    }

    public void Set(object target, string val)
    {
        ((GpuProgram)target).IsSkeletalAnimationIncluded = bool.Parse(val);
    }
}
 */


#if false
namespace Axiom.Core
{
    public enum ParameterType
    {
        Bool,
        Real,
        Int, 
        UnsignedInt,
        Short,
        UnsignedShort,
        Long,
        UnsignedLong,
        String,
        Vector3,
        Matrix3,
        Matrix4,
        Quaternion,
        Colourvalue
    }

    /// <summary>
    /// Definition of a parameter supported by a StringInterface class, for introspection
    /// </summary>
    public class ParameterDef
    {
        public String Name;

        public String Description;

        public ParameterType ParamType;

        public ParameterDef(String newName, String newDescription, ParameterType newType)
        {
            Name = newName;
            Description = newDescription;
            ParamType = newType;
        }
    }

    public class ParameterList: List<ParameterDef>
    {
    }

    public abstract class ParamCommand
    {
        public abstract String DoGet(object target);
        public abstract void DoSet(object target, String val);
    }

    public class ParamCommandMap: Dictionary<string, ParamCommand>
    {
    }

    /// <summary>
    /// Class to hold a dictionary of parameters for a single class.
    /// </summary>
    public class ParamDictionary
    {
        /// <summary>
        /// Definitions of parameters
        /// </summary>
        public ParameterList Parameters { get; protected set; }

        /// <summary>
        /// Command objects to get/set
        /// </summary>
        protected ParamCommandMap ParamCommands = new ParamCommandMap();

        public static readonly ParameterList Empty = new ParameterList();

        /// <summary>
        /// Retrieves the parameter command object for a named parameter. */
        /// </summary>
        protected internal ParamCommand GetParamCommand(String name)
        {
            ParamCommand cmd;
            if (ParamCommands.TryGetValue( name, out cmd))
                return cmd;
            return null;
        }

        /// <summary>
        /// Method for adding a parameter definition for this class. 
        /// </summary>
        /// <param name="paramDef">A ParameterDef object defining the parameter</param>
        /// <param name="paramCmd">Reference to a ParamCommand subclass to handle the getting / setting of this parameter.
        /// NB this class will not destroy this on shutdown, please ensure you do</param>
        public void AddParameter(ParameterDef paramDef, ParamCommand paramCmd)
        {
            Parameters.Add(paramDef);
            ParamCommands.Add( paramDef.Name, paramCmd );
        }
    }

    public class ParamDictionaryMap: Dictionary<string, ParamDictionary>
    {
    }


    /// <summary>
    /// Class defining the common interface which classes can use to 
    /// present a reflection-style, self-defining parameter set to callers.
    /// </summary>
    /// <remarks>
    /// This class also holds a static map of class name to parameter dictionaries
    /// for each subclass to use. See ParamDictionary for details. 
    /// </remarks>
    /// <remarks>
    /// In order to use this class, each subclass must call createParamDictionary in their constructors
    /// which will create a parameter dictionary for the class if it does not exist yet.
    /// </remarks>
    public class StringInterface
    {
        private readonly static object DictionaryMutex = new object();

        /// <summary>
        /// Dictionary of parameters
        /// </summary>
        private static readonly ParamDictionaryMap Dictionary = new ParamDictionaryMap();

        /// <summary>
        /// Class name for this instance to be used as a lookup (must be initialised by subclasses)
        /// </summary>
        private String _paramDictName;

        public ParamDictionary ParamDictionary { get; private set; }

        /// <summary>
        /// Internal method for creating a parameter dictionary for the class, if it does not already exist.
        /// </summary>
        /// <remarks>
        /// This method will check to see if a parameter dictionary exist for this class yet,
        /// and if not will create one. NB you must supply the name of the class (RTTI is not 
        /// used or performance).
        /// </remarks>
        /// <param name="className">the name of the class using the dictionary</param>
        /// <returns>true if a new dictionary was created, false if it was already there</returns>
        protected bool CreateParamDictionary(String className)
        {
            lock(DictionaryMutex)
            {
                ParamDictionary value;
                if (!Dictionary.TryGetValue(className, out value))
                {
                    ParamDictionary = new ParamDictionary();
                    _paramDictName = className;
                    Dictionary.Add(className, ParamDictionary);
                    return true;
                }
                // else
                {
                    ParamDictionary = value;
                    _paramDictName = className;
                    return false;
                }
            }
        }

        /// <summary>
        /// Retrieves a list of parameters valid for this object. 
        /// </summary>
        public ParameterList Parameters
        {
            get
            {
                var dict = ParamDictionary;
                if (dict != null)
                    return dict.Parameters;
                return ParamDictionary.Empty;
            }
        }

        /// <summary>
        /// Generic parameter setting method.
        /// </summary>
        /// <remarks>Call this method with the name of a parameter and a string version of the value
        /// to set. The implementor will convert the string to a native type internally.
        /// If in doubt, check the parameter definition in the list returned from 
        /// StringInterface.GetParameters.
        /// </remarks>
        /// <param name="name">The name of the parameter to set</param>
        /// <param name="value">String value. Must be in the right format for the type specified in the parameter definition.
        /// See the StringConverter class for more information.</param>
        /// <returns>true if set was successful, false otherwise (NB no exceptions thrown - tolerant method)</returns>
        public virtual bool SetParameter(String name, String value)
        {
            var dict = ParamDictionary;

            if (dict != null)
            {
                var cmd = dict.GetParamCommand(name);
                if (cmd != null)
                {
                    cmd.DoSet( this, value );
                    return true;
                }
            }

            // Fallback
            return false;
        }

        ///<summary>
        /// Generic multiple parameter setting method.
        ///</summary>
        ///<remarks>
        /// Call this method with a list of name / value pairs
        /// to set. The implementor will convert the string to a native type internally.
        /// If in doubt, check the parameter definition in the list returned from 
        /// StringInterface.GetParameters.
        ///</remarks>
        ///<param name="paramList">Name/value pair list</param>
        public virtual void SetParameterList(NameValuePairList paramList)
        {
            foreach (var p in paramList)
                SetParameter( p.Key, p.Value );
        }


        ///<summary>
        /// Generic parameter retrieval method.
        ///</summary>
        ///<remarks>Call this method with the name of a parameter to retrieve a string-format value of
        /// the parameter in question. If in doubt, check the parameter definition in the
        /// list returned from getParameters for the type of this parameter. If you
        /// like you can use StringConverter to convert this string back into a native type.
        /// </remarks>
        ///<param name="name">name The name of the parameter to get</param>
        ///<returns>String value of parameter, blank if not found</returns>
        public virtual string GetParameter(String name)
        {
            var dict = ParamDictionary;

            if (dict != null)
            {
                // Look up command object
                var cmd = dict.GetParamCommand(name);
                if (cmd != null)
                {
                    return cmd.DoGet( this );
                }
            }

            // Fallback
            return "";
        }

        /// <summary>
        /// Method for copying this object's parameters to another object.
        /// </summary>
        /// <remarks>This method takes the values of all the object's parameters and tries to set the
        /// same values on the destination object. This provides a completely type independent
        /// way to copy parameters to other objects. Note that because of the String manipulation 
        /// involved, this should not be regarded as an efficient process and should be saved for
        /// times outside of the rendering loop.
        /// </remarks>
        /// <param name="dest">Pointer to object to have it's parameters set the same as this object.</param>
        public virtual void CopyParametersTo(StringInterface dest)
        {
            var dict = ParamDictionary;

            if (dict != null)
            {
                foreach (var par in dict.Parameters)
                {
                    dest.SetParameter( par.Name, GetParameter( par.Name ) );
                }
            }
        }

        /// <summary>
        /// Cleans up the static 'msDictionary' required to reset Ogre,
        /// otherwise the containers are left with invalid pointers, which will lead to a crash
        /// as soon as one of the ResourceManager implementers (e.g. MaterialManager) initializes.
        /// </summary>
        public static void CleanupDictionary()
        {
            lock(DictionaryMutex)
            {
                Dictionary.Clear();
            }
        }
    }
}
#endif