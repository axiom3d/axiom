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
				<script type="text/javascript">
					<xsl:text>var store = new CookieDataStore('docs');</xsl:text>
					<xsl:text>registerEventHandler(window, 'load', function() { var ss = new SplitScreen('control', 'main'); selectLanguage(store.get('lang')); });</xsl:text>
				</script>
				<xsl:call-template name="control"/>
				<xsl:call-template name="main"/>
			</body>
		</html>
	</xsl:template>

	<!-- useful global variables -->

	<xsl:variable name="group" select="/document/reference/apidata/@group" />
	<xsl:variable name="subgroup" select="/document/reference/apidata/@subgroup" />
	<xsl:variable name="subsubgroup" select="/document/reference/apidata/@subsubgroup" />

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
			<includeAttribute name="src" item="scriptPath"><parameter>StyleUtilities.js</parameter></includeAttribute>
		</script>
		<script type="text/javascript">
			<includeAttribute name="src" item="scriptPath"><parameter>SplitScreen.js</parameter></includeAttribute>
		</script>
		<script type="text/javascript">
			<includeAttribute name="src" item="scriptPath"><parameter>ElementCollection.js</parameter></includeAttribute>
		</script>
		<script type="text/javascript">
			<includeAttribute name="src" item="scriptPath"><parameter>MemberFilter.js</parameter></includeAttribute>
		</script>
		<script type="text/javascript">
			<includeAttribute name="src" item="scriptPath"><parameter>CollapsibleSection.js</parameter></includeAttribute>
		</script>
		<script type="text/javascript">
			<includeAttribute name="src" item="scriptPath"><parameter>LanguageFilter.js</parameter></includeAttribute>
		</script>
		<script type="text/javascript">
			<includeAttribute name="src" item="scriptPath"><parameter>CookieDataStore.js</parameter></includeAttribute>
		</script>
	</xsl:template>

	<xsl:template match="parameters">
		<xsl:call-template name="section">
			<xsl:with-param name="title"><include item="parametersTitle" /></xsl:with-param>
			<xsl:with-param name="content">
				<dl>
					<xsl:for-each select="parameter">
						<xsl:variable name="parameterName" select="@name" />
						<dt>
							<span class="parameter"><xsl:value-of select="$parameterName"/></span>
							<xsl:text>&#xa0;(</xsl:text>
							<xsl:apply-templates select="*[1]" />
							<xsl:text>)</xsl:text>
						</dt>
						<dd>
							<xsl:call-template name="getParameterDescription">
								<xsl:with-param name="name" select="@name" />
							</xsl:call-template>
						</dd>
					</xsl:for-each>
				</dl>
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

	<xsl:template match="element" mode="namespace">
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

	<xsl:template match="element" mode="type">
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
			<span class="productTitle"><include item="productTitle" /></span><br/>
			<span class="topicTitle"><xsl:call-template name="topicTitleDecorated" /></span><br/>
			<div id="toolbar">
				<span id="chickenFeet"><xsl:call-template name="chickenFeet" /></span>
				<xsl:if test="count($languages/language) &gt; 0">
					<span id="languageFilter">
						<select id="languageSelector" onchange="var names = this.value.split(' '); toggleVisibleLanguage(names[1]); lfc.switchLanguage(names[0]); store.set('lang',this.value); store.save();">
							<xsl:for-each select="$languages/language">
								<option value="{@name} {@style}"><include item="{@label}Label" /></option>
							</xsl:for-each>
						</select>
					</span>
					<script>var sd = getStyleDictionary(); var lfc = new LanguageFilterController();</script>
				</xsl:if>
			</div>
		</div>
	</xsl:template>

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
<!--
				<xsl:choose>
					<xsl:when test="boolean($group='type')">
						<xsl:for-each select="/document/reference"><xsl:call-template name="typeTitle" /></xsl:for-each>
					</xsl:when>
					<xsl:when test="$subgroup='constructor'">
						<xsl:for-each select="/document/reference/containers/type"><xsl:call-template name="typeTitle"/></xsl:for-each>
					</xsl:when>
					<xsl:otherwise>
						<xsl:value-of select="/document/reference/apidata/@name" />
					</xsl:otherwise>
				</xsl:choose>
