using System;

namespace Axiom.MathLib
{
    /// <summary>
    ///    Type of intersection detected between 2 object.
    /// </summary>
    public enum IntersectionType { 
        /// <summary>
        ///    The objects are not colliding.
        /// </summary>
        None,
        /// <summary>
        ///    An object is fully contained within another object.
        /// </summary>
        Contained, 
        /// <summary>
        ///    The objects are partially intersecting each other.
        /// </summary>
        Intersects 
    } 
}
