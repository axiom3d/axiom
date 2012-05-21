using System.Globalization;
using System.Runtime.InteropServices;

#if (SILVERLIGHT || WINDOWS_PHONE || XBOX )

namespace System
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Delegate | AttributeTargets.Enum | AttributeTargets.Struct, Inherited = false)]
    [ComVisible(true)]
    public sealed class SerializableAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    [ComVisible(true)]
    public sealed class NonSerializedAttribute : Attribute
    {
    }

    public static class Extensions
    {
    }
}

#endif

#if WINDOWS_PHONE

namespace System.IO
{
    [ComVisible(true)]
    [Serializable]
    public enum SearchOption
    {
        TopDirectoryOnly,
        AllDirectories,
    }
}

#endif