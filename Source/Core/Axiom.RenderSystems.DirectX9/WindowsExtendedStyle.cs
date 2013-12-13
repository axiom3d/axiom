using System;

namespace Axiom.RenderSystems.DirectX9
{
	[Flags]
	public enum WindowsExtendedStyle : uint
	{
		DialogModalFrame = 0x00000001,
		NoParentNotify = 0x00000004,
		TopMost = 0x00000008,
		AcceptFiles = 0x00000010,
		Transparent = 0x00000020,
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
		OverlappedWindow = ( WindowEdge | ClientEdge ),
		PaletteWindow = ( WindowEdge | ToolWindow | TopMost )
	}
}