using System;
using System.Collections;
using System.Text;

namespace Axiom
{
    /// <summary>
    /// Packages the details of a configuration option.
    /// </summary>
    /// <remarks>Used for RenderSystem::getConfigOptions. If immutable is true, this option must be disabled for modifying.</remarks>
    public struct ConfigOption
    {
        public string Name;
        public string Value;
        public ArrayList PossibleValues;
        public bool Immutable;

        public ConfigOption(string name, string value, bool immutable)
        {
            this.PossibleValues = new ArrayList();
            this.Name = name;
            this.Value = value;
            this.Immutable = immutable;
        }

        public override string ToString()
        {
            return string.Format( "{0} : {1}", this.Name, this.Value );
        }
    }
}
