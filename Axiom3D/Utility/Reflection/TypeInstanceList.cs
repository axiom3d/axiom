using System;
using System.Collections;

namespace RealmForge.Reflection
{
    /// <summary>
    /// 
    /// </summary>
    public class TypeInstanceList
    {
        public TypeInstanceList( Type type, ArrayList list )
        {
            Type = type;
            TypeName = type.Name;
            List = list;
        }
        public Type Type;
        public ArrayList List;
        public string TypeName;
    }
}
