using System;

namespace RealmForge.Scripting
{

    /// <summary>
    /// An attribute that when applied to an IScript class will not allow its instance to be edited with the property grid
    /// in the event set designer dialog or even included in the RF.Scripts list
    /// </summary>
    /// <remarks>This is used by wrapper classes like EventHandlerCaller.</remarks>
    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct )]
    public class IgnoredScriptAttribute : Attribute
    {
        #region Constructors
        public IgnoredScriptAttribute()
        {
        }
        #endregion
    }
}
