<?xml version="1.0"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.1">

	<!-- stuff specified to comments authored in DDUEXML -->

	<xsl:include href="utilities_reference.xsl" />

	<xsl:variable name="summary" select="normalize-space(/document/comments/summary)" />

	<xsl:template name="body">

		<!-- auto-inserted info -->
		<xsl:apply-templates select="/document/reference/attributes" />
		<xsl:apply-templates select="/document/comments/summary" />
		<!-- syntax -->
		<xsl:apply-templates select="/document/syntax" />
		<!-- parameters & return value -->
		<xsl:apply-templates select="/document/reference/parameters" />
		<xsl:apply-templates select="/document/comments/value" />
		<xsl:apply-templates select="/document/comments/returns" />
		<!-- members -->
		<xsl:choose>
			<xsl:when test="$group='root'">
				<xsl:apply-templates select="/document/reference/elements" mode="root" />
			</xsl:when>
			<xsl:when test="$group='namespace'">
				<xsl:apply-templates select="/document/reference/elements" mode="namespace" />
			</xsl:when>
			<xsl:when test="$subgroup='enumeration'">
				<xsl:apply-templates select="/document/reference/elements" mode="enumeration" />
			</xsl:when>
			<xsl:when test="$group='type'">
				<xsl:apply-templates select="/document/reference/elements" mode="type" />
			</xsl:when>
			<xsl:when test="$group='member'">
				<xsl:apply-templates select="/document/reference/elements" mode="overload" />
			</xsl:when>
		</xsl:choose>
		<!-- remarks -->
		<xsl:apply-templates select="/document/comments/remarks" />
		<!-- example -->
		<xsl:apply-templates select="/document/comments/example" />
		<!-- other comment sections -->
		<!-- permissions -->
		<!-- exceptions -->
		<xsl:call-template name="exceptions" />
		<!-- inheritance -->
		<xsl:apply-templates select="/document/reference/family" />
		<!-- assembly information -->
		<xsl:apply-templates select="/document/reference/containers/library" />
		<!-- see also -->

	</xsl:template> 


	<xsl:template name="getParameterDescription">
		<xsl:param name="name" />
		<xsl:apply-templates select="/document/comments/param[@name=$name]" />
	</xsl:template>

	<xsl:template name="getReturnsDescription">
		<xsl:param name="name" />
		<xsl:apply-templates select="/document/comments/param[@name=$name]" />
	</xsl:template>

	<xsl:template name="getElementDescription">
		<xsl:apply-templates select="summary" />
	</xsl:template>

	<!-- block sections -->

	<xsl:template match="summary">
		<div class="summary">
			<xsl:apply-templates />
		</div>
	</xsl:template>

	<xsl:template match="value">
		<xsl:call-template name="section">
			<xsl:with-param name="title">Value</xsl:with-param>
			<xsl:with-param name="content"><xsl:apply-templates /></xsl:with-param>
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="returns">
		<xsl:call-template name="section">
			<xsl:with-param name="title">Return Value</xsl:with-param>
			<xsl:with-param name="content"><xsl:apply-templates /></xsl:with-param>
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="remarks">
		<xsl:call-template name="section">
			<xsl:with-param name="title"><include item="remarksTitle" /></xsl:with-param>
			<xsl:with-param name="content"><xsl:apply-templates /></xsl:with-param>
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="example">
		<xsl:call-template name="section">
			<xsl:with-param name="title"><include item="examplesTitle" /></xsl:with-param>
			<xsl:with-param name="content"><xsl:apply-templates /></xsl:with-param>
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="para">
		<p><xsl:apply-templates /></p>
	</xsl:template>

	<xsl:template match="code">
		<div class="code"><pre><xsl:apply-templates /></pre></div>
	</xsl:template>

	<xsl:template name="exceptions">
		<xsl:if test="count(/document/comments/exception) &gt; 0">
			<xsl:call-template name="section">
				<xsl:with-param name="title"><include item="exceptionsTitle" /></xsl:with-param>
				<xsl:with-param name="content">
				<table class="exceptions">
					<tr>
						<th class="exceptionNameColumn"><include item="exceptionNameHeader" /></th>
						<th class="exceptionConditionColumn"><include item="exceptionConditionHeader" /></th>
					</tr>
					<xsl:for-each select="/document/comments/exception">
						<tr>
							<td><referenceLink target="{@cref}" /></td>
							<td><xsl:apply-templates select="." /></td>
						</tr>
					</xsl:for-each>
				</table>
				</xsl:with-param>
			</xsl:call-template>
		</xsl:if>
	</xsl:template>

	<xsl:template match="list[@class='bullet']">
		<ul>
			<xsl:for-each select="item">
				<li><xsl:apply-templates /></li>
			</xsl:for-each>
		</ul>
	</xsl:template>

	<xsl:template match="list[@class='number']">
		<ul>
			<xsl:for-each select="item">
				<li><xsl:apply-templates /></li>
			</xsl:for-each>
		</ul>
	</xsl:template>

	<xsl:template match="list[@class='table']">
		<table class="authoredTable">
			<xsl:for-each select="listheader">
				<tr>
					<xsl:for-each select="*">
						<th><xsl:apply-templates /></th>
					</xsl:for-each>
				</tr>
			</xsl:for-each>
			<xsl:for-each select="item">
				<tr>
					<xsl:for-each select="*">
						<td><xsl:apply-templates /></td>
					</xsl:for-each>
				</tr>
			</xsl:for-each>
		</table>
	</xsl:template>

	<!-- inline tags -->

	<xsl:template match="see">
		<referenceLink target="{@cref}" />
	</xsl:template>

	<xsl:template match="seealso">
		<referenceLink target="{@cref}" />
	</xsl:template>

	<xsl:template match="c">
		<span class="code"><xsl:value-of select="." /></span>
	</xsl:template>

	<xsl:template match="paramref">
		<span class="parameter"><xsl:value-of select="@name" /></span>
	</xsl:template>

	<!-- pass through html tags -->

	<xsl:template match="p|ol|ul|li|dl|dt|dd|table|tr|th|td|a|img|b|i|strong|em|del|sub|sup">
		<xsl:copy>
			<xsl:copy-of select="@*" />
			<xsl:apply-templates />
		</xsl:copy>
	</xsl:template>

	<!-- move these off into a shared file -->

	<xsl:template name="createReferenceLink">
		<xsl:param name="id" />
		<xsl:param name="qualified" select="false()" />
		<b><referenceLink target="{$id}" qualified="{$qualified}" /></b>
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


</xsl:stylesheet>
