window.onload=LoadPage;
window.onunload=Window_Unload;
//window.onresize=ResizeWindow;
window.onbeforeprint = set_to_print;
window.onafterprint = reset_form;

var vbDeclaration;
var vbUsage;
var csLang;
var cLang;
var jsharpLang;
var jsLang;
// AlanSm: start custom code
var xamlLang;
// AlanSm: end custom code

var scrollPos = 0;

var inheritedMembers;
var protectedMembers;
var netcfMembersOnly;
var netXnaMembersOnly;

// Initialize array of section states

var sectionStates = new Array();
var sectionStatesInitialized = false;
var allCollapsed = false;
var allExpanded = false;

//Hide sample source in select element
function HideSelect()
{
	var selectTags = document.getElementsByTagName("SELECT");
	var spanEles = document.getElementsByTagName("span");
	var i = 10;
	var m;
	
	if (selectTags.length != null || selectTags.length >0)
	{
		for (n=0; n<selectTags.length; n++)
		{			
			var lan = selectTags(n).getAttribute("id").substr("10");
			//hide the first select that is on
			switch (lan.toLowerCase())
			{
				case "visualbasic":
					//alert(lan);
					for (m=0; m<spanEles.length; m++)
					{					
						if (spanEles[m].getAttribute("codeLanguage") == "VisualBasic" && spanEles[m].style.display != "none" && n <i)
							i = n;				
					}
					break;
				case "visualbasicdeclaration":
					for (m=0; m<spanEles.length; m++)
					{					
						if (spanEles[m].getAttribute("codeLanguage") == "VisualBasicDeclaration" && spanEles[m].style.display != "none" && n < i)
							i = n;
					}
					break;
				case "visualbasicusage":
					//alert(lan);
					for (m=0; m<spanEles.length; m++)
					{					
						if (spanEles[m].getAttribute("codeLanguage") == "VisualBasicUsage" && spanEles[m].style.display != "none" && n <i)
							i = n;				
					}
					break;
				case "csharp":
					for (m=0; m<spanEles.length; m++)
					{					
						if (spanEles[m].getAttribute("codeLanguage") == "CSharp" && spanEles[m].style.display != "none" && n < i)
							i = n;
					}
					break;
				case "managedcplusplus":
					for (m=0; m<spanEles.length; m++)
					{					
						if (spanEles[m].getAttribute("codeLanguage") == "ManagedCPlusPlus" && spanEles[m].style.display != "none" && n < i)
							i = n;
					}
					break;
				case "jsharp":
					for (m=0; m<spanEles.length; m++)
					{					
						if (spanEles[m].getAttribute("codeLanguage") == "JSharp" && spanEles[m].style.display != "none" && n < i)
							i = n;
					}
					break;
				case "jscript":
					for (m=0; m<spanEles.length; m++)
					{					
						if (spanEles[m].getAttribute("codeLanguage") == "JScript" && spanEles[m].style.display != "none" && n < i)
							i = n;
					}
					break;				
				case "xaml":
					//alert(lan);
					for (m=0; m<spanEles.length; m++)
					{					
						if (spanEles[m].getAttribute("codeLanguage") == "XAML" && spanEles[m].style.display != "none" && n <i)
							i = n;				
					}
					break;
			}							
		}
		if (i != 10)		
			selectTags(i).style.visibility = "hidden";
	}
	else{ alert("Not found!");}	
}

function UnHideSelect()
{		
	var selectTags = document.getElementsByTagName("SELECT");
	var n;
	
	//un-hide all the select sections
	if (selectTags.length != null || selectTags.length >0)
	{
		for (n=0; n<selectTags.length; n++)
			selectTags(n).style.visibility = "visible";
	}	
}

function InitSectionStates()
{
    // SectionStates has the format:
    //
    //     firstSectionId:state;secondSectionId:state;thirdSectionId:state; ... ;lastSectionId:state
    //
    // where state is either "e" (expanded) or "c" (collapsed)
    
	allExpanded = Load("AllExpanded");
	allCollapsed = Load("AllCollapsed");
	
	if (allExpanded == null || allCollapsed == null)
	{
	    allExpanded = true;
	    allCollapsed = false;
	}
	
	var states = Load("SectionStates");
	if (states == null || states == "") return;
	
    var start = 0;
    var end;
    var section;
    var state;
    
    while (start < states.length)
    {
        end = states.indexOf(":", start);
        
        section = states.substring(start, end);
        
        start = end + 1;
        end = states.indexOf(";", start);
        if (end == -1) end = states.length;
        
        state = states.substring(start, end);
        
    	sectionStates[section] = (allExpanded) ? "e" : state;
    	
    	start = end + 1;
    }
}

var noReentry = false;

function OnLoadImage(eventObj)
{
    if (noReentry) return;
    
    if (!sectionStatesInitialized) 
    {
		globals = GetGlobals();
		InitSectionStates(); 
		sectionStatesInitialized = true; 
    }
   
    var elem;
    if(document.all) elem = eventObj.srcElement;
    else elem = eventObj.target;
        
    if (sectionStates[elem.id] == null) sectionStates[elem.id] = GetDefaultState();
        
    if (ShouldExpand(elem))
    {
        noReentry = true;
        
        try
        {
            var collapseImage = document.getElementById("collapseImage");
			elem.src = collapseImage.src;
			elem.alt = collapseImage.alt;
			
			ExpandSection(elem);
        }
        catch (e)
        {
        }
        
        noReentry = false;
    }
}

function GetDefaultState()
{
    if (allExpanded) return "e";
    if (allCollapsed) return "c";
    
    return "e";
}

function ShouldExpand(elem)
{
    return (allExpanded || sectionStates[elem.id] == "e");
}


/*	
**********
**********   Begin
**********
*/

function LoadPage()
{
	// If not initialized, grab the DTE.Globals object
	if (globals == null) globals = GetGlobals();
	
	// show correct language
	LoadLanguages();
	LoadMembersOptions();
	
	Set_up_checkboxes();

	DisplayLanguages();
	
	DisplayFilteredMembers();
		
	ChangeMembersOptionsFilterLabel();
	
	SetCollapseAll();

//	ResizeWindow();
	// split screen
	var screen = new SplitScreen('header', 'mainSection');

	// filtering dropdowns
	if (document.getElementById('languageSpan') != null) {
		var languageMenu = new Dropdown('languageFilterToolTip', 'languageSpan');
	}
	if (document.getElementById('membersOptionsFilterToolTip') != null) {
		var languageMenu = new Dropdown('membersOptionsFilterToolTip', 'membersOptionsSpan');
	}

    var mainSection = document.getElementById("mainSection");
	
	// vs70.js did this to allow up/down arrow scrolling, I think
	try { mainSection.setActive(); } catch(e) { }

	//set the scroll position
	try{mainSection.scrollTop = scrollPos;}
	catch(e){}
}

function Window_Unload()
{
	SaveLanguages();
	SaveMembersOptions();
	SaveSections();
}

/*
function ResizeWindow()
{
	if (document.body.clientWidth==0) return;
	var header = document.all.item("header");
	var mainSection = document.all.item("mainSection");
	if (mainSection == null) return;
	
	
	document.body.scroll = "no"
	mainSection.style.overflow= "auto";
	header.style.width= document.body.offsetWidth - 2;
	//mainSection.style.paddingRight = "20px"; // Width issue code
	mainSection.style.width= document.body.offsetWidth - 2;
	mainSection.style.top=0;  
	if (document.body.offsetHeight > header.offsetHeight + 10)
		mainSection.style.height= document.body.offsetHeight - (header.offsetHeight + 2);
	else
		mainSection.style.height=0;
	
	try
	{
		mainSection.setActive();
	}
	catch(e)
	{
	}
}
*/

function set_to_print()
{
	//breaks out of divs to print
	var i;

	if (window.text)document.all.text.style.height = "auto";
			
	for (i=0; i < document.all.length; i++)
	{
		if (document.all[i].tagName == "body")
		{
			document.all[i].scroll = "yes";
		}
		if (document.all[i].id == "header")
		{
			document.all[i].style.margin = "0px 0px 0px 0px";
			document.all[i].style.width = "100%";
		}
		if (document.all[i].id == "mainSection")
		{
			document.all[i].style.overflow = "visible";
			document.all[i].style.top = "5px";
			document.all[i].style.width = "100%";
			document.all[i].style.padding = "0px 10px 0px 30px";
		}
	}
}

function reset_form()
{
	//returns to the div nonscrolling region after print
	 document.location.reload();
}

function Set_up_checkboxes()
{
	var checkbox;
	
	checkbox = document.getElementById("vbDeclarationCheckbox");
	if(checkbox != null)
	{
		if(vbDeclaration == "on")
			checkbox.checked = true;
		else
			checkbox.checked = false;
	}
	
	checkbox = document.getElementById("vbUsageCheckbox");
	if(checkbox != null)
	{
		if(vbUsage == "on")
			checkbox.checked = true;
		else
			checkbox.checked = false;
	}
		
	checkbox = document.getElementById("csCheckbox");
	if(checkbox != null)
	{
		if(csLang == "on")
			checkbox.checked = true;
		else
			checkbox.checked = false;
	}
		
	checkbox = document.getElementById("cCheckbox");
	if(checkbox != null)
	{
		if(cLang == "on")
			checkbox.checked = true;
		else
			checkbox.checked = false;
	}
	
	checkbox = document.getElementById("jsharpCheckbox");
	if(checkbox != null)
	{
		if(jsharpLang == "on")
			checkbox.checked = true;
		else
			checkbox.checked = false;
	}
		
	checkbox = document.getElementById("jsCheckbox");
	if(checkbox != null)
	{
		if(jsLang == "on")
			checkbox.checked = true;
		else
			checkbox.checked = false;
	}
	
	checkbox = document.getElementById("inheritedCheckbox");
	if(checkbox != null)
	{
		if(inheritedMembers == "on")
			checkbox.checked = true;
		else
			checkbox.checked = false;
	}
	
	checkbox = document.getElementById("protectedCheckbox");
	if(checkbox != null)
	{
		if(protectedMembers == "on")
			checkbox.checked = true;
		else
			checkbox.checked = false;
	}
	
	checkbox = document.getElementById("netcfCheckbox");
	if(checkbox != null)
	{
		if(netcfMembersOnly == "on")
			checkbox.checked = true;
		else
			checkbox.checked = false;
	}
	
// AlanSm: start custom code
	checkbox = document.getElementById("xamlCheckbox");
	if(checkbox != null)
	{
		if(xamlLang == "on")
			checkbox.checked = true;
		else
			checkbox.checked = false;
	}
// AlanSm: end custom code
}

/*	
**********
**********   End
**********
*/


/*	
**********
**********   Begin Language Filtering
**********
*/

function SetLanguage(key)
{
    var i = 0;
	if(vbDeclaration == "on")
		i++;
	if(vbUsage == "on")
		i++;
	if(csLang == "on")
		i++;
	if(cLang == "on")
		i++;
	if(jsharpLang == "on")
		i++;
	if(jsLang == "on")
		i++;
// AlanSm: start custom code
	if(xamlLang == "on")
		i++;
// AlanSm: end custom code
	
	if(key.id == "vbDeclarationCheckbox")
	{
	    if(vbDeclaration == "on")
		{
		    if(i == 1)
			{
			    key.checked = true;
				return;
			}
			vbDeclaration = "off";
		}
		else
			vbDeclaration = "on";
	}
	if(key.id == "vbUsageCheckbox")
	{
		if(vbUsage == "on")
		{
			if(i == 1)
			{
				key.checked = true;
				return;
			}
				
			vbUsage = "off";
		}
		else
			vbUsage = "on";
	}
	if(key.id == "csCheckbox")
	{
		if(csLang == "on")
		{
			if(i == 1)
			{
				key.checked = true;
				return;
			}
			
			csLang = "off";
		}
		else
			csLang = "on";
	}
	if(key.id == "cCheckbox")
	{
		if(cLang == "on")
		{
			if(i == 1)
			{
				key.checked = true;
				return;
			}
				
			cLang = "off";
		}
		else
			cLang = "on";
	}
	if(key.id == "jsharpCheckbox")
	{
		if(jsharpLang == "on")
		{
			if(i == 1)
			{
				key.checked = true;
				return;
			}
				
			jsharpLang = "off";
		}
		else
			jsharpLang = "on";
	}
	if(key.id == "jsCheckbox")
	{
		if(jsLang == "on")
		{
			if(i == 1)
			{
				key.checked = true;
				return;
			}
				
			jsLang = "off";
		}
		else
			jsLang = "on";
	}
// AlanSm: start custom code
	if(key.id == "xamlCheckbox")
	{
		if(xamlLang == "on")
		{
			if(i == 1)
			{
				key.checked = true;
				return;
			}
				
			xamlLang = "off";
		}
		else
			xamlLang = "on";
	}
// AlanSm: end custom code
	
	DisplayLanguages();
}

function DisplayLanguages()
{
	var spanElements = document.getElementsByTagName("span");
	var x = 0;
	if(vbDeclaration == "on")
		x++;
	if(vbUsage == "on")
		x++;
	if(csLang == "on")
		x++;
	if(cLang == "on")
		x++;
	if(jsharpLang == "on")
		x++;
	if(jsLang == "on")
		x++;
// AlanSm: start custom code
	if(xamlLang == "on")
		x++;
// AlanSm: end custom code

	var i;
	for(i = 0; i < spanElements.length; ++i)
	{
	    if(spanElements[i].getAttribute("codeLanguage") != null)
		{
		    if(spanElements[i].getAttribute("codeLanguage") == "VisualBasic")
			{
				if(vbDeclaration == "on" || vbUsage == "on")
					spanElements[i].style.display = "";
				else
					spanElements[i].style.display = "none";
			}
			if(spanElements[i].getAttribute("codeLanguage") == "VisualBasicDeclaration")
			{
			
				if(vbDeclaration == "on")
					spanElements[i].style.display = "";
				else{
				
					spanElements[i].style.display = "none";
					}
			}
			if(spanElements[i].getAttribute("codeLanguage") == "VisualBasicUsage")
			{
				if(vbUsage == "on")
					spanElements[i].style.display = "";
				else
					spanElements[i].style.display = "none";
			}
			if(spanElements[i].getAttribute("codeLanguage") == "CSharp")
			{
				if(csLang == "on")
					spanElements[i].style.display = "";
				else
					spanElements[i].style.display = "none";
			}
			if(spanElements[i].getAttribute("codeLanguage") == "ManagedCPlusPlus")
			{
				if(cLang == "on")
					spanElements[i].style.display = "";
				else
					spanElements[i].style.display = "none";
			}
			if(spanElements[i].getAttribute("codeLanguage") == "JSharp")
			{
				if(jsharpLang == "on")
					spanElements[i].style.display = "";
				else
					spanElements[i].style.display = "none";
			}
			if(spanElements[i].getAttribute("codeLanguage") == "JScript")
			{
				if(jsLang == "on")
					spanElements[i].style.display = "";
				else
					spanElements[i].style.display = "none";
			}
// AlanSm: start custom code
			if(spanElements[i].getAttribute("codeLanguage") == "XAML")
			{
				if(xamlLang == "on")
					spanElements[i].style.display = "";
				else
					spanElements[i].style.display = "none";
			}
// AlanSm: end custom code
			
			if(spanElements[i].getAttribute("codeLanguage") == "NotVisualBasicUsage")
			{
				if((x == 1) && (vbUsage == "on"))
				{
					spanElements[i].style.display = "none";
				}
				else
				{
					spanElements[i].style.display = "";
				}
			}
		}
	}
	ChangeLanguageFilterLabel();
}

function ChangeLanguageFilterLabel()
{	
	var i = 0;
	if(vbDeclaration == "on")
		i++;
	if(vbUsage == "on")
		i++;
	if(csLang == "on")
		i++;
	if(cLang == "on")
		i++;
	if(jsharpLang == "on")
		i++;
	if(jsLang == "on")
		i++;
// AlanSm: start custom code
	if(xamlLang == "on")
		i++;
// AlanSm: end custom code
		
	var labelElement;
	
	labelElement = document.getElementById("showAllLabel");
	
	if(labelElement == null)
		return;
		
	labelElement.style.display = "none";
	
	labelElement = document.getElementById("multipleLabel");
	labelElement.style.display = "none";
	
	labelElement = document.getElementById("vbLabel");
	labelElement.style.display = "none";
	
	labelElement = document.getElementById("csLabel");
	labelElement.style.display = "none";
	
	labelElement = document.getElementById("cLabel");
	labelElement.style.display = "none";
	
	labelElement = document.getElementById("jsharpLabel");
	labelElement.style.display = "none";
	
	labelElement = document.getElementById("jsLabel");
	labelElement.style.display = "none";
	
// AlanSm: start custom code
	labelElement = document.getElementById("xamlLabel");
	labelElement.style.display = "none";
	
	if(i == 7)
// AlanSm: end custom code
	{
		labelElement = document.getElementById("showAllLabel");
		labelElement.style.display = "inline";
	}
// AlanSm: start custom code
	else if ((i > 1) && (i < 7))
// AlanSm: end custom code
	{
		if((i == 2) && ((vbDeclaration == "on") && (vbUsage == "on")))
		{
			labelElement = document.getElementById("vbLabel");
			labelElement.style.display = "inline";
		}
		else
		{
			labelElement = document.getElementById("multipleLabel");
			labelElement.style.display = "inline";
		}
	}
	else if (i == 1)
	{
		if(vbDeclaration == "on" || vbUsage == "on")
		{
			labelElement = document.getElementById("vbLabel");
			labelElement.style.display = "inline";
		}
		if(csLang == "on")
		{
			labelElement = document.getElementById("csLabel");
			labelElement.style.display = "inline";
		}
		if(cLang == "on")
		{
			labelElement = document.getElementById("cLabel");
			labelElement.style.display = "inline";
		}
		if(jsharpLang == "on")
		{
			labelElement = document.getElementById("jsharpLabel");
			labelElement.style.display = "inline";
		}
		if(jsLang == "on")
		{
			labelElement = document.getElementById("jsLabel");
			labelElement.style.display = "inline";
		}
// AlanSm: start custom code
		if(xamlLang == "on")
		{
			labelElement = document.getElementById("xamlLabel");
			labelElement.style.display = "inline";
		}
// AlanSm: end custom code
	}
}

function LoadLanguages()
{
	var value;
	value = Load("vbDeclaration");
	if(value == null)
		vbDeclaration = "on";
	else
		vbDeclaration = value;
		
	value = Load("vbUsage");
	if(value == null)
		vbUsage = "on";
	else
		vbUsage = value;
		
	value = Load("csLang");
	if(value == null)
		csLang = "on";
	else
		csLang = value;
		
	value = Load("cLang");
	if(value == null)
		cLang = "on";
	else
		cLang = value;
	
	value = Load("jsharpLang");
	if(value == null)
		jsharpLang = "on";
	else
		jsharpLang = value;
		
	value = Load("jsLang");
	if(value == null)
		jsLang = "on";
	else
		jsLang = value;

// AlanSm: start custom code
	value = Load("xamlLang");
	if(value == null)
		xamlLang = "on";
	else
		xamlLang = value;
// AlanSm: end custom code
}

function SaveLanguages()
{
	Save("vbDeclaration", vbDeclaration);
	Save("vbUsage", vbUsage);
	Save("csLang", csLang);
	Save("cLang", cLang);
	Save("jsharpLang", jsharpLang);
	Save("jsLang", jsLang);
// AlanSm: start custom code
	Save("xamlLang", xamlLang);
// AlanSm: end custom code
}

/*	
**********
**********   End Language Filtering
**********
*/


/*	
**********
**********   Begin Members Options Filtering
**********
*/

function SetMembersOptions(key)
{
	if(key.id == "inheritedCheckbox")
	{
		if(key.checked == true)
			inheritedMembers = "on";
		else
			inheritedMembers = "off";
	}
	if(key.id == "protectedCheckbox")
	{
		if(key.checked == true)
			protectedMembers = "on";
		else
			protectedMembers = "off";
	}
	if(key.id == "netcfCheckbox")
	{
		if(key.checked == true)
			netcfMembersOnly = "on";
		else
			netcfMembersOnly = "off";
	}
	if(key.id == "netXnaCheckbox")
	{
		if(key.checked == true)
			netXnaMembersOnly = "on";
		else
			netXnaMembersOnly = "off";
	}
	DisplayFilteredMembers();
	
	ChangeMembersOptionsFilterLabel();
}

function DisplayFilteredMembers()
{
	var iAllMembers = document.getElementsByTagName("tr");
	var i;
	
	for(i = 0; i < iAllMembers.length; ++i)
	{
		if (((iAllMembers[i].getAttribute("protected") == "true") && (protectedMembers == "off")) ||
		    ((iAllMembers[i].notSupportedOnXna == "true") && (netXnaMembersOnly == "on")) ||
			((iAllMembers[i].getAttribute("name") == "inheritedMember") && (inheritedMembers == "off")) ||
			((iAllMembers[i].getAttribute("notSupportedOn") == "netcf") && (netcfMembersOnly == "on")))
			iAllMembers[i].style.display = "none";
		else
			iAllMembers[i].style.display = "";
	}
}

function ChangeMembersOptionsFilterLabel()
{	

	var filtered = false;
	
	if((inheritedMembers=="off") || (protectedMembers=="off") || (netXnaMembersOnly == "on") || (netcfMembersOnly=="on"))
		filtered = true;
		
	var labelElement;
	
	labelElement = document.getElementById("showAllMembersLabel");
	
	if(labelElement == null)
		return;
		
	labelElement.style.display = "none";
	
	labelElement = document.getElementById("filteredMembersLabel");
	labelElement.style.display = "none";
	
	if(filtered)
	{
		labelElement = document.getElementById("filteredMembersLabel");
		labelElement.style.display = "inline";
	}
	else
	{
		labelElement = document.getElementById("showAllMembersLabel");
		labelElement.style.display = "inline";
	}
}

function LoadMembersOptions()
{
	var value;
	value = Load("inheritedMembers");
	if(value == null)
		inheritedMembers = "on";
	else
		inheritedMembers = value;
		
	value = Load("protectedMembers");
	if(value == null)
		protectedMembers = "on";
	else
		protectedMembers = value;
		
	value = Load("netcfMembersOnly");
	if(value == null)
		netcfMembersOnly = "off";
	else
		netcfMembersOnly = value;
	
	value = Load("netXnaMembersOnly");
	if(value == null)
		netXnaMembersOnly = "off";
	else
		netXnaMembersOnly = value;
}

function SaveMembersOptions()
{
	Save("inheritedMembers", inheritedMembers);
	Save("protectedMembers", protectedMembers);
	Save("netcfMembersOnly", netcfMembersOnly);
	Save("netXnaMembersOnly", netXnaMembersOnly);
}

/*	
**********
**********   End Members Options Filtering
**********
*/


/*	
**********
**********   Begin Expand/Collapse
**********
*/

function ExpandCollapse(imageItem)
{
    noReentry = true; // Prevent entry to OnLoadImage
    
	if (ItemCollapsed(imageItem.id) == true)
	{
        var collapseImage = document.getElementById("collapseImage");
		imageItem.src = collapseImage.src;
		imageItem.alt = collapseImage.alt;
		
		ExpandSection(imageItem);
		
		if (imageItem.id.indexOf("Family", 0) == 0)
		{
			protectedMembers = "on";
			Set_up_checkboxes();
			ChangeMembersOptionsFilterLabel();
		}
	}
	else
	{
        var expandImage = document.getElementById("expandImage");
		imageItem.src = expandImage.src;
		imageItem.alt = expandImage.alt;
		
		CollapseSection(imageItem);
	}
	
	allCollapsed = false;
	allExpanded = false;

	SetCollapseAll();
	
	noReentry = false;
}

function ExpandCollapseAll(imageItem)
{
    noReentry = true; // Prevent entry to OnLoadImage
    
	var imgElements = document.getElementsByName("toggleSwitch");
	var i;
    var expandAllImage = document.getElementById("expandAllImage");
	
	if (imageItem.src == expandAllImage.src)
	{
        var collapseAllImage = document.getElementById("collapseAllImage");
        var collapseImage = document.getElementById("collapseImage");
		imageItem.src = collapseAllImage.src;
		imageItem.alt = collapseAllImage.alt;

		for (i = 0; i < imgElements.length; ++i)
		{
			imgElements[i].src = collapseImage.src;
			imgElements[i].alt = collapseImage.alt;
			
			ExpandSection(imgElements[i]);
			
			if (imgElements[i].id.indexOf("Family", 0) == 0) protectedMembers = "on";
		}
		
		SetToggleAllLabel(false);
		
		allExpanded = true;
		allCollapsed = false;
	}
	else
	{
        var expandImage = document.getElementById("expandImage");
		imageItem.src = expandAllImage.src;
		imageItem.alt = expandAllImage.alt;

		for (i = 0; i < imgElements.length; ++i)
		{
			imgElements[i].src = expandImage.src;
			imgElements[i].alt = expandImage.alt;
			
			CollapseSection(imgElements[i]);
		}
		
		SetToggleAllLabel(true);
		
		allExpanded = false;
		allCollapsed = true;
	}
	
	noReentry = false;
}

function ExpandCollapse_CheckKey(imageItem, eventObj)
{
	if(eventObj.keyCode == 13)
		ExpandCollapse(imageItem);
}

function ExpandCollapseAll_CheckKey(imageItem, eventObj)
{
	if(eventObj.keyCode == 13)
		ExpandCollapseAll(imageItem);
}

function ExpandSection(imageItem)
{
	imageItem.parentNode.parentNode.nextSibling.style.display = "";
	sectionStates[imageItem.id] = "e";
}

function CollapseSection(imageItem)
{
	imageItem.parentNode.parentNode.nextSibling.style.display = "none";
	sectionStates[imageItem.id] = "c";
}

function SetCollapseAll()
{
	var imageElement = document.getElementById("toggleAllImage");
	
	if (imageElement == null) return;
	
	if (allCollapsed)
	{
        var expandAllImage = document.getElementById("expandAllImage");
		imageElement.src = expandAllImage.src;
		imageElement.alt = expandAllImage.alt;
	}
	else
	{
        var collapseAllImage = document.getElementById("collapseAllImage");
		imageElement.src = collapseAllImage.src;
		imageElement.alt = collapseAllImage.alt;
	}
	
	SetToggleAllLabel(allCollapsed);
}

function SetToggleAllLabel(collapseAll)
{
	var labelElement = document.getElementById("collapseAllLabel");
	
	if (labelElement == null) return;
		
	labelElement.style.display = "none";
	
	labelElement = document.getElementById("expandAllLabel");
	labelElement.style.display = "none";
	
	if (collapseAll)
	{
		labelElement = document.getElementById("expandAllLabel");
		labelElement.style.display = "inline";
	}
	else
	{
		labelElement = document.getElementById("collapseAllLabel");
		labelElement.style.display = "inline";
	}
}

function ItemCollapsed(imageId)
{
	return sectionStates[imageId] == "c";
}

function SaveSections()
{
    try
    {
        var states = "";
    
        for (var sectionId in sectionStates) states += sectionId + ":" + sectionStates[sectionId] + ";";

        Save("SectionStates", states.substring(0, states.length - 1));
    }
    catch (e)
    {
    }
    
	Save("AllExpanded", allExpanded);
	Save("AllCollapsed", allCollapsed);
}

function ShouldSave(imageId)
{
	var toggleName;
	
	if(imageId == "toggleAllImage")
		return false;
	
	toggleName = "procedureToggle";
	if(imageId.indexOf(toggleName, 0) == 0)
		return false;
		
	toggleName = "sectionToggle";
	if(imageId.indexOf(toggleName, 0) == 0)
		return false;
	
	return true;
}

function OpenSection(imageItem)
{
	if (sectionStates[imageItem.id] == "c") ExpandCollapse(imageItem);
}

/*	
**********
**********   End Expand/Collapse
**********
*/



/*	
**********
**********   Begin Copy Code
**********
*/

function CopyCode(key)
{
	var trElements = document.getElementsByTagName("tr");
	var i;
	for(i = 0; i < trElements.length; ++i)
	{
		if(key.parentNode.parentNode.parentNode == trElements[i].parentNode)
		{
			window.clipboardData.setData("Text", trElements[i].innerText);
		}
	}
}

function ChangeCopyCodeIcon(key)
{
	var i;
	var imageElements = document.getElementsByName("ccImage")
	for(i=0; i<imageElements.length; ++i)
	{
		if(imageElements[i].parentNode == key)
		{
			if(imageElements[i].src == copyImage.src)
			{
				imageElements[i].src = copyHoverImage.src;
				imageElements[i].alt = copyHoverImage.alt;
			}
			else
			{
				imageElements[i].src = copyImage.src;
				imageElements[i].alt = copyImage.alt;
			}
		}
	}
}

function CopyCode_CheckKey(key, eventObj)
{
	if(eventObj.keyCode == 13)
		CopyCode(key);
}

/*	
**********
**********   End Copy Code
**********
*/


/*	
**********
**********   Begin Maintain Scroll Position
**********
*/

function loadAll(){
	try 
	{
		scrollPos = allHistory.getAttribute("Scroll");
	}
	catch(e){}
}

function saveAll(){
	try
	{
		allHistory.setAttribute("Scroll", mainSection.scrollTop);
	}
	catch(e){}
}

/*	
**********
**********   End Maintain Scroll Position
**********
*/


/*	
**********
**********   Begin Send Mail
**********
*/

function formatMailToLink(anchor)
{
	var release = "Release: " + anchor.doc_Release;
	var topicId = "Topic ID: " + anchor.doc_TopicID;
	var topicTitle = "Topic Title: " + anchor.doc_TopicTitle;
	var url = "URL: " + document.URL;
	var browser = "Browser: " + window.navigator.userAgent;

	var crlf = "%0d%0a"; 
	var body = release + crlf + topicId + crlf + topicTitle + crlf + url + crlf + browser + crlf + crlf + "Comments:" + crlf + crlf;
	
	anchor.href = anchor.href + "&body=" + body;
}

/*	
**********
**********   End Send Mail
**********
*/


/*	
**********
**********   Begin Persistence
**********
*/

var globals;

function GetGlobals()
{
	var tmp;
	
	// Try to get VS implementation
	try { tmp = window.external.Globals; }
	catch (e) { tmp = null; }
	
	// Try to get DExplore implementation
	try { if (tmp == null) tmp = window.external.GetObject("DTE", "").Globals; }
	catch (e) { tmp = null; }
	
	return tmp;
}

function Load(key)
{
	try 
	{
		return globals.VariableExists(key) ? globals.VariableValue(key) : null;
	}
	catch (e)
	{
		return null;
	}
}

function Save(key, value)
{
	try
	{
		globals.VariableValue(key) = value;
		globals.VariablePersists(key) = true;
	}
	catch (e)
	{
	}
}

/*	
**********
**********   End Persistence
**********
*/

// AlanSm: start custom code
/* This is the part for Glossary popups */
// The method is called when the user positions the mouse cursor over a glossary term in a document.
// Current implementation assumes the existence of an associative array (g_glossary). 
// The keys of the array correspond to the argument passed to this function.

var bGlossary=true;
var oDialog;
var oTimeout="";
var oTimein="";
var iTimein=.5;
var iTimeout=30;
var oLastNode;
var oNode;
var bInit=false;
var aTerms=new Array();

// Called from mouseover and when the contextmenu behavior fires oncontextopen.
function clearDef(eventObj){
    if(eventObj){
        var elem;
        if(document.all) elem = eventObj.toElement;
        else elem = eventObj.relatedTarget;
	    if(elem!=null || elem!="undefined"){
		    if(typeof(oTimein)=="number"){
			    window.clearTimeout(oTimein);
		    }
		    if(oDialog.dlg_status==true){
			    hideDef();
		    }
		}
	}
}
function hideDef(eventObj){
	window.clearTimeout(oTimeout);
	oTimeout="";
	oDialog.style.display="none";
	oDialog.dlg_status=false;	
}
function showDef(oSource){
	if(bInit==false){
		glossaryInit();
		bInit=true;
	}
	if(bGlossary==true){
		if(typeof(arguments[0])=="object"){
			oNode=oSource;
		}
		else{
		    if(document.all) oNode = eventObj.srcElement;
		    else oNode = eventObj.target;
		}
		var bStatus=oDialog.dlg_status; // BUGBUG: oDialog is null.
		if((oLastNode!=oNode)||(bStatus==false)){
			if((typeof(oTimein)=="number")&& eventObj){
			    
			    var elem;
			    if(document.all) elem = eventObj.fromElement;
			    else elem = eventObj.relatedTarget;
			    
			    if( elem != null || elem != "undefined")
				    window.clearTimeout(oTimein);
			}
			oTimein=window.setTimeout("openDialog(oNode)",iTimein*1000);
		}	
	}
}



function glossaryInit(){
		oDialog=fnCreateDialog(150,50);
}

