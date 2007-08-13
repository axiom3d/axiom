<?xml version="1.0"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.1">

  <xsl:output indent="yes" encoding="UTF-8" />

  <xsl:key name="index" match="/*/apis/api" use="@id" />

  <xsl:template match="/">
    <reflection>
      <xsl:apply-templates select="/*/assemblies" />
      <xsl:apply-templates select="/*/apis" />
    </reflection>
  </xsl:template>


  <xsl:template match="assemblies">
    <xsl:copy-of select="." />
  </xsl:template>

  <xsl:template match="apis">
    <apis>
      <xsl:apply-templates select="api" />
    </apis>
  </xsl:template>

  <xsl:template match="api">
    <xsl:copy-of select="." />
  </xsl:template>

  <xsl:template match="api[apidata/@group='type']">
    <!-- first reproduce the type API -->
    <xsl:copy-of select="." />
    <!-- now create overload APIs -->
    <xsl:variable name="typeId" select="@id" />
    <xsl:variable name="members" select="key('index',elements/element/@api)" />
    <xsl:for-each select="$members">
      <xsl:variable name="name" select="apidata/@name" />
      <xsl:variable name="subgroup" select="apidata/@subgroup" />
      <xsl:variable name="set" select="$members[apidata/@name=$name and apidata/@subgroup=$subgroup]" />
      <xsl:if test="(count($set) &gt; 1) and (($set[containers/type/@api=$typeId][1]/@id)=@id)">
        <api>
          <xsl:attribute name="id">
            <xsl:call-template name="overloadId">
              <xsl:with-param name="memberId" select="@id" />
            </xsl:call-template>
          </xsl:attribute>
          <apidata name="{apidata/@name}" group="{apidata/@group}" subgroup="{apidata/@subgroup}" pseudo="true" />
          <containers>
            <library assembly="{containers/library/@assembly}" module="{containers/library/@module}"/>
            <namespace api="{containers/namespace/@api}" />
            <type api="{containers/type/@api}" />
          </containers>
          <elements>
            <xsl:for-each select="$set">
              <element api="{@id}" />
            </xsl:for-each>
          </elements>
        </api>
      </xsl:if>
    </xsl:for-each>
  </xsl:template>

  <xsl:template match="api[apidata/@group='member']">
    <xsl:if test="not(key('index',containers/type/@api)/apidata/@subgroup='enumeration')">
      <xsl:copy-of select="." />
    </xsl:if>
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