-->
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

	<!-- chicken feet navigation -->

	<xsl:template name="chickenFeet">
		<include item="rootLink" />
		<xsl:if test="boolean(/document/reference/containers/namespace)">
				<xsl:text> &gt; </xsl:text>
				<referenceLink target="{document/reference/containers/namespace/@api}" />
		</xsl:if>
		<xsl:if test="boolean(/document/reference/containers/type)">
				<xsl:text> &gt; </xsl:text>
				<xsl:apply-templates select="/document/reference/containers/type" />
		</xsl:if>
		<xsl:if test="not($group='root')">
			<xsl:text> &gt; </xsl:text>
			<referenceLink target="{$key}" />
		</xsl:if>
	</xsl:template>

	<!-- main window -->

	<xsl:template name="main">
		<div id="main">
			<xsl:call-template name="head" />
			<xsl:call-template name="body" />
			<xsl:call-template name="foot" />
		</div>
	</xsl:template>

	<xsl:template name="head">
		<include item="header" />
	</xsl:template>

	<xsl:template match="syntax">
		<xsl:if test="count(*) > 0">
			<xsl:call-template name="section">
				<xsl:with-param name="title"><include item="syntaxTitle" /></xsl:with-param>
				<xsl:with-param name="content">
					<table class="filter">
						<tr class="tabs" id="syntaxTabs">
							<xsl:for-each select="div[@codeLanguage]">
								<td class="tab" x-lang="{@codeLanguage}" onclick="st.toggleClass('x-lang','{@codeLanguage}','activeTab','tab'); sb.toggleStyle('x-lang','{@codeLanguage}','display','block','none');" ><include item="{@codeLanguage}Label" /></td>
							</xsl:for-each>
						</tr>
					</table>
					<div id="syntaxBlocks">
							<xsl:for-each select="div[@codeLanguage]">
								<div class="code" x-lang="{@codeLanguage}"><pre><xsl:copy-of select="./node()" /></pre></div>
							</xsl:for-each>
					</div>
					<script type="text/javascript"><xsl:text>
						var st = new ElementCollection('syntaxTabs');
						var sb = new ElementCollection('syntaxBlocks');
						lfc.registerTabbedArea(st, sb);
						st.toggleClass('x-lang','</xsl:text><xsl:value-of select="div[1]/@codeLanguage" /><xsl:text>','activeTab','tab');
						sb.toggleStyle('x-lang','</xsl:text><xsl:value-of select="div[1]/@codeLanguage" /><xsl:text>','display','block','none');
					</xsl:text></script>
				</xsl:with-param>
			</xsl:call-template>
		</xsl:if>
	</xsl:template>

	<xsl:template match="elements" mode="root">
		<xsl:if test="count(element) > 0">
			<xsl:call-template name="section">
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

	<xsl:template match="elements" mode="namespace">
		<xsl:call-template name="section">
			<xsl:with-param name="title"><include item="typesTitle" /></xsl:with-param>
			<xsl:with-param name="content">
				<table class="filter">
					<tr class="tabs" id="typeFilter">
						<td class="tab" value="all" onclick="tt.toggleClass('value','all','activeTab','tab'); tf.subgroup='all'; ts.process(getInstanceDelegate(tf,'filterElement'));"><include item="allTypesFilterLabel" /></td>
						<td class="tab" value="class" onclick="tt.toggleClass('value','class','activeTab','tab'); tf.subgroup='class'; ts.process(getInstanceDelegate(tf,'filterElement'));"><include item="classTypesFilterLabel" /></td>
						<td class="tab" value="structure" onclick="tt.toggleClass('value','structure','activeTab','tab'); tf.subgroup='structure'; ts.process(getInstanceDelegate(tf,'filterElement'));"><include item="structureTypesFilterLabel" /></td>
						<td class="tab" value="interface" onclick="tt.toggleClass('value','interface','activeTab','tab'); tf.subgroup='interface'; ts.process(getInstanceDelegate(tf,'filterElement'));"><include item="interfaceTypesFilterLabel" /></td>
						<td class="tab" value="enumeration" onclick="tt.toggleClass('value','enumeration','activeTab','tab'); tf.subgroup='enumeration'; ts.process(getInstanceDelegate(tf,'filterElement'));"><include item="enumerationTypesFilterLabel" /></td>
						<td class="tab" value="delegate" onclick="tt.toggleClass('value','delegate','activeTab','tab'); tf.subgroup='delegate'; ts.process(getInstanceDelegate(tf,'filterElement'));"><include item="delegateTypesFilterLabel" /></td>
					</tr>
				</table>
				<table id="typeList" class="members">
					<tr>
						<th class="iconColumn"><include item="typeIconHeader"/></th>
						<th class="nameColumn"><include item="typeNameHeader"/></th>
						<th class="descriptionColumn"><include item="typeDescriptionHeader" /></th>
					</tr>
					<xsl:apply-templates select="element" mode="namespace">
						<xsl:sort select="apidata/@name" />
					</xsl:apply-templates>
				</table>
				<script type="text/javascript"><xsl:text>
					var tt = new ElementCollection('typeFilter');
					var ts = new ElementCollection('typeList');
					var tf = new TypeFilter();
					tt.toggleClass('value','all','activeTab','tab');
				</xsl:text></script>
			</xsl:with-param>
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="elements" mode="enumeration">
		<xsl:if test="count(element) > 0">
			<xsl:call-template name="section">
				<xsl:with-param name="title"><include item="membersTitle" /></xsl:with-param>
				<xsl:with-param name="content">
					<table class="members" id="memberList">
						<tr>
							<th class="nameColumn"><include item="memberNameHeader"/></th>
							<th class="descriptionColumn"><include item="memberDescriptionHeader" /></th>
						</tr>
						<xsl:apply-templates select="element" mode="enumeration">
							<xsl:sort select="apidata/@name" />
						</xsl:apply-templates>
					</table>
				</xsl:with-param>
			</xsl:call-template>
		</xsl:if>
	</xsl:template>

	<xsl:template match="elements" mode="type">
		<xsl:if test="count(element) > 0">
			<xsl:call-template name="section">
				<xsl:with-param name="title"><include item="membersTitle" /></xsl:with-param>
				<xsl:with-param name="content">
				<table class="filter">
					<tr class="tabs" id="memberTabs">
						<td class="tab" value="all" onclick="mt.toggleClass('value','all','activeTab','tab'); mf.subgroup='all'; ms.process(getInstanceDelegate(mf,'filterElement'));"><include item="allMembersFilterLabel" /></td>
						<td class="tab" value="constructor" onclick="mt.toggleClass('value','constructor','activeTab','tab'); mf.subgroup='constructor'; ms.process(getInstanceDelegate(mf,'filterElement'));"><include item="constructorMembersFilterLabel" /></td>
						<td class="tab" value="method" onclick="mt.toggleClass('value','method','activeTab','tab'); mf.subgroup='method'; ms.process(getInstanceDelegate(mf,'filterElement'));"><include item="methodMembersFilterLabel" /></td>
						<td class="tab" value="property" onclick="mt.toggleClass('value','property','activeTab','tab'); mf.subgroup='property'; ms.process(getInstanceDelegate(mf,'filterElement'));"><include item="propertyMembersFilterLabel" /></td>
						<td class="tab" value="field" onclick="mt.toggleClass('value','field','activeTab','tab'); mf.subgroup='field'; ms.process(getInstanceDelegate(mf,'filterElement'));"><include item="fieldMembersFilterLabel" /></td>
						<td class="tab" value="event" onclick="mt.toggleClass('value','event','activeTab','tab'); mf.subgroup='event'; ms.process(getInstanceDelegate(mf,'filterElement'));"><include item="eventMembersFilterLabel" /></td>
					</tr>
					<tr>
						<td class="line" colspan="2">
						        <label for="public"><input id="public" type="checkbox" checked="true" onclick="mf['public'] = this.checked; ms.process(getInstanceDelegate(mf,'filterElement'));" /> <include item="publicMembersFilterLabel" /></label><br/>
        						<label for="protected"><input id="protected" type="checkbox" checked="true" onclick="mf['protected'] = this.checked; ms.process(getInstanceDelegate(mf,'filterElement'));" /> <include item="protectedMembersFilterLabel" /></label>
						</td>
						<td class="line" colspan="2">
						        <label for="instance"><input id="instance" type="checkbox" checked="true" onclick="mf['instance'] = this.checked; ms.process(getInstanceDelegate(mf,'filterElement'));" /> <include item="instanceMembersFilterLabel" /></label><br/>
        						<label for="static"><input id="static" type="checkbox" checked="true" onclick="mf['static'] = this.checked; ms.process(getInstanceDelegate(mf,'filterElement'));" /> <include item="staticMembersFilterLabel" /></label>
						</td>
						<td class="line" colspan="2">
						        <label for="declared"><input id="declared" type="checkbox" checked="true" onclick="mf['declared'] = this.checked; ms.process(getInstanceDelegate(mf,'filterElement'));" /> <include item="declaredMembersFilterLabel" /></label><br/>
        						<label for="inherited"><input id="inherited" type="checkbox" checked="true" onclick="mf['inherited'] = this.checked; ms.process(getInstanceDelegate(mf,'filterElement'));" /> <include item="inheritedMembersFilterLabel" /></label>
						</td>
					</tr>
				</table>
				<table class="members" id="memberList">
					<tr>
						<th class="iconColumn"><include item="memberIconHeader"/></th>
						<th class="nameColumn"><include item="memberNameHeader"/></th>
						<th class="descriptionColumn"><include item="memberDescriptionHeader" /></th>
					</tr>
					<xsl:apply-templates select="element" mode="type">
						<xsl:sort select="apidata/@name" />
					</xsl:apply-templates>
				</table>
				<script type="text/javascript"><xsl:text>
					var mt = new ElementCollection('memberTabs');
					var ms = new ElementCollection('memberList');
					var mf = new MemberFilter();
					mt.toggleClass('value','all','activeTab','tab');
				</xsl:text></script>
				</xsl:with-param>
			</xsl:call-template>
		</xsl:if>
	</xsl:template>

	<xsl:template match="elements" mode="overload">
		<xsl:if test="count(element) > 0">
			<xsl:call-template name="section">
				<xsl:with-param name="title"><include item="membersTitle" /></xsl:with-param>
				<xsl:with-param name="content">
				<table class="members" id="memberList">
					<tr>
						<th class="iconColumn"><include item="memberIconHeader"/></th>
						<th class="nameColumn"><include item="memberNameHeader"/></th>
						<th class="descriptionColumn"><include item="memberDescriptionHeader" /></th>
					</tr>
					<xsl:apply-templates select="element" mode="type">
						<xsl:sort select="apidata/@name" />
					</xsl:apply-templates>
				</table>
				</xsl:with-param>
			</xsl:call-template>
		</xsl:if>
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
		<include item="footer" />
	</xsl:template>

	<!-- Assembly information -->

	<xsl:template match="library">
		<p><include item="locationInformation">
			<parameter><xsl:value-of select="@assembly"/></parameter>
			<parameter><xsl:value-of select="@module" /></parameter>
		</include></p>
	</xsl:template>

  <!-- Version information -->

  <xsl:template match="versions">
    <xsl:call-template name="section">
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
			<xsl:with-param name="title"><include item="familyTitle" /></xsl:with-param>
			<xsl:with-param name="content">
				<xsl:variable name="ancestorCount" select="count(ancestors/*)" />
				<xsl:variable name="childCount" select="count(descendents/*)" />
				<xsl:variable name="columnCount">
					<xsl:choose>
						<xsl:when test="$childCount = 0">
							<xsl:value-of select="$ancestorCount + 1" />
						</xsl:when>
						<xsl:otherwise>
							<xsl:value-of select="$ancestorCount + 2" />
						</xsl:otherwise>
					</xsl:choose>
				</xsl:variable>

				<table cellspacing="0" cellpadding="0">
					<xsl:for-each select="ancestors/*">
						<xsl:sort select="position()" data-type="number" order="descending" />
						<tr>
							<xsl:call-template name="createTableEntries">
								<xsl:with-param name="count" select="position() - 2" />
							</xsl:call-template>

							<xsl:if test="position() &gt; 1">
								<td>
									<img>
										<includeAttribute name="src" item="iconPath">
											<parameter>LastChild.gif</parameter>
										</includeAttribute>
									</img>
								</td>
							</xsl:if>

							<td colspan="{$columnCount - position() + 1}">
								<xsl:apply-templates select="." />
							</td>
						</tr>
					</xsl:for-each>

					<tr>
						<xsl:call-template name="createTableEntries">
							<xsl:with-param name="count" select="$ancestorCount - 1" />
						</xsl:call-template>

						<xsl:if test="$ancestorCount &gt; 0">
							<td>
								<img>
									<includeAttribute name="src" item="iconPath">
										<parameter>LastChild.gif</parameter>
									</includeAttribute>
								</img>
							</td>
						</xsl:if>

						<td>
							<xsl:if test="$childCount &gt; 0">
								<xsl:attribute name="colspan">2</xsl:attribute>
							</xsl:if>
							<referenceLink target="{$key}" />
						</td>
					</tr>

					<xsl:for-each select="descendents/*">

						<tr>

						<xsl:call-template name="createTableEntries">
							<xsl:with-param name="count" select="$ancestorCount" />
						</xsl:call-template>

						<td>
							<xsl:choose>
								<xsl:when test="position()=last()">
									<img>
										<includeAttribute name="src" item="iconPath">
											<parameter>LastChild.gif</parameter>
										</includeAttribute>
									</img>
								</xsl:when>
								<xsl:otherwise>
									<img>
										<includeAttribute name="src" item="iconPath">
											<parameter>NotLastChild.gif</parameter>
										</includeAttribute>
									</img>
								</xsl:otherwise>
							</xsl:choose>
						</td>

						<td><xsl:apply-templates select="." /></td>

						</tr>

					</xsl:for-each>
					
				</table>
			</xsl:with-param>
		</xsl:call-template>
	</xsl:template>

	<xsl:template name="createTableEntries">
		<xsl:param name="count" />
		<xsl:if test="number($count) > 0">
			<td>&#xa0;</td>
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
				<xsl:for-each select="/document/reference"><xsl:call-template name="typeNameDecorated" /></xsl:for-each>
			</xsl:when>
			<xsl:when test="$subgroup='constructor'">
				<xsl:for-each select="/document/reference/containers/type"><xsl:call-template name="typeNameDecorated" /></xsl:for-each>
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
		<span class="cs">&lt;</span><span class="vb"><xsl:text>(Of </xsl:text></span>
		<xsl:for-each select="templates/template">
			<xsl:value-of select="@name" />
			<xsl:if test="not(position()=last())">
				<xsl:text>, </xsl:text>
			</xsl:if>
		</xsl:for-each>
		<span class="cs">&gt;</span><span class="vb">)</span>
	</xsl:template>

	<!-- plain names -->

	<xsl:template name="shortNamePlain">
		<xsl:choose>
			<xsl:when test="$group='type'">
				<xsl:for-each select="/document/reference"><xsl:call-template name="typeNamePlain" /></xsl:for-each>
			</xsl:when>
			<xsl:when test="$subgroup='constructor'">
				<xsl:for-each select="/document/reference/containers/type"><xsl:call-template name="typeNamePlain" /></xsl:for-each>
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
