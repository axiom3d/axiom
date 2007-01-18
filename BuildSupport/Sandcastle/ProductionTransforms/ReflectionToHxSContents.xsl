<?xml version="1.0"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.1">

	<xsl:output indent="yes" encoding="UTF-8" doctype-system="MS-Help://Hx/Resources/HelpTOC.dtd" />

	<xsl:key name="index" match="/reflection/apis/api" use="@id" />

	<xsl:template match="/">
		<HelpTOC DTDVersion="1.0">
			<xsl:choose>
				<xsl:when test="count(/reflection/apis/api[apidata/@group='root']) > 0">
					<xsl:apply-templates select="/reflection/apis/api[apidata/@group='root']" />
				</xsl:when>
				<xsl:when test="count(/reflection/apis/api[apidata/@group='namespace']) > 0">
					<xsl:apply-templates select="/reflection/apis/api[apidata/@group='namespace']">
						<xsl:sort select="apidata/@name" />
					</xsl:apply-templates>
				</xsl:when>
				<xsl:otherwise>
					<xsl:apply-templates select="/reflection/apis/api[apidata/@group='type']">
						<xsl:sort select="apidata/@name" />
					</xsl:apply-templates>
				</xsl:otherwise>
			</xsl:choose>
		</HelpTOC>
	</xsl:template>

	<!-- create a root entry and namespace sub-entries -->
	<xsl:template match="api[apidata/@group='root']">
		<HelpTOCNode Id="{@id}" Url="{concat('html\',file/@name,'.htm')}">
			<xsl:apply-templates select="key('index',elements/element/@api)">
				<xsl:sort select="apidata/@name" />
			</xsl:apply-templates>
		</HelpTOCNode>
	</xsl:template>


	<!-- for each namespace, create namespace entry and type sub-entries -->
	<xsl:template match="api[apidata/@group='namespace']">
		<HelpTOCNode Id="{@id}" Url="{concat('html\',file/@name,'.htm')}">
			<xsl:apply-templates select="key('index',elements/element/@api)">
				<xsl:sort select="apidata/@name" />
			</xsl:apply-templates>
		</HelpTOCNode>
	</xsl:template>

	<!-- for each type, create type entry and either overload entries or member entries as sub-entries -->
	<xsl:template match="api[apidata/@group='type']">
		<HelpTOCNode Id="{@id}" Url="{concat('html\',file/@name,'.htm')}">
			<xsl:variable name="typeId" select="@id" />
			<xsl:variable name="members" select="key('index',elements/element/@api)[containers/type/@api=$typeId]" />
			<xsl:for-each select="$members">
				<xsl:sort select="apidata/@name" />
				<xsl:variable name="name" select="apidata/@name" />
				<xsl:variable name="subgroup" select="apidata/@subgroup" />
				<xsl:variable name="set" select="$members[apidata/@name=$name and apidata/@subgroup=$subgroup]" />
				<xsl:choose>
					<xsl:when test="count($set) &gt; 1">
						<xsl:if test="($set[1]/@id)=@id">
							<xsl:variable name="overloadId">
								<xsl:call-template name="overloadId">
									<xsl:with-param name="memberId" select="@id" />
								</xsl:call-template>
							</xsl:variable>
							<HelpTOCNode Id="{@id}" Url="{concat('html\',key('index',$overloadId)/file/@name,'.htm')}">
								<xsl:for-each select="$set">
									<xsl:apply-templates select="." />
								</xsl:for-each>
							</HelpTOCNode>
						</xsl:if>
					</xsl:when>
					<xsl:otherwise>
						<xsl:apply-templates select="." />
					</xsl:otherwise>
				</xsl:choose>
			</xsl:for-each>
		</HelpTOCNode>
	</xsl:template>

	<!-- for each member, create a leaf entry -->
	<xsl:template match="api[apidata/@group='member']">
		<HelpTOCNode Id="{@id}" Url="{concat('html\',file/@name,'.htm')}" />
	</xsl:template>

	<xsl:template name="overloadId">
		<xsl:param name="memberId" />
		<xsl:text>Overload:</xsl:text>
		<xsl:variable name="noParameters">
			<xsl:choose>
				<xsl:when test="contains($memberId,'(')">
					<xsl:value-of select="substring-before($memberId,'(')" />
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="$memberId" />
				</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<xsl:variable name="noGeneric">
			<xsl:choose>
				<xsl:when test="contains($noParameters,'``')">
					<xsl:value-of select="substring-before($noParameters,'``')" />
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="$noParameters" />
				</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<xsl:value-of select="substring($noGeneric,3)" />
	</xsl:template>
<!--
	<xsl:template name="topicFile">
		<xsl:param name="topicId" />
		<xsl:for-each select="document($map,.)">
			<xsl:variable name="result" select="key('topics',$topicId)/@file" />
			<xsl:if test="not($result)">
				<xsl:message terminate="yes">
					<xsl:value-of select="concat('No file is defined for the topic ',$topicId)" />
				</xsl:message>
			</xsl:if>
			<xsl:value-of select="concat('html\',$result,'.htm')" />
		</xsl:for-each>
	</xsl:template>

	<xsl:key name="topics" match="/topics/topic" use="@id" />
-->
</xsl:stylesheet>
