#region LGPL License
/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/
#endregion

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
    /// <summary>
    /// DriverVersion is used by RenderSystemCapabilities and both GL and D3D9
    /// to store the version of the current GPU driver
    /// </summary>
    public struct DriverVersion
    {
        public int Major    { get; set; }
        public int Minor    { get; set; }
        public int Release  { get; set; }
        public int Build    { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DriverVersion( int major, int minor, int release, int build )
            : this()
        {
            Major = major;
            Minor = minor;
            Release = release;
            Build = build;
        }

        #region System.Object overrides
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format( "{0}.{1}.{2}.{3}", Major, Minor, Release, Build );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals( object obj )
        {
            if ( obj == null )
                return false;

            if ( !( obj is DriverVersion ) )
                return false;

            var v = (DriverVersion)obj;

            return ( v.Major == Major ) && ( v.Minor == Minor ) && ( v.Release == Release ) && ( v.Build == Build );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Major ^ Minor ^ Release ^ Build;
        }
        #endregion System.Object overrides

        /// <summary>
        /// 
        /// </summary>
        /// <param name="versionString"></param>
        public void FromString( string versionString )
        {
            var tokens = versionString.Split( '.' );
            if ( tokens.Length > 0 )
            {
                try
                {
                    Major = int.Parse( tokens[ 0 ] );

                    if ( tokens.Length > 1 )
                        Minor = int.Parse( tokens[ 1 ] );

                    if ( tokens.Length > 2 )
                        Release = int.Parse( tokens[ 2 ] );

                    if ( tokens.Length > 3 )
                        Build = int.Parse( tokens[ 3 ] );
                }
                catch
                {
                    LogManager.Instance.Write( "Unable to parse the device version" );
                }
            }
        }

        #region Operators

        public static bool operator ==( DriverVersion a, DriverVersion b )
        {
            if ( Object.ReferenceEquals( a, b ) )
                return true;

            return ( a.Major == b.Major ) && ( a.Minor == b.Minor ) && ( a.Release == b.Release ) && ( a.Build == b.Build );
        }

        public static bool operator !=( DriverVersion a, DriverVersion b )
        {
            return !( a == b );
        }

        public static bool operator >( DriverVersion a, DriverVersion b )
        {
            var majorCmp = a.Major == b.Major;
            var minorCmp = a.Minor == b.Minor;
            var releaseCmp = a.Release == b.Release;

            if ( a.Major > b.Major )
                return true;

            else if ( majorCmp && a.Minor > b.Minor )
                return true;

            else if ( majorCmp && minorCmp && a.Release > b.Release )
                return true;

            else if ( majorCmp && minorCmp && releaseCmp && a.Build > b.Build )
                return true;

            return false;
        }

        public static bool operator <( DriverVersion a, DriverVersion b )
        {
            var majorCmp = a.Major == b.Major;
            var minorCmp = a.Minor == b.Minor;
            var releaseCmp = a.Release == b.Release;

            if ( a.Major < b.Major )
                return true;

            else if ( majorCmp && a.Minor < b.Minor )
                return true;

            else if ( majorCmp && minorCmp && a.Release < b.Release )
                return true;

            else if ( majorCmp && minorCmp && releaseCmp && a.Build < b.Build )
                return true;

            return false;
        }

        public static bool operator >=( DriverVersion a, DriverVersion b )
        {
            return ( a > b ) || ( a == b );
        }

        public static bool operator <=( DriverVersion a, DriverVersion b )
        {
            return ( a < b ) || ( a == b );
        }

        #endregion Operators
    };
}
