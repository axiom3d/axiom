#if (SILVERLIGHT || WINDOWS_PHONE)

using System.Runtime.InteropServices;

namespace System.Security
{
    public enum PartialTrustVisibilityLevel
    {
        VisibleToAllHosts,
        NotVisibleByDefault,
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    [ComVisible(true)]
    public sealed class AllowPartiallyTrustedCallersAttribute : Attribute
    {
        public PartialTrustVisibilityLevel PartialTrustVisibilityLevel { get; set; }
    }

    [ComVisible(true)]
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Delegate | AttributeTargets.Interface | AttributeTargets.Method,
        AllowMultiple = true, Inherited = false)]
    public sealed class SuppressUnmanagedCodeSecurityAttribute : Attribute {}
}

#endif