<?xml version="1.0"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.1">

	<!-- stuff specified to comments authored in DDUEXML -->

	
 <xsl:include href="htmlBody.xsl"/>
  <xsl:include href="utilities_reference.xsl" />

	<xsl:variable name="summary" select="normalize-space(/document/comments/summary)" />
  <xsl:variable name="hasSeeAlsoSection" select="boolean(count(/document/comments/seealso) > 0)"/>
  <xsl:variable name="examplesSection" select="boolean(string-length(/document/comments/example[normalize-space(.)]) > 0)"/>
  
  <xsl:template name="body">
        
		<!-- auto-inserted info -->
		<!-- <xsl:apply-templates select="/document/reference/attributes" /> -->
    <xsl:apply-templates select="/document/comments/preliminary" />
		<xsl:apply-templates select="/document/comments/summary" />
    <xsl:if test="$group='member'">
      <xsl:apply-templates select="/document/reference/elements" mode="overloadSummary" />
    </xsl:if>
    <!-- assembly information -->
    <xsl:if test="not($group='member' or $group='members')">
      <xsl:apply-templates select="/document/reference/containers/library" />
    </xsl:if>
		<!-- syntax -->
    <xsl:if test="not($group='members') and not($group ='namespace')">
      <xsl:apply-templates select="/document/syntax" />
    </xsl:if>
    <!-- generic templates -->
    <xsl:apply-templates select="/document/reference/templates" />
		<!-- members -->
		<xsl:choose>
			<xsl:when test="$group='root'">
				<xsl:apply-templates select="/document/reference/elements" mode="root" />
			</xsl:when>
			<xsl:when test="$group='namespace'">
				<xsl:apply-templates select="/document/reference/elements" mode="namespace" />
			</xsl:when>
			<xsl:when test="$subgroup='enumeration'">
				<xsl:apply-templates select="/document/reference/elements" mode="enumeration" />
			</xsl:when>
			<xsl:when test="$group='type'">
				<xsl:apply-templates select="/document/reference/elements" mode="type" />
			</xsl:when>
			<xsl:when test="$group='member'">
				<xsl:apply-templates select="/document/reference/elements" mode="overload" />
			</xsl:when>
      <xsl:when test="$group='members'">
        <xsl:apply-templates select="/document/reference/elements" mode="member" />
      </xsl:when>
		</xsl:choose>
		<!-- remarks -->
		<xsl:apply-templates select="/document/comments/remarks" />
    <xsl:apply-templates select="/document/comments/threadsafety" />
		<!-- example -->
		<xsl:apply-templates select="/document/comments/example" />
		<!-- other comment sections -->
		<!-- permissions -->
    <xsl:call-template name="permissions" />
		<!-- exceptions -->
		<xsl:call-template name="exceptions" />
		<!-- inheritance -->
		<xsl:apply-templates select="/document/reference/family" />
		<!-- see also -->
    <xsl:call-template name="seealso" />

  </xsl:template> 

	<xsl:template name="getParameterDescription">
		<xsl:param name="name" />
		<xsl:apply-templates select="/document/comments/param[@name=$name]" />
	</xsl:template>

	<xsl:template name="getReturnsDescription">
		<xsl:param name="name" />
		<xsl:apply-templates select="/document/comments/param[@name=$name]" />
	</xsl:template>

	<xsl:template name="getElementDescription">
		<xsl:apply-templates select="summary" />
	</xsl:template>

  <xsl:template name="getOverloadSummary">
    <xsl:apply-templates select="overloads" mode="summary"/>
  </xsl:template>

  <xsl:template name="getOverloadSections">
    <xsl:apply-templates select="overloads" mode="sections"/>
  </xsl:template>

 


  <!-- block sections -->

	<xsl:template match="summary">
		<div class="summary">
			<xsl:apply-templates />
		</div>
	</xsl:template>

  <xsl:template match="overloads" mode="summary">
    <xsl:choose>
        <xsl:when test="count(summary) > 0">
          <xsl:apply-templates select="summary" />
        </xsl:when>
        <xsl:otherwise>
          <div class="summary">
            <xsl:apply-templates/>
          </div>
        </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="overloads" mode="sections">
    <xsl:apply-templates select="remarks" />
    <xsl:apply-templates select="example"/>
  </xsl:template>

	<xsl:template match="value">
    <xsl:call-template name="subSection">
      <xsl:with-param name="title">
        <include item="fieldValueTitle" />
      </xsl:with-param>
      <xsl:with-param name="content">
        <xsl:apply-templates />
      </xsl:with-param>
    </xsl:call-template>
	</xsl:template>

	<xsl:template match="returns">
    <xsl:call-template name="subSection">
      <xsl:with-param name="title">
        <include item="methodValueTitle" />
      </xsl:with-param>
      <xsl:with-param name="content">
        <xsl:apply-templates />
      </xsl:with-param>
    </xsl:call-template>
  </xsl:template>

  <xsl:template match="templates">
    <xsl:call-template name="section">
      <xsl:with-param name="toggleSwitch" select="'templates'" />
      <xsl:with-param name="title">
        <include item="templatesTitle" />
      </xsl:with-param>
      <xsl:with-param name="content">
        <dl>
          <xsl:for-each select="template">
            <xsl:variable name="templateName" select="@name" />
            <dt>
              <span class="parameter">
                <xsl:value-of select="$templateName"/>
              </span>
            </dt>
            <dd>
              <xsl:apply-templates select="/document/comments/typeparam[@name=$templateName]" />
            </dd>
          </xsl:for-each>
        </dl>
      </xsl:with-param>
    </xsl:call-template>
  </xsl:template>

	<xsl:template match="remarks">
		<xsl:call-template name="section">
      <xsl:with-param name="toggleSwitch" select="'remarks'"/>
			<xsl:with-param name="title"><include item="remarksTitle" /></xsl:with-param>
			<xsl:with-param name="content"><xsl:apply-templates /></xsl:with-param>
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="example">
		<xsl:call-template name="section">
      <xsl:with-param name="toggleSwitch" select="'example'"/>
			<xsl:with-param name="title"><include item="examplesTitle" /></xsl:with-param>
			<xsl:with-param name="content"><xsl:apply-templates /></xsl:with-param>
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="para">
		<p><xsl:apply-templates /></p>
	</xsl:template>

	<xsl:template match="code">
		<div class="code"><pre><xsl:apply-templates /></pre></div>
	</xsl:template>

	<xsl:template name="exceptions">
		<xsl:if test="count(/document/comments/exception) &gt; 0">
			<xsl:call-template name="section">
        <xsl:with-param name="toggleSwitch" select="'exceptions'"/>
				<xsl:with-param name="title"><include item="exceptionsTitle" /></xsl:with-param>
				<xsl:with-param name="content">
				<table class="exceptions">
					<tr>
						<th class="exceptionNameColumn"><include item="exceptionNameHeader" /></th>
						<th class="exceptionConditionColumn"><include item="exceptionConditionHeader" /></th>
					</tr>
					<xsl:for-each select="/document/comments/exception">
						<tr>
							<td><referenceLink target="{@cref}" qualified="true" /></td>
							<td><xsl:apply-templates select="." /></td>
						</tr>
					</xsl:for-each>
				</table>
				</xsl:with-param>
			</xsl:call-template>
		</xsl:if>
	</xsl:template>

  <xsl:template name="permissions">
    <xsl:if test="count(/document/comments/permission) &gt; 0">
      <xsl:call-template name="section">
        <xsl:with-param name="toggleSwitch" select="'permissions'" />
        <xsl:with-param name="title">
          <include item="permissionsTitle" />
        </xsl:with-param>
        <xsl:with-param name="content">
          <table class="permissions">
            <tr>
              <th class="permissionNameColumn">
                <include item="permissionNameHeader" />
              </th>
              <th class="permissionDescriptionColumn">
                <include item="permissionDescriptionHeader" />
              </th>
            </tr>
            <xsl:for-each select="/document/comments/permission">
              <tr>
                <td>
                  <referenceLink target="{@cref}" qualified="true" />
                </td>
                <td>
                  <xsl:apply-templates select="." />
                </td>
              </tr>
            </xsl:for-each>
          </table>
        </xsl:with-param>
      </xsl:call-template>
    </xsl:if>
  </xsl:template>

  <xsl:template name="seealso">
		<xsl:if test="count(/document/comments/seealso | /document/comments/summary/seealso) &gt; 0">
      <xsl:call-template name="section">
        <xsl:with-param name="toggleSwitch" select="'seealso'" />
        <xsl:with-param name="title">
          <include item="relatedTitle" />
        </xsl:with-param>
        <xsl:with-param name="content">
					<xsl:for-each select="/document/comments/seealso | /document/comments/summary/seealso">
            <xsl:apply-templates select="." />
            <br />
          </xsl:for-each>
        </xsl:with-param>
      </xsl:call-template>
    </xsl:if>
  </xsl:template>

	<xsl:template match="list[@type='bullet']">
		<ul>
			<xsl:for-each select="item">
				<li><xsl:apply-templates /></li>
			</xsl:for-each>
		</ul>
	</xsl:template>

	<xsl:template match="list[@type='number']">
		<ul>
			<xsl:for-each select="item">
				<li><xsl:apply-templates /></li>
			</xsl:for-each>
		</ul>
	</xsl:template>

	<xsl:template match="list[@type='table']">
		<table class="authoredTable">
			<xsl:for-each select="listheader">
				<tr>
					<xsl:for-each select="*">
						<th><xsl:apply-templates /></th>
					</xsl:for-each>
				</tr>
			</xsl:for-each>
			<xsl:for-each select="item">
				<tr>
					<xsl:for-each select="*">
						<td><xsl:apply-templates /></td>
					</xsl:for-each>
				</tr>
			</xsl:for-each>
		</table>
	</xsl:template>

	<!-- inline tags -->

  <xsl:template match="see[@cref]">
    <xsl:choose>
      <xsl:when test="normalize-space(.)">
        <referenceLink target="{@cref}" qualified="true">
          <xsl:value-of select="." />
        </referenceLink>
      </xsl:when>
      <xsl:otherwise>
        <referenceLink target="{@cref}" qualified="true" />
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="see[@langword]">
    <span class="keyword">
      <xsl:choose>
        <xsl:when test="@langword='null' or @langword='Nothing' or @langword='nullptr'">
          <span class="cs">null</span>
          <span class="vb">Nothing</span>
          <span class="cpp">nullptr</span>
        </xsl:when>
        <xsl:when test="@langword='static' or @langword='Shared'">
          <span class="cs">static</span>
          <span class="vb">Shared</span>
          <span class="cpp">static</span>
        </xsl:when>
        <xsl:when test="@langword='virtual' or @langword='Overridable'">
          <span class="cs">virtual</span>
          <span class="vb">Overridable</span>
          <span class="cpp">virtual</span>
        </xsl:when>
        <xsl:when test="@langword='true' or @langword='True'">
          <span class="cs">true</span>
          <span class="vb">True</span>
          <span class="cpp">true</span>
        </xsl:when>
        <xsl:when test="@langword='false' or @langword='False'">
          <span class="cs">false</span>
          <span class="vb">False</span>
          <span class="cpp">false</span>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="@langword" />
        </xsl:otherwise>
      </xsl:choose>
    </span>
  </xsl:template>


  <xsl:template match="seealso">
    <xsl:choose>
    <xsl:when test="normalize-space(.)">
      <referenceLink target="{@cref}" qualified="true">
        <xsl:value-of select="." />
      </referenceLink>
    </xsl:when>
      <xsl:otherwise>
        <referenceLink target="{@cref}" qualified="true" />
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="c">
    <span class="code">
      <xsl:value-of select="." />
    </span>
  </xsl:template>

  <xsl:template match="paramref">
    <span class="parameter">
      <xsl:value-of select="@name" />
    </span>
  </xsl:template>

  <xsl:template match="typeparamref">
    <span class="typeparameter">
      <xsl:value-of select="@name" />
    </span>
  </xsl:template>

	<!-- pass through html tags -->

	<xsl:template match="p|ol|ul|li|dl|dt|dd|table|tr|th|td|a|img|b|i|strong|em|del|sub|sup|br|hr|h1|h2|h3|h4|h5|h6|pre|div|span|blockquote|abbr|acronym">
		<xsl:copy>
			<xsl:copy-of select="@*" />
			<xsl:apply-templates />
		</xsl:copy>
	</xsl:template>

  <!-- extra tag support -->

  <xsl:template match="threadsafety">
    <xsl:call-template name="section">
      <xsl:with-param name="toggleSwitch" select="'threadSafety'" />
      <xsl:with-param name="title">
        <include item="threadSafetyTitle" />
      </xsl:with-param>
      <xsl:with-param name="content">
        <xsl:choose>
          <xsl:when test="normalize-space(.)">
            <xsl:apply-templates />
          </xsl:when>
          <xsl:otherwise>
            <xsl:if test="@static='true'">
              <include item="staticThreadSafe" />
            </xsl:if>
            <xsl:if test="@static='false'">
              <include item="staticNotThreadSafe" />
            </xsl:if>
            <xsl:if test="@instance='true'">
              <include item="instanceThreadSafe" />
            </xsl:if>
            <xsl:if test="@instance='false'">
              <include item="instanceNotThreadSafe" />
            </xsl:if>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:with-param>
    </xsl:call-template>
  </xsl:template>

  <xsl:template match="note">
    <div class="alert">
      <img>
        <includeAttribute item="iconPath" name="src">
          <parameter>alert_note.gif</parameter>
        </includeAttribute>
      </img>
      <xsl:text> </xsl:text>
      <include item="noteTitle" />
      <xsl:apply-templates />
    </div>
  </xsl:template>

  <xsl:template match="preliminary">
    <div class="preliminary">
      <include item="preliminaryText" />
    </div>
  </xsl:template>

	<!-- move these off into a shared file -->

  <xsl:template name="createReferenceLink">
    <xsl:param name="id" />
    <xsl:param name="qualified" select="false()" />
    <b>
      <referenceLink target="{$id}" qualified="{$qualified}" />
    </b>
  </xsl:template>
 
	<xsl:template name="section">
    <xsl:param name="toggleSwitch" />
		<xsl:param name="title" />
		<xsl:param name="content" />
    
    <xsl:variable name="toggleTitle" select="concat($toggleSwitch,'Toggle')" />
    <xsl:variable name="toggleSection" select="concat($toggleSwitch,'Section')" />
            
      <h1 class="heading">
        <span onclick="ExpandCollapse({$toggleTitle})" style="cursor:default;" onkeypress="ExpandCollapse_CheckKey({$toggleTitle}, event)" tabindex="0">
          <img id="{$toggleTitle}" onload="OnLoadImage(event)" class="toggle" name="toggleSwitch">
            <includeAttribute name="src" item="iconPath">
              <parameter>collapse_all.gif</parameter>
            </includeAttribute>
          </img>
          <xsl:copy-of select="$title" />
        </span>
      </h1>
    
    <div id="{$toggleSection}" class="section" name="collapseableSection" style="">
      <xsl:copy-of select="$content" />
    </div>
    
	</xsl:template>

  <xsl:template name="subSection">
    <xsl:param name="title" />
    <xsl:param name="content" />

    <h4 class="subHeading">
      <xsl:copy-of select="$title" />
    </h4>
    <xsl:copy-of select="$content" />
    
  </xsl:template>


</xsl:stylesheet>
