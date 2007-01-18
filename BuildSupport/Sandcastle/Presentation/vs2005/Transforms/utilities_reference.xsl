<?xml version="1.0"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.1"
		xmlns:MSHelp="http://msdn.microsoft.com/mshelp" >

	<xsl:output method="html" omit-xml-declaration="yes" encoding="utf-8" doctype-public="-//W3C//DTD HTML 4.0 Transitional//EN" doctype-system="http://www.w3.org/TR/html4/loose.dtd" />

	<!-- key parameter is the api identifier string -->
	<xsl:param name="key" />
	<xsl:param name="metadata" value="false" />
	<xsl:param name="languages" />
    
	<xsl:include href="utilities_metadata.xsl" />
  
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
              <xsl:text>&#x20;(</xsl:text>
              <xsl:apply-templates select="*[1]" />
              <xsl:text>)</xsl:text>
            </dt>
            <dd>
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
				<xsl:call-template name="createReferenceLink">
					<xsl:with-param name="id" select="@api" />
				</xsl:call-template>
			</td>
			<td>
				<xsl:call-template name="getElementDescription" />
			</td>
		</tr>
	</xsl:template>

	<xsl:template match="element" mode="member">
   <xsl:param name="subgroup" />
   <xsl:param name="visibility" />
   <xsl:variable name="subgroupId" select="apidata/@subgroup"/>
    <xsl:variable name="visibilityId" select="memberdata/@visibility"/>
   
    <xsl:if test="$subgroup = $subgroupId and $visibility=$visibilityId">
      <tr>
        <xsl:attribute name="data">
          <xsl:value-of select="apidata/@subgroup" />
          <xsl:text>; public</xsl:text>
        </xsl:attribute>
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
          <xsl:call-template name="getElementDescription" />
          <xsl:choose>
            <xsl:when test="($group != 'member') and not(starts-with($key,containers/type/@api))">
              <xsl:text> </xsl:text>
              <include item="inheritedFrom">
                <parameter>
                  <xsl:apply-templates select="containers/type" />
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
          <xsl:if test="attributes/@obsolete='true'">
            <xsl:text> </xsl:text>
            <include item="obsoleteShort" />
          </xsl:if>
        </td>
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
          <xsl:call-template name="getElementDescription" />
          <xsl:if test="attributes/@obsolete='true'">
            <xsl:text> </xsl:text>
            <include item="obsoleteShort" />
          </xsl:if>
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
        <xsl:call-template name="getElementDescription" />
        <xsl:if test="attributes/@obsolete='true'">
          <xsl:text> </xsl:text>
          <include item="obsoleteShort" />
        </xsl:if>
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
				<xsl:call-template name="getElementDescription" />
			</td>
		</tr>
	</xsl:template>

  <xsl:template match="element" mode="derivedType">
    <tr>
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
        <xsl:call-template name="getElementDescription" />
        <xsl:choose>
          <xsl:when test="($group != 'member') and ($group != 'derivedType') and string(containers/type/@api) != $key">
            <xsl:text> </xsl:text>
            <include item="inheritedFrom">
              <parameter>
                <xsl:apply-templates select="containers/type" />
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
        <xsl:if test="attributes/@obsolete='true'">
          <xsl:text> </xsl:text>
          <include item="obsoleteShort" />
        </xsl:if>
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
        <xsl:call-template name="getElementDescription" />
        <xsl:choose>
					<xsl:when test="($group != 'member') and (string(containers/type/@api) != $key)">
            <xsl:text> </xsl:text>
            <include item="inheritedFrom">
              <parameter>
                <xsl:apply-templates select="containers/type" />
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
        <xsl:if test="attributes/@obsolete='true'">
          <xsl:text> </xsl:text>
          <include item="obsoleteShort" />
        </xsl:if>
      </td>
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
        <xsl:call-template name="parameterNames" />
      </parameter>
    </include>
  </xsl:template>

  <xsl:template name="topicTitleDecorated">
    <include>
      <xsl:attribute name="item">
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
        <xsl:call-template name="shortNameDecorated" />
      </parameter>
      <parameter>
        <xsl:call-template name="parameterNames" />
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

	<xsl:template match="syntax">
    <xsl:if test="count(*) > 0">
      <xsl:call-template name="section">
        <xsl:with-param name="toggleSwitch" select="'syntax'" />
        <xsl:with-param name="title">
          <include item="syntaxTitle"/>
        </xsl:with-param>
        <xsl:with-param name="content">
          <div id="syntaxCodeBlocks" class="code">
            <xsl:call-template name="syntaxBlocks" />
          </div>
          <!-- parameters & return value -->
          <xsl:apply-templates select="/document/reference/parameters" />
          <xsl:apply-templates select="/document/comments/value" />
          <xsl:apply-templates select="/document/comments/returns" />
        </xsl:with-param>
      </xsl:call-template>
    </xsl:if>
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

    <xsl:for-each select="/document/syntax/div[@codeLanguage='JSharp']">
      <xsl:call-template name="languageSyntaxBlock"/>
    </xsl:for-each>

    <xsl:for-each select="/document/syntax/div[@codeLanguage='JScript']">
      <xsl:call-template name="languageSyntaxBlock"/>
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
          <include item="typeIconHeader"/>
       </th>
        <th class="nameColumn">
          <include item="typeNameHeader"/>
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
    <xsl:param name="visibility" />
    <table id="typeList" class="members">
      <tr>
        <th class="iconColumn">
          <include item="typeIconHeader"/>
        </th>
        <th class="nameColumn">
          <include item="typeNameHeader"/>
        </th>
        <th class="descriptionColumn">
          <include item="typeDescriptionHeader" />
        </th>
      </tr>
      <xsl:apply-templates select="element" mode="member">
        <xsl:sort select="apidata/@name" />
        
        <xsl:with-param name="subgroup" select="$subgroup"/>
        <xsl:with-param name="visibility" select="$visibility"/>
      </xsl:apply-templates>
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
      <xsl:if test="memberdata[@visibility='family'] and apidata[@subgroup=$subgroup]">
        protected;
      </xsl:if>
  </xsl:template>

  <xsl:template name="memberSection">
    <xsl:param name="subgroup" />

    <xsl:variable name="visibility">
      <xsl:apply-templates select="element" mode="members">
        <xsl:with-param name="subgroup" select="$subgroup"/>
        <xsl:sort select="apidata/@name"/>
      </xsl:apply-templates>
    </xsl:variable>

    <xsl:if test="contains($visibility, 'public')">
      <xsl:variable name="header" select="concat('Public', $subgroup)" />
      <xsl:call-template name="section">
        <xsl:with-param name="toggleSwitch" select="$header" />
        <xsl:with-param name="title">
          <include item="{$header}" />
        </xsl:with-param>
        <xsl:with-param name="content">
          <xsl:call-template name="membersList">
            <xsl:with-param name="subgroup" select="$subgroup" />
            <xsl:with-param name="visibility" select="'public'" />
          </xsl:call-template>
        </xsl:with-param>
      </xsl:call-template>
    </xsl:if>
    <xsl:if test="contains($visibility, 'protected')">
      <xsl:variable name="header" select="concat('Protected', $subgroup)" />
      <xsl:call-template name="section">
        <xsl:with-param name="toggleSwitch" select="$header" />
        <xsl:with-param name="title">
          <include item="{$header}" />
        </xsl:with-param>
        <xsl:with-param name="content">
          <xsl:call-template name="membersList">
            <xsl:with-param name="subgroup" select="$subgroup" />
            <xsl:with-param name="visibility" select="'family'" />
          </xsl:call-template>
        </xsl:with-param>
      </xsl:call-template>
    </xsl:if>
  </xsl:template>

  <xsl:template match="elements" mode="member">
    
    <xsl:if test="element/apidata[@subgroup='method']">
      <xsl:call-template name="memberSection">
        <xsl:with-param name="subgroup" select="'method'" />
      </xsl:call-template>
    </xsl:if>
    
    <xsl:if test="element/apidata[@subgroup='field']">
      <xsl:call-template name="memberSection">
        <xsl:with-param name="subgroup" select="'field'" />
      </xsl:call-template>
    </xsl:if>
       
    <xsl:if test="element/apidata[@subgroup='constructor']">
      <xsl:call-template name="memberSection">
        <xsl:with-param name="subgroup" select="'constructor'" />
      </xsl:call-template>
    </xsl:if>
   
    <xsl:if test="element/apidata[@subgroup='property']">
      <xsl:call-template name="memberSection">
        <xsl:with-param name="subgroup" select="'property'" />
      </xsl:call-template>
    </xsl:if>
    
    <xsl:if test="element/apidata[@subgroup='event']">
     <xsl:call-template name="memberSection">
        <xsl:with-param name="subgroup" select="'event'" />
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
              <th class="iconColumn">
                <include item="memberIconHeader"/>
              </th>
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
						<th class="iconColumn"><include item="memberIconHeader"/></th>
						<th class="nameColumn"><include item="memberNameHeader"/></th>
						<th class="descriptionColumn"><include item="memberDescriptionHeader" /></th>
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
						<xsl:when test="memberdata/@visiblity='protected'">
							<xsl:text>prot</xsl:text>
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
				<xsl:choose>
					<xsl:when test="apidata/@subgroup='field'">
						<img>
							<includeAttribute name="src" item="iconPath">
								<parameter>pubfield.gif</parameter>
							</includeAttribute>
						</img>
					</xsl:when>
					<xsl:when test="apidata/@subgroup='property'">
						<img>
							<includeAttribute name="src" item="iconPath">
								<parameter>pubproperty.gif</parameter>
							</includeAttribute>
						</img>
					</xsl:when>
					<xsl:when test="apidata/@subgroup='method' or apidata/@subgroup='constructor'">
						<img>
							<includeAttribute name="src" item="iconPath">
								<parameter><xsl:value-of select="$memberIcon" /></parameter>
							</includeAttribute>
						</img>
					</xsl:when>
					<xsl:when test="apidata/@subgroup='event'">
						<img>
							<includeAttribute name="src" item="iconPath">
								<parameter>pubevent.gif</parameter>
							</includeAttribute>
						</img>
					</xsl:when>
				</xsl:choose>
				<xsl:if test="memberdata/@static='true'">
					<img>
						<includeAttribute name="src" item="iconPath">
							<parameter>static.gif</parameter>
						</includeAttribute>
						<includeAttribute name="alt" item="staticAltText" />
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

	<xsl:template match="library">
    <p/>
    <include item="requirementsNamespaceLayout">
      <parameter>
        <xsl:value-of select="$namespaceName"/>
      </parameter>
    </include>
    <br/>
    <include item="requirementsAssemblyLayout">
      <parameter>
        <xsl:value-of select="@assembly"/>
      </parameter>
      <parameter>
        <xsl:value-of select="@module"/>
      </parameter>
    </include>
		
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
        <ul>
          <xsl:for-each select="versions">
            <li>
              <include item="{@name}" />
              <xsl:text>: </xsl:text>
              <xsl:call-template name="processVersions" />
            </li>
          </xsl:for-each>
        </ul>
      </xsl:when>
      <xsl:otherwise>
        <xsl:for-each select="version">
          <include item="{@name}" />
          <xsl:if test="not(position()=last())">
            <xsl:text>, </xsl:text>
          </xsl:if>
        </xsl:for-each>
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

          <xsl:apply-templates select="." />
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

              <xsl:apply-templates select="." />
              <br/>
            </xsl:for-each>
          </xsl:otherwise>
        </xsl:choose>

      </xsl:with-param>
    </xsl:call-template>
  </xsl:template>

  <!-- indent by 2*n spaces -->
  <xsl:template name="indent">
    <xsl:param name="count" />
    <xsl:if test="$count &gt; 1">
      <xsl:text>&#160;&#160;</xsl:text>
      <xsl:call-template name="indent">
        <xsl:with-param name="count" select="$count - 1" />
      </xsl:call-template>
    </xsl:if>
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
		<xsl:apply-templates />
		<span class="cs">[<xsl:if test="number(@rank) &gt; 1">,</xsl:if>]</span>
		<span class="vb">(<xsl:if test="number(@rank) &gt; 1">,</xsl:if>)</span>
	</xsl:template>

	<xsl:template match="pointerTo">
		<xsl:apply-templates /><xsl:text>*</xsl:text>
	</xsl:template>

	<xsl:template match="referenceTo">
		<xsl:apply-templates />
	</xsl:template>

	<xsl:template match="type">
		<referenceLink target="{@api}">
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
		<xsl:value-of select="@name" />
	</xsl:template>

	<xsl:template match="specialization">
		<span class="cs">&lt;</span>
		<span class="vb"><xsl:text>(Of </xsl:text></span>
		<xsl:for-each select="*">
			<xsl:apply-templates select="." />
			<xsl:if test="position() != last()">
				<xsl:text>, </xsl:text>
			</xsl:if>
		</xsl:for-each>
		<span class="cs">&gt;</span>
		<span class="vb">)</span>		
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
    <xsl:choose>
      <xsl:when test="$group='type'">
        <xsl:for-each select="/document/reference">
          <xsl:call-template name="typeNameDecorated" />
        </xsl:for-each>
      </xsl:when>
      <xsl:when test="$subgroup='constructor'">
        <xsl:for-each select="/document/reference/containers/type">
          <xsl:call-template name="typeNameDecorated" />
        </xsl:for-each>
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

  <xsl:template name="longNameDecorated">
    <xsl:call-template name="shortNameDecorated" />
    <xsl:call-template name="parameterNames" />
  </xsl:template>

  <xsl:template name="parameterNames">
    <xsl:if test="count(/document/reference/parameters/parameter) &gt; 0">
      <xsl:text>(</xsl:text>
      <xsl:for-each select="/document/reference/parameters/parameter">
        <xsl:value-of select="@name" />
        <xsl:if test="position() != last()">
          <xsl:text>, </xsl:text>
        </xsl:if>
      </xsl:for-each>
      <xsl:text>)</xsl:text>
    </xsl:if>
  </xsl:template>

  <xsl:template name="allTemplatesDecorated">
    <span class="cs">&lt;</span>
    <span class="vb">
      <xsl:text>(Of </xsl:text>
    </span>
    <xsl:for-each select="templates/template">
      <xsl:value-of select="@name" />
      <xsl:if test="not(position()=last())">
        <xsl:text>, </xsl:text>
      </xsl:if>
    </xsl:for-each>
    <span class="cs">&gt;</span>
    <span class="vb">)</span>
  </xsl:template>

  <!-- plain names -->

  <xsl:template name="shortNamePlain">
    <xsl:choose>
      <xsl:when test="$group='type'">
        <xsl:for-each select="/document/reference">
          <xsl:call-template name="typeNamePlain" />
        </xsl:for-each>
      </xsl:when>
      <xsl:when test="$subgroup='constructor'">
        <xsl:for-each select="/document/reference/containers/type">
          <xsl:call-template name="typeNamePlain" />
        </xsl:for-each>
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

  <xsl:template name="longNamePlain">
    <xsl:call-template name="shortNamePlain" />
    <xsl:call-template name="parameterNames" />
  </xsl:template>



</xsl:stylesheet>
