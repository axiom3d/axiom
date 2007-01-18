<?xml version="1.0"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.1">

	<xsl:output indent="yes" encoding="UTF-8" />

	<xsl:key name="index" match="/reflection/apis/api" use="@id" />

	<xsl:template match="/">
		<topics>
			<xsl:apply-templates select="/reflection/apis/api" />
		</topics>
	</xsl:template>

	<!-- namespace and member topics -->
	<xsl:template match="api">
		<topic id="{@id}" />
	</xsl:template>

	<!-- type topics need to be handled specially to also generate overload pages -->
	<xsl:template match="api[apidata/@group='type']">
		<topic id="{@id}" />
		<xsl:variable name="typeId" select="@id" />
		<xsl:variable name="members" select="key('index',elements/element/@member)" />
		<xsl:for-each select="$members">
			<xsl:variable name="name" select="apidata/@name" />
			<xsl:variable name="subgroup" select="apidata/@subgroup" />
			<xsl:variable name="set" select="$members[apidata/@name=$name and apidata/@subgroup=$subgroup]" />
			<xsl:if test="(count($set) &gt; 1) and (($set[memberdata/@type=$typeId][1]/@id)=@id)">
				<topic>
					<xsl:attribute name="id">
						<xsl:call-template name="overloadId">
							<xsl:with-param name="memberId" select="@id" />
						</xsl:call-template>
					</xsl:attribute>
				</topic>
			</xsl:if>
		</xsl:for-each>
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

</xsl:stylesheet>
