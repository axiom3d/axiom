<?xml version="1.0"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.1"
		xmlns:ddue="http://ddue.schemas.microsoft.com/authoring/2003/5"
		xmlns:xlink="http://www.w3.org/1999/xlink"
		xmlns:mshelp="http://msdn.microsoft.com/mshelp" >

	<!-- sections -->

	<xsl:template match="ddue:summary">
		<div class="summary">
			<xsl:apply-templates />
		</div>
	</xsl:template>

	<xsl:template match="ddue:remarks">
    <xsl:variable name="remarksSection" select="boolean(/document/comments/ddue:dduexml/ddue:remarks[normalize-space(.)!=''])"/>
    <xsl:variable name="notesForImplementers" select="boolean(/document/comments/ddue:dduexml/ddue:notesForImplementers[normalize-space(.)!=''])"/>
    <xsl:variable name="notesForCallers" select="boolean(/document/comments/ddue:dduexml/ddue:notesForCallers[normalize-space(.)!=''])"/>
    <xsl:variable name="notesForInheritors" select="boolean(/document/comments/ddue:dduexml/ddue:notesForInheritors[normalize-space(.)!=''])"/>
    <xsl:variable name="platformNotes" select="boolean(/document/comments/ddue:dduexml/ddue:platformNotes[normalize-space(.)!=''])"/>

    <xsl:if test="$remarksSection or $notesForImplementers or $notesForCallers or $notesForInheritors or $platformNotes">
			<xsl:call-template name="section">
         <xsl:with-param name="toggleSwitch" select="'remarks'"/>
				<xsl:with-param name="title"><include item="remarksTitle" /></xsl:with-param>
				<xsl:with-param name="content">
          <xsl:apply-templates />
          <xsl:if test="$notesForImplementers">
            <xsl:apply-templates select="/document/comments/ddue:dduexml/ddue:notesForImplementers"/>
          </xsl:if>
          <xsl:if test="$notesForCallers">
            <xsl:apply-templates select="/document/comments/ddue:dduexml/ddue:notesForCallers"/>
          </xsl:if>
          <xsl:if test="$notesForInheritors">
            <xsl:apply-templates select="/document/comments/ddue:dduexml/ddue:notesForInheritors"/>
          </xsl:if>
          <xsl:if test="$platformNotes">
            <xsl:apply-templates select="/document/comments/ddue:dduexml/ddue:platformNotes"/>
          </xsl:if>
        </xsl:with-param>
			</xsl:call-template>
		</xsl:if>
	</xsl:template>

	<xsl:template match="ddue:codeExamples">
		<xsl:if test="normalize-space(.)">
			<xsl:call-template name="section">
        <xsl:with-param name="toggleSwitch" select="'codeExamples'"/>
				<xsl:with-param name="title"><include item="examplesTitle" /></xsl:with-param>
				<xsl:with-param name="content"><xsl:apply-templates /></xsl:with-param>
			</xsl:call-template>
		</xsl:if>
	</xsl:template>

  <xsl:template name="threadSafety">
    <xsl:call-template name="section">
      <xsl:with-param name="toggleSwitch" select="'threadSafety'"/>
			<xsl:with-param name="title"><include item="threadSafetyTitle" /></xsl:with-param>
			<xsl:with-param name="content">
        <xsl:choose>
          <xsl:when test="/document/comments/ddue:dduexml/ddue:threadSafety">
            <xsl:apply-templates select="/document/comments/ddue:dduexml/ddue:threadSafety"/>
          </xsl:when>
          <xsl:otherwise>
            <include item="ThreadSafetyBP"/>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:with-param>
		</xsl:call-template>
	</xsl:template>

  <xsl:template match="ddue:notesForImplementers">
    <p/>
    <b>
      <include item="NotesForImplementers"/>
    </b>
    <xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="ddue:notesForCallers">
    <p/>
    <b>
      <include item="NotesForCallers"/>
    </b>
    <xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="ddue:notesForInheritors">
    <p/>
    <b>
      <include item="NotesForInheritors"/>
    </b>
    <xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="ddue:platformNotes">
    <xsl:if test="string-length(../ddue:platformNotes[normalize-space(.)]) > 0">
      <xsl:for-each select="ddue:platformNote">
        <p>
          <include item="PlatformNote">
            <parameter>
              <xsl:for-each select="ddue:platforms/ddue:platform">
                <xsl:variable name="platformName">
                  <xsl:value-of select="."/>
                </xsl:variable>
                <include item="{$platformName}"/>
                <xsl:if test="position() != last()">, </xsl:if>
              </xsl:for-each>
            </parameter>
            <parameter>
              <xsl:apply-templates select="ddue:content"/>
            </parameter>

          </include>
        </p>
      </xsl:for-each>
    </xsl:if>
  </xsl:template>
	
	<xsl:template match="ddue:syntaxSection">
		<xsl:call-template name="section">
      <xsl:with-param name="toggleSwitch" select="'syntaxSection'"/>
			<xsl:with-param name="title"><include item="syntaxTitle" /></xsl:with-param>
			<xsl:with-param name="content">
				<xsl:apply-templates />
			</xsl:with-param>
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="ddue:legacySyntax">
		<pre><xsl:copy-of select="."/></pre>
	</xsl:template>

	<xsl:template name="seeAlsoSection">
		
			<xsl:call-template name="section">
        <xsl:with-param name="toggleSwitch" select="'relatedTopics'"/>
				<xsl:with-param name="title"><include item="relatedTitle" /></xsl:with-param>
				<xsl:with-param name="content">
          
          <!-- Tasks sub-section -->
          <xsl:if test='(count(/document/comments/ddue:dduexml/ddue:relatedTopics/tasks/*)) > 0'>
            <xsl:call-template name="subSection">
              <xsl:with-param name="title">
                <include item="SeeAlsoTasks"/>
              </xsl:with-param>
              <xsl:with-param name="content">
                <xsl:for-each select="tasks/*">
                  <xsl:apply-templates select="."/>
                  <br/>
                </xsl:for-each>
              </xsl:with-param>
            </xsl:call-template>
          </xsl:if>

          <!-- Reference sub-section (always one of these in an API topic) -->
          <xsl:call-template name="subSection">
            <xsl:with-param name="title">
              <include item="SeeAlsoReference"/>
            </xsl:with-param>
            <xsl:with-param name="content">
              <xsl:choose>
              <xsl:when test="$group!='root' and $group!='namespace'">
                <xsl:call-template name="autogenSeeAlsoLinks"/>
              </xsl:when>
                <xsl:otherwise>
                  <xsl:for-each select="/document/comments/ddue:dduexml/ddue:relatedTopics/*">
                    <xsl:apply-templates select="."/>
                    <br/>
                  </xsl:for-each>
                </xsl:otherwise>
              </xsl:choose>
            </xsl:with-param>
          </xsl:call-template>

          <!-- Concepts sub-section -->
          <xsl:if test='(count(/document/comments/ddue:dduexml/ddue:relatedTopics/concepts/*)) > 0'>
            <xsl:call-template name="subSection">
              <xsl:with-param name="title">
                <include item="SeeAlsoConcepts"/>
              </xsl:with-param>
              <xsl:with-param name="content">
                <xsl:for-each select="concepts/*">
                  <xsl:apply-templates select="."/>
                  <br/>
                </xsl:for-each>
              </xsl:with-param>
            </xsl:call-template>
          </xsl:if>

          <!-- Other Resources sub-section -->
          <xsl:if test='(count(/document/comments/ddue:dduexml/ddue:relatedTopics/otherResources/*)) > 0'>
            <xsl:call-template name="subSection">
              <xsl:with-param name="title">
                <include item="SeeAlsoOtherResources"/>
              </xsl:with-param>
              <xsl:with-param name="content">
                <xsl:for-each select="otherResources/*">
                  <xsl:apply-templates select="."/>
                  <br/>
                </xsl:for-each>
              </xsl:with-param>
            </xsl:call-template>
          </xsl:if>
                  
				</xsl:with-param>
			</xsl:call-template>
		
	</xsl:template>

	<!-- just skip over these -->
	<xsl:template match="ddue:content | ddue:codeExample | ddue:legacy">
		<xsl:apply-templates />
	</xsl:template>

	<!-- block elements -->

	<xsl:template match="ddue:para">
		<p><xsl:apply-templates /></p>
	</xsl:template>

	<xsl:template match="ddue:list">
		<xsl:choose>
			<xsl:when test="@class='bullet'">
				<ul>
					<xsl:apply-templates select="ddue:listItem" />
				</ul>
			</xsl:when>
			<xsl:when test="@class='ordered'">
				<ol>
					<xsl:apply-templates select="ddue:listItem" />
				</ol>
			</xsl:when>
			<xsl:otherwise>
				<span class="processingError">Unknown List Class</span>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template match="ddue:listItem">
		<li>
			<xsl:apply-templates />
		</li>
	</xsl:template>		

	<xsl:template match="ddue:table">
		<table class="authoredTable">
			<xsl:apply-templates />
		</table>
	</xsl:template>

	<xsl:template match="ddue:tableHeader">
		<xsl:apply-templates />
	</xsl:template>

	<xsl:template match="ddue:row">
		<tr>
			<xsl:apply-templates />
		</tr>
	</xsl:template>

	<xsl:template match="ddue:entry">
		<td>
			<xsl:apply-templates />
		</td>
	</xsl:template>

	<xsl:template match="ddue:tableHeader/ddue:row/ddue:entry">
		<th>
			<xsl:apply-templates />
		</th>
	</xsl:template>

	<xsl:template match="ddue:definitionTable">
		<dl>
			<xsl:apply-templates />
		</dl>
	</xsl:template>

	<xsl:template match="ddue:definedTerm">
		<dt>
			<xsl:apply-templates />
		</dt>
	</xsl:template>

	<xsl:template match="ddue:definition">
		<dd>
			<xsl:apply-templates />
		</dd>
	</xsl:template>

	<xsl:template match="ddue:code">
		<div class="code"><pre><xsl:apply-templates /></pre></div>
	</xsl:template>

	<xsl:template match="ddue:sampleCode">
		<div><b><xsl:value-of select="@language"/></b></div>
		<div class="code"><pre><xsl:apply-templates /></pre></div>
	</xsl:template>

	<xsl:template name="composeCode">
		<xsl:copy-of select="." />
		<xsl:variable name="next" select="following-sibling::*[1]" />
		<xsl:if test="boolean($next/@language) and boolean(local-name($next)=local-name())">
			<xsl:for-each select="$next">
				<xsl:call-template name="composeCode" />
			</xsl:for-each>
		</xsl:if>
	</xsl:template>

	<xsl:template match="ddue:alert">
		<div class="alert">
			<xsl:choose>
				<xsl:when test="@class='caution'">
					<img>
						<includeAttribute item="iconPath" name="src">
							<parameter>alert_caution.gif</parameter>
						</includeAttribute>
					</img>
					<xsl:text> </xsl:text>
					<include item="cautionTitle" />
				</xsl:when>
				<xsl:when test="@class='security'">
					<img>
						<includeAttribute item="iconPath" name="src">
							<parameter>alert_security.gif</parameter>
						</includeAttribute>
					</img>
					<xsl:text> </xsl:text>
					<include item="securityTitle" />
				</xsl:when>
				<xsl:when test="@class='note'">
					<img>
						<includeAttribute item="iconPath" name="src">
							<parameter>alert_note.gif</parameter>
						</includeAttribute>
					</img>
					<xsl:text> </xsl:text>
					<include item="noteTitle" />
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="@class" />
				</xsl:otherwise>
			</xsl:choose>
			<xsl:apply-templates />
		</div>
	</xsl:template>

  <xsl:template match="ddue:sections">
    <xsl:apply-templates select="ddue:section" />
  </xsl:template>

  <xsl:template match="ddue:section">
    <xsl:apply-templates select="@address" />
    <span class="subsectionTitle">
      <xsl:value-of select="ddue:title"/>
    </span>
    <div class="subsection">
      <xsl:apply-templates select="ddue:content"/>
      <xsl:apply-templates select="ddue:sections" />
    </div>
  </xsl:template>

  <xsl:template match="@address">
    <a name="{string(.)}" />
	</xsl:template>

	<xsl:template match="ddue:mediaLink">
		<div class="media">
			<artLink target="{ddue:image/@xlink:href}" />
			<div class="caption">
				<xsl:apply-templates select="ddue:caption" />
			</div>
		</div>
	</xsl:template>

	<xsl:template match="ddue:procedure">
		<xsl:call-template name="section">
      <xsl:with-param name="toggleSwitch" select="'procedure'"/>
			<xsl:with-param name="title"><xsl:value-of select="ddue:title" /></xsl:with-param>
			<xsl:with-param name="content">
				<xsl:apply-templates select="ddue:steps" />
				<xsl:apply-templates select="ddue:conclusion" />
			</xsl:with-param>
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="ddue:steps">
		<xsl:choose>
			<xsl:when test="@class='ordered'">
				<ol>
					<xsl:apply-templates select="ddue:step" />
				</ol>
			</xsl:when>
			<xsl:when test="@class='bullet'">
				<ul>
					<xsl:apply-templates select="ddue:step" />
				</ul>
			</xsl:when>
		</xsl:choose>
	</xsl:template>

	<xsl:template match="ddue:step">
		<li><xsl:apply-templates /></li>
	</xsl:template>


	<xsl:template match="ddue:inThisSection">
		<xsl:call-template name="section">
      <xsl:with-param name="toggleSwitch" select="'inThisSection'"/>
			<xsl:with-param name="title"><include item="inThisSectionTitle" /></xsl:with-param>
			<xsl:with-param name="content">
				<xsl:apply-templates />
			</xsl:with-param>
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="ddue:buildInstructions">
		<xsl:call-template name="section">
      <xsl:with-param name="toggleSwitch" select="'buildInstructions'"/>
			<xsl:with-param name="title"><include item="buildInstructionsTitle" /></xsl:with-param>
			<xsl:with-param name="content">
				<xsl:apply-templates />
			</xsl:with-param>
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="ddue:nextSteps">
		<xsl:call-template name="section">
      <xsl:with-param name="toggleSwitch" select="'nextSteps'"/>
			<xsl:with-param name="title"><include item="nextStepsTitle" /></xsl:with-param>
			<xsl:with-param name="content">
				<xsl:apply-templates />
			</xsl:with-param>
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="ddue:requirements">
		<xsl:call-template name="section">
      <xsl:with-param name="toggleSwitch" select="'requirementsTitle'"/>
			<xsl:with-param name="title"><include item="requirementsTitle" /></xsl:with-param>
			<xsl:with-param name="content">
				<xsl:apply-templates />
			</xsl:with-param>
		</xsl:call-template>
	</xsl:template>
  
	<!-- inline elements -->

	<xsl:template match="ddue:parameterReference">
		<span class="parameter"><xsl:value-of select="." /></span>
	</xsl:template>

	<xsl:template match="ddue:languageKeyword">
		<span class="keyword">
			<xsl:variable name="word" select="." />
			<xsl:choose>
				<xsl:when test="$word='null' or $word='Nothing' or $word='nullptr'">
					<span class="cs">null</span>
					<span class="vb">Nothing</span>
					<span class="cpp">nullptr</span>
				</xsl:when>
				<xsl:when test="$word='static' or $word='Shared'">
					<span class="cs">static</span>
					<span class="vb">Shared</span>
					<span class="cpp">static</span>
				</xsl:when>
				<xsl:when test="$word='virtual' or $word='Overridable'">
					<span class="cs">virtual</span>
					<span class="vb">Overridable</span>
					<span class="cpp">virtual</span>
				</xsl:when>
				<xsl:when test="$word='true' or $word='True'">
					<span class="cs">true</span>
					<span class="vb">True</span>
					<span class="cpp">true</span>
				</xsl:when>
				<xsl:when test="$word='false' or $word='False'">
					<span class="cs">false</span>
					<span class="vb">False</span>
					<span class="cpp">false</span>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="." />
				</xsl:otherwise>
			</xsl:choose>
		</span>
	</xsl:template>

	<xsl:template match="ddue:ui">
		<span class="ui"><xsl:value-of select="." /></span>
	</xsl:template>

	<xsl:template match="ddue:userInput | ddue:userInputLocalizable">
		<span class="input"><xsl:value-of select="." /></span>
	</xsl:template>

	<xsl:template match="ddue:newTerm">
		<span class="term"><xsl:value-of select="." /></span>
	</xsl:template>

	<xsl:template match="ddue:math">
		<span class="math"><xsl:value-of select="." /></span>
	</xsl:template>

	<xsl:template match="ddue:codeInline">
		<span class="code"><xsl:value-of select="." /></span>
	</xsl:template>

	<xsl:template match="ddue:subscript">
		<sub><xsl:value-of select="." /></sub>
	</xsl:template>

	<xsl:template match="ddue:superscript">
		<sup><xsl:value-of select="." /></sup>
	</xsl:template>

	<xsl:template match="ddue:legacyBold">
		<b><xsl:apply-templates /></b>
	</xsl:template>

	<xsl:template match="ddue:legacyItalic">
		<i><xsl:apply-templates /></i>
	</xsl:template>

	<xsl:template match="ddue:legacyUnderline">
		<u><xsl:apply-templates /></u>
	</xsl:template>

	<xsl:template match="ddue:embeddedLabel">
		<span class="label"><xsl:value-of select="." /></span>
	</xsl:template>
	<!-- links -->

	<xsl:template match="ddue:externalLink">
		<a>
			<xsl:attribute name="href"><xsl:value-of select="ddue:linkUri" /></xsl:attribute>
			<xsl:value-of select="ddue:linkText" />
		</a>
	</xsl:template>

	<xsl:template match="ddue:codeEntityReference">
		<referenceLink target="{string(.)}">
			<xsl:if test="@qualifyHint">
				<xsl:attribute name="show-container">
					<xsl:value-of select="@qualifyHint" />
				</xsl:attribute>
				<xsl:attribute name="show-parameters">
					<xsl:value-of select="@qualifyHint" />
				</xsl:attribute>
			</xsl:if>
			<xsl:if test="@autoUpgrade">
				<xsl:attribute name="prefer-overload">
					<xsl:value-of select="@autoUpgrade" />
				</xsl:attribute>
			</xsl:if>
		</referenceLink>
	</xsl:template>

	<xsl:template match="ddue:link">
    <xsl:choose>
      <xsl:when test="starts-with(@xlink:href,'#')">
        <a href="{@xlink:href}">
          <xsl:apply-templates />
        </a>
      </xsl:when>
      <xsl:otherwise>
        <conceptualLink target="{@xlink:href}" />
      </xsl:otherwise>
    </xsl:choose>
	</xsl:template>

	<xsl:template match="ddue:legacyLink">
		<conceptualLink target="{@xlink:href}">
			<xsl:value-of select="." />
		</conceptualLink>
	</xsl:template>

	
