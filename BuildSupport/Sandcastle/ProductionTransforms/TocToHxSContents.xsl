<?xml version="1.0"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.1">

	<xsl:output indent="yes" encoding="UTF-8" doctype-system="MS-Help://Hx/Resources/HelpTOC.dtd" />

	<xsl:template match="/">
		<HelpTOC DTDVersion="1.0">
			<xsl:apply-templates select="/tableOfContents/topic" />
		</HelpTOC>
	</xsl:template>

	<xsl:template match="topic">
		<HelpTOCNode Url="html\{@id}.htm">
			<xsl:apply-templates />
		</HelpTOCNode>
	</xsl:template>	


</xsl:stylesheet>
