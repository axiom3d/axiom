using System.IO;
using System.Xml;
using System.Collections;

namespace OctreeZone
{
    public class ConfigFile
    {
        private XmlDocument _doc = new XmlDocument();
        private string baseSchema;
        public ConfigFile(string baseSchemaName)
        {
            baseSchema = baseSchemaName;
        }

        public bool Load(Stream stream)
        {
            _doc.Load(stream);
            return true;
        }

        public string this[string key]
        {
            get
            {
                return _doc[baseSchema][key].InnerText;
            }
        }

        public IEnumerable GetEnumerator()
        {
            foreach (XmlElement el in _doc[baseSchema])
            {
                yield return new string[]{ el.Name, el.InnerText };
            }
        }

        public string getSetting(string key)
        {
            return this[key];
        }
    }
}