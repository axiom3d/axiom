#if (SILVERLIGHT || WINDOWS_PHONE || XBOX )

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Serialization
{
    public interface ISerializable
    {
        [SecurityCritical]
        void GetObjectData(SerializationInfo info, StreamingContext context);
    }

    public sealed class SerializationInfo
    {
        private readonly Dictionary<string, object> values = new Dictionary<string, object>();

        public void AddValue<T>(String name, T value)
        {
            values.Add(name, value);
        }

        public object GetValue(String name, Type type )
        {
            return values[ name ];
        }
    }

    [ComVisible(true)]
    public interface IDeserializationCallback
    {
        void OnDeserialization(object sender);
    }
}

#endif