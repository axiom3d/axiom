using System;

namespace Axiom.RenderSystems.DirectX9
{
	[Flags]
	public enum WindowStyles : uint
	{
		Overlapped = 0x00000000,
		Popup = 0x80000000,
		Child = 0x40000000,
		Minimize = 0x20000000,
		Visible = 0x10000000,
		Disabled = 0x08000000,
		ClipSiblings = 0x04000000,
		ClipChildren = 0x02000000,
		Maximize = 0x01000000,
		Border = 0x00800000,
		DialogFrame = 0x00400000,
		VerticalScroll = 0x00200000,
		HorizontalScroll = 0x00100000,
		SystemMenu = 0x00080000,
		ThickFrame = 0x00040000,
		Group = 0x00020000,
		TabStop = 0x00010000,

		MinimizeBox = 0x00020000,
		MaximizeBox = 0x00010000,

		Caption = Border | DialogFrame,
		Tiled = Overlapped,
		Iconic = Minimize,
		SizeBox = ThickFrame,
		TiledWindow = OverlappedWindow,

		OverlappedWindow = Overlapped | Caption | SystemMenu | ThickFrame | MinimizeBox | MaximizeBox,
		PopupWindow = Popup | Border | SystemMenu,
		ChildWindow = Child,

		//Extended Window Styles

		DialogModalFrame = 0x00000001,
		NoParentNotify = 0x00000004,
		TopMost = 0x00000008,
		AcceptFiles = 0x00000010,
		Transparent = 0x00000020,


		//#if(WINVER >= 0x0400)

		MdiChild = 0x00000040,
		ToolWindow = 0x00000080,
		WindowEdge = 0x00000100,
		ClientEdge = 0x00000200,
		ContextHelp = 0x00000400,

		Right = 0x00001000,
		Left = 0x00000000,
		RightToLeftReading = 0x00002000,
		LeftToRightReading = 0x00000000,
		LeftScrollbar = 0x00004000,
		RightScrollbar = 0x00000000,

		ControlParent = 0x00010000,
		StaticEdge = 0x00020000,
		AppWindow = 0x00040000,

		OverlappedWindowEdge = ( WindowEdge | ClientEdge ),
		PaletteWindow = ( WindowEdge | ToolWindow | TopMost ),

		//#endif /* WINVER >= 0x0400 */

		//#if(WIN32WINNT >= 0x0500)

		Layered = 0x00080000,

		//#endif /* WIN32WINNT >= 0x0500 */

		//#if(WINVER >= 0x0500)

		NoInheritLayout = 0x00100000, // Disable inheritence of mirroring by children
		LaoutRightToLeft = 0x00400000, // RightEx to left mirroring

		//#endif /* WINVER >= 0x0500 */

		//#if(WIN32WINNT >= 0x0500)

		Composited = 0x02000000,
		NoActivate = 0x08000000

		//#endif /* WIN32WINNT >= 0x0500 */
	}
}