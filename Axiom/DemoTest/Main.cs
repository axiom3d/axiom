#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

using Axiom.Animating;
using Axiom.Controllers;
using Axiom.Controllers.Canned;
using Axiom.Core;
using Axiom.Enumerations;
using Axiom.Exceptions;
using Axiom.Fonts;
using Axiom.MathLib;
using Axiom.ParticleSystems;
using Axiom.Physics;
using Axiom.Scripting;
using Axiom.SubSystems.Rendering;
using Axiom.Utility;
using Vector3 = Axiom.MathLib.Vector3;
using Quaternion = Axiom.MathLib.Quaternion;

namespace Demos
{

	/// <summary>
	/// Summary description for DemoTest.
	/// </summary>
	public class DemoTest
	{
		[STAThread]
		private static void Main(String[] args)
		{
			try
			{
				Application.Run(new DemoBrowser());
			}
			catch(Exception ex)
			{
				// BUG: Log is already closed at this point in some scenarios
				//Trace.WriteLine(ex.ToString());

				// call the existing global exception handler
				TechDemo.GlobalErrorHandler(null, new System.Threading.ThreadExceptionEventArgs(ex));
			}
		
		}
	}
}
