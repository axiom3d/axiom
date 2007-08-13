<?xml version="1.0"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.1"
		xmlns:MSHelp="http://msdn.microsoft.com/mshelp" >

	<xsl:output method="html" omit-xml-declaration="yes" encoding="utf-8" doctype-public="-//W3C//DTD HTML 4.0 Transitional//EN" doctype-system="http://www.w3.org/TR/html4/loose.dtd" />

	<!-- key parameter is the api identifier string -->
	<xsl:param name="key" />
	<xsl:param name="metadata" value="false" />
	<xsl:param name="languages" />
    
	<xsl:include href="utilities_metadata.xsl" />
  <xsl:include href="xamlSyntax.xsl"/>

  <xsl:template match="/">
		<html>
			<head>
        <title><xsl:call-template name="topicTitlePlain"/></title>
				<xsl:call-template name="insertStylesheets" />
				<xsl:call-template name="insertScripts" />
				<xsl:call-template name="insertFilename" />
				<xsl:call-template name="insertMetadata" />
			</head>
			<body>
        
       <xsl:call-template name="upperBodyStuff"/>
				<!--<xsl:call-template name="control"/>-->
				<xsl:call-template name="main"/>
			</body>
		</html>
	</xsl:template>

	<!-- useful global variables -->

	<xsl:variable name="group" select="/document/reference/apidata/@group" />
	<xsl:variable name="subgroup" select="/document/reference/apidata/@subgroup" />
	<xsl:variable name="subsubgroup" select="/document/reference/apidata/@subsubgroup" />
  	<xsl:variable name="pseudo" select="boolean(/document/reference/apidata[@pseudo='true'])"/>
  
   	<xsl:variable name="namespaceName">
    		<xsl:value-of select="substring-after(/document/reference/containers/namespace/@api,':')"/>
  	</xsl:variable>
  
 
	<!-- document head -->

	<xsl:template name="insertStylesheets">
		<link rel="stylesheet" type="text/css" href="../styles/presentation.css" />
		<!-- make mshelp links work -->
		<link rel="stylesheet" type="text/css" href="ms-help://Hx/HxRuntime/HxLink.css" />
	</xsl:template>

	<xsl:template name="insertScripts">
    <script type="text/javascript">
      <includeAttribute name="src" item="scriptPath"><parameter>EventUtilities.js</parameter></includeAttribute>
    </script>
    <script type="text/javascript">
      <includeAttribute name="src" item="scriptPath"><parameter>SplitScreen.js</parameter></includeAttribute>
    </script>
    <script type="text/javascript">
      <includeAttribute name="src" item="scriptPath"><parameter>Dropdown.js</parameter></includeAttribute>
    </script>
    <script type="text/javascript">
      <includeAttribute name="src" item="scriptPath"><parameter>script_manifold.js</parameter></includeAttribute>
    </script>
  </xsl:template>

  <xsl:template match="parameters">
    
    <xsl:call-template name="subSection">
      <xsl:with-param name="title">
        <include item="parametersTitle"/>
      </xsl:with-param>
      <xsl:with-param name="content">
        <xsl:for-each select="parameter">
          <xsl:variable name="paramName" select="@name"/>
          <dl paramName="{$paramName}">
            <dt>
              <span class="parameter">
                <xsl:value-of select="$paramName"/>
              </span>
            </dt>
            <dd>
              <xsl:choose>
                <xsl:when test="type">
                  <xsl:call-template name="typeReferenceLink">
                    <xsl:with-param name="api" select="type/@api" />
                    <xsl:with-param name="qualified" select="true()" />
                  </xsl:call-template>
                </xsl:when>
                <xsl:otherwise>
                  <xsl:apply-templates select="*[1]" />
                </xsl:otherwise>
              </xsl:choose>
                <br />
              <xsl:call-template name="getParameterDescription">
                <xsl:with-param name="name" select="@name" />
              </xsl:call-template>
            </dd>
          </dl>
        </xsl:for-each>
      </xsl:with-param>
    </xsl:call-template>
    
  </xsl:template>
 
  
	<xsl:template match="element" mode="root">
		<tr>
			<td>
        <xsl:choose>
          <xsl:when test="apidata/@name = ''">
            <referenceLink target="{@api}" qualified="false">
              <include item="defaultNamespace" />
            </referenceLink>
          </xsl:when>
          <xsl:otherwise>
            <xsl:call-template name="createReferenceLink">
              <xsl:with-param name="id" select="@api" />
            </xsl:call-template>
          </xsl:otherwise>
        </xsl:choose>
      </td>
			<td>
				<xsl:call-template name="getElementDescription" /><br />
			</td>
		</tr>
	</xsl:template>

	<xsl:template match="element" mode="member">
   <xsl:param name="subgroup" />
   <xsl:param name="subsubgroup" />
   <xsl:param name="visibility" />
    <xsl:variable name="subgroupId" select="apidata/@subgroup"/>
    <xsl:variable name="subsubgroupId" select="apidata/@subsubgroup" />
    <xsl:variable name="visibilityId" select="memberdata/@visibility"/>
           
    <xsl:if test="($subgroup = $subgroupId and not($subsubgroupId) and ($visibility=$visibilityId or contains($visibility, $visibilityId))) or ($subgroup = 'explicitInterface' and $visibility=$visibilityId and proceduredata[@virtual = 'true'])">
      <tr>
        <xsl:if test="($group != 'member') and not(contains($key,containers/type/@api))">
          <xsl:attribute name="name">inheritedMember</xsl:attribute>
        </xsl:if>
        <xsl:if test="memberdata[@visibility='family' or @visibility='family or assembly' or @visibility='assembly']">
          <xsl:attribute name="protected">true</xsl:attribute>
        </xsl:if>
        <xsl:if test="not(count(versions/versions[@name='xnafw']/version) &gt; 0)">
          <xsl:attribute name="notSupportedOnXna">true</xsl:attribute>
        </xsl:if>
        <xsl:if test="not(count(versions/versions[@name='netcfw']/version) &gt; 0)">
          <xsl:attribute name="notSupportedOn">netcf</xsl:attribute>
        </xsl:if>
                
        <td>
          <xsl:call-template name="apiIcon" />
        </td>
        <td>
          
          <xsl:choose>
            <xsl:when test="@display-api">
              <referenceLink target="{@api}" display-target="{@display-api}" show-parameters="false" />
            </xsl:when>
            <xsl:otherwise>
              <referenceLink target="{@api}" show-parameters="false" />
            </xsl:otherwise>
          </xsl:choose>
        </td>
        <td>
          <xsl:if test="not(apidata[@pseudo='true'])">
            <xsl:call-template name="getInternalOnlyDescription" />
          </xsl:if>
          <xsl:if test="attributes/attribute/type[@api='T:System.ObsoleteAttribute']">
            <xsl:text> </xsl:text>
            <include item="obsoleteRed" />
          </xsl:if>
          <xsl:if test="attributes/attribute/type[@api='T:System.Security.Permissions.HostProtectionAttribute']">
            <xsl:text> </xsl:text>
            <include item="hostProtectionAttributeShort" />
          </xsl:if>
          <xsl:if test="apidata[@pseudo='true']">
            <include item="Overloaded"/>
            <xsl:text> </xsl:text>
          </xsl:if>
          <xsl:call-template name="getElementDescription" />
          <xsl:choose>
            <xsl:when test="($group != 'member') and not(contains($key,containers/type/@api))">
              <xsl:text> </xsl:text>
              <include item="inheritedFrom">
                <parameter>
                  <xsl:call-template name="typeReferenceLink">
                    <xsl:with-param name="api" select="containers/type/@api" />
                    <xsl:with-param name="qualified" select="false()" />
                  </xsl:call-template>
                </parameter>
              </include>
            </xsl:when>
            <xsl:when test="overrides">
              <xsl:text> </xsl:text>
              <include item="overridesMember">
                <parameter>
                  <xsl:apply-templates select="overrides/member" />
                </parameter>
              </include>
            </xsl:when>
          </xsl:choose>
          <br />
        </td>
      </tr>
    </xsl:if>
    </xsl:template>

  <xsl:template match="element" mode="attachedMember">
    <xsl:param name="subgroup" />
    <xsl:param name="subsubgroup" />
    <xsl:param name="visibility" />
    <xsl:variable name="subgroupId" select="apidata/@subgroup"/>
    <xsl:variable name="subsubgroupId" select="apidata/@subsubgroup" />
    <xsl:variable name="visibilityId" select="memberdata/@visibility"/>
    <xsl:variable name="supportedByXna" select="boolean(versions/versions[@name='xnafw'])"/>
    <xsl:variable name="supportedByNetCF" select="boolean(versions/versions[@name='netcfw'])"/>
   
    <xsl:if test="($subgroup = $subgroupId and $subsubgroup = $subsubgroupId and ($visibility=$visibilityId or contains($visibility, $visibilityId))) or ($subgroup = 'explicitInterface' and $visibility=$visibilityId and proceduredata[@virtual = 'true'])">
      <tr>
        <xsl:if test="($group != 'member') and not(contains($key,containers/type/@api))">
          <xsl:attribute name="name">inheritedMember</xsl:attribute>
        </xsl:if>
        <xsl:if test="memberdata[@visibility='family' or @visibility='family or assembly' or @visibility='assembly']">
          <xsl:attribute name="protected">true</xsl:attribute>
        </xsl:if>
        <xsl:if test="not($supportedByNetCF)">
          <xsl:attribute name="notSupportedOn">netcf</xsl:attribute>
        </xsl:if>
        <xsl:if test="not($supportedByXna = 'true')">
          <xsl:attribute name="notSupportedOnXna">true</xsl:attribute>
        </xsl:if>

        <td>
          <xsl:call-template name="apiIcon" />
        </td>
        <td>
          <xsl:choose>
            <xsl:when test="@display-api">
              <referenceLink target="{@api}" display-target="{@display-api}" />
            </xsl:when>
            <xsl:otherwise>
              <referenceLink target="{@api}" />
            </xsl:otherwise>
          </xsl:choose>
        </td>
        <td>
          <xsl:if test="not(apidata[@psuedo='true'])">
            <xsl:call-template name="getInternalOnlyDescription" />
          </xsl:if>
          <xsl:if test="attributes/attribute/type[@api='T:System.ObsoleteAttribute']">
            <xsl:text> </xsl:text>
            <include item="obsoleteRed" />
          </xsl:if>
          <xsl:if test="attributes/attribute/type[@api='T:System.Security.Permissions.HostProtectionAttribute']">
            <xsl:text> </xsl:text>
            <include item="hostProtectionAttributeShort" />
          </xsl:if>
          <xsl:if test="apidata[@psuedo='true']">
            <include item="Overloaded"/>
            <xsl:text> </xsl:text>
          </xsl:if>
          <xsl:call-template name="getElementDescription" />
          <xsl:choose>
            <xsl:when test="($group != 'member') and not(contains($key,containers/type/@api))">
              <xsl:text> </xsl:text>
              <include item="inheritedFrom">
                <parameter>
                  <xsl:call-template name="typeReferenceLink">
                    <xsl:with-param name="api" select="containers/type/@api" />
                    <xsl:with-param name="qualified" select="false()" />
                  </xsl:call-template>
                </parameter>
              </include>
            </xsl:when>
            <xsl:when test="overrides">
              <xsl:text> </xsl:text>
              <include item="overridesMember">
                <parameter>
                  <xsl:apply-templates select="overrides/member" />
                </parameter>
              </include>
            </xsl:when>
          </xsl:choose>
        </td>
        <br />
      </tr>
    </xsl:if>

  </xsl:template>

  <xsl:template match="element" mode="namespace">
    <xsl:param name="subgroup" />
    <xsl:variable name="subgroupId" select="apidata/@subgroup"/>

    <xsl:if test="$subgroup = $subgroupId">
      <tr>
        <xsl:attribute name="data">
          <xsl:value-of select="apidata/@subgroup" />
          <xsl:text>; public</xsl:text>
        </xsl:attribute>
        <td>
          <xsl:call-template name="apiIcon" />
        </td>
        <td>
          <xsl:call-template name="createReferenceLink">
            <xsl:with-param name="id" select="@api" />
          </xsl:call-template>
        </td>
        <td>
          <xsl:call-template name="getInternalOnlyDescription" />
          <xsl:if test="attributes/attribute/type[@api='T:System.ObsoleteAttribute']">
            <xsl:text> </xsl:text>
            <include item="obsoleteRed" />
          </xsl:if>
          <xsl:if test="attributes/attribute/type[@api='T:System.Security.Permissions.HostProtectionAttribute']">
            <xsl:text> </xsl:text>
            <include item="hostProtectionAttributeShort" />
          </xsl:if>
          <xsl:call-template name="getElementDescription" />
          <br />
        </td>
      </tr>
    </xsl:if>

  </xsl:template>

  <xsl:template name="typeList">
    <xsl:param name="subgroup" />
    <xsl:value-of select="$subgroup"/>
    <tr>
      <xsl:attribute name="data">
        <xsl:value-of select="apidata/@subgroup" />
        <xsl:text>; public</xsl:text>
      </xsl:attribute>
      <td>
        <xsl:call-template name="apiIcon" />
      </td>
      <td>
        <xsl:call-template name="createReferenceLink">
          <xsl:with-param name="id" select="@api" />
        </xsl:call-template>
      </td>
      <td>
        <xsl:call-template name="getInternalOnlyDescription" />
        <xsl:if test="attributes/attribute/type[@api='T:System.ObsoleteAttribute']">
          <xsl:text> </xsl:text>
          <include item="obsoleteRed" />
        </xsl:if>
        <xsl:if test="attributes/attribute/type[@api='T:System.Security.Permissions.HostProtectionAttribute']">
          <xsl:text> </xsl:text>
          <include item="hostProtectionAttributeShort" />
        </xsl:if>
        <xsl:call-template name="getElementDescription" />
        <br />
      </td>
    </tr>
  </xsl:template>

	<xsl:template match="element" mode="enumeration">
		<tr>
			<td>
				<xsl:call-template name="createReferenceLink">
					<xsl:with-param name="id" select="@api" />
				</xsl:call-template>
			</td>
			<td>
				<xsl:call-template name="getElementDescription" /><br /></td>
		</tr>
	</xsl:template>

  <xsl:template match="element" mode="derivedType">
    <tr>
      <td>
        <xsl:choose>
          <xsl:when test="@display-api">
            <referenceLink target="{@api}" display-target="{@display-api}" />
          </xsl:when>
          <xsl:otherwise>
            <referenceLink target="{@api}" />
          </xsl:otherwise>
        </xsl:choose>
      </td>
      <td>
        <xsl:call-template name="getInternalOnlyDescription" />
        <xsl:if test="attributes/attribute/type[@api='T:System.ObsoleteAttribute']">
          <xsl:text> </xsl:text>
          <include item="obsoleteRed" />
        </xsl:if>
        <xsl:if test="attributes/attribute/type[@api='T:System.Security.Permissions.HostProtectionAttribute']">
          <xsl:text> </xsl:text>
          <include item="hostProtectionAttributeShort" />
        </xsl:if>
        <xsl:call-template name="getElementDescription" />
        <xsl:choose>
          <xsl:when test="($group != 'member') and ($group != 'derivedType') and string(containers/type/@api) != $key">
            <xsl:text> </xsl:text>
            <include item="inheritedFrom">
              <parameter>
                <xsl:call-template name="typeReferenceLink">
                  <xsl:with-param name="api" select="containers/type/@api" />
                  <xsl:with-param name="qualified" select="false()" />
                </xsl:call-template>
              </parameter>
            </include>
          </xsl:when>
          <xsl:when test="overrides">
            <xsl:text> </xsl:text>
            <include item="overridesMember">
              <parameter>
                <xsl:apply-templates select="overrides/member" />
              </parameter>
            </include>
          </xsl:when>
        </xsl:choose>
        <br />
      </td>
    </tr>


  </xsl:template>

  <xsl:template match="element" mode="overload">
    <tr>
      <xsl:attribute name="data">
        <xsl:value-of select="apidata/@subgroup" />
        <xsl:choose>
          <xsl:when test="memberdata/@visibility='public'">
            <xsl:text>; public</xsl:text>
          </xsl:when>
          <xsl:when test="memberdata/@visibility='family'">
            <xsl:text>; protected</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>; public</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:choose>
          <xsl:when test="memberdata/@static = 'true'">
            <xsl:text>; static</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>; instance</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:choose>
          <xsl:when test="string(containers/type/@api) = $key">
            <xsl:text>; declared</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>; inherited</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:attribute>
      <td>
        <xsl:choose>
          <xsl:when test="@display-api">
            <referenceLink target="{@api}" display-target="{@display-api}" />
          </xsl:when>
          <xsl:otherwise>
            <referenceLink target="{@api}" />
          </xsl:otherwise>
        </xsl:choose>
      </td>
      <td>
        <xsl:call-template name="getInternalOnlyDescription" />
        <xsl:if test="attributes/attribute/type[@api='T:System.ObsoleteAttribute']">
          <xsl:text> </xsl:text>
          <include item="obsoleteRed" />
        </xsl:if>
        <xsl:if test="attributes/attribute/type[@api='T:System.Security.Permissions.HostProtectionAttribute']">
          <xsl:text> </xsl:text>
          <include item="hostProtectionAttributeShort" />
        </xsl:if>
        <xsl:call-template name="getElementDescription" />
        <xsl:choose>
					<xsl:when test="($group != 'member') and (string(containers/type/@api) != $key)">
            <xsl:text> </xsl:text>
            <include item="inheritedFrom">
              <parameter>
                <xsl:call-template name="typeReferenceLink">
                  <xsl:with-param name="api" select="containers/type/@api" />
                  <xsl:with-param name="qualified" select="false()" />
                </xsl:call-template>
              </parameter>
            </include>
          </xsl:when>
          <xsl:when test="overrides">
            <xsl:text> </xsl:text>
            <include item="overridesMember">
              <parameter>
                <xsl:apply-templates select="overrides/member" />
              </parameter>
            </include>
          </xsl:when>
        </xsl:choose>
        <xsl:if test="count(versions/versions[@name='netcfw']/version) &gt; 0">
          <p/><include item="supportedByNetCF" />
        </xsl:if>
        <xsl:if test="count(versions/versions[@name='xnafw']/version) &gt; 0">
          <p/><include item="supportedByXNA" />
        </xsl:if>
      </td>
      <br />
    </tr>
  </xsl:template>

	<xsl:template name="insertFilename">
		<meta name="guid">
			<xsl:attribute name="content">
				<xsl:value-of select="/document/reference/file/@name" />
			</xsl:attribute>
		</meta>
	</xsl:template>

	<!-- writing templates -->

	<xsl:template name="csTemplates">
		<xsl:param name="seperator" select="string(',')" />
		<xsl:text>&lt;</xsl:text>
		<xsl:for-each select="template">
			<xsl:value-of select="@name" />
			<xsl:if test="not(position()=last())">
				<xsl:value-of select="$seperator" />
			</xsl:if>
		</xsl:for-each>
		<xsl:text>&gt;</xsl:text>
	</xsl:template>

	<xsl:template name="csTemplatesInIndex" >
		<xsl:text>%3C</xsl:text>
		<xsl:for-each select="template">
			<xsl:value-of select="@name" />
			<xsl:if test="not(position()=last())">
				<xsl:text>%2C </xsl:text>
			</xsl:if>
		</xsl:for-each>
		<xsl:text>%3E</xsl:text>
	</xsl:template>

	<xsl:template name="vbTemplates">
		<xsl:param name="seperator" select="string(',')" />
		<xsl:text>(Of </xsl:text>
		<xsl:for-each select="template">
			<xsl:value-of select="@name" />
			<xsl:if test="not(position()=last())">
				<xsl:value-of select="$seperator" />
			</xsl:if>
		</xsl:for-each>
		<xsl:text>)</xsl:text>
	</xsl:template>

	<xsl:template name="typeTitle">
		<xsl:if test="containers/container[@type]">
			<xsl:for-each select="containers/container[@type]">
				<xsl:call-template name="typeTitle" />
			</xsl:for-each>
			<xsl:text>.</xsl:text>
		</xsl:if>
		<xsl:value-of select="apidata/@name" />
		<xsl:if test="count(templates/template) > 0">
			<xsl:for-each select="templates"><xsl:call-template name="csTemplates" /></xsl:for-each>
		</xsl:if> 
	</xsl:template>

	<!-- document body -->

	<!-- control window -->

	<xsl:template name="control">
		<div id="control">
			<span class="topicTitle"><xsl:call-template name="topicTitleDecorated" /></span><br/>
		</div>
	</xsl:template>

	<!-- Title in topic -->

  <!-- Title in topic -->

  <xsl:template name="topicTitlePlain">
    <include>
      <xsl:attribute name="item">
        <xsl:if test="boolean(/document/reference/templates) and not($group='members')">
          <xsl:text>generic_</xsl:text>
        </xsl:if>
        <xsl:choose>
          <xsl:when test="boolean($subsubgroup)">
            <xsl:value-of select="$subsubgroup" />
          </xsl:when>
          <xsl:when test="boolean($subgroup)">
            <xsl:value-of select="$subgroup" />
          </xsl:when>
          <xsl:otherwise>
            <xsl:value-of select="$group" />
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>TopicTitle</xsl:text>
      </xsl:attribute>
      <parameter>
        <xsl:call-template name="shortNamePlain" />
      </parameter>
      <parameter>
        <xsl:if test="document/reference/memberdata/@overload = 'true'" >
          <xsl:call-template name="parameterTypesPlain" />
        </xsl:if>
      </parameter>
    </include>
  </xsl:template>

  <xsl:template name="topicTitleDecorated">
    <xsl:param name="titleType" />
    <include>
      <xsl:attribute name="item">
        <xsl:choose>
          <xsl:when test="$titleType = 'tocTitle' and $group='namespace'">
            <xsl:text>tocTitle</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:choose>
              <xsl:when test="boolean($subsubgroup)">
                <xsl:value-of select="$subsubgroup" />
              </xsl:when>
              <xsl:when test="boolean($subgroup)">
                <xsl:value-of select="$subgroup" />
              </xsl:when>
              <xsl:otherwise>
                <xsl:value-of select="$group" />
              </xsl:otherwise>
            </xsl:choose>
            <xsl:text>TopicTitle</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:attribute>
      <parameter>
        <xsl:call-template name="shortNameDecorated">
          <xsl:with-param name="titleType" select="$titleType" />
        </xsl:call-template>
      </parameter>
      <parameter>
        <xsl:if test="document/reference/memberdata/@overload = 'true'" >
          <xsl:call-template name="parameterTypes" />
        </xsl:if>
      </parameter>
    </include>
  </xsl:template>

	
	<!-- Title in TOC -->

	<!-- Index entry -->
	
	<!-- main window -->

  <xsl:template name="main">
    <div id="mainSection">

      <div id="mainBody">
        <div id="allHistory" class="saveHistory" onsave="saveAll()" onload="loadAll()"/>
        <xsl:call-template name="head" />
        <xsl:call-template name="body" />
        <xsl:call-template name="foot" />
      </div>
    </div>
    
  </xsl:template>

	<xsl:template name="head">
		<include item="header" />
	</xsl:template>

  <xsl:template name="syntaxBlocks">
    
    <xsl:for-each select="/document/syntax/div[@codeLanguage='VisualBasic']">
      <xsl:call-template name="languageSyntaxBlock">
        <xsl:with-param name="language">VisualBasicDeclaration</xsl:with-param>
      </xsl:call-template>
    </xsl:for-each>

    <xsl:for-each select="/document/syntax/div[@codeLanguage='VisualBasicUsage']">
      <xsl:call-template name="languageSyntaxBlock"/>
    </xsl:for-each>

    <xsl:for-each select="/document/syntax/div[@codeLanguage='CSharp']">
      <xsl:call-template name="languageSyntaxBlock"/>
    </xsl:for-each>

    <xsl:for-each select="/document/syntax/div[@codeLanguage='ManagedCPlusPlus']">
      <xsl:call-template name="languageSyntaxBlock"/>
    </xsl:for-each>

    <xsl:if test="count(/document/reference/versions/versions[@name='netfw']/version) &gt; 1">
      <xsl:for-each select="/document/syntax/div[@codeLanguage='JSharp']">
        <xsl:call-template name="languageSyntaxBlock"/>
      </xsl:for-each>
    </xsl:if>
    
    <xsl:for-each select="/document/syntax/div[@codeLanguage='JScript']">
      <xsl:call-template name="languageSyntaxBlock"/>
    </xsl:for-each>

    <xsl:for-each select="/document/syntax/div[@codeLanguage='XAML']">
      <xsl:call-template name="XamlSyntaxBlock"/>
    </xsl:for-each>
  </xsl:template>

  <xsl:template name="languageSyntaxBlock">
    <xsl:param name="language" select="@codeLanguage"/>
    <span codeLanguage="{$language}">
      <table>
        <tr>
          <th>
            <include item="{$language}" />
          </th>
        </tr>
        <tr>
          <td>
            <pre xml:space="preserve"><xsl:text/><xsl:copy-of select="node()"/><xsl:text/></pre>
          </td>
        </tr>
      </table>
    </span>
  </xsl:template>

	<xsl:template match="elements" mode="root">
		<xsl:if test="count(element) > 0">
           
			<xsl:call-template name="section">
        <xsl:with-param name="toggleSwitch" select="'namespaces'"/>
				<xsl:with-param name="title"><include item="namespacesTitle" /></xsl:with-param>
				<xsl:with-param name="content">
				<table class="members" id="memberList">
					<tr>
						<th class="nameColumn"><include item="namespaceNameHeader"/></th>
						<th class="descriptionColumn"><include item="namespaceDescriptionHeader" /></th>
					</tr>
					<xsl:apply-templates select="element" mode="root">
						<xsl:sort select="apidata/@name" />
					</xsl:apply-templates>
				</table>
				</xsl:with-param>
			</xsl:call-template>
		</xsl:if>
	</xsl:template>

  <xsl:template name="namespaceSection">
    <xsl:param name="subgroup" />
    <xsl:variable name="header" select="concat($subgroup, 'TypesFilterLabel')"/>
    <xsl:call-template name="section">
      <xsl:with-param name="toggleSwitch" select="$subgroup"/>
      <xsl:with-param name="title">
        <include item="{$header}" />
      </xsl:with-param>
      <xsl:with-param name="content">
        <xsl:call-template name="namespaceList">
          <xsl:with-param name="subgroup" select="$subgroup" />
        </xsl:call-template>
      </xsl:with-param>
    </xsl:call-template>
  </xsl:template>

  <xsl:template match="elements" mode="namespace">
   
    <xsl:if test="element/apidata/@subgroup = 'class'">
      <xsl:call-template name="namespaceSection">
        <xsl:with-param name="subgroup" select="'class'" />
      </xsl:call-template>
    </xsl:if>
    
    <xsl:if test="element/apidata/@subgroup = 'structure'">
      <xsl:call-template name="namespaceSection">
        <xsl:with-param name="subgroup" select="'structure'" />
      </xsl:call-template>
    </xsl:if>
    
    <xsl:if test="element/apidata/@subgroup = 'interface'">
      <xsl:call-template name="namespaceSection">
        <xsl:with-param name="subgroup" select="'interface'" />
      </xsl:call-template>
    </xsl:if>

    <xsl:if test="element/apidata/@subgroup = 'delegate'">
      <xsl:call-template name="namespaceSection">
        <xsl:with-param name="subgroup" select="'delegate'" />
      </xsl:call-template>
    </xsl:if>

    <xsl:if test="element/apidata/@subgroup = 'enumeration'">
      <xsl:call-template name="namespaceSection">
        <xsl:with-param name="subgroup" select="'enumeration'" />
      </xsl:call-template>
    </xsl:if>

  </xsl:template>

  <xsl:template name="namespaceList">
    <xsl:param name="subgroup" />

    <table id="typeList" class="members">
      <tr>
        <th class="iconColumn">
          &#160;
       </th>
        <th class="nameColumn">
          <include item="{$subgroup}NameHeader"/>
        </th>
        <th class="descriptionColumn">
          <include item="typeDescriptionHeader" />
        </th>
      </tr>
      <xsl:apply-templates select="element" mode="namespace">
        <xsl:sort select="apidata/@name" />
        <xsl:with-param name="subgroup" select="$subgroup"/>
      </xsl:apply-templates>
    </table>
    
  </xsl:template>

  <xsl:template name="membersList">
    <xsl:param name="subgroup" />
    <xsl:param name="subsubgroup" />
    <xsl:param name="visibility" />
    <table id="typeList" class="members">
      <tr>
        <th class="iconColumn">
          &#160;
        </th>
        <th class="nameColumn">
          <include item="typeNameHeader"/>
        </th>
        <th class="descriptionColumn">
          <include item="typeDescriptionHeader" />
        </th>
      </tr>
      <xsl:if test="$subgroup = $subsubgroup">
      <xsl:apply-templates select="element" mode="member">
        <xsl:sort select="apidata/@name" />
        
        <xsl:with-param name="subgroup" select="$subgroup"/>
        <xsl:with-param name="subsubgroup" select="$subsubgroup" />
        <xsl:with-param name="visibility" select="$visibility"/>
      </xsl:apply-templates>
      </xsl:if>
      <xsl:if test="$subgroup != $subsubgroup">
        <xsl:apply-templates select="element" mode="attachedMember">
          <xsl:sort select="apidata/@name" />

          <xsl:with-param name="subgroup" select="$subgroup"/>
          <xsl:with-param name="subsubgroup" select="$subsubgroup" />
          <xsl:with-param name="visibility" select="$visibility"/>
        </xsl:apply-templates>
      </xsl:if>
    </table>

  </xsl:template>
    
  <xsl:template match="elements" mode="enumeration">
    <xsl:if test="count(element) > 0">
      <xsl:call-template name="section">
        <xsl:with-param name="toggleSwitch" select="'members'"/>
        <xsl:with-param name="title">
          <include item="enumMembersTitle" />
        </xsl:with-param>
        <xsl:with-param name="content">
          <table class="members" id="memberList">
            <tr>
              <th class="nameColumn">
                <include item="memberNameHeader"/>
              </th>
              <th class="descriptionColumn">
                <include item="memberDescriptionHeader" />
              </th>
            </tr>
            <xsl:apply-templates select="element" mode="enumeration">
              <xsl:sort select="apidata/@name" />
            </xsl:apply-templates>
          </table>
        </xsl:with-param>
      </xsl:call-template>
    </xsl:if>
  </xsl:template>

  <xsl:template match="element" mode="members">
    <xsl:param name="subgroup"/>
      <xsl:if test="memberdata[@visibility='public'] and apidata[@subgroup=$subgroup]">
          public;
      </xsl:if>
      <xsl:if test="memberdata[@visibility='family' or @visibility='family or assembly' or @visibility='assembly'] and apidata[@subgroup=$subgroup]">
        protected;
      </xsl:if>
      <xsl:if test="memberdata[@visibility='private'] and apidata[@subgroup=$subgroup] and not(proceduredata[@virtual = 'true'])">
        private;
      </xsl:if>
      <xsl:if test="memberdata[@visibility='private'] and proceduredata[@virtual = 'true']">
        explicit;
      </xsl:if>
  </xsl:template>

  <xsl:template match="element" mode="attachedMembers">
    <xsl:param name="subgroup"/>
    <xsl:if test="not(apidata/@subsubgroup) and apidata[@subgroup=$subgroup]">
      member
    </xsl:if>
    <xsl:if test="apidata/@subsubgroup and apidata[@subgroup=$subgroup]">
      attached
    </xsl:if>
  </xsl:template>
  <xsl:template name="memberSection">
    <xsl:param name="subgroup" />
    <xsl:param name="subsubgroup" />

    <xsl:variable name="visibility">
      <xsl:apply-templates select="element" mode="members">
        <xsl:with-param name="subgroup" select="$subgroup"/>
        <xsl:sort select="apidata/@name"/>
      </xsl:apply-templates>
    </xsl:variable>

    <xsl:if test="contains($visibility, 'public')">
      <xsl:variable name="header" select="concat('Public', $subsubgroup)" />
      <xsl:call-template name="section">
        <xsl:with-param name="toggleSwitch" select="$header" />
        <xsl:with-param name="title">
          <include item="{$header}" />
        </xsl:with-param>
        <xsl:with-param name="content">
          <xsl:call-template name="membersList">
            <xsl:with-param name="subgroup" select="$subgroup" />
            <xsl:with-param name="subsubgroup" select="$subsubgroup" />
            <xsl:with-param name="visibility" select="'public'" />
          </xsl:call-template>
        </xsl:with-param>
      </xsl:call-template>
    </xsl:if>
    <xsl:if test="contains($visibility, 'private')">
      <xsl:variable name="header" select="concat('Private', $subsubgroup)" />
      <xsl:call-template name="section">
        <xsl:with-param name="toggleSwitch" select="$header" />
        <xsl:with-param name="title">
          <include item="{$header}" />
        </xsl:with-param>
        <xsl:with-param name="content">
          <xsl:call-template name="membersList">
            <xsl:with-param name="subgroup" select="$subgroup" />
            <xsl:with-param name="subsubgroup" select="$subsubgroup" />
            <xsl:with-param name="visibility" select="'private'" />
          </xsl:call-template>
        </xsl:with-param>
      </xsl:call-template>
    </xsl:if>
    <xsl:if test="contains($visibility, 'protected')">
      <xsl:variable name="header" select="concat('Protected', $subsubgroup)" />
      <xsl:call-template name="section">
        <xsl:with-param name="toggleSwitch" select="$header" />
        <xsl:with-param name="title">
          <include item="{$header}" />
        </xsl:with-param>
        <xsl:with-param name="content">
          <xsl:call-template name="membersList">
            <xsl:with-param name="subgroup" select="$subgroup" />
            <xsl:with-param name="subsubgroup" select="$subsubgroup" />
            <xsl:with-param name="visibility" select="'family or assembly'" />
          </xsl:call-template>
        </xsl:with-param>
      </xsl:call-template>
    </xsl:if>
  </xsl:template>

  <xsl:template match="elements" mode="member">

    <xsl:call-template name="memberIntro" />
    
    
    <xsl:if test="element/apidata[@subgroup='method']">
      <xsl:call-template name="memberSection">
        <xsl:with-param name="subgroup" select="'method'" />
        <xsl:with-param name="subsubgroup" select="'method'" />
      </xsl:call-template>
    </xsl:if>
    
    <xsl:if test="element/apidata[@subgroup='field']">
      <xsl:call-template name="memberSection">
        <xsl:with-param name="subgroup" select="'field'" />
        <xsl:with-param name="subsubgroup" select="'field'" />
      </xsl:call-template>
    </xsl:if>
       
    <xsl:if test="element/apidata[@subgroup='constructor']">
      <xsl:call-template name="memberSection">
        <xsl:with-param name="subgroup" select="'constructor'" />
        <xsl:with-param name="subsubgroup" select="'constructor'" />
      </xsl:call-template>
    </xsl:if>
   
    <xsl:if test="element/apidata[@subgroup='property']">
      <xsl:variable name="member">
        <xsl:apply-templates select="element" mode="attachedMembers">
          <xsl:with-param name="subgroup" select="'property'"/>
          <xsl:sort select="apidata/@name"/>
        </xsl:apply-templates>
      </xsl:variable>
      <xsl:if test="contains($member, 'member')">
        <xsl:call-template name="memberSection">
          <xsl:with-param name="subgroup" select="'property'" />
          <xsl:with-param name="subsubgroup" select="'property'" />
        </xsl:call-template>
      </xsl:if>
      <xsl:if test="contains($member, 'attached')">
        <xsl:call-template name="memberSection">
          <xsl:with-param name="subgroup" select="'property'" />
          <xsl:with-param name="subsubgroup" select="'attachedProperty'" />
        </xsl:call-template>
      </xsl:if>
    </xsl:if>
    
    <xsl:if test="element/apidata[@subgroup='event']">
      <xsl:variable name="member">
        <xsl:apply-templates select="element" mode="attachedMembers">
          <xsl:with-param name="subgroup" select="'event'"/>
          <xsl:sort select="apidata/@name"/>
        </xsl:apply-templates>
      </xsl:variable>
      <xsl:if test="contains($member, 'member')">
     <xsl:call-template name="memberSection">
        <xsl:with-param name="subgroup" select="'event'" />
          <xsl:with-param name="subsubgroup" select="'event'" />
     </xsl:call-template>
      </xsl:if>
      <xsl:if test="contains($member, 'attached')">
        <xsl:call-template name="memberSection">
          <xsl:with-param name="subgroup" select="'event'" />
          <xsl:with-param name="subsubgroup" select="'attachedEvent'" />
        </xsl:call-template>
      </xsl:if>
    </xsl:if>

    <xsl:variable name="visibility">
      <xsl:apply-templates select="element" mode="members">
        <xsl:with-param name="subgroup" select="subgroup"/>
        <xsl:sort select="apidata/@name"/>
      </xsl:apply-templates>
    </xsl:variable>
   
    <xsl:if test="contains($visibility, 'explicit')">
      <xsl:call-template name="section">
        <xsl:with-param name="toggleSwitch" select="'ExplicitInterfaceImplementation'" />
        <xsl:with-param name="title">
          <include item="ExplicitInterfaceImplementationTitle" />
        </xsl:with-param>
        <xsl:with-param name="content">
          <xsl:call-template name="membersList">
            <xsl:with-param name="subgroup" select="'explicitInterface'" />
            <xsl:with-param name="subsubgroup" select="'explicitInterface'" />
            <xsl:with-param name="visibility" select="'private'" />
          </xsl:call-template>
        </xsl:with-param>
      </xsl:call-template>
    </xsl:if>
    
  </xsl:template>
  
  <xsl:template match="elements" mode="type">
            
	</xsl:template>

  <xsl:template match="elements" mode="derivedType">
    <xsl:if test="count(element) > 0">
      <xsl:call-template name="section">
        <xsl:with-param name="toggleSwitch" select="'DerivedClasses'"/>
        <xsl:with-param name="title">
          <include item="derivedClasses" />
        </xsl:with-param>
        <xsl:with-param name="content">
          <table class="members" id="memberList">
            <tr>
              <th class="nameColumn">
                <include item="memberNameHeader"/>
              </th>
              <th class="descriptionColumn">
                <include item="memberDescriptionHeader" />
              </th>
            </tr>
            <xsl:apply-templates select="element" mode="derivedType">
              <xsl:sort select="apidata/@name" />
            </xsl:apply-templates>
          </table>
        </xsl:with-param>
      </xsl:call-template>
    </xsl:if>
  </xsl:template>

	<xsl:template match="elements" mode="overload">
   <xsl:if test="count(element) > 0">
			<xsl:call-template name="section">
        <xsl:with-param name="toggleSwitch" select="'overloadMembers'"/>
				<xsl:with-param name="title"><include item="membersTitle" /></xsl:with-param>
				<xsl:with-param name="content">
				<table class="members" id="memberList">
					<tr>
						<th class="nameColumn"><include item="typeNameHeader"/></th>
						<th class="descriptionColumn"><include item="typeDescriptionHeader" /></th>
					</tr>
					<xsl:apply-templates select="element" mode="overload">
						<xsl:sort select="apidata/@name" />
					</xsl:apply-templates>
				</table>
				</xsl:with-param>
			</xsl:call-template>
		</xsl:if>
    <xsl:apply-templates select="element" mode="overloadSections">
      <xsl:sort select="apidata/@name" />
    </xsl:apply-templates>
  </xsl:template>

  <xsl:template match="elements" mode="overloadSummary">
    <xsl:apply-templates select="element" mode="overloadSummary" >
      <xsl:sort select="apidata/@name"/>
    </xsl:apply-templates>
  </xsl:template>

  <xsl:template match="element" mode="overloadSummary">
    <xsl:call-template name="getOverloadSummary" />
  </xsl:template>

  <xsl:template match="element" mode="overloadSections">
    <xsl:call-template name="getOverloadSections" />
  </xsl:template>

	<xsl:template name="apiIcon">
		<xsl:choose>
			<xsl:when test="apidata/@group='type'">
				<xsl:choose>
					<xsl:when test="apidata/@subgroup='class'">
						<img>
							<includeAttribute name="src" item="iconPath">
								<parameter>pubclass.gif</parameter>
							</includeAttribute>
							<includeAttribute name="alt" item="publicClassAltText" />
						</img>
					</xsl:when>
					<xsl:when test="apidata/@subgroup='structure'">
						<img>
							<includeAttribute name="src" item="iconPath">
								<parameter>pubstructure.gif</parameter>
							</includeAttribute>
							<includeAttribute name="alt" item="publicStructureAltText" />
						</img>
					</xsl:when>
					<xsl:when test="apidata/@subgroup='interface'">
						<img>
							<includeAttribute name="src" item="iconPath">
								<parameter>pubinterface.gif</parameter>
							</includeAttribute>
							<includeAttribute name="alt" item="publicInterfaceAltText" />
						</img>
					</xsl:when>
					<xsl:when test="apidata/@subgroup='delegate'">
						<img>
							<includeAttribute name="src" item="iconPath">
								<parameter>pubdelegate.gif</parameter>
							</includeAttribute>
							<includeAttribute name="alt" item="publicDelegateAltText" />
						</img>
					</xsl:when>
					<xsl:when test="apidata/@subgroup='enumeration'">
						<img>
							<includeAttribute name="src" item="iconPath">
								<parameter>pubenum.gif</parameter>
							</includeAttribute>
							<includeAttribute name="alt" item="publicEnumerationAltText" />
						</img>
					</xsl:when>
				</xsl:choose>
			</xsl:when>
			<xsl:when test="apidata/@group='member'">
				<xsl:variable name="memberVisibility">
					<xsl:choose>
						<xsl:when test="memberdata/@visibility='public'">
							<xsl:text>pub</xsl:text>
						</xsl:when>
						<xsl:when test="memberdata/@visibility='family' or memberdata/@visibility='family or assembly' or memberdata/@visibility='assembly'">
							<xsl:text>prot</xsl:text>
						</xsl:when>
            <xsl:when test="memberdata/@visibility='private'">
              <xsl:text>priv</xsl:text>
            </xsl:when>
						<xsl:otherwise>
							<xsl:text>pub</xsl:text>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:variable>
				<xsl:variable name="memberSubgroup">
					<xsl:choose>
						<xsl:when test="apidata/@subgroup='constructor'">
							<xsl:text>method</xsl:text>
						</xsl:when>
						<xsl:otherwise>
							<xsl:value-of select="apidata/@subgroup" />
						</xsl:otherwise>
					</xsl:choose>
				</xsl:variable>
        <xsl:variable name="memberIcon" select="concat($memberVisibility,$memberSubgroup,'.gif')" />
        <xsl:if test="memberdata/@visibility='private' and proceduredata/@virtual='true'">
          <img>
            <includeAttribute name="src" item="iconPath">
              <parameter>pubinterface.gif</parameter>
            </includeAttribute>
            <includeAttribute name="alt" item="ExplicitInterfaceAltText" />
          </img>
        </xsl:if>
        <img>
          <includeAttribute name="src" item="iconPath">
            <parameter>
              <xsl:value-of select="$memberIcon" />
            </parameter>
          </includeAttribute>
        </img>
        <xsl:if test="memberdata/@static='true'">
					<img>
						<includeAttribute name="src" item="iconPath">
							<parameter>static.gif</parameter>
						</includeAttribute>
						<includeAttribute name="alt" item="staticAltText" />
					</img>
        </xsl:if>
        <xsl:if test="count(versions/versions[@name='netcfw']/version) &gt; 0">
          <img>
            <includeAttribute name="src" item="iconPath">
              <parameter>CFW.gif</parameter>
            </includeAttribute>
            <includeAttribute name="alt" item="CompactFrameworkAltText" />
          </img>
        </xsl:if>
        <xsl:if test="count(versions/versions[@name='xnafw']/version) &gt; 0">
          <img>
            <includeAttribute name="src" item="iconPath">
              <parameter>xna.gif</parameter>
            </includeAttribute>
            <includeAttribute name="alt" item="XNAFrameworkAltText" />
          </img>
				</xsl:if>
			</xsl:when>
		</xsl:choose>
	</xsl:template>

	<!-- Footer stuff -->
	
	<xsl:template name="foot">
    <div id="footer">
      <div class="footerLine">
        <img alt="Footer image" width="100%" height="3px">
          <includeAttribute name="src" item="iconPath">
            <parameter>footer.gif</parameter>
          </includeAttribute>
        </img>
      </div>

      <include item="footer">
        <parameter>
          <xsl:value-of select="$key"/>
        </parameter>
        <parameter>
          <xsl:call-template name="topicTitlePlain"/>
        </parameter>
      </include>
    </div>
	</xsl:template>

	<!-- Assembly information -->

	<xsl:template name="requirementsInfo">
    <p/>
    <include item="requirementsNamespaceLayout">
      <parameter>
        <xsl:value-of select="$namespaceName"/>
      </parameter>
    </include>
    <br/>
    <xsl:call-template name="assembliesInfo"/>

    <!-- some apis display a XAML xmlns uri -->
    <xsl:call-template name="xamlXmlnsInfo"/>
  </xsl:template>

  <xsl:template name="assemblyNameAndModule">
    <xsl:param name="library" select="/document/reference/containers/library"/>
    <include item="assemblyNameAndModule">
      <parameter>
        <xsl:value-of select="$library/@assembly"/>
      </parameter>
      <parameter>
        <xsl:value-of select="$library/@module"/>
      </parameter>
    </include>
  </xsl:template>

  <xsl:template name="assembliesInfo">
    <xsl:choose>
      <xsl:when test="count(/document/reference/containers/library)&gt;1">
        <include item="requirementsAssembliesLabel"/>
        <xsl:for-each select="/document/reference/containers/library">
          <xsl:text>&#xa0;&#xa0;</xsl:text>
          <xsl:call-template name="assemblyNameAndModule">
            <xsl:with-param name="library" select="."/>
          </xsl:call-template>
          <br/>
        </xsl:for-each>
      </xsl:when>
      <xsl:otherwise>
        <include item="requirementsAssemblyLabel"/>
        <xsl:text>&#xa0;</xsl:text>
        <xsl:call-template name="assemblyNameAndModule"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <!-- Platform information -->

  <xsl:template match="platforms">
    <xsl:call-template name="section">
      <xsl:with-param name="toggleSwitch" select="'platformsTitle'"/>
      <xsl:with-param name="title">
        <include item="platformsTitle" />
      </xsl:with-param>
      <xsl:with-param name="content">
        <p>
          <xsl:for-each select="platform">
            <include item="{.}" /><xsl:if test="position()!=last()"><xsl:text>, </xsl:text></xsl:if>
          </xsl:for-each>
        </p>
        <p>
          <include item="developmentPlatformsLayout"/>
        </p>
      </xsl:with-param>
    </xsl:call-template>
  </xsl:template>

  <!-- Version information -->

  <xsl:template match="versions">
    <xsl:call-template name="section">
      <xsl:with-param name="toggleSwitch" select="'versionsTitle'"/>
      <xsl:with-param name="title">
        <include item="versionsTitle" />
      </xsl:with-param>
      <xsl:with-param name="content">
        <xsl:call-template name="processVersions" />
      </xsl:with-param>
    </xsl:call-template>
  </xsl:template>

  <xsl:template name="processVersions">
    <xsl:choose>
      <xsl:when test="versions">
        <xsl:for-each select="versions">
           <xsl:if test="count(version) &gt; 0">
              <h4 class ="subHeading">
                <include item="{@name}" />
              </h4>
              <xsl:call-template name="processVersions" />
           </xsl:if>
        </xsl:for-each>
      </xsl:when>
      <xsl:otherwise>
        <xsl:variable name="count" select="count(version)"/>
        <xsl:if test="$count &gt; 0">
          <include item="supportedIn_{$count}">
            <xsl:for-each select="version">
              <parameter>
                <include item="{@name}" />
              </parameter>
            </xsl:for-each>
          </include>
          </xsl:if>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

	<!-- Inheritance hierarchy -->

  <xsl:template match="family">

    <xsl:call-template name="section">
      <xsl:with-param name="toggleSwitch" select="'family'"/>
      <xsl:with-param name="title">
        <include item="familyTitle" />
      </xsl:with-param>
      <xsl:with-param name="content">
        <xsl:variable name="ancestorCount" select="count(ancestors/*)" />
        <xsl:variable name="childCount" select="count(descendents/*)" />
        
        <xsl:for-each select="ancestors/*">
          <xsl:sort select="position()" data-type="number" order="descending" />

          <xsl:call-template name="indent">
            <xsl:with-param name="count" select="position()" />
          </xsl:call-template>

         <xsl:call-template name="typeReferenceLink">
           <xsl:with-param name="api" select="@api" />
            <xsl:with-param name="qualified" select="true()" />
          </xsl:call-template>
          
          <br/>
        </xsl:for-each>

        <xsl:call-template name="indent">
          <xsl:with-param name="count" select="$ancestorCount + 1" />
        </xsl:call-template>
       
        <referenceLink target="{$key}" qualified="true"/>
        <br/>
        
        <xsl:choose>

          <xsl:when test="descendents/@derivedTypes">
            <xsl:call-template name="indent">
              <xsl:with-param name="count" select="$ancestorCount + 2" />
            </xsl:call-template>
            <referenceLink target="{descendents/@derivedTypes}" qualified="true">
              <include item="derivedClasses"/>
            </referenceLink>
          </xsl:when>
          <xsl:otherwise>
            
            <xsl:for-each select="descendents/*">
              <xsl:call-template name="indent">
                <xsl:with-param name="count" select="$ancestorCount + 2" />
              </xsl:call-template>
              
              <xsl:call-template name="typeReferenceLink">
                <xsl:with-param name="api" select="@api" />
                <xsl:with-param name="qualified" select="true()" />
              </xsl:call-template>
              <br/>
            </xsl:for-each>
          </xsl:otherwise>
        </xsl:choose>

      </xsl:with-param>
    </xsl:call-template>
  </xsl:template>
  
  <xsl:template name="createTableEntries">
    <xsl:param name="count" />
    <xsl:if test="number($count) > 0">
      <td>&#x20;</td>
      <xsl:call-template name="createTableEntries">
        <xsl:with-param name="count" select="number($count)-1" />
      </xsl:call-template>
		</xsl:if>
	</xsl:template>


	<!-- Link to create type -->

	<xsl:template match="arrayOf">
    <xsl:choose>
      <xsl:when test="type">
        <xsl:call-template name="typeReferenceLink">
          <xsl:with-param name="api" select="type/@api" />
          <xsl:with-param name="qualified" select="true()" />
        </xsl:call-template>
      </xsl:when>
      <xsl:otherwise>
        <xsl:apply-templates />
      </xsl:otherwise>
    </xsl:choose>
		<span class="cs">[<xsl:if test="number(@rank) &gt; 1">,</xsl:if>]</span>
		<!--<span class="vb">(<xsl:if test="number(@rank) &gt; 1">,</xsl:if>)</span>-->
	</xsl:template>

	<xsl:template match="pointerTo">
		<xsl:apply-templates /><xsl:text>*</xsl:text>
	</xsl:template>

	<xsl:template match="referenceTo">
		<xsl:apply-templates />
	</xsl:template>

  <xsl:template name="typeReferenceLink">
    <xsl:param name="api" />
    <xsl:param name="qualified" />
    
   <referenceLink target="{$api}" qualified="{$qualified}">
      <xsl:choose>
        <xsl:when test="specialization">
          <xsl:attribute name="show-templates">false</xsl:attribute>
        </xsl:when>
        <xsl:otherwise>
          <xsl:attribute name="show-templates">true</xsl:attribute>
        </xsl:otherwise>
      </xsl:choose>
    </referenceLink>
    <xsl:apply-templates select="specialization" />
  </xsl:template>
 
	<xsl:template match="template">
    <xsl:choose>
    <xsl:when test="@api=$key">
      <xsl:value-of select="@name" />
    </xsl:when>
      <xsl:otherwise>
        <include item="typeLinkToTypeParameter">
            <parameter>
              <xsl:value-of select="@name"/>
            </parameter>
            <parameter>
              <referenceLink target="{@api}" qualified="true" />
            </parameter>
         </include>
      </xsl:otherwise>
    </xsl:choose>
	</xsl:template>

	<xsl:template match="specialization">
		<span class="cs">&lt;</span>
		<!--<span class="vb"><xsl:text>(Of </xsl:text></span>-->
		<xsl:for-each select="*">
      <xsl:apply-templates select="." />
			<xsl:if test="position() != last()">
				<xsl:text>, </xsl:text>
			</xsl:if>
		</xsl:for-each>
		<span class="cs">&gt;</span>
		<!--<span class="vb">)</span>		-->
	</xsl:template>

	<xsl:template match="member">
		<xsl:apply-templates select="type" />
		<xsl:text>.</xsl:text>
		<xsl:choose>
			<xsl:when test="@display-api">
				<referenceLink target="{@api}" display-target="{@display-api}" />
			</xsl:when>
			<xsl:otherwise>
				<referenceLink target="{@api}" />
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<!-- Naming -->

	<xsl:template name="shortName">
		<xsl:choose>
			<xsl:when test="$subgroup='constructor'">
				<xsl:value-of select="/document/reference/containers/type/apidata/@name" />
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="/document/reference/apidata/@name" />
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

  <!--
