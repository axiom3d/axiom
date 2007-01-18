//=============================================================================
// System  : Color Syntax Highlighter
// File    : Highlight.js
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 11/20/2006
// Note    : Copyright 2006, Eric Woodruff, All rights reserved
//
// This contains the script to expand and collapse the regions in the
// syntax highlighted code.
//
//=============================================================================

function HighlightExpandCollapse(showId, hideId)
{
    var showSpan = document.getElementById(showId),
        hideSpan = document.getElementById(hideId);
    
    showSpan.style.display = "inline";
    hideSpan.style.display = "none";
}
