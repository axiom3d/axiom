<?xml version="1.0" encoding="utf-8"?>

<!--  Doc Studio Contents to HTML Help Compiler Contents Transform

      Invoke with: xsltransform TocToChmContents.xsl toc.xml [/arg:html=html-dir] /out:output.hhc
      
      - comments - specifies the directory where the HTML files are stored.
      
      NOTE: The output looks like HTML, but it isn't. Whitespace is significant.
-->

<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
		xmlns:ddue="http://ddue.schemas.microsoft.com/authoring/2003/5">

  <xsl:param name="html" select="string('Output/html')"/>

	<xsl:output method="text" encoding="utf-8"/>

  <xsl:template match="/">
    <xsl:text>&lt;!DOCTYPE HTML PUBLIC "-//IETF//DTD HTML/EN"&gt;&#x0a;</xsl:text>
    <xsl:text>&lt;HTML&gt;&#x0a;</xsl:text>
    <xsl:text>  &lt;BODY&gt;&#x0a;</xsl:text>
    <xsl:apply-templates select="tableOfContents"/>
    <xsl:text>  &lt;/BODY&gt;&#x0a;</xsl:text>
    <xsl:text>&lt;/HTML&gt;&#x0a;</xsl:text>
  </xsl:template>
  
  <xsl:template match="tableOfContents">
    <xsl:call-template name="parentNode"/>
  </xsl:template>
  
  <xsl:template name="parentNode">
    <xsl:if test="topic">
      <xsl:call-template name="indent"/>
      <xsl:text>&lt;UL&gt;&#x0a;</xsl:text>
      <xsl:apply-templates select="topic"/>
      <xsl:call-template name="indent"/>
      <xsl:text>&lt;/UL&gt;&#x0a;</xsl:text>
    </xsl:if>
  </xsl:template>
  
  <xsl:template match="topic">
    <xsl:call-template name="indent"/>
    <xsl:text><![CDATA[<LI><OBJECT type="text/sitemap">]]>&#x0a;</xsl:text>
    <xsl:call-template name="indent"/>
    <xsl:text>  &lt;param name="Name" value="</xsl:text>
    <xsl:value-of select="document(concat($html, '/', @id, '.htm'),.)/html/head/title"/>
    <xsl:text>"&gt;&#x0a;</xsl:text>
    <xsl:call-template name="indent"/>
    <xsl:text>  &lt;param name="Local" value="html\</xsl:text>
    <xsl:value-of select="@id"/>
    <xsl:text>.htm"&gt;&#x0a;</xsl:text>
    <xsl:call-template name="indent"/>
    <xsl:text><![CDATA[</OBJECT></LI>]]>&#x0a;</xsl:text>
    
    <xsl:call-template name="parentNode"/>
  </xsl:template>
  	
  <xsl:template name="indent"><xsl:for-each select="ancestor::*"><xsl:text>&#x20;&#x20;</xsl:text></xsl:for-each></xsl:template>

</xsl:stylesheet> 
