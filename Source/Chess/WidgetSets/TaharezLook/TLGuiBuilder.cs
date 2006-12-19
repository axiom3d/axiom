using System;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.TaharezLook
{
	/// <summary>
	/// Gui builder and widget factory for the Taharez CEGUI theme
	/// </summary>
	public class TLGuiBuilder : GuiBuilder
	{
		#region Constructors
		public TLGuiBuilder ()
		{

			Name = "Taharez";
			ImagesetName = "TaharezLook";
		}
		#endregion

		#region Overiden Factory Methods

		public override PushButton CreateButton (string name)
		{
			return new TLButton ("", name);
		}
		public override Checkbox CreateCheckbox (string name)
		{
			return new TLCheckbox ("", name);
		}
		public override RadioButton CreateRadioButton (string name)
		{
			return new TLRadioButton ("", name);
		}

		public override ComboBox CreateComboBox (string name)
		{
			return new TLComboBox ("", name);
		}
		public override Listbox CreateListBox (string name)
		{
			return new TLListbox ("", name);
		}
		public override MultiColumnList CreateGrid (string name)
		{
			return new TLMultiColumnList ("", name);
		}

		public override EditBox CreateEditBox (string name)
		{
			return new TLEditBox ("", name);
		}
		public override FrameWindow CreateFrameWindow (string name)
		{
			return new TLFrameWindow ("", name);
		}
		public override ProgressBar CreateProgressBar (string name)
		{
			return new TLProgressBar ("", name);
		}
		public override Slider CreateSlider (string name)
		{
			return new TLSlider ("", name);
		}
		public override TitleBar CreateTitleBar (string name)
		{
			return new TLTitleBar ("", name);
		}

		public override Scrollbar CreateVertScrollbar (string name)
		{
			return new TLMiniVerticalScrollbar ("", name);
		}
		public override Scrollbar CreateHorzScrollbar (string name)
		{
			return new TLMiniHorizontalScrollbar ("", name);
		}

		public override ListHeader CreateListHeader (string name)
		{
			return new TLListHeader ("", name);
		}
		public override ListHeaderSegment CreateListHeaderSegment (string name)
		{
			return new TLListHeaderSegment ("", name);
		}
		public override ComboDropList CreateComboDropList (string name)
		{
			return new TLComboDropList ("", name);
		}

		#endregion
	}
}
