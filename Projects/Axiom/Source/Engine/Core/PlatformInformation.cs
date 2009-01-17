#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id: Root.cs 1445 2008-12-02 19:25:22Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

using Axiom.Animating;
using Axiom.Collections;
using Axiom.Controllers;
using Axiom.FileSystem;
using Axiom.Fonts;
using Axiom.Media;
using Axiom.Overlays;
using Axiom.Input;
using Axiom.ParticleSystems;
using Axiom.Graphics;
#if MONO_SIMD
using Mono.Simd;
#endif
#if SYSTEM_MANAGEMENT
using System.Management;
#endif

#endregion Namespace Declarations

namespace Axiom.Core
{
    sealed class PlatformInformation
    {
        public enum CPUFeature
        {
            SSE1,
            SSE2,
            SSE3,
            SSE41,
            SSE42,
            SSE4A,
            SSSE3,
            Count
        }

        /* the idea of this is so that we can use accelerated methods while not knowing the
         * each cpu that provides that acceration. 
         */
        public static readonly bool shuffle_accel = false;
        public static readonly bool general_accel = false;
        public static readonly bool horizontal_add_sub_accel = false;
        
        private static bool[] cpuFeatures = new bool[ (int)CPUFeature.Count ];
        private static string cpuIdentifier = "CPU Identification not available";

        /// <summary>
        /// Empty static constructor
        /// DO NOT DELETE.  It needs to be here because:
        /// 
        ///     # The presence of a static constructor suppresses beforeFieldInit.
        ///     # Static field variables are initialized before the static constructor is called.
        ///     # Having a static constructor is the only way to ensure that all resources are 
        ///       initialized before other static functions are called.
        /// 
        /// (from "Static Constructors Demystified" by Satya Komatineni
        ///  http://www.ondotnet.com/pub/a/dotnet/2003/07/07/staticxtor.html)
        /// </summary>
        static PlatformInformation()
        {
#if MONO_SIMD
			/* cpu agnostic */
			shuffle_accel = SimdRuntime.IsMethodAccelerated( typeof( Vector4f ), "Shuffle");

			horizontal_add_sub_accel = SimdRuntime.IsMethodAccelerated( typeof( Vector4f ), "HorizontalAdd") &
									   SimdRuntime.IsMethodAccelerated( typeof( Vector4f ), "HorizontalSub") ;
			
			general_accel = (SimdRuntime.AccelMode != AccelMode.None);
			
			
            if ( ( SimdRuntime.AccelMode & AccelMode.SSSE3 ) == AccelMode.SSSE3 )
            {
                cpuFeatures[ (int)CPUFeature.SSSE3 ] = true;
            }
            if ( ( SimdRuntime.AccelMode & AccelMode.SSE4A ) == AccelMode.SSE4A )
            {
                cpuFeatures[ (int)CPUFeature.SSE4A ] = true;
            }
            if ( ( SimdRuntime.AccelMode & AccelMode.SSE42 ) == AccelMode.SSE42 )
            {
                cpuFeatures[ (int)CPUFeature.SSE42 ] = true;
            }
            if ( ( SimdRuntime.AccelMode & AccelMode.SSE41 ) == AccelMode.SSE41 )
            {
                cpuFeatures[ (int)CPUFeature.SSE41 ] = true;
            }
            if ( ( SimdRuntime.AccelMode & AccelMode.SSE3 ) == AccelMode.SSE3 )
            {
                cpuFeatures[ (int)CPUFeature.SSE3 ] = true;
            }
            if ( ( SimdRuntime.AccelMode & AccelMode.SSE2 ) == AccelMode.SSE2 )
            {
                cpuFeatures[ (int)CPUFeature.SSE2 ] = true;
            }
            if ( ( SimdRuntime.AccelMode & AccelMode.SSE1 ) == AccelMode.SSE1 )
            {
                cpuFeatures[ (int)CPUFeature.SSE1 ] = true;
            }
#endif

#if SYSTEM_MANAGEMENT
            ManagementObjectSearcher searcher = new ManagementObjectSearcher( "select * from Win32_Processor" );
            foreach ( var item in searcher.Get() )
            {
                cpuIdentifier = item.Properties[ "Name" ].Value.ToString();
            }
#endif
        }

        public static bool IsSupported( CPUFeature feature )
        {
            return cpuFeatures[ (int)feature ];
        }

        public static void Log( Log log )
        {
            log.Write( "CPU Identifier & Features" );
            log.Write( "-------------------------" );
            log.Write( cpuIdentifier );
#if MONO_SIMD
			foreach( AccelMode type in Enum.GetValues(typeof(AccelMode))) {
				if (type == AccelMode.None) continue;
				
				log.Write( " *\t{0}: {1}", type,  ((type & SimdRuntime.AccelMode) == type));
			}
#else
            log.Write( " *     SSE1: {0}", IsSupported( CPUFeature.SSE1 ) );
            log.Write( " *     SSE2: {0}", IsSupported( CPUFeature.SSE2 ) );
            log.Write( " *     SSE3: {0}", IsSupported( CPUFeature.SSE3 ) );
            log.Write( " *    SSE41: {0}", IsSupported( CPUFeature.SSE41 ) );
            log.Write( " *    SSE42: {0}", IsSupported( CPUFeature.SSE42 ) );
            log.Write( " *    SSE4A: {0}", IsSupported( CPUFeature.SSE4A ) );
            log.Write( " *    SSSE3: {0}", IsSupported( CPUFeature.SSSE3 ) );
#endif
			log.Write(" *Vector4f.HorizontalAdd() and HorizontalAdd(): {0}", horizontal_add_sub_accel);
			log.Write(" *Vector4f.Shuffle()                          : {0}", shuffle_accel);
            log.Write( "-------------------------" );

        }
    }
}