Naming convention:
	html title
	topic title
	toc title
-->

  <!-- decorated names -->

  <xsl:template name="shortNameDecorated">
    <!--<xsl:param name="titleType" /> -->
    <xsl:choose>
      <!-- type overview pages get the type name -->
      <xsl:when test="$group='type'">
        <xsl:for-each select="/document/reference[1]">
          <xsl:call-template name="typeNameDecorated" />
        </xsl:for-each>
      </xsl:when>
      <!-- constructors and member list pages also use the type name -->
      <xsl:when test="$subgroup='constructor' or $group='members'">
        <xsl:for-each select="/document/reference/containers/type[1]">
          <xsl:call-template name="typeNameDecorated" />
        </xsl:for-each>
      </xsl:when>
      <!--
      <xsl:when test="$group='member'">
        <xsl:variable name="type">
          <xsl:for-each select="/document/reference">
            <xsl:call-template name="GetTypeName" />
          </xsl:for-each>
        </xsl:variable>
        <xsl:choose>
          <xsl:when test="$titleType = 'tocTitle'">
            <xsl:value-of select="$type" />
          </xsl:when>
          <xsl:otherwise>
            <xsl:value-of select="concat($typeName, '.', $type)"/>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:when>
      -->
      <!-- member pages use the qualified member name -->
      <xsl:when test="$group='member'">
        <xsl:for-each select="/document/reference/containers/type[1]">
          <xsl:call-template name="typeNameDecorated" />
        </xsl:for-each>
        <span class="cs">.</span>
        <span class="vb">.</span>
        <span class="cpp">::</span>
        <xsl:for-each select="/document/reference[1]">
          <xsl:value-of select="apidata/@name" />
          <xsl:call-template name="allTemplatesDecorated" />
        </xsl:for-each>
      </xsl:when>
      <!-- namespace (and any other) topics just use the name -->
      <xsl:when test="/document/reference/apidata/@name = ''">
        <include item="defaultNamespace" />
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="/document/reference/apidata/@name" />
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template name="typeNameDecorated">
    <xsl:if test="(containers/type)|type">
      <xsl:for-each select="(containers/type)|type">
        <xsl:call-template name="typeNameDecorated" />
      </xsl:for-each>
      <xsl:text>.</xsl:text>
    </xsl:if>
    <xsl:value-of select="apidata/@name" />
    <xsl:if test="count(templates/template) > 0">
      <xsl:call-template name="allTemplatesDecorated" />
    </xsl:if>
  </xsl:template>
