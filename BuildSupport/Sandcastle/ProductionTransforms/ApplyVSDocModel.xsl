<?xml version="1.0"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.1">

  <xsl:output indent="yes" encoding="UTF-8" />
  <xsl:variable name="derivedTypesLimit">10</xsl:variable>

  <xsl:key name="index" match="/reflection/apis/api" use="@id" />

  <xsl:template match="/">
    <reflection>
      <xsl:apply-templates select="/reflection/assemblies" />
      <xsl:apply-templates select="/reflection/apis" />
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
  
  <xsl:template name="UpdateTypeApiNode">
    <xsl:param name="derivedTypesTopicId" />
    <xsl:param name="allMembersTopicId" />
    <api>
      <xsl:copy-of select="@*"/>
      <xsl:for-each select="*">
        <xsl:choose>
          <xsl:when test="local-name(.)='elements' and $allMembersTopicId!=''">
            <elements allMembersTopicId="{$allMembersTopicId}">
              <xsl:copy-of select="."/>
            </elements>
          </xsl:when>
          <xsl:when test="local-name(.)='family' and $derivedTypesTopicId!=''">
            <family>
            <!-- copy the ancestors node -->
            <xsl:copy-of select="ancestors"/>
            <!-- Modify the descendents node -->  
            <descendents>
              <xsl:attribute name="derivedTypes">
                <xsl:value-of select="$derivedTypesTopicId"/>
              </xsl:attribute>
              <type>
                <xsl:attribute name="api">
                  <xsl:value-of select="$derivedTypesTopicId"/>
                </xsl:attribute>
                <xsl:attribute name="ref">
                  <xsl:value-of select="'true'"/>
                </xsl:attribute>
              </type>
            </descendents>
            </family>
          </xsl:when>
          <xsl:otherwise>
            <xsl:copy-of select="."/>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:for-each>
    </api>
  </xsl:template>

	<xsl:template match="api[apidata/@group='type']">
	       
		<xsl:variable name="typeId" select="@id" />
    
    <xsl:variable name="allMembersTopicId">
        <xsl:if test="(count(elements/*) &gt; 0) and apidata[not(@subgroup='enumeration')]">
          <xsl:value-of select="concat($typeId, '.Members')"/>
        </xsl:if>
    </xsl:variable>

    <xsl:variable name="derivedTypesTopicId">
      <xsl:if test="count(family/descendents/*) &gt; $derivedTypesLimit">
        <xsl:value-of select="concat($typeId, '.DerivedTypes')"/>
      </xsl:if>
    </xsl:variable>

    <xsl:call-template name="UpdateTypeApiNode">
      <xsl:with-param name="derivedTypesTopicId" select="$derivedTypesTopicId" />
      <xsl:with-param name="allMembersTopicId" select="$allMembersTopicId" />
    </xsl:call-template>

    <!-- create derived type APIs -->
    <xsl:if test="$derivedTypesTopicId!=''">
      <api>
        <xsl:attribute name="id">
          <xsl:value-of select="$typeId"/>
          <xsl:text>.DerivedTypes</xsl:text>
        </xsl:attribute>
        <apidata name="{apidata/@name}" group="derivedType" subgroup="DerivedTypeList"/>
        <containers>
          <library assembly="{containers/library/@assembly}" module="{containers/library/@module}"/>
          <namespace api="{containers/namespace/@api}" />
          <type api="{containers/type/@api}" />
        </containers>
        <elements>
          <xsl:for-each select="family/descendents/*">
            <element api="{@api}" />
          </xsl:for-each>
        </elements>
      </api>
    </xsl:if>

    <!-- now create member APIs -->
    <xsl:if test="$allMembersTopicId!=''">
      <api>
        <xsl:attribute name="id">
          <xsl:value-of select="$typeId"/>
          <xsl:text>.Members</xsl:text>
        </xsl:attribute>
        <apidata name="{apidata/@name}" group="members" subgroup="members"/>
        <containers>
          <library assembly="{containers/library/@assembly}" module="{containers/library/@module}"/>
          <namespace api="{containers/namespace/@api}" />
          <type api="{containers/type/@api}" />
        </containers>
        <elements>
          <xsl:for-each select="elements/element">
            <element api="{@api}" />
          </xsl:for-each>
        </elements>
      </api>
    </xsl:if>

    <xsl:if test="not(apidata/@subgroup = 'enumeration')">
      <!-- create constructor APIs -->
      <xsl:if test="(count(elements/element[contains(@api, '#ctor')]) &gt; 0)">
        <api>
          <xsl:attribute name="id">
            <xsl:value-of select="$typeId"/>
            <xsl:text>.Constructors</xsl:text>
          </xsl:attribute>
          <apidata name="{apidata/@name}" group="members" subgroup="constructors"/>
          <containers>
            <library assembly="{containers/library/@assembly}" module="{containers/library/@module}"/>
            <namespace api="{containers/namespace/@api}" />
            <type api="{containers/type/@api}" />
          </containers>
          <elements>
            <xsl:for-each select="elements/element">
              <xsl:if test="contains(@api, '#ctor')">
                <element api="{@api}" />
              </xsl:if>
            </xsl:for-each>
          </elements>
        </api>
      </xsl:if>

      <!-- create property APIs -->
      <xsl:if test="(count(elements/element[contains(@api, 'P:')]) &gt; 0)">
        <api>
          <xsl:attribute name="id">
            <xsl:value-of select="$typeId"/>
            <xsl:text>.Properties</xsl:text>
          </xsl:attribute>
          <apidata name="{apidata/@name}" group="members" subgroup="properties"/>
          <containers>
            <library assembly="{containers/library/@assembly}" module="{containers/library/@module}"/>
            <namespace api="{containers/namespace/@api}" />
            <type api="{containers/type/@api}" />
          </containers>
          <elements>
            <xsl:for-each select="elements/element">
              <xsl:if test="contains(@api, 'P:')">
                <element api="{@api}" />
              </xsl:if>
            </xsl:for-each>
          </elements>
        </api>
      </xsl:if>

      <!-- create event APIs -->
      <xsl:if test="(count(elements/element[contains(@api, 'E:')]) &gt; 0)">
        <api>
          <xsl:attribute name="id">
            <xsl:value-of select="$typeId"/>
            <xsl:text>.Events</xsl:text>
          </xsl:attribute>
          <apidata name="{apidata/@name}" group="members" subgroup="events"/>
          <containers>
            <library assembly="{containers/library/@assembly}" module="{containers/library/@module}"/>
            <namespace api="{containers/namespace/@api}" />
            <type api="{containers/type/@api}" />
          </containers>
          <elements>
            <xsl:for-each select="elements/element">
              <xsl:if test="contains(@api, 'E:')">
                <element api="{@api}" />
              </xsl:if>
            </xsl:for-each>
          </elements>
        </api>
      </xsl:if>

      <!-- create field APIs -->
      <xsl:if test="(count(elements/element[contains(@api, 'F:')]) &gt; 0)">
        <api>
          <xsl:attribute name="id">
            <xsl:value-of select="$typeId"/>
            <xsl:text>.Fields</xsl:text>
          </xsl:attribute>
          <apidata name="{apidata/@name}" group="members" subgroup="fields"/>
          <containers>
            <library assembly="{containers/library/@assembly}" module="{containers/library/@module}"/>
            <namespace api="{containers/namespace/@api}" />
            <type api="{containers/type/@api}" />
          </containers>
          <elements>
            <xsl:for-each select="elements/element">
              <xsl:if test="contains(@api, 'F:')">
                <element api="{@api}" />
              </xsl:if>
            </xsl:for-each>
          </elements>
        </api>
      </xsl:if>

      <!-- create method APIs -->
      <xsl:if test="(count(elements/element[contains(@api, 'M:') and not(contains(@api, '#ctor'))]) &gt; 0)">
        <api>
          <xsl:attribute name="id">
            <xsl:value-of select="$typeId"/>
            <xsl:text>.Methods</xsl:text>
          </xsl:attribute>
          <apidata name="{apidata/@name}" group="members" subgroup="methods"/>
          <containers>
            <library assembly="{containers/library/@assembly}" module="{containers/library/@module}"/>
            <namespace api="{containers/namespace/@api}" />
            <type api="{containers/type/@api}" />
          </containers>
          <elements>
            <xsl:for-each select="elements/element">
              <xsl:if test="(contains(@api, 'M:') and not(contains(@api, '#ctor')))">
                <element api="{@api}" />
              </xsl:if>
            </xsl:for-each>
          </elements>
        </api>
      </xsl:if>
    </xsl:if>
   
    <!-- now create overload APIs -->
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
    <xsl:copy-of select="." />
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