<!--
	<xsl:template match="ddue:legacyLink | ddue:link">
		<a>
			<xsl:attribute name="href">
				<xsl:value-of select="@xlink:href" />
				<xsl:text>.htm</xsl:text>
			</xsl:attribute>
			<xsl:apply-templates />
		</a>
	</xsl:template>
-->

	<xsl:template name="createReferenceLink">
		<xsl:param name="id" />
		<xsl:param name="qualified" select="false()" />
		<b><referenceLink target="{$id}" qualified="{$qualified}" /></b>
	</xsl:template>

  
      <xsl:template match="ddue:snippets">
          <div class="code" >
            <xsl:for-each select="ddue:snippet">
              <span codeLanguage="{@language}">
                <table width="100%" cellspacing="0" cellpadding="0">
                  <tr>
                    <th>
                      <include item="{@language}"/>
                      <xsl:text>&#xa0;</xsl:text>
                    </th>
                    <th>
                      <span class="copyCode" onclick="CopyCode(this)" onkeypress="CopyCode_CheckKey(this)" onmouseover="ChangeCopyCodeIcon(this)" onmouseout="ChangeCopyCodeIcon(this)" tabindex="0">
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
            </xsl:for-each>
          </div>
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
            <parameter>exp.gif</parameter>
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

	<!-- fail if any unknown elements are encountered -->
<!--
	<xsl:template match="*">
		<xsl:message terminate="yes">
			<xsl:text>An unknown element was encountered.</xsl:text>
		</xsl:message>
	</xsl:template>
-->


</xsl:stylesheet>
