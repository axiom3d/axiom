#region LGPL License
/*
This file is part of the RealmForge GDK.
Copyright (C) 2003-2004 Daniel L. Moorehead

The RealmForge GDK is a cross-platform game development framework and toolkit written in Mono/C# and powered by the Axiom 3D engine. It will allow for the rapid development of cutting-edge software and MMORPGs with advanced graphics, audio, and networking capabilities.

dan@xeonxstudios.com
http://xeonxstudios.com
http://sf.net/projects/realmforge

If you have or intend to contribute any significant amount of code or changes to RealmForge you must go have completed the Xeonx Studios Copyright Assignment.

RealmForge is free software; you can redistribute it and/or modify it under the terms of  the GNU Lesser General Public License as published by the Free Software Foundation; either version 2 or (at your option) any later version.

RealmForge is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the accompanying RealmForge License and GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License along with RealmForge; if not, write to the Free Software Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA.
*/
#endregion

using System;

namespace RealmForge
{
    /// <summary>
    /// The generic exception that is thrown when an application level error occurs.
    /// </summary>
    public class GeneralException : ApplicationException
    {
        #region Constructors

        public GeneralException( string messageFormat, params object[] args )
            : base( string.Format( messageFormat, args ), null )
        {
        }

        public GeneralException( string messageFormat, Exception innerException, params object[] args )
            : base( string.Format( messageFormat, args ), innerException )
        {
        }

        public GeneralException( string message, Exception innerException )
            : base( message, innerException )
        {
        }

        public GeneralException( string message )
            : base( message )
        {
        }

        public GeneralException()
            : base( "An exception has occured" )
        {
        }
        #endregion
    }
}
