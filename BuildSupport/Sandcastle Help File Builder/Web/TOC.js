//=============================================================================
// System  : Sandcastle Help File Builder
// File    : TOC.js
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 12/14/2006
// Note    : Copyright 2006, Eric Woodruff, All rights reserved
// Compiler: JavaScript
//
// A few methods to implement a simple tree view for the table of content.
//
// This file may be freely used for any purpose including commercial
// applications PROVIDING that this notice and the author's name and all
// copyright notices remain intact.  In addition, altered source versions of
// this file must be clearly marked as such.
//
// This code is provided "as is" with no warranty either express or implied.
// The author accepts no liability for any damage or loss of business that
// this product may cause.
//
// Version     Date     Who  Comments
// ============================================================================
// 1.3.0.0  09/12/2006  EFW  Created the code
//=============================================================================

var lastNode;

// Resize the tree div so that it fills the frame
function ResizeTree()
{
    var x, y, newWidth, newHeight,
        divOpts = document.getElementById("divOpts"),
        divTree = document.getElementById("divTree");

    if(self.innerHeight)    // All but IE
    {
        x = self.innerWidth;
        y = self.innerHeight;
    }
    else    // IE - Strict
        if(document.documentElement && document.documentElement.clientHeight)
        {
            x = document.documentElement.clientWidth;
            y = document.documentElement.clientHeight;
        }
        else    // Everything else
            if(document.body)
            {
                x = document.body.clientWidth;
                y = document.body.clientHeight;
            }

    newWidth = x - 6;
    newHeight = y - parseInt(divOpts.style.height) - 30;

    if(newWidth < 10)
        newWidth = 10;

    if(newHeight < 50)
        newHeight = 50;

    divTree.style.width = newWidth;
    divTree.style.height = newHeight;
}

// Synchronize the table of content with the selected page if possible
function SyncTOC()
{
    var idx, anchor, base, href, url, anchors, treeNode, saveNode;

    base = window.parent.frames["TOC"].document.URL.replace(/\\/g, "/");
    base = base.substr(0, base.lastIndexOf("/") + 1);

    if(base.substr(0, 5) == "file:" && base.substr(0, 8) != "file:///")
        base = base.replace("file://", "file:///");

    url = GetCurrentUrl();
    if(url == "")
        return false;

    if(url.substr(0, 5) == "file:" && url.substr(0, 8) != "file:///")
        url = url.replace("file://", "file:///");

    while(true)
    {
        anchors = document.getElementById("divTree").getElementsByTagName("A");
        anchor = null;

        for(idx = 0; idx < anchors.length; idx++)
        {
            href = anchors[idx].href;

            if(href.substring(0, 7) != 'http://' &&
              href.substring(0, 8) != 'https://' &&
              href.substring(0, 7) != 'file://')
                href = base + href;

            if(href == url)
            {
                anchor = anchors[idx];
                break;
            }
        }

        if(anchor == null)
        {
            // If it contains a "#", strip anything after that and try again
            if(url.indexOf("#") != -1)
            {
                url = url.substr(0, url.indexOf("#"));
                continue;
            }

            return;
        }

        break;
    }

    // If found, select it and find the parent tree node
    SelectNode(anchor);
    saveNode = anchor;
    lastNode = null;

    while(anchor != null)
    {
        if(anchor.className == "TreeNode")
        {
            treeNode = anchor;
            break;
        }

        anchor = anchor.parentNode;
    }

    // Expand it and all of its parents
    while(anchor != null)
    {
        Expand(anchor);

        anchor = anchor.parentNode;

        while(anchor != null)
        {
            if(anchor.className == "TreeNode")
                break;

            anchor = anchor.parentNode;
        }
    }

    lastNode = saveNode;

    // Scroll the node into view
    var treeDiv = document.getElementById('divTree');
    var windowTop = lastNode.offsetTop - treeDiv.offsetTop - treeDiv.scrollTop;
    var windowBottom = treeDiv.clientHeight - windowTop - lastNode.offsetHeight;

    if(windowTop < 0)
        treeDiv.scrollTop += windowTop - 30;
    else
        if(windowBottom < 0)
            treeDiv.scrollTop -= windowBottom - 30;
}

// Get the currently loaded URL from the other frame
function GetCurrentUrl()
{
    var base, url = "";

    try
    {
        url = window.parent.frames["Content"].document.URL.replace(/\\/g, "/");
    }
    catch(e)
    {
        // If this happens the user probably navigated to another frameset
        // that didn't make itself the topmost frameset and we don't have
        // control of the other frame anymore.  In that case, just reload
        // our index page.
        base = window.parent.frames["TOC"].document.URL.replace(/\\/g, "/");
        base = base.substr(0, base.lastIndexOf("/") + 1);

        if(base.substr(0, 5) == "file:" && base.substr(0, 8) != "file:///")
            base = base.replace("file://", "file:///");

        top.location = base + "Index.html";
    }

    return url;
}

// Expand or collapse all nodes
function ExpandOrCollapseAll(expandNodes)
{
    var divIdx, childIdx, img, divs = document.getElementsByTagName("DIV");
    var childNodes, child, div, link, img;

    for(divIdx = 0; divIdx < divs.length; divIdx++)
        if(divs[divIdx].className == "Hidden" ||
          divs[divIdx].className == "Visible")
        {
            childNodes = divs[divIdx].parentNode.childNodes;

            for(childIdx = 0; childIdx < childNodes.length; childIdx++)
            {
                child = childNodes[childIdx];

                if(child.className == "TreeNodeImg")
                    img = child;

                if(child.className == "Hidden" || child.className == "Visible")
                {
                    div = child;
                    break;
                }
            }

            if(div.className == "Visible" && !expandNodes)
            {
                div.className = "Hidden";
                img.src = "Collapsed.gif";
            }
            else
                if(div.className == "Hidden" && expandNodes)
                {
                    div.className = "Visible";
                    img.src = "Expanded.gif";
                }
        }
}

// Toggle the state of the specified node
function Toggle(node)
{
    var i, childNodes, child, div, link;

    childNodes = node.parentNode.childNodes;

    for(i = 0; i < childNodes.length; i++)
    {
        child = childNodes[i];

        if(child.className == "Hidden" || child.className == "Visible")
        {
            div = child;
            break;
        }
    }

    if(div.className == "Visible")
    {
        div.className = "Hidden";
        node.src = "Collapsed.gif";
    }
    else
    {
        div.className = "Visible";
        node.src = "Expanded.gif";
    }
}

// Expand the selected node
function Expand(node)
{
    var i, childNodes, child, div, img;

    // If not valid, don't bother
    if(GetCurrentUrl() == "")
        return false;

    if(node.tagName == "A")
        childNodes = node.parentNode.childNodes;
    else
        childNodes = node.childNodes;

    for(i = 0; i < childNodes.length; i++)
    {
        child = childNodes[i];

        if(child.className == "TreeNodeImg")
            img = child;

        if(child.className == "Hidden" || child.className == "Visible")
        {
            div = child;
            break;
        }
    }

    if(lastNode != null)
        lastNode.className = "UnselectedNode";

    div.className = "Visible";
    img.src = "Expanded.gif";

    if(node.tagName == "A")
    {
        node.className = "SelectedNode";
        lastNode = node;
    }

    return true;
}

// Set the style of the specified node to "selected"
function SelectNode(node)
{
    // If not valid, don't bother
    if(GetCurrentUrl() == "")
        return false;

    if(lastNode != null)
        lastNode.className = "UnselectedNode";

    node.className = "SelectedNode";
    lastNode = node;

    return true;
}
