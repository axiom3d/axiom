<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0" 
				xmlns:MSHelp="http://msdn.microsoft.com/mshelp"
				xmlns:ddue="http://ddue.schemas.microsoft.com/authoring/2003/5"
				xmlns:xlink="http://www.w3.org/1999/xlink"
        >

  <xsl:template name="autogenSeeAlsoLinks">
      
    <xsl:if test="$group='enumeration' or $group='member' or $group='members' or $group='derivedType'">
      <include item="SeeAlsoTypeLink">
        <parameter>
          <referenceLink target="{$typeId}" />
        </parameter>
      </include>
      <br/> 
    </xsl:if>

    <!-- a link to type's All Members list -->
    <xsl:if test="$subgroup='class' or $subgroup='structure' or $subgroup='interface' 
                 or $subgroup='DerivedTypeList' or $pseudo">
      <include item="SeeAlsoMembersLink">
        <parameter>
          <referenceLink target="{concat('AllMembers.',$typeId)}" />
        </parameter>
      </include>
      <!--
      <referenceLink target="{concat($typeId, '.Members')}">
        <include item="SeeAlsoTypeMembersLink">
          <parameter>
            <xsl:value-of  select="$typeName"/>
          </parameter>
        </include>
      </referenceLink>
      -->
      <br/>
    </xsl:if>
       
    <!-- a link to the namespace topic -->
    <xsl:if test="$group='type' or $group='enumeration' or $group='member' or $group='members' or $group='derivedType'">
    <include item="SeeAlsoNamespaceLink">
      <parameter>
        <referenceLink target="{$namespaceId}" />
      </parameter>
    </include>
    <br/>
    </xsl:if>
   
  </xsl:template>

  <xsl:variable name="typeId">
    <xsl:choose>
      <xsl:when test="/document/reference/apidata[@group='type']">
        <xsl:value-of select="$key"/>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="/document/reference/containers/type/@api"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:variable>

  <xsl:variable name="typeName">
    <xsl:choose>
      <xsl:when test="/document/reference/apidata[@group='type']">
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
    <xsl:value-of select="/document/reference/containers/namespace/@api"/>
  </xsl:variable>

  <!-- indent by 2*n spaces -->
  <xsl:template name="indent">
    <xsl:param name="count" />
    <xsl:if test="$count &gt; 1">
      <xsl:text>&#160;&#160;</xsl:text>
      <xsl:call-template name="indent">
        <xsl:with-param name="count" select="$count - 1" />
      </xsl:call-template>
    </xsl:if>
  </xsl:template>

  <xsl:template name="codeSection">
    <xsl:param name="codeLang" />
    <div class="code">
      <span codeLanguage="{$codeLang}">
        <table width="100%" cellspacing="0" cellpadding="0">
          <tr>
            <th>
              <include item="{$codeLang}"/>
              <xsl:text>&#xa0;</xsl:text>
            </th>
            <th>
              <span class="copyCode" onclick="CopyCode(this)" onkeypress="CopyCode_CheckKey(this, event)" onmouseover="ChangeCopyCodeIcon(this)" onmouseout="ChangeCopyCodeIcon(this)" tabindex="0">
                <img class="copyCodeImage" name="ccImage" align="absmiddle">
                  <includeAttribute name="alt" item="CopyCodeImage" />
                  <includeAttribute name="src" item="iconPath">
                    <parameter>copycode.gif</parameter>
                  </includeAttribute>
                </img>
                <include item="copyCode"/>
              </span>
            </th>
          </tr>
          <tr>
            <td colspan="2">
              <pre>
                <xsl:apply-templates/>
              </pre>
            </td>
          </tr>
        </table>
      </span>
    </div>

  </xsl:template>

</xsl:stylesheet>