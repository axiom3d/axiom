#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team

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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

#endregion Namespace Declarations


namespace Axiom.RenderSystems.OpenGL
{
	public class Xrandr
	{
		public struct XRRScreenSize 
		{
			int	width, height;
			int	mwidth, mheight;
		} 

		/*
		 *  Events.
		 */

#if NOT
		public struct XRRScreenChangeNotifyEvent 
		{
			int type;			/* event base */
			ulong serial;	/* # of last request processed by server */
			bool send_event;		/* true if this came from a SendEvent request */
			Display *display;		/* Display the event was read from */
			Window window;		/* window which selected for this event */
			Window root;		/* Root window for changed screen */
			Time timestamp;		/* when the screen change occurred */
			Time config_timestamp;	/* when the last configuration change */
			SizeID size_index;
			SubpixelOrder subpixel_order;
			Rotation rotation;
			int width;
			int height;
			int mwidth;
			int mheight;
		} ;


		/* internal representation is private to the library */
		typedef struct _XRRScreenConfiguration XRRScreenConfiguration;	

		Bool XRRQueryExtension (Display *dpy, int *event_basep, int *error_basep);
		Status XRRQueryVersion (Display *dpy,
						int     *major_versionp,
						int     *minor_versionp);

		XRRScreenConfiguration *XRRGetScreenInfo (Display *dpy,
							  Drawable draw);
		    
		void XRRFreeScreenConfigInfo (XRRScreenConfiguration *config);

		/* 
		 * Note that screen configuration changes are only permitted if the client can
		 * prove it has up to date configuration information.  We are trying to
		 * insist that it become possible for screens to change dynamically, so
		 * we want to ensure the client knows what it is talking about when requesting
		 * changes.
		 */
		Status XRRSetScreenConfig (Display *dpy, 
					   XRRScreenConfiguration *config,
					   Drawable draw,
					   int size_index,
					   Rotation rotation,
					   Time timestamp);

		/* added in v1.1, sorry for the lame name */
		Status XRRSetScreenConfigAndRate (Display *dpy, 
						  XRRScreenConfiguration *config,
						  Drawable draw,
						  int size_index,
						  Rotation rotation,
						  short rate,
						  Time timestamp);


		Rotation XRRConfigRotations(XRRScreenConfiguration *config, Rotation *current_rotation);

		Time XRRConfigTimes (XRRScreenConfiguration *config, Time *config_timestamp);

		XRRScreenSize *XRRConfigSizes(XRRScreenConfiguration *config, int *nsizes);

		short *XRRConfigRates (XRRScreenConfiguration *config, int sizeID, int *nrates);

		SizeID XRRConfigCurrentConfiguration (XRRScreenConfiguration *config, 
						  Rotation *rotation);
		    
		short XRRConfigCurrentRate (XRRScreenConfiguration *config);

		int XRRRootToScreen(Display *dpy, Window root);

		/* 
		 * returns the screen configuration for the specified screen; does a lazy
		 * evalution to delay getting the information, and caches the result.
		 * These routines should be used in preference to XRRGetScreenInfo
		 * to avoid unneeded round trips to the X server.  These are new
		 * in protocol version 0.1.
		 */


		XRRScreenConfiguration *XRRScreenConfig(Display *dpy, int screen);
		XRRScreenConfiguration *XRRConfig(Screen *screen);
		void XRRSelectInput(Display *dpy, Window window, int mask);

		/* 
		 * the following are always safe to call, even if RandR is not implemented 
		 * on a screen 
		 */


		Rotation XRRRotations(Display *dpy, int screen, Rotation *current_rotation);
		XRRScreenSize *XRRSizes(Display *dpy, int screen, int *nsizes);
		short *XRRRates (Display *dpy, int screen, int sizeID, int *nrates);
		Time XRRTimes (Display *dpy, int screen, Time *config_timestamp);


		/* 
		 * intended to take RRScreenChangeNotify,  or 
		 * ConfigureNotify (on the root window)
		 * returns 1 if it is an event type it understands, 0 if not
		 */
		int XRRUpdateConfiguration(XEvent *event);
#endif
	}
}
