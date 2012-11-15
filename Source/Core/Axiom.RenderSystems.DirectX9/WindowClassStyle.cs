using System;

namespace Axiom.RenderSystems.DirectX9
{
	[Flags]
	public enum WindowClassStyle : uint
	{
		VerticalRedraw = 0x0001,
		HorizontalRedraw = 0x0002,
		DoubleClicks = 0x0008,
		OwnDC = 0x0020,
		ClassDC = 0x0040,
		ParentDC = 0x0080,
		NoClose = 0x0200,
		SaveBits = 0x0800,
		ByteAlignClient = 0x1000,
		ByteAlignWindow = 0x2000,
		GlobalClass = 0x4000,
		IME = 0x00010000,
		DropShadow = 0x00020000
	}
}