<!--
  <xsl:template name="longNameDecorated">
    <xsl:call-template name="shortNameDecorated" />
    <xsl:call-template name="parameterNames" />
  </xsl:template>
-->
  <xsl:template name="parameterTypes">
    <xsl:if test="count(/document/reference/parameters/parameter) &gt; 0">
      <xsl:text>(</xsl:text>
      <xsl:for-each select="/document/reference/parameters/parameter">
        <xsl:apply-templates select="type|arrayOf|pointerTo|template" mode="paramtype"/>
        <xsl:if test="position() != last()">
          <xsl:text>, </xsl:text>
        </xsl:if>
      </xsl:for-each>
      <xsl:text>)</xsl:text>
    </xsl:if>
  </xsl:template>

  <xsl:template match="arrayOf" mode="paramtype">
    <xsl:apply-templates mode="paramtype"/>
    <span class="cs">[<xsl:if test="number(@rank) &gt; 1">,</xsl:if>]</span>
    <span class="vb">(<xsl:if test="number(@rank) &gt; 1">,</xsl:if>)</span>
  </xsl:template>

  <xsl:template match="pointerTo" mode="paramtype">
    <xsl:apply-templates mode="paramtype"/><xsl:text>*</xsl:text>
  </xsl:template>

  <xsl:template match="type" mode="paramtype">
    <xsl:value-of select="apidata/@name" />
    <xsl:apply-templates select="specialization" mode="paramtype" />
  </xsl:template>
  
  <xsl:template match="template" mode="paramtype">
    <xsl:value-of select="@name" />
  </xsl:template>

  <xsl:template match="specialization" mode="paramtype">
    <span class="cs">&lt;</span>
    <span class="vb">
      <xsl:text>(Of </xsl:text>
    </span>
    <span class="cpp">&lt;</span>
    <xsl:for-each select="*">
      <xsl:apply-templates select="." mode="paramtype" />
      <xsl:if test="position() != last()">
        <xsl:text>, </xsl:text>
      </xsl:if>
    </xsl:for-each>
    <span class="cs">&gt;</span>
    <span class="vb">)</span>
    <span class="cpp">&gt;</span>
  </xsl:template>
  
  <xsl:template name="allTemplatesDecorated">
    <xsl:if test="templates/template">
      <span class="cs">&lt;</span>
      <span class="vb">
        <xsl:text>(Of </xsl:text>
      </span>
      <span class="cpp">&lt;</span>
      <xsl:for-each select="templates/template">
        <xsl:value-of select="@name" />
        <xsl:if test="not(position()=last())">
          <xsl:text>, </xsl:text>
        </xsl:if>
      </xsl:for-each>
      <span class="cs">&gt;</span>
      <span class="vb">)</span>
      <span class="cpp">&gt;</span>
    </xsl:if>
  </xsl:template>

  <!-- plain names -->

  <xsl:template name="shortNamePlain">
    <xsl:choose>
      <!-- type overview pages get the type name -->
      <xsl:when test="$group='type'">
        <xsl:for-each select="/document/reference[1]">
          <xsl:call-template name="typeNamePlain" />
        </xsl:for-each>
      </xsl:when>
      <!-- constructors and member list pages also use the type name -->
      <xsl:when test="$subgroup='constructor' or $group='members'">
        <xsl:for-each select="/document/reference/containers/type[1]">
          <xsl:call-template name="typeNamePlain" />
        </xsl:for-each>
      </xsl:when>
      <!-- namespace, member (and any other) topics just use the name -->
      <xsl:when test="/document/reference/apidata/@name = ''">
        <include item="defaultNamespace" />
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="/document/reference/apidata/@name" />
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template name="typeNamePlain">
    <xsl:if test="(containers/type)|type">
      <xsl:for-each select="(containers/type)|type">
        <xsl:call-template name="typeNamePlain" />
      </xsl:for-each>
      <xsl:text>.</xsl:text>
    </xsl:if>
    <xsl:value-of select="apidata/@name" />
  </xsl:template>

  <xsl:template name="parameterTypesPlain">
    <xsl:if test="count(/document/reference/parameters/parameter) &gt; 0">
      <xsl:text>(</xsl:text>
      <xsl:for-each select="/document/reference/parameters/parameter">
        <xsl:apply-templates select="type|arrayOf|pointerTo|template" mode="parameterTypesPlain"/>
        <xsl:if test="position() != last()">
          <xsl:text>, </xsl:text>
        </xsl:if>
      </xsl:for-each>
      <xsl:text>)</xsl:text>
    </xsl:if>
  </xsl:template>

  <xsl:template match="arrayOf" mode="parameterTypesPlain">
    <xsl:apply-templates mode="parameterTypesPlain"/>
    <xsl:text>[</xsl:text>
    <xsl:if test="number(@rank) &gt; 1">,</xsl:if>
    <xsl:text>]</xsl:text>
  </xsl:template>

  <xsl:template match="pointerTo" mode="parameterTypesPlain">
    <xsl:apply-templates mode="parameterTypesPlain"/>
    <xsl:text>*</xsl:text>
  </xsl:template>

  <xsl:template match="type" mode="parameterTypesPlain">
    <xsl:if test="specialization">
      <xsl:text>Generic </xsl:text>
    </xsl:if>
    <xsl:value-of select="apidata/@name" />
  </xsl:template>

  <xsl:template match="template" mode="parameterTypesPlain">
    <xsl:value-of select="@name" />
  </xsl:template>

  <!--
  <xsl:template name="longNamePlain">
    <xsl:call-template name="shortNamePlain" />
    <xsl:call-template name="parameterNames" />
  </xsl:template>
  -->

</xsl:stylesheet>