function navigateTerm(eventObj){
    var oNode;
    if(document.all) oNode = eventObj.srcElement;
    else oNode = eventObj.target;
	
	var iTermID=oNode.termID;
	if(oNode!=aTerms[iTermID]){
		var iAbsTop=getAbsoluteTop(aTerms[iTermID]);
		if(iAbsTop<document.body.scrollTop){
			window.scrollTo(document.body.scrollLeft,getAbsoluteTop(aTerms[iTermID]));
		}
		openDialog(aTerms[iTermID]);
	}
}
function disableGlossary(eventObj){
	if(bGlossary==true){
	    if(document.all) eventObj.srcElement.innerText="Enable Automatic Glossary";
		else eventObj.target.innerText="Enable Automatic Glossary";
		bGlossary=false;
		hideDef();		
	}
	else{
	    if(document.all) eventObj.srcElement.innerText="Disable Automatic Glossary";
		else eventObj.target.innerText="Disable Automatic Glossary";
		bGlossary=true;
	}
}
function openGlossary(){

}
function fnSetMenus(eventObj){
    var oNode;
    if(document.all) oNode = eventObj.srcElement;
    else oNode = eventObj.target;
	
	var oMenu=oNode.createMenu("SPAN","G_RID");
	var oSubItem1=oNode.createMenuItem("Glossary",fnStub,oMenu,true);
	document.body.createMenuItem("Open External Glossary",openGlossary,oSubItem1.subMenu);
	document.body.createMenuItem("Disable Automatic Glossary",disableGlossary,oSubItem1.subMenu);	
	for(var i=0;i<aTerms.length;i++){
		var oItem=document.body.createMenuItem(aTerms[i].innerText,navigateTerm,oMenu);
		oItem.termID=i;
	}
}
// This is a bogus stub.  It should be sniffed out rather than added in.
function fnStub(){

}
function fnAttachMenus(aTips){
	// This walk is only necessary for the context menu.
	var aTips=document.getElementsByTagName("SPAN");
	for(var i=0;i<aTips.length;i++){
		var oNode=aTips[i];
		if(oNode.getAttribute("G_RID")){
			var sTerm=oNode.getAttribute("G_RID");
			if(typeof(g_glossary[sTerm])=="string"){
				// Removed client-side scripting to add events.  This entire process should be singled out for IE 5 and later .. and, its only for the context menu.
				aTerms[aTerms.length]=oNode;
			}
		}
	}
	if(oBD.majorVer>=5){
		document.body.addBehavior(gsContextMenuPath);
		document.body.onbehaviorready="fnSetMenus()";
		document.body.oncontextopen="clearDef()";
	}

}
// Called by showDef.  The showDef function sniffs for initialization.
function openDialog(oNode,x,y){
 	var bStatus=oDialog.dlg_status; // BUGBUG: This code assumes that oDialog has been initialized
	if(bStatus==false){
		oDialog.dlg_status=true;
		oDialog.style.display="block";
	}
	else{
		if(typeof(oTimeout)=="number"){
			window.clearTimeout(oTimeout);
		}
	}
	
	var sTerm=oNode.getAttribute("G_RID");	
	var oDef=oNode.children(0);
	var sDef=oDef.text;
	sDef=sDef.substr(4,sDef.length-7);	//Strips the html comment markers from the definition.
	oDialog.innerHTML=sDef
	
	
	//oDialog.innerHTML=g_glossary[sTerm];
		
	var iScrollLeft=document.body.scrollLeft;
	var iScrollTop=document.body.scrollTop;
	var iOffsetLeft=getAbsoluteLeft(oNode)// - iScrollLeft;
	var iOffsetWidth=oNode.offsetWidth;
	var oParent=oNode.parentNode;
	var iOffsetParentLeft=getAbsoluteLeft(oParent);
	var iOffsetTop=getAbsoluteTop(oNode); //- iScrollTop;
	var iOffsetDialogWidth=oDialog.offsetWidth;
	
	
	if((iOffsetLeft + iOffsetWidth) > (iOffsetParentLeft + oParent.offsetWidth)){
		iOffsetLeft=iOffsetParentLeft;
		if(iOffsetLeft - iOffsetDialogWidth>0){
			iOffsetTop+=oNode.offsetHeight;
		}
	}
	var iLeft=0;
	var iTop=0;
	if((iOffsetLeft + iOffsetWidth - iScrollLeft + iOffsetDialogWidth) < document.body.offsetWidth ){
		iLeft=iOffsetLeft + iOffsetWidth;
	}
	else{
		if(iOffsetLeft - iOffsetDialogWidth>0){
			iLeft=iOffsetLeft - iOffsetDialogWidth;
		}
		else{
			iLeft=iOffsetParentLeft;
		}
	}
	if(iOffsetTop - iScrollTop<oDialog.offsetHeight){
		iTop=iOffsetTop + oNode.offsetHeight;
	}
	else{
		iTop=iOffsetTop - oDialog.offsetHeight;
	}
	oDialog.style.top=iTop;
	oDialog.style.left=iLeft;
	oTimeout=window.setTimeout("hideDef()",iTimeout*1000);	
}
function getAbsoluteTop(oNode){
	var oCurrentNode=oNode;
	var iTop=0;
	while(oCurrentNode.tagName!="BODY"){
		iTop+=oCurrentNode.offsetTop;
		oCurrentNode=oCurrentNode.offsetParent;
	}
	return iTop;
}
function getAbsoluteLeft(oNode){
	var oCurrentNode=oNode;
	var iLeft=0;
	while(oCurrentNode.tagName!="BODY"){
		iLeft+=oCurrentNode.offsetLeft;
		oCurrentNode=oCurrentNode.offsetParent;
	}
	return iLeft;
}
function fnCreateDialog(iWidth,iHeight){
	document.body.insertAdjacentHTML("BeforeEnd","<DIV></DIV>");
	oNewDialog=document.body.children(document.body.children.length-1);
	oNewDialog.className="clsTooltip";
	oNewDialog.style.width=iWidth;
	oNewDialog.dlg_status=false;
	return oNewDialog;
}
// AlanSm: end custom code
// DaemondC: Begin custom code
function sendfeedback(subject, id,alias){
	var rExp = /\"/gi;
	var url = location.href;
	// Need to replace the double quotes with single quotes for the mailto to work.
	var rExpSingleQuotes = /\'\'"/gi;
	var title = document.getElementsByTagName("TITLE")[0].innerText.replace(rExp, "''");
	location.href = "mailto:" + alias + "?subject=" + subject + title + "&body=Topic%20ID:%20" + id + "%0d%0aURL:%20" + url + "%0d%0a%0d%0aComments:%20";
}
// DaemondC: end custom code