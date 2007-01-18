<?xml version="1.0"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.1"
		xmlns:ddue="http://ddue.schemas.microsoft.com/authoring/2003/5"
		xmlns:xlink="http://www.w3.org/1999/xlink"
		xmlns:mshelp="http://msdn.microsoft.com/mshelp" >

	<!-- sections -->

	<xsl:template match="ddue:summary">
		<div class="summary">
			<xsl:apply-templates />
		</div>
	</xsl:template>

	<xsl:template match="ddue:remarks">
		<xsl:if test="normalize-space(.)">
			<xsl:call-template name="section">
				<xsl:with-param name="title"><include item="remarksTitle" /></xsl:with-param>
				<xsl:with-param name="content"><xsl:apply-templates /></xsl:with-param>
			</xsl:call-template>
		</xsl:if>
	</xsl:template>

	<xsl:template match="ddue:codeExamples">
		<xsl:if test="normalize-space(.)">
			<xsl:call-template name="section">
				<xsl:with-param name="title"><include item="examplesTitle" /></xsl:with-param>
				<xsl:with-param name="content"><xsl:apply-templates /></xsl:with-param>
			</xsl:call-template>
		</xsl:if>
	</xsl:template>

	<xsl:template match="ddue:threadSaftey">
		<xsl:call-template name="section">
			<xsl:with-param name="title"><include item="threadSafteyTitle" /></xsl:with-param>
			<xsl:with-param name="content"><xsl:apply-templates /></xsl:with-param>
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="ddue:notesForImplementers">
		<xsl:call-template name="section">
			<xsl:with-param name="title"><include item="notesForImplementersTitle" /></xsl:with-param>
			<xsl:with-param name="content"><xsl:apply-templates /></xsl:with-param>
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="ddue:notesForCallers">
		<xsl:call-template name="section">
			<xsl:with-param name="title"><include item="notesForCallersTitle" /></xsl:with-param>
			<xsl:with-param name="content"><xsl:apply-templates /></xsl:with-param>
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="ddue:exceptions">
		<xsl:call-template name="section">
			<xsl:with-param name="title"><include item="exceptionsTitle" /></xsl:with-param>
			<xsl:with-param name="content">
				<xsl:choose>
					<xsl:when test="ddue:exception">
						<table class="exceptions">
							<tr>
								<th class="exceptionNameColumn"><include item="exceptionNameHeader" /></th>
								<th class="exceptionConditionColumn"><include item="exceptionConditionHeader" /></th>
							</tr>
							<xsl:for-each select="ddue:exception">
								<tr>
									<td><xsl:apply-templates select="ddue:codeEntityReference" /></td>
									<td><xsl:apply-templates select="ddue:content" /></td>
								</tr>
							</xsl:for-each>
						</table>
					</xsl:when>
					<xsl:otherwise>
						<xsl:apply-templates />
					</xsl:otherwise>
				</xsl:choose>
			</xsl:with-param>
		</xsl:call-template>
	</xsl:template>

	<!-- just skip over these -->
	<xsl:template match="ddue:content | ddue:codeExample | ddue:legacy">
		<xsl:apply-templates />
	</xsl:template>

	<!-- block elements -->

	<xsl:template match="ddue:para">
		<p><xsl:apply-templates /></p>
	</xsl:template>

	<xsl:template match="ddue:list">
		<xsl:choose>
			<xsl:when test="@class='bullet'">
				<ul>
					<xsl:apply-templates select="ddue:listItem" />
				</ul>
			</xsl:when>
			<xsl:when test="@class='ordered'">
				<ol>
					<xsl:apply-templates select="ddue:listItem" />
				</ol>
			</xsl:when>
			<xsl:otherwise>
				<span class="processingError">Unknown List Class</span>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template match="ddue:listItem">
		<li>
			<xsl:apply-templates />
		</li>
	</xsl:template>		

	<xsl:template match="ddue:table">
		<table class="authoredTable">
			<xsl:apply-templates />
		</table>
	</xsl:template>

	<xsl:template match="ddue:tableHeader">
		<xsl:apply-templates />
	</xsl:template>

	<xsl:template match="ddue:row">
		<tr>
			<xsl:apply-templates />
		</tr>
	</xsl:template>

	<xsl:template match="ddue:entry">
		<td>
			<xsl:apply-templates />
		</td>
	</xsl:template>

	<xsl:template match="ddue:tableHeader/ddue:row/ddue:entry">
		<th>
			<xsl:apply-templates />
		</th>
	</xsl:template>

	<xsl:template match="ddue:definitionTable">
		<dl>
			<xsl:apply-templates />
		</dl>
	</xsl:template>

	<xsl:template match="ddue:definedTerm">
		<dt>
			<xsl:apply-templates />
		</dt>
	</xsl:template>

	<xsl:template match="ddue:definition">
		<dd>
			<xsl:apply-templates />
		</dd>
	</xsl:template>

	<xsl:template match="ddue:code">
		<div class="code"><pre><xsl:apply-templates /></pre></div>
	</xsl:template>

	<xsl:template match="ddue:sampleCode">
		<div><b><xsl:value-of select="@language"/></b></div>
		<div class="code"><pre><xsl:apply-templates /></pre></div>
	</xsl:template>

	<xsl:template name="composeCode">
		<xsl:copy-of select="." />
		<xsl:variable name="next" select="following-sibling::*[1]" />
		<xsl:if test="boolean($next/@language) and boolean(local-name($next)=local-name())">
			<xsl:for-each select="$next">
				<xsl:call-template name="composeCode" />
			</xsl:for-each>
		</xsl:if>
	</xsl:template>

	<xsl:template match="ddue:alert">
		<div class="alert">
			<xsl:choose>
				<xsl:when test="@class='caution'">
					<img>
						<includeAttribute item="artPath" name="src">
							<parameter>alert_caution.gif</parameter>
						</includeAttribute>
					</img>
					<xsl:text> </xsl:text>
					<include item="cautionTitle" />
				</xsl:when>
				<xsl:when test="@class='security'">
					<img>
						<includeAttribute item="artPath" name="src">
							<parameter>alert_security.gif</parameter>
						</includeAttribute>
					</img>
					<xsl:text> </xsl:text>
					<include item="securityTitle" />
				</xsl:when>
				<xsl:when test="@class='note'">
					<img>
						<includeAttribute item="artPath" name="src">
							<parameter>alert_note.gif</parameter>
						</includeAttribute>
					</img>
					<xsl:text> </xsl:text>
					<include item="noteTitle" />
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="@class" />
				</xsl:otherwise>
			</xsl:choose>
			<xsl:apply-templates />
		</div>
	</xsl:template>

	<xsl:template match="ddue:section">
		<span class="subsectionTitle"><xsl:value-of select="ddue:title"/></span>
		<div class="subsection">
			<xsl:apply-templates select="ddue:content"/>
		</div>
	</xsl:template>

	<xsl:template match="ddue:mediaLink">
		<div class="media">
			<artLink target="{ddue:image/@xlink:href}" />
			<div class="caption">
				<xsl:apply-templates select="ddue:caption" />
			</div>
		</div>
	</xsl:template>

	<!-- inline elements -->

	<xsl:template match="ddue:parameterReference">
		<span class="parameter"><xsl:value-of select="." /></span>
	</xsl:template>

	<xsl:template match="ddue:languageKeyword">
		<span class="keyword"><xsl:value-of select="." /></span>
	</xsl:template>

	<xsl:template match="ddue:ui">
		<span class="ui"><xsl:value-of select="." /></span>
	</xsl:template>

	<xsl:template match="ddue:userInput | ddue:userInputLocalizable">
		<span class="input"><xsl:value-of select="." /></span>
	</xsl:template>

	<xsl:template match="ddue:newTerm">
		<span class="term"><xsl:value-of select="." /></span>
	</xsl:template>

	<xsl:template match="ddue:math">
		<span class="math"><xsl:value-of select="." /></span>
	</xsl:template>

	<xsl:template match="ddue:codeInline">
		<span class="code"><xsl:value-of select="." /></span>
	</xsl:template>

	<xsl:template match="ddue:subscript">
		<sub><xsl:value-of select="." /></sub>
	</xsl:template>

	<xsl:template match="ddue:superscript">
		<sup><xsl:value-of select="." /></sup>
	</xsl:template>

	<xsl:template match="ddue:legacyBold">
		<b><xsl:apply-templates /></b>
	</xsl:template>

	<xsl:template match="ddue:legacyItalic">
		<i><xsl:apply-templates /></i>
	</xsl:template>

	<xsl:template match="ddue:legacyUnderline">
		<u><xsl:apply-templates /></u>
	</xsl:template>

	<!-- links -->

	<xsl:template match="ddue:externalLink">
		<a>
			<xsl:attribute name="href"><xsl:value-of select="ddue:linkUri" /></xsl:attribute>
			<xsl:value-of select="ddue:linkText" />
		</a>
	</xsl:template>

	<xsl:template match="ddue:codeEntityReference">
		<referenceLink target="{string(.)}">
			<xsl:if test="@qualifyHint">
				<xsl:attribute name="show-container">
					<xsl:value-of select="@qualifyHint" />
				</xsl:attribute>
				<xsl:attribute name="show-parameters">
					<xsl:value-of select="@qualifyHint" />
				</xsl:attribute>
			</xsl:if>
			<xsl:if test="@autoUpgrade">
				<xsl:attribute name="prefer-overload">
					<xsl:value-of select="@autoUpgrade" />
				</xsl:attribute>
			</xsl:if>
		</referenceLink>
	</xsl:template>

	<xsl:template match="ddue:link">
		<conceptualLink target="{@xlink:href}" />
	</xsl:template>

	<xsl:template match="ddue:legacyLink">
		<conceptualLink target="{@xlink:href}" />
		<!-- <xsl:text>LEGACY</xsl:text> -->
	</xsl:template>

	
<!--
	<xsl:template match="ddue:legacyLink | ddue:link">
		<a>
			<xsl:attribute name="href">
				<xsl:value-of select="@xlink:href" />
				<xsl:text>.htm</xsl:text>
			</xsl:attribute>
			<xsl:apply-templates />
		</a>
	</xsl:template>
-->

	<xsl:template name="createReferenceLink">
		<xsl:param name="id" />
		<xsl:param name="qualified" select="false()" />
		<b><referenceLink target="{$id}" qualified="{$qualified}" /></b>
	</xsl:template>


	<!-- this is temporary -->
        <xsl:template match="ddue:snippets">
		<xsl:variable name="codeId" select="generate-id()" />
		<table class="filter"><tr class="tabs" id="ct_{$codeId}">
			<xsl:for-each select="ddue:snippet">
				<td class="tab" x-lang="{@language}" onclick="ct{$codeId}.toggleClass('x-lang','{@language}','activeTab','tab'); cb{$codeId}.toggleStyle('x-lang','{@language}','display','block','none');"><include item="{@language}Label" /></td>
			</xsl:for-each>
		</tr></table>
		<div id="cb_{$codeId}">
			<xsl:for-each select="ddue:snippet">
				<div class="code" x-lang="{@language}"><pre><xsl:copy-of select="node()" /></pre></div>
			</xsl:for-each>
		</div>
		<script type="text/javascript"><xsl:text>
			var ct</xsl:text><xsl:value-of select="$codeId" /><xsl:text> = new ElementCollection('ct_</xsl:text><xsl:value-of select="$codeId" /><xsl:text>');
			var cb</xsl:text><xsl:value-of select="$codeId" /><xsl:text> = new ElementCollection('cb_</xsl:text><xsl:value-of select="$codeId" /><xsl:text>');
			lfc.registerTabbedArea(ct</xsl:text><xsl:value-of select="$codeId" /><xsl:text>, cb</xsl:text><xsl:value-of select="$codeId" /><xsl:text>);
			ct</xsl:text><xsl:value-of select="$codeId" /><xsl:text>.toggleClass('x-lang','CSharp','activeTab','tab');
			cb</xsl:text><xsl:value-of select="$codeId" /><xsl:text>.toggleStyle('x-lang','CSharp','display','block','none');
		</xsl:text></script>
	</xsl:template>

	<xsl:template name="section">
		<xsl:param name="title" />
		<xsl:param name="content" />
		<div class="section">
			<div class="sectionTitle" onclick="toggleSection(this.parentNode)">
				<img>
					<includeAttribute name="src" item="artPath">
						<parameter>collapse_all.gif</parameter>
					</includeAttribute>
				</img>
				<xsl:text> </xsl:text>
				<xsl:copy-of select="$title" />
			</div>
			<div class="sectionContent">
				<xsl:copy-of select="$content" />
			</div>
		</div>
	</xsl:template>

	<!-- fail if any unknown elements are encountered -->
<!--
	<xsl:template match="*">
		<xsl:message terminate="yes">
			<xsl:text>An unknown element was encountered.</xsl:text>
		</xsl:message>
	</xsl:template>
-->


</xsl:stylesheet>
