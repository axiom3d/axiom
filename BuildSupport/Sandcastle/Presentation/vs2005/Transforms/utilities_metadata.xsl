<?xml version="1.0"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.1"
		xmlns:MSHelp="http://msdn.microsoft.com/mshelp" >


	<xsl:template name="insertMetadata">
		<xsl:if test="$metadata='true'">
		<xml>
			<MSHelp:Attr Name="AssetID" Value="{$key}" />
			<!-- toc metadata -->
			<xsl:call-template name="linkMetadata" />
			<xsl:call-template name="indexMetadata" />
			<xsl:call-template name="helpMetadata" />
			<MSHelp:Attr Name="TopicType" Value="apiref" />
			<xsl:call-template name="apiTaggingMetadata" />
			<MSHelp:Attr Name="Locale">
				<includeAttribute name="Value" item="locale" />
			</MSHelp:Attr>
			<xsl:if test="boolean($summary) and (string-length($summary) &lt; 255)">
				<MSHelp:Attr Name="Abstract">
					<xsl:attribute name="Value"><xsl:value-of select="$summary" /></xsl:attribute>
				</MSHelp:Attr>
			</xsl:if>
		</xml>
		</xsl:if>
	</xsl:template>

	<xsl:template name="apiTaggingMetadata">
		<xsl:if test="($group='type' or $group='member') and not(/document/reference/apidata/@pseudo)">
			<MSHelp:Attr Name="APIType" Value="Managed" />
			<MSHelp:Attr Name="APILocation" Value="{/document/reference/containers/library/@assembly}.dll" />
			<xsl:choose>
				<xsl:when test="$group='type'">
					<xsl:variable name="apiTypeName">
						<xsl:value-of select="concat(/document/reference/containers/container[@namespace]/apidata/@name,'.',/document/reference/apidata/@name)" />
						<xsl:if test="count(/document/reference/templates/template) > 0">
							<xsl:value-of select="concat('`',count(/document/reference/templates/template))" />
						</xsl:if>
					</xsl:variable>
					<!-- Namespace + Type -->
					<MSHelp:Attr Name="APIName" Value="{$apiTypeName}" />
					<xsl:choose>
						<xsl:when test="boolean($subgroup='delegate')">
							<MSHelp:Attr Name="APIName" Value="{concat($apiTypeName,'.','.ctor')}" />
							<MSHelp:Attr Name="APIName" Value="{concat($apiTypeName,'.','Invoke')}" />
							<MSHelp:Attr Name="APIName" Value="{concat($apiTypeName,'.','BeginInvoke')}" />
							<MSHelp:Attr Name="APIName" Value="{concat($apiTypeName,'.','EndInvoke')}" />
						</xsl:when>
						<xsl:when test="$subgroup='enumeration'">
							<xsl:for-each select="/document/reference/elements/element">
								<MSHelp:Attr Name="APIName" Value="{substring(@api,2)}" />
							</xsl:for-each>
							<!-- Namespace + Type + Member for each member -->
						</xsl:when>
					</xsl:choose>
				</xsl:when>
				<xsl:when test="$group='member'">
					<xsl:variable name="apiTypeName">
						<xsl:value-of select="concat(/document/reference/containers/container[@namespace]/apidata/@name,'.',/document/reference/containers/container[@type]/apidata/@name)" />
						<xsl:if test="count(/document/reference/templates/template) > 0">
							<xsl:value-of select="concat('`',count(/document/reference/templates/template))" />
						</xsl:if>
					</xsl:variable>
					<!-- Namespace + Type + Member -->
					<MSHelp:Attr Name="APIName" Value="{concat($apiTypeName,'.',/document/reference/apidata/@name)}" />
					<xsl:choose>
						<xsl:when test="boolean($subgroup='property')">
							<!-- Namespace + Type + get_Member if get-able -->
							<!-- Namespace + Type + set_Member if set-able -->
						</xsl:when>
						<xsl:when test="boolean($subgroup='event')">
							<!-- Namespace + Type + add_Member -->
							<!-- Namespace + Type + remove_Member -->
						</xsl:when>
					</xsl:choose>
				</xsl:when>
			</xsl:choose>
		</xsl:if>
	</xsl:template>

	<xsl:template name="linkMetadata">
		<!-- code entity reference keyword -->
		<MSHelp:Keyword Index="A" Term="{$key}" />
		<!-- frlrf keywords -->
		<xsl:choose>
			<xsl:when test="$group='namespace'">
				<MSHelp:Keyword Index="A" Term="{concat('frlrf',translate(@name,'.',''))}" />
			</xsl:when>
			<!-- types & members, too -->
		</xsl:choose>
	</xsl:template>

	<xsl:template name="indexMetadata">
		<!-- K keywords -->
		<xsl:choose>
			<xsl:when test="$group='namespace'">
				<!-- namespace -->
				<MSHelp:Keyword Index="K">
					<includeAttribute name="Term" item="namespaceIndexEntry">
						<parameter><xsl:value-of select="/document/reference/apidata/@name" /></parameter>
					</includeAttribute>
				</MSHelp:Keyword>
			</xsl:when>
			<xsl:when test="$group='type'">
				<!-- type -->
				<xsl:choose>
					<xsl:when test="count(/document/reference/templates/template) = 0">
						<!-- non-generic type -->
						<MSHelp:Keyword Index="K">
							<includeAttribute name="Term" item="{$subgroup}IndexEntry">
								<parameter><xsl:value-of select="/document/reference/apidata/@name" /></parameter>
							</includeAttribute>
						</MSHelp:Keyword>
						<MSHelp:Keyword Index="K">
							<includeAttribute name="Term" item="{$subgroup}IndexEntry">
								<parameter><xsl:value-of select="concat(/document/reference/containers/container[@namespace]/apidata/@name,'.',/document/reference/apidata/@name)" /></parameter>
							</includeAttribute>
						</MSHelp:Keyword>
					</xsl:when>
					<xsl:otherwise>
						<!-- generic type -->
						<MSHelp:Keyword Index="K">
							<includeAttribute name="Term" item="{$subgroup}IndexEntry">
								<parameter>
									<xsl:value-of select="/document/reference/apidata/@name" />
									<xsl:for-each select="/document/reference/templates"><xsl:call-template name="csTemplatesInIndex" /></xsl:for-each>
								</parameter>
							</includeAttribute>
						</MSHelp:Keyword>
						<MSHelp:Keyword Index="K">
							<includeAttribute name="Term" item="{$subgroup}IndexEntry">
								<parameter>
									<xsl:value-of select="/document/reference/apidata/@name" />
									<xsl:for-each select="/document/reference/templates"><xsl:call-template name="vbTemplates"><xsl:with-param name="seperator" select="string('%2C ')" /></xsl:call-template></xsl:for-each>
								</parameter>
							</includeAttribute>
						</MSHelp:Keyword>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
			<xsl:when test="($group='member') and (starts-with($key,'Overload:') or not(/document/reference/proceduredata/@overload='true'))">
				<!-- member -->
				<xsl:variable name="indexEntryItem">
					<xsl:choose>
						<xsl:when test="boolean($subsubgroup)">
							<xsl:value-of select="concat($subsubgroup,'IndexEntry')" />
						</xsl:when>
						<xsl:when test="boolean($subgroup)">
							<xsl:value-of select="concat($subgroup,'IndexEntry')" />
						</xsl:when>
					</xsl:choose>
				</xsl:variable>
				<xsl:choose>
					<xsl:when test="count(/document/reference/templates/template) = 0">
						<!-- non-generic member -->
						<MSHelp:Keyword Index="K">
							<includeAttribute name="Term" item="{$indexEntryItem}">
								<parameter><xsl:value-of select="/document/reference/apidata/@name" /></parameter>
							</includeAttribute>
						</MSHelp:Keyword>
					</xsl:when>
					<xsl:otherwise>
						<!-- generic member -->
						<MSHelp:Keyword Index="K">
							<includeAttribute name="Term" item="{$indexEntryItem}">
								<parameter>
									<xsl:value-of select="/document/reference/apidata/@name" />
									<xsl:for-each select="/document/reference/templates"><xsl:call-template name="csTemplatesInIndex" /></xsl:for-each>
								</parameter>
							</includeAttribute>
						</MSHelp:Keyword>
						<MSHelp:Keyword Index="K">
							<includeAttribute name="Term" item="{$indexEntryItem}">
								<parameter>
									<xsl:value-of select="/document/reference/apidata/@name" />
									<xsl:for-each select="/document/reference/templates"><xsl:call-template name="vbTemplates"><xsl:with-param name="seperator" select="string('%2C ')" /></xsl:call-template></xsl:for-each>
								</parameter>
							</includeAttribute>
						</MSHelp:Keyword>
					</xsl:otherwise>
				</xsl:choose>			
				<!-- type + member -->
				<xsl:choose>
					<xsl:when test="count(/document/reference/containers/container[@type]/templates/template) = 0">
						<!-- non-generic type -->
						<MSHelp:Keyword Index="K">
							<includeAttribute name="Term" item="{$indexEntryItem}">
								<parameter><xsl:value-of select="concat(/document/reference/containers/container[@type]/apidata/@name,'.',/document/reference/apidata/@name)" /></parameter>
							</includeAttribute>
						</MSHelp:Keyword>
					</xsl:when>
					<xsl:otherwise>
						<!-- generic type -->
						<MSHelp:Keyword Index="K">
							<includeAttribute name="Term" item="{$indexEntryItem}">
								<parameter>
									<xsl:value-of select="/document/reference/containers/container[@type]/apidata/@name"/>
									<xsl:for-each select="/document/reference/containers/container[@type]/templates"><xsl:call-template name="vbTemplates"><xsl:with-param name="seperator" select="string('%2C ')" /></xsl:call-template></xsl:for-each>
									<xsl:text>.</xsl:text>
									<xsl:value-of select="/document/reference/apidata/@name" />
								</parameter>
							</includeAttribute>
						</MSHelp:Keyword>
						<MSHelp:Keyword Index="K">
							<includeAttribute name="Term" item="{$indexEntryItem}">
								<parameter>
									<xsl:value-of select="/document/reference/containers/container[@type]/apidata/@name"/>
									<xsl:for-each select="/document/reference/containers/container[@type]/templates"><xsl:call-template name="csTemplatesInIndex" /></xsl:for-each>
									<xsl:text>.</xsl:text>
									<xsl:value-of select="/document/reference/apidata/@name" />
								</parameter>
							</includeAttribute>
						</MSHelp:Keyword>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
		</xsl:choose>
	</xsl:template>

	<xsl:template name="helpMetadata">
		<!-- F keywords -->
		<xsl:choose>
			<xsl:when test="$group='namespace'">
				<MSHelp:Keyword Index="F" Term="{/document/reference/apidata/@name}" />
			</xsl:when>
			<xsl:when test="$group='type'">
				<MSHelp:Keyword Index="F" Term="{/document/reference/apidata/@name}" />
				<MSHelp:Keyword Index="F" Term="{concat(/document/reference/containers/namespace/apidata/@name,'.',/document/reference/apidata/@name)}" />
			</xsl:when>
			<xsl:when test="$group='member'">
				<MSHelp:Keyword Index="F" Term="{/document/reference/apidata/@name}" />
				<!-- qualified name -->
			</xsl:when>
		</xsl:choose>
	</xsl:template>

	<xsl:template name="apiName">
		<xsl:choose>
			<xsl:when test="$subgroup='constructor'">
				<xsl:value-of select="/document/reference/containers/type/apidata/@name" />
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="/document/reference/apidata/@name" />
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

</xsl:stylesheet>
