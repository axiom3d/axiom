<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0" 
				xmlns:MSHelp="http://msdn.microsoft.com/mshelp"
				xmlns:ddue="http://ddue.schemas.microsoft.com/authoring/2003/5"
				xmlns:xlink="http://www.w3.org/1999/xlink"
        >

  <xsl:template name="autogenSeeAlsoLinks">
    <!-- a link to parent type -->
    <xsl:if test="$group='member' or ($pseudo and not($group='root' or $group='namespace'))">
      <xsl:call-template name="createReferenceLink">
        <xsl:with-param name="id" select="$typeId" />
        <xsl:with-param name="forceHot" select="true()" />
      </xsl:call-template>
      <br/>
    </xsl:if>

    <!-- a link to type's All Members list -->
    <xsl:if test="$subgroup='class' or $subgroup='structure' or $subgroup='interface' 
                 or $group='member' 
                 or ($pseudo and not($group='root' or $group='namespace'))">
      <xsl:call-template name="createReferenceLink">
        <xsl:with-param name="id" select="concat('AllMembers.',$typeId)" />
        <xsl:with-param name="forceHot" select="true()" />
        <xsl:with-param name="displayText">
          <include item="SeeAlsoTypeMembersLink">
            <parameter>
              <xsl:value-of  select="$typeName"/>
            </parameter>
          </include>
        </xsl:with-param>
      </xsl:call-template>
      <br/>
    </xsl:if>

    <!-- a link to the namespace topic -->
    <xsl:call-template name="createReferenceLink">
      <xsl:with-param name="id">
        <xsl:value-of select="$namespaceId"/>
      </xsl:with-param>
      <xsl:with-param name="forceHot" select="true()" />
    </xsl:call-template>
    <br/>

  </xsl:template>

  <xsl:variable name="typeId">
    <xsl:choose>
      <xsl:when test="/document/reference/apidata[@group='type' and not(@pseudo='true')]">
        <xsl:value-of select="$key"/>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="/document/reference/containers/type/@api"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:variable>

  <xsl:variable name="typeName">
    <xsl:choose>
      <xsl:when test="/document/reference/apidata[@group='type' and not(@pseudo='true')]">
        <xsl:for-each select="/document/reference">
          <xsl:call-template name="GetTypeName"/>
        </xsl:for-each>
      </xsl:when>
      <xsl:otherwise>
        <xsl:for-each select="/document/reference/containers/type">
          <xsl:call-template name="GetTypeName"/>
        </xsl:for-each>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:variable>

  <xsl:template name="GetTypeName">
    <xsl:value-of  select="apidata/@name"/>
    <!-- show a type parameter list on generic types -->
    <xsl:if test="templates/template">
      <xsl:text>&lt;</xsl:text>
      <xsl:for-each select="templates/template">
        <xsl:if test="position()!=1">,</xsl:if>
        <xsl:value-of select="@name"/>
      </xsl:for-each>
      <xsl:text>&gt;</xsl:text>
    </xsl:if>
  </xsl:template>

  <xsl:variable name="namespaceId">
    <xsl:choose>
      <xsl:when test="/document/reference/apidata[@group='type' and not(@pseudo='true')]">
        <xsl:value-of select="/document/reference/containers/namespace/@api"/>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="/document/reference/containers/type/containers/namespace/@api"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:variable>

</xsl:stylesheet>