using System;
using System.Collections;
using System.Collections.Generic;
using Axiom.Collections;

namespace Axiom.RenderSystems.SlimDX9
{
    /// <summary>
    /// Summary description for DriverCollection.
    /// </summary>
    public class DriverCollection : UnsortedCollection<Driver>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        public Driver this[string description]
        {
            get
            {
                foreach (Driver drv in this)
                {
                    if (drv.Description == description)
                        return drv;
                }
                return null;
            }
        }
    }
}
