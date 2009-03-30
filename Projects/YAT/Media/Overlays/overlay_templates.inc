// YAT - Yet Another Tetris clone
// Copyright (C) 2004  Christian Larsen (feedback@dword.dk)
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.

///////////////////////
// General templates //
///////////////////////

// GeneralTextTemplate: Template for text areas with small font.
template element TextArea(GeneralTextTemplate)
{
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
	top -0.25
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

/////////////////////////
// Highscore templates //
/////////////////////////

template element TextArea(HighscoreNameTemplate)
{
	horz_align left
	left 0.1
	font_name BerlinSans32
	char_height 0.04167
	colour_top 1.0 1.0 1.0
	colour_bottom 0.8 0.8 0.8
}

template element TextArea(HighscorePointsTemplate)
{
	horz_align right
	left -0.1
	font_name BerlinSans32
	char_height 0.04167
	alignment right
	colour_top 1.0 1.0 1.0
	colour_bottom 0.8 0.8 0.8
}
