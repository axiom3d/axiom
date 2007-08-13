<?xml version="1.0"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.1">

  <xsl:output indent="yes" encoding="UTF-8" />
  <xsl:param name="derivedTypesLimit" />

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
              <xsl:copy-of select="element"/>
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
          <xsl:value-of select="concat('AllMembers.', $typeId)"/>
        </xsl:if>
    </xsl:variable>

    <xsl:variable name="derivedTypesTopicId">
      <xsl:if test="count(family/descendents/*) &gt; $derivedTypesLimit">
        <xsl:value-of select="concat('DerivedTypes.', $typeId)"/>
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
          <xsl:text>DerivedTypes.</xsl:text>
          <xsl:value-of select="$typeId"/>
        </xsl:attribute>
        <apidata name="{apidata/@name}" group="derivedType" subgroup="DerivedTypeList"/>
	<xsl:copy-of select="templates" />
        <containers>
          <library assembly="{containers/library/@assembly}" module="{containers/library/@module}"/>
          <namespace api="{containers/namespace/@api}" />
          <type api="{$typeId}" >
            <xsl:copy-of select="containers/type"/>
          </type>
        </containers>
        <elements>
          <xsl:for-each select="family/descendents/*">
            <element api="{@api}" />
          </xsl:for-each>
        </elements>
      </api>
    </xsl:if>

    <!-- now create all member APIs -->
    <xsl:if test="$allMembersTopicId!=''">
      <api>
        <xsl:attribute name="id">
	        <xsl:text>AllMembers.</xsl:text>	
          <xsl:value-of select="$typeId"/>
        </xsl:attribute>
        <apidata name="{apidata/@name}" group="members" subgroup="members"/>
        <xsl:copy-of select="templates"/>
        <containers>
          <library assembly="{containers/library/@assembly}" module="{containers/library/@module}"/>
          <namespace api="{containers/namespace/@api}" />
          <type api="{$typeId}" >
            <xsl:copy-of select="containers/type"/>
          </type>
        </containers>
        <elements>
                   
          <xsl:variable name="members" select="key('index',elements/element/@api)" />
          <xsl:for-each select="$members">
            <xsl:variable name="name" select="apidata/@name" />
            <xsl:variable name="subgroup" select="apidata/@subgroup" />
            <xsl:variable name="set" select="$members[apidata/@name=$name and apidata/@subgroup=$subgroup]" />
            <xsl:if test="(count($set) &gt; 1) and (($set[containers/type/@api=$typeId][1]/@id)=@id)">
              <xsl:variable name="id">
                <xsl:call-template name="overloadId">
                  <xsl:with-param name="memberId" select="@id" />
                </xsl:call-template>
              </xsl:variable>
              <element api="{$id}" />
            </xsl:if>
            <xsl:if test="not(memberdata/@overload='true')">
              <element api="{@id}" />
            </xsl:if>
          </xsl:for-each>

          <xsl:for-each select="elements/element">
            <xsl:if test="apidata/@subgroup">
              <element api="{@api}" />
            </xsl:if>
          </xsl:for-each>
          
        </elements>
      </api>
    </xsl:if>

    <!-- Add method APIs -->
    <xsl:call-template name="AddMemberlistAPI">
      <xsl:with-param name="subgroup">method</xsl:with-param>
      <xsl:with-param name="subsubgroup">method</xsl:with-param>
      <xsl:with-param name="topicSubgroup">Methods</xsl:with-param>
      <xsl:with-param name="typeId" select="$typeId" />
    </xsl:call-template>

    <!-- Add property APIs -->
    <xsl:call-template name="AddMemberlistAPI">
      <xsl:with-param name="subgroup">property</xsl:with-param>
      <xsl:with-param name="subsubgroup">property</xsl:with-param>
      <xsl:with-param name="topicSubgroup">Properties</xsl:with-param>
      <xsl:with-param name="typeId" select="$typeId" />
    </xsl:call-template>

    <!-- Add event APIs -->
    <xsl:call-template name="AddMemberlistAPI">
      <xsl:with-param name="subgroup">event</xsl:with-param>
      <xsl:with-param name="subsubgroup">event</xsl:with-param>
      <xsl:with-param name="topicSubgroup">Events</xsl:with-param>
      <xsl:with-param name="typeId" select="$typeId" />
    </xsl:call-template>

    <!-- Add field APIs -->
    <xsl:call-template name="AddMemberlistAPI">
      <xsl:with-param name="subgroup">field</xsl:with-param>
      <xsl:with-param name="subsubgroup">field</xsl:with-param>
      <xsl:with-param name="topicSubgroup">Fields</xsl:with-param>
      <xsl:with-param name="typeId" select="$typeId" />
    </xsl:call-template>
   
    <!-- Add attached Property APIs -->
    <xsl:call-template name="AddAttachedMemberlistAPI">
      <xsl:with-param name="subgroup">property</xsl:with-param>
      <xsl:with-param name="subsubgroup">attachedProperty</xsl:with-param>
      <xsl:with-param name="topicSubgroup">AttachedProperties</xsl:with-param>
      <xsl:with-param name="typeId" select="$typeId" />
    </xsl:call-template>

    <!-- Add attached Event APIs -->
    <xsl:call-template name="AddAttachedMemberlistAPI">
      <xsl:with-param name="subgroup">event</xsl:with-param>
      <xsl:with-param name="subsubgroup">attachedEvent</xsl:with-param>
      <xsl:with-param name="topicSubgroup">AttachedEvents</xsl:with-param>
      <xsl:with-param name="typeId" select="$typeId" />
    </xsl:call-template>
       
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
          <xsl:copy-of select="templates" />
          <memberdata visibility="{memberdata/@visibility}" />
          <containers>
            <library assembly="{containers/library/@assembly}" module="{containers/library/@module}"/>
            <namespace api="{containers/namespace/@api}" />
            <type api="{$typeId}">
              <xsl:copy-of select="containers/type" />
            </type>
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

  <xsl:template name="AddMemberlistAPI">
    <xsl:param name="subgroup"/>
    <xsl:param name="subsubgroup" />
    <xsl:param name="topicSubgroup"/>
    <xsl:param name="typeId" />
    
    <xsl:variable name="declaredMembers" select="key('index',elements/element/@api)[apidata/@subgroup=$subgroup and not(apidata/@subsubgroup)]" />
    <xsl:variable name="inheritedMembers" select="elements/element[apidata/@subgroup=$subgroup and not(apidata/@subsubgroup)]" />
     
    <xsl:if test="(count($declaredMembers) &gt; 0) or (count($inheritedMembers) &gt; 0)">
        <api>
          <xsl:attribute name="id">
            <xsl:value-of select="concat($topicSubgroup, '.', $typeId)"/> 	
          </xsl:attribute>
          <apidata name="{apidata/@name}" group="members" subgroup="{$topicSubgroup}"/>
          <xsl:copy-of select="templates" />
          <containers>
            <library assembly="{containers/library/@assembly}" module="{containers/library/@module}"/>
            <namespace api="{containers/namespace/@api}" />
            <type api="{$typeId}" >
              <xsl:copy-of select="containers/type"/>
            </type>
          </containers>
          <elements>
                       
            <xsl:variable name="members" select="key('index',elements/element/@api)" />
            <xsl:for-each select="$members">
              <xsl:variable name="name" select="apidata/@name" />
              <xsl:variable name="set" select="$members[apidata/@name=$name and apidata/@subgroup=$subgroup]" />
              <xsl:if test="(count($set) &gt; 1) and (($set[containers/type/@api=$typeId][1]/@id)=@id)">
                <xsl:variable name="id">
                  <xsl:call-template name="overloadId">
                    <xsl:with-param name="memberId" select="@id" />
                  </xsl:call-template>
                </xsl:variable>
                <element api="{$id}" />
              </xsl:if>
              <xsl:if test="apidata/@subgroup=$subgroup and not(apidata/@subsubgroup) and not(memberdata/@overload = 'true')">
                <element api="{@id}" />
              </xsl:if>
            </xsl:for-each>
            
            <xsl:for-each select="elements/element">
              <xsl:if test="apidata/@subgroup=$subgroup and not(apidata/@subsubgroup)">
                <element api="{@api}" />
              </xsl:if>
            </xsl:for-each>
         </elements>
        </api>
          
    </xsl:if>
  </xsl:template>

  <xsl:template name="AddAttachedMemberlistAPI">
    <xsl:param name="subgroup"/>
    <xsl:param name="subsubgroup" />
    <xsl:param name="topicSubgroup"/>
    <xsl:param name="typeId" />

    <xsl:variable name="declaredMembers" select="key('index',elements/element/@api)[apidata/@subsubgroup=$subsubgroup]" />
    <xsl:variable name="inheritedMembers" select="elements/element[apidata/@subsubgroup=$subsubgroup]" />
   
    <xsl:if test="(count($declaredMembers) &gt; 0) or (count($inheritedMembers) &gt; 0)">
        <api>
          <xsl:attribute name="id">
            <xsl:value-of select="concat($topicSubgroup, '.', $typeId)"/>
          </xsl:attribute>
          <apidata name="{apidata/@name}" group="members" subgroup="{$topicSubgroup}" subsubgroup="{$subsubgroup}"/>
          <xsl:copy-of select="templates" />
          <containers>
            <library assembly="{containers/library/@assembly}" module="{containers/library/@module}"/>
            <namespace api="{containers/namespace/@api}" />
            <type api="{$typeId}" >
              <xsl:copy-of select="containers/type"/>
            </type>
          </containers>
          <elements>
          
            <xsl:variable name="members" select="key('index',elements/element/@api)" />
            <xsl:for-each select="$members">
              <xsl:variable name="name" select="apidata/@name" />
              <xsl:variable name="set" select="$members[apidata/@name=$name and apidata/@subgroup=$subgroup and apidata/@subsubgroup=$subsubgroup]" />
              <xsl:if test="(count($set) &gt; 1) and (($set[containers/type/@api=$typeId][1]/@id)=@id)">
                <xsl:variable name="id">
                  <xsl:call-template name="overloadId">
                    <xsl:with-param name="memberId" select="@id" />
                  </xsl:call-template>
                </xsl:variable>
                <element api="{$id}" />
              </xsl:if>
              <xsl:if test="apidata/@subgroup=$subgroup and apidata/@subsubgroup=$subsubgroup and not(memberdata/@overload = 'true')">
                <element api="{@id}" />
              </xsl:if>
            </xsl:for-each>

            <xsl:for-each select="elements/element">
              <xsl:if test="apidata/@subgroup=$subgroup and apidata/@subsubgroup=$subsubgroup">
                <element api="{@api}" />
              </xsl:if>
            </xsl:for-each>
          </elements>
        </api>
      
    </xsl:if>
  </xsl:template>

</xsl:stylesheet>
