using System;

namespace RealmForge
{

    /// <summary>
    /// Represents a object which has a unique ID
    /// </summary>
    /// <remarks>For instance with all scene objects must have unique IDs respect to eachother
    /// regardless of whether they are Entities or Scene Nodes or another type></remarks>
    public interface IIdentifiable
    {
        /// <summary>
        /// Gets or Sets the unique identifier which is also the key for IDictionary collections
        /// </summary>
        string ID
        {
            get;
            set;
        }
    }
}
