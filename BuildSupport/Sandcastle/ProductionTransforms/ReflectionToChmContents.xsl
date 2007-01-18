<?xml version="1.0"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.1">

	<xsl:param name="html" select="string('Output/html')" />

	<xsl:output method="text" encoding="iso-8859-1" />

	<xsl:key name="index" match="/reflection/apis/api" use="@id" />

	<xsl:template match="/">
		<xsl:text>&lt;!DOCTYPE HTML PUBLIC "-//IETF//DTD HTML/EN"&gt;&#x0a;</xsl:text>
		<xsl:text>&lt;HTML&gt;&#x0a;</xsl:text>
		<xsl:text>  &lt;BODY&gt;&#x0a;</xsl:text>
		<xsl:text>    &lt;UL&gt;&#x0a;</xsl:text>

		<xsl:choose>
			<xsl:when test="/reflection/apis/api[apidata/@group='root']">
				<xsl:apply-templates select="/reflection/apis/api[apidata/@group='root']"/>
			</xsl:when>
			<xsl:when test="/reflection/apis/api[apidata/@group='namespace']">
				<xsl:apply-templates select="/reflection/apis/api[apidata/@group='namespace']"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:apply-templates select="/reflection/apis/api[apidata/@group='type']"/>
			</xsl:otherwise>
		</xsl:choose>

		<xsl:text>    &lt;/UL&gt;&#x0a;</xsl:text>
		<xsl:text>  &lt;/BODY&gt;&#x0a;</xsl:text>
		<xsl:text>&lt;/HTML&gt;&#x0a;</xsl:text>
	</xsl:template>

	<xsl:template match="api">
		<xsl:text><![CDATA[<LI><OBJECT type="text/sitemap">]]>&#x0a;</xsl:text>
		<xsl:text>  &lt;param name="Name" value="</xsl:text>
		<!-- <xsl:value-of select="apidata/@name"/> -->
		<xsl:value-of select="document(concat($html,'/', file/@name, '.htm'),.)/html/head/title"/>
		<xsl:text>"&gt;&#x0a;</xsl:text>
		<xsl:text>  &lt;param name="Local" value="html\</xsl:text>
		<xsl:value-of select="file/@name"/>
		<xsl:text>.htm"&gt;&#x0a;</xsl:text>
		<xsl:text><![CDATA[</OBJECT></LI>]]>&#x0a;</xsl:text>

		<xsl:if test="count(elements/element) &gt; 0">
			<xsl:text>&lt;UL&gt;&#x0a;</xsl:text>
			<xsl:choose>
				<xsl:when test="apidata/@group='type'">
					<xsl:call-template name="typeElements" />
				</xsl:when>
				<xsl:otherwise>
					<xsl:for-each select="elements/element">
						<xsl:sort select="@api" />
						<xsl:apply-templates select="key('index',@api)" />
					</xsl:for-each>
<!--
					<xsl:apply-templates select="key('index',elements/element/@api)">
						<xsl:sort select="apidata/@name" />
					</xsl:apply-templates>
-->
				</xsl:otherwise>
			</xsl:choose>
			<xsl:text>&lt;/UL&gt;&#x0a;</xsl:text>
		</xsl:if>
	</xsl:template>

	<xsl:template name="typeElements">
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

		<xsl:text><![CDATA[<LI><OBJECT type="text/sitemap">]]>&#x0a;</xsl:text>
		<xsl:text>  &lt;param name="Name" value="</xsl:text>
		<!-- <xsl:value-of select="apidata/@name"/> -->
		<xsl:value-of select="document(concat($html,'/', key('index',$overloadId)/file/@name, '.htm'),.)/html/head/title"/>
		<xsl:text>"&gt;&#x0a;</xsl:text>
		<xsl:text>  &lt;param name="Local" value="html\</xsl:text>
		<xsl:value-of select="key('index',$overloadId)/file/@name" />
		<xsl:text>.htm"&gt;&#x0a;</xsl:text>
		<xsl:text><![CDATA[</OBJECT></LI>]]>&#x0a;</xsl:text>


			<xsl:text>&lt;UL&gt;&#x0a;</xsl:text>
								<xsl:for-each select="$set">
									<xsl:apply-templates select="." />
								</xsl:for-each>
			<xsl:text>&lt;/UL&gt;&#x0a;</xsl:text>


						</xsl:if>
					</xsl:when>
					<xsl:otherwise>
						<xsl:apply-templates select="." />
					</xsl:otherwise>
				</xsl:choose>
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
