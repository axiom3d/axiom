using System;
using System.Reflection;

namespace RealmForge.Reflection
{
    /// <summary>
    /// Summary description for MemberAttributeInfo.
    /// </summary>
    public struct MemberAttributeInfo
    {
        public object Attribute;
        public MemberInfo Member;
        public Type MemberType;

        public MemberAttributeInfo( object attrib, MemberInfo member, Type memberType )
        {
            Attribute = attrib;
            Member = member;
            MemberType = memberType;
        }

    }
}
