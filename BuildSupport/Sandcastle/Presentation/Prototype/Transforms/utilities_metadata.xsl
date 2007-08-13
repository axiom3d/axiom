<?xml version="1.0"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.1"
		xmlns:MSHelp="http://msdn.microsoft.com/mshelp" xmlns:msxsl="urn:schemas-microsoft-com:xslt" >


	<xsl:template name="insertMetadata">
		<xsl:if test="$metadata='true'">
		<xml>
			<MSHelp:Attr Name="AssetID" Value="{$key}" />
			<!-- toc metadata -->
			<xsl:call-template name="linkMetadata" />
      <xsl:call-template name="writeIndexEntries" />
		<!--	<xsl:call-template name="indexMetadata" /> -->
			<xsl:call-template name="helpMetadata" />
			<MSHelp:Attr Name="TopicType" Value="apiref" />
      <!-- attribute to allow F1 help integration -->
      <MSHelp:Attr Name="TopicType" Value="kbSyntax" />
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
				<MSHelp:Keyword Index="A" Term="{translate(concat('frlrf',/document/reference/apidata/@name),'.','')}" />
			</xsl:when>
			<!-- types & members, too -->
      <xsl:when test="$group='type'">
        <MSHelp:Keyword Index="A" Term="{translate(concat('frlrf',/document/reference/containers/namespace/apidata/@name, /document/reference/apidata/@name, 'ClassTopic'),'.','')}" />
        <MSHelp:Keyword Index="A" Term="{translate(concat('frlrf',/document/reference/containers/namespace/apidata/@name, /document/reference/apidata/@name, 'MembersTopic'),'.','')}" />
      </xsl:when>
      <xsl:when test="$group='member'">
        <MSHelp:Keyword Index="A" Term="{translate(concat('frlrf',/document/reference/containers/namespace/apidata/@name, /document/reference/containers/type/apidata/@name, 'Class', /document/reference/apidata/@name, 'Topic'),'.','')}" />      
      </xsl:when>
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
								<parameter>
                  <xsl:choose>
                    <xsl:when test="$subgroup='constructor'">
                      <xsl:value-of select="/document/reference/containers/type/apidata/@name"/>
                    </xsl:when>
                    <xsl:otherwise>
                      <xsl:value-of select="/document/reference/apidata/@name" />
                    </xsl:otherwise>
                  </xsl:choose>
                </parameter>
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
      <!-- namespace pages get the namespace keyword, if it exists -->
      <xsl:when test="$group='namespace'">
        <xsl:variable name="namespace" select="/document/reference/apidata/@name" />
        <xsl:if test="boolean($namespace)">
          <MSHelp:Keyword Index="F" Term="{$namespace}" />
        </xsl:if>
			</xsl:when>
      <!-- type pages get type and namespace.type keywords -->
      <xsl:when test="$group='type'">
        <xsl:variable name="namespace" select="/document/reference/containers/namespace/apidata/@name" />
        <xsl:variable name="type">
           <xsl:for-each select="/document/reference[1]">
            <xsl:call-template name="typeNamePlain" />
          </xsl:for-each>
        </xsl:variable>
        <MSHelp:Keyword Index="F" Term="{$type}" />
        <xsl:if test="boolean($namespace)">
          <MSHelp:Keyword Index="F" Term="{concat($namespace,'.',$type)}" />
        </xsl:if>
			</xsl:when>
      <!-- member pages get member, type.member, and namepsace.type.member keywords -->
      <xsl:when test="$group='member'">
        <xsl:variable name="namespace" select="/document/reference/containers/namespace/apidata/@name" />
        <xsl:variable name="type">
          <xsl:for-each select="/document/reference/containers/type[1]">
            <xsl:call-template name="typeNamePlain" />
          </xsl:for-each>
        </xsl:variable>
        <xsl:variable name="member">
          <xsl:choose>
            <!-- if the member is a constructor, use the member name for the type name -->
            <xsl:when test="$subgroup='constructor'">
              <xsl:value-of select="$type" />
            </xsl:when>
            <xsl:otherwise>
              <xsl:value-of select="/document/reference/apidata/@name"/>
            </xsl:otherwise>
          </xsl:choose>
        </xsl:variable>
        <MSHelp:Keyword Index="F" Term="{$member}" />
        <MSHelp:Keyword Index="F" Term="{concat($type, '.', $member)}" />
        <xsl:if test="boolean($namespace)">
          <MSHelp:Keyword Index="F" Term="{concat($namespace, '.', $type, '.', $member)}" />
        </xsl:if>
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

  <xsl:template name="writeIndexEntries">
    <xsl:choose>
      <!-- namespace topics get one unqualified index entry -->
      <xsl:when test="$group='namespace'">
        <xsl:variable name="names">
          <xsl:for-each select="/document/reference">
            <xsl:call-template name="textNames" />
          </xsl:for-each>
        </xsl:variable>
        <MSHelp:Keyword Index="K">
          <includeAttribute name="Term" item="namespaceIndexEntry">
            <parameter>
              <xsl:value-of select="msxsl:node-set($names)/name" />
            </parameter>
          </includeAttribute>
        </MSHelp:Keyword>
      </xsl:when>
      <!-- type topics get unqualified and qualified index entries -->
      <xsl:when test="$group='type'">
        <xsl:variable name="names">
          <xsl:for-each select="/document/reference">
            <xsl:call-template name="textNames" />
          </xsl:for-each>
        </xsl:variable>
        <xsl:for-each select="msxsl:node-set($names)/name">
          <MSHelp:Keyword Index="K">
            <includeAttribute name="Term" item="{$subgroup}IndexEntry">
              <parameter>
                <xsl:value-of select="." />
              </parameter>
            </includeAttribute>
          </MSHelp:Keyword>
        </xsl:for-each>
        <xsl:variable name="qnames">
          <xsl:for-each select="/document/reference">
            <xsl:call-template name="qualifiedTextNames" />
          </xsl:for-each>
        </xsl:variable>
        <xsl:for-each select="msxsl:node-set($qnames)/name">
          <MSHelp:Keyword Index="K">
            <includeAttribute name="Term" item="{$subgroup}IndexEntry">
              <parameter>
                <xsl:value-of select="." />
              </parameter>
            </includeAttribute>
          </MSHelp:Keyword>
        </xsl:for-each>
        <!-- enumeration topics also get entries for each member -->
      </xsl:when>
      <!-- constructor (or constructor overload) topics get unqualified entries using the type names -->
      <xsl:when test="$subgroup='constructor' and not(/document/reference/memberdata/@overload='true')">
        <xsl:variable name="names">
          <xsl:for-each select="/document/reference/containers/type">
            <xsl:call-template name="textNames" />
          </xsl:for-each>
        </xsl:variable>
        <xsl:for-each select="msxsl:node-set($names)/name">
          <MSHelp:Keyword Index="K">
            <includeAttribute name="Term" item="constructorIndexEntry">
                  <parameter>
                    <xsl:value-of select="." />
                  </parameter>
            </includeAttribute>
          </MSHelp:Keyword>
        </xsl:for-each>
      </xsl:when>
      <!-- other member (or overload) topics get qualified and unqualified entries using the member names -->
      <xsl:when test="$group='member' and not(/document/reference/memberdata/@overload='true')">
        <xsl:variable name="entryType">
          <xsl:choose>
            <xsl:when test="$subsubgroup">
              <xsl:value-of select="$subsubgroup" />
            </xsl:when>
            <xsl:otherwise>
              <xsl:value-of select="$subgroup" />
            </xsl:otherwise>
          </xsl:choose>
        </xsl:variable>
        <xsl:variable name="names">
          <xsl:for-each select="/document/reference">
            <xsl:call-template name="textNames" />
          </xsl:for-each>
        </xsl:variable>
        <xsl:for-each select="msxsl:node-set($names)/name">
          <MSHelp:Keyword Index="K">
            <includeAttribute name="Term" item="{$entryType}IndexEntry">
              <parameter>
                <xsl:value-of select="." />
              </parameter>
            </includeAttribute>
          </MSHelp:Keyword>
        </xsl:for-each>
        <xsl:variable name="qnames">
          <xsl:for-each select="/document/reference">
            <xsl:call-template name="qualifiedTextNames" />
          </xsl:for-each>
        </xsl:variable>
        <xsl:for-each select="msxsl:node-set($qnames)/name">
          <MSHelp:Keyword Index="K">
            <includeAttribute name="Term" item="{$entryType}IndexEntry">
              <parameter>
                <xsl:value-of select="." />
              </parameter>
            </includeAttribute>
          </MSHelp:Keyword>
        </xsl:for-each>
      </xsl:when>
    </xsl:choose>
  </xsl:template>

  <xsl:template name="qualifiedTextNames">
    <xsl:choose>
      <!-- members get qualified by type name -->
      <xsl:when test="containers/type">
        <xsl:variable name="left">
          <xsl:for-each select="containers/type">
            <xsl:call-template name="textNames"/>
          </xsl:for-each>
        </xsl:variable>
        <xsl:variable name="right">
          <xsl:call-template name="textNames" />
        </xsl:variable>
        <xsl:call-template name="combineTextNames">
          <xsl:with-param name="left" select="msxsl:node-set($left)" />
          <xsl:with-param name="right" select="msxsl:node-set($right)" />
        </xsl:call-template>
      </xsl:when>
      <!-- types get qualified by namespace name -->
      <xsl:when test="containers/namespace">
        <xsl:variable name="left">
          <xsl:for-each select="containers/namespace">
            <xsl:call-template name="textNames"/>
          </xsl:for-each>
        </xsl:variable>
        <xsl:variable name="right">
          <xsl:call-template name="textNames" />
        </xsl:variable>
        <xsl:call-template name="combineTextNames">
          <xsl:with-param name="left" select="msxsl:node-set($left)" />
          <xsl:with-param name="right" select="msxsl:node-set($right)" />
        </xsl:call-template>
      </xsl:when>
    </xsl:choose>
  </xsl:template>
  
  <xsl:template name="combineTextNames">
    <xsl:param name="left" />
    <xsl:param name="right" />
    <xsl:choose>
      <xsl:when test="count($left/name) &gt; 1">
        <xsl:choose>
          <xsl:when test="count($right/name) &gt; 1">
            <!-- both left and right are multi-language -->
            <xsl:for-each select="$left/name">
              <xsl:variable name="language" select="@language" />
              <name language="{$language}">
                <xsl:value-of select="concat($left/name[@language=$language],'.',$right/name[@language=$language])"/>
              </name>
            </xsl:for-each>
          </xsl:when>
          <xsl:otherwise>
            <!-- left is multi-language, right is not -->
            <xsl:for-each select="$left/name">
              <xsl:variable name="language" select="@language" />
              <name language="{$language}">
                <xsl:value-of select="concat($left/name[@language=$language],'.',$right/name)"/>
              </name>
            </xsl:for-each>            
          </xsl:otherwise>
        </xsl:choose>
      </xsl:when>
      <xsl:otherwise>
        <xsl:choose>
          <xsl:when test="count($right/name) &gt; 1">
            <!-- right is multi-language, left is not -->
            <xsl:for-each select="$right/name">
              <xsl:variable name="language" select="@language" />
              <name language="{.}">
                <xsl:value-of select="concat($left/name,'.',$right/name[@language=$language])"/>
              </name>
            </xsl:for-each>
          </xsl:when>
          <xsl:otherwise>
            <!-- neiter is multi-language -->
            <name>
              <xsl:value-of select="concat($left/name,'.',$right/name)" />
            </name>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  
  <xsl:template name="textNames">
    <xsl:choose>
      <xsl:when test="count(templates/template) &gt; 0">
        <name language="c">
          <xsl:value-of select="apidata/@name" />
          <xsl:call-template name="csTemplateText" />
        </name>
        <name language="v">
          <xsl:value-of select="apidata/@name" />
          <xsl:call-template name="vbTemplateText" />
        </name>
      </xsl:when>
      <xsl:otherwise>
        <name>
          <xsl:value-of select="apidata/@name"/>
        </name>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template name="csTemplateText">
    <xsl:text>%3C</xsl:text>
    <xsl:call-template name="templateText" />
    <xsl:text>%3E</xsl:text>
  </xsl:template>

  <xsl:template name="vbTemplateText">
    <xsl:text>(Of </xsl:text>
    <xsl:call-template name="templateText" />
    <xsl:text>)</xsl:text>
  </xsl:template>
  
  <xsl:template name="templateText">
    <xsl:for-each select="templates/template">
      <xsl:value-of select="@name" />
      <xsl:if test="not(position()=last())">
        <xsl:text>%2C </xsl:text>
      </xsl:if>
    </xsl:for-each>
  </xsl:template>
  
</xsl:stylesheet>
