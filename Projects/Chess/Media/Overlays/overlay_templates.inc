///////////////////////
// General templates //
///////////////////////

// GeneralTextTemplate: Template for text areas with small font.
template element TextArea(GeneralTextTemplate)
{
	horz_align center
	alignment center
	font_name BerlinSans32	
	char_height 0.04167
	colour_top 1.0 1.0 1.0
	colour_bottom 0.8 0.8 0.8
}

////////////////////
// Menu templates //
////////////////////

// MenuBackgroundTemplate: Semi-transparent panel filling the entire screen.
template container Panel(MenuBackgroundTemplate)
{
	left 0.0
	top 0.0
	width 1.0
	height 1.0
	material Menu/MenuBackground
}

// MenuPanelTemplate: Semi-transparent panel for the menu itself.
template container Panel(MenuPanelTemplate)
{
	horz_align center
	vert_align center
	left -0.25
	top -0.30
	width 0.5
	height 0.5
	material Menu/MenuPanel
}

// MenuLogoTemplate: Game logo displayed at the top of menus.
template container Panel(MenuLogoTemplate)
{
	horz_align center
	left -0.25
	top -0.16667
	width 0.5
	height 0.33333
	material Menu/MenuLogo
}

// MenuItemTemplate: Template for menu items.
template element TextArea(MenuItemTemplate)
{
	horz_align center
	font_name BerlinSans64
	char_height 0.08333
	alignment center
	colour_top 1.0 1.0 1.0
	colour_bottom 0.8 0.8 0.8
}
