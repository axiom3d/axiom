using System;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.WindowsLook
{
	/// <summary>
	/// Gui builder and widget factory for the Windows CEGUI theme
	/// </summary>
	public class WLGuiBuilder : GuiBuilder
	{

		#region Constructors
		public WLGuiBuilder ()
		{
			Name = "Windows";
			ImagesetName = "WindowsLook";
		}
		#endregion

		#region Overiden Factory Methods

		public override PushButton CreateButton (string name)
		{
			return new WLButton ("", name);
		}
		public override Checkbox CreateCheckbox (string name)
		{
			return new WLCheckbox ("", name);
		}
		public override RadioButton CreateRadioButton (string name)
		{
			return new WLRadioButton ("", name);
		}

		public override ComboBox CreateComboBox (string name)
		{
			return new WLComboBox ("", name);
		}
		public override Listbox CreateListBox (string name)
		{
			return new WLListbox ("", name);
		}
		public override MultiColumnList CreateGrid (string name)
		{
			return new WLMultiColumnList ("", name);
		}

		public override EditBox CreateEditBox (string name)
		{
			return new WLEditBox ("", name);
		}
		public override FrameWindow CreateFrameWindow (string name)
		{
			return new WLFrameWindow ("", name);
		}
		public override ProgressBar CreateProgressBar (string name)
		{
			return new WLProgressBar ("", name);
		}
		public override Slider CreateSlider (string name)
		{
			return new WLSlider ("", name);
		}
		public override TitleBar CreateTitleBar (string name)
		{
			return new WLTitleBar ("", name);
		}

		public override Scrollbar CreateVertScrollbar (string name)
		{
			return new WLVerticalScrollbar ("", name);
		}
		public override Scrollbar CreateHorzScrollbar (string name)
		{
			return new WLHorizontalScrollbar ("", name);
		}

		public override ListHeader CreateListHeader (string name)
		{
			return new WLListHeader ("", name);
		}
		public override ListHeaderSegment CreateListHeaderSegment (string name)
		{
			return new WLListHeaderSegment ("", name);
		}

		public override ComboDropList CreateComboDropList (string name)
		{
			return new WLComboDropList ("", name);
		}

		#endregion
	}
}
