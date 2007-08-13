//=============================================================================
// System  : Color Syntax Highlighter
// File    : Highlight.js
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 03/09/2007
// Note    : Copyright 2006, Eric Woodruff, All rights reserved
//
// This contains the script to expand and collapse the regions in the
// syntax highlighted code.
//
//=============================================================================

// Expand/collapse a region
function HighlightExpandCollapse(showId, hideId)
{
    var showSpan = document.getElementById(showId),
        hideSpan = document.getElementById(hideId);
    
    showSpan.style.display = "inline";
    hideSpan.style.display = "none";
}

// Copy the code if Enter or Space is hit with the image focused
function CopyColorizedCodeCheckKey(titleDiv, eventObj)
{
    if(eventObj != undefined && (eventObj.keyCode == 13 ||
      eventObj.keyCode == 32))
        CopyColorizedCode(titleDiv);
}

// Copy the code from a colorized code block to the clipboard.
function CopyColorizedCode(titleDiv)
{
    var preTag, idx, line, block, htmlLines, lines, codeText, hasLineNos, hasRegions;
    var reLineNo = /^\s*\d{1,4}/;
    var reRegion = /^\s*\d{1,4}\+.*?\d{1,4}-/;

    // Find the <pre> tag containing the code.  It should be in the next
    // element or one of its children.
    block = titleDiv.nextSibling;

    while(block.nodeName == "#text")
        block = block.nextSibling;

    while(block.tagName != "PRE")
    {
        block = block.firstChild;

        while(block.nodeName == "#text")
            block = block.nextSibling;
    }

    if(block.innerText != undefined)
        codeText = block.innerText;
    else
        codeText = block.textContent;

    hasLineNos = block.innerHTML.indexOf("highlight-lineno");
    hasRegions = block.innerHTML.indexOf("highlight-collapsebox");
    htmlLines = block.innerHTML.split("\n");
    lines = codeText.split("\n");

    // Remove the line numbering and collapsible regions if present
    if(hasLineNos != -1 || hasRegions != -1)
    {
        codeText = "";

        for(idx = 0; idx < lines.length; idx++)
        {
            line = lines[idx];

            if(hasRegions && reRegion.test(line))
                line = line.replace(reRegion, "");
            else
            {
                line = line.replace(reLineNo, "");

                // Lines in expanded blocks have an extra space
                if(htmlLines[idx].indexOf("highlight-expanded") != -1 ||
                  htmlLines[idx].indexOf("highlight-endblock") != -1)
                    line = line.substr(1);
            }

            codeText += line;

            // Not all browsers keep the line feed when split
            if(line[line.length - 1] != "\n")
                codeText += "\n";
        }
    }

    if(window.clipboardData)
        window.clipboardData.setData("Text", codeText);
    else
        alert("Copy to Clipboard is not supported by this browser");
}
