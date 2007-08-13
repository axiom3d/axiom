<?xml version="1.0"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.1">

  <xsl:param name="segregated" select="false()" />

  <xsl:output indent="yes" encoding="UTF-8" />

  <xsl:key name="index" match="/reflection/apis/api" use="@id" />

	<xsl:template match="/">
		<topics>
			<xsl:choose>
				<xsl:when test="count(/reflection/apis/api[apidata/@group='root']) > 0">
					<xsl:apply-templates select="/reflection/apis/api[apidata/@group='root']" />
				</xsl:when>
				<xsl:when test="count(/reflection/apis/api[apidata/@group='namespace']) > 0">
					<xsl:apply-templates select="/reflection/apis/api[apidata/@group='namespace']">
						<xsl:sort select="apidata/@name" />
					</xsl:apply-templates>
				</xsl:when>
				<xsl:otherwise>
					<xsl:apply-templates select="/reflection/apis/api[apidata/@group='type']">
						<xsl:sort select="apidata/@name" />
					</xsl:apply-templates>
				</xsl:otherwise>
			</xsl:choose>
		</topics>
	</xsl:template>

	<!-- create a root entry and namespace sub-entries -->
	<xsl:template match="api[apidata/@group='root']">
		<topic id="{@id}" file="{file/@name}">
			<xsl:apply-templates select="key('index',elements/element/@api)">
				<xsl:sort select="apidata/@name" />
			</xsl:apply-templates>
		</topic>
	</xsl:template>

	<!-- for each namespace, create namespace entry and type sub-entries -->
	<xsl:template match="api[apidata/@group='namespace']">
		<topic id="{@id}" file="{file/@name}">
      <xsl:apply-templates select="key('index',elements/element/@api)">
      </xsl:apply-templates>
    </topic>
  </xsl:template>

  <!-- for each type, create type entry and either overload entries or member entries as sub-entries -->
  <xsl:template match="api[apidata/@group='type']">
    <xsl:choose>
      <xsl:when test="$segregated">
        <stopic id="{@id}" project="{containers/library/@assembly}" file="{file/@name}">
          <xsl:call-template name="AddMemberListTopics"/>
        </stopic>
      </xsl:when>
      <xsl:otherwise>
        <topic id="{@id}" file="{file/@name}">
          <xsl:call-template name="AddMemberListTopics"/>
        </topic>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <!-- For class, struct, and interface, insert nodes for the member list topics,
       and insert nodes for the declared member topics under the appropriate list topic. -->
  <xsl:template name="AddMemberListTopics">
    <xsl:variable name="typeId" select="@id" />
   
       
    <xsl:if test="apidata[@subgroup='class' or @subgroup='structure' or @subgroup='interface']">
      <!-- get the set of declared members on this type -->
      <xsl:variable name="members" select="key('index',elements/element/@api)[containers/type/@api=$typeId]" />
      
      <!-- insert the all members topic, if present -->
      <xsl:choose>
        <xsl:when test="$segregated">
          <xsl:for-each select="key('index', elements/@allMembersTopicId)">
            <stopic id="{@id}" project="{containers/library/@assembly}" file="{file/@name}"/>
          </xsl:for-each>
        </xsl:when>
        <xsl:otherwise>
          <xsl:for-each select="key('index', elements/@allMembersTopicId)">
            <topic id="{@id}" file="{file/@name}"/>
          </xsl:for-each>
        </xsl:otherwise>
      </xsl:choose>

      <xsl:for-each select="$members">
        <xsl:sort select="apidata/@name" />
        <xsl:variable name="name" select="apidata/@name" />
        <xsl:variable name="subgroup" select="apidata/@subgroup" />
        <xsl:variable name="set1" select="$members[apidata/@subgroup=$subgroup]" />
        <xsl:variable name="set2" select="$members[apidata/@name=$name and apidata/@subgroup=$subgroup]" />
        <xsl:choose>
          <xsl:when test="count($set2) &gt; 1 and $subgroup = 'constructor'">
            <xsl:call-template name="AddOverloads">
              <xsl:with-param name="members" select="$members"/>
            </xsl:call-template>
          </xsl:when>
          <xsl:when test="not(count($set1) &gt; 1) and $subgroup = 'constructor'">
            <xsl:apply-templates select="."/>
          </xsl:when>
        </xsl:choose>
        </xsl:for-each>
     
      <!-- insert the Fields topic, if present -->
      <xsl:choose>
        <xsl:when test="$segregated">
          <xsl:for-each select="key('index', concat('Fields.', $typeId))">
            <stopic id="{@id}" project="{containers/library/@assembly}" file="{file/@name}">
              <xsl:for-each select="$members[apidata[@subgroup='field']]">
                <xsl:sort select="apidata/@name" />
                <stopic id="{@id}" project="{containers/library/@assembly}" file="{file/@name}" />
              </xsl:for-each>
            </stopic>
          </xsl:for-each>
        </xsl:when>
        <xsl:otherwise>
          <xsl:for-each select="key('index', concat('Fields.', $typeId))">
            <topic id="{@id}" file="{file/@name}">
              <xsl:for-each select="$members[apidata[@subgroup='field']]">
                <xsl:sort select="apidata/@name" />
                <topic id="{@id}" file="{file/@name}" />
              </xsl:for-each>
            </topic>
          </xsl:for-each>
        </xsl:otherwise>
      </xsl:choose>

      <!-- insert the Methods topic, if present -->
      <xsl:choose>
        <xsl:when test="$segregated">
          <xsl:for-each select="key('index', concat('Methods.', $typeId))">
            <stopic id="{@id}" project="{containers/library/@assembly}" file="{file/@name}">
              <xsl:for-each select="$members[apidata[@subgroup='method']]">
                <xsl:sort select="apidata/@name" />
                <xsl:call-template name="AddOverloads">
                  <xsl:with-param name="members" select="$members"/>
                </xsl:call-template>
              </xsl:for-each>
            </stopic>
          </xsl:for-each>
        </xsl:when>
        <xsl:otherwise>
          <xsl:for-each select="key('index', concat('Methods.', $typeId))">
            <topic id="{@id}" file="{file/@name}">
              <xsl:for-each select="$members[apidata[@subgroup='method']]">
                <xsl:sort select="apidata/@name" />
                <xsl:call-template name="AddOverloads">
                  <xsl:with-param name="members" select="$members"/>
                </xsl:call-template>
              </xsl:for-each>
            </topic>
          </xsl:for-each>
        </xsl:otherwise>
      </xsl:choose>

      <!-- insert the Properties topic, if present -->
      <xsl:choose>
        <xsl:when test="$segregated">
          <xsl:for-each select="key('index', concat('Properties.', $typeId))">
            <stopic id="{@id}" project="{containers/library/@assembly}" file="{file/@name}">
              <xsl:for-each select="$members[apidata[@subgroup='property']  and not(apidata[@subsubgroup])]">
                <xsl:sort select="apidata/@name" />
                <xsl:call-template name="AddOverloads">
                  <xsl:with-param name="members" select="$members"/>
                </xsl:call-template>
              </xsl:for-each>
            </stopic>
          </xsl:for-each>
        </xsl:when>
        <xsl:otherwise>
          <xsl:for-each select="key('index', concat('Properties.', $typeId))">
            <topic id="{@id}" file="{file/@name}">
              <xsl:for-each select="$members[apidata[@subgroup='property'] and not(apidata[@subsubgroup])]">
                <xsl:sort select="apidata/@name" />
                <xsl:call-template name="AddOverloads">
                  <xsl:with-param name="members" select="$members"/>
                </xsl:call-template>
              </xsl:for-each>
            </topic>
          </xsl:for-each>
        </xsl:otherwise>
      </xsl:choose>

      <!-- insert the Events topic, if present -->
      <xsl:choose>
        <xsl:when test="$segregated">
          <xsl:for-each select="key('index', concat('Events.', $typeId))">
            <stopic id="{@id}" project="{containers/library/@assembly}" file="{file/@name}">
              <xsl:for-each select="$members[apidata[@subgroup='event'] and not(apidata[@subsubgroup])]">
                <xsl:sort select="apidata/@name" />
                <stopic id="{@id}" project="{containers/library/@assembly}" file="{file/@name}" />
              </xsl:for-each>
            </stopic>
          </xsl:for-each>
        </xsl:when>
        <xsl:otherwise>
          <xsl:for-each select="key('index', concat('Events.', $typeId))">
            <topic id="{@id}" file="{file/@name}">
              <xsl:for-each select="$members[apidata[@subgroup='event'] and not(apidata[@subsubgroup])]">
                <xsl:sort select="apidata/@name" />
                <topic id="{@id}" file="{file/@name}" />
              </xsl:for-each>
            </topic>
          </xsl:for-each>
        </xsl:otherwise>
      </xsl:choose>

      <!-- insert the AttachedProperties topic, if present -->
      <xsl:choose>
        <xsl:when test="$segregated">
          <xsl:for-each select="key('index', concat('AttachedProperties.', $typeId))">
            <stopic id="{@id}" project="{containers/library/@assembly}" file="{file/@name}">
              <xsl:for-each select="$members[apidata[@subsubgroup='attachedProperty']]">
                <xsl:sort select="apidata/@name" />
                <stopic id="{@id}" project="{containers/library/@assembly}" file="{file/@name}" />
              </xsl:for-each>
            </stopic>
          </xsl:for-each>
        </xsl:when>
        <xsl:otherwise>
          <xsl:for-each select="key('index', concat('AttachedProperties.', $typeId))">
            <topic id="{@id}" file="{file/@name}">
              <xsl:for-each select="$members[apidata[@subsubgroup='attachedProperty']]">
                <xsl:sort select="apidata/@name" />
                <topic id="{@id}" file="{file/@name}" />
              </xsl:for-each>
            </topic>
          </xsl:for-each>
        </xsl:otherwise>
      </xsl:choose>

      <!-- insert the AttachedEvents topic, if present -->
      <xsl:choose>
        <xsl:when test="$segregated">
          <xsl:for-each select="key('index', concat('AttachedEvents.', $typeId))">
            <topic id="{@id}" file="{file/@name}">
              <xsl:for-each select="$members[apidata[@subsubgroup='attachedEvent']]">
                <xsl:sort select="apidata/@name" />
                <topic id="{@id}" file="{file/@name}" />
              </xsl:for-each>
            </topic>
          </xsl:for-each>
        </xsl:when>
        <xsl:otherwise>
          <xsl:for-each select="key('index', concat('AttachedEvents.', $typeId))">
            <stopic id="{@id}" project="{containers/library/@assembly}" file="{file/@name}">
              <xsl:for-each select="$members[apidata[@subsubgroup='attachedEvent']]">
                <xsl:sort select="apidata/@name" />
                <topic id="{@id}" file="{file/@name}" />
              </xsl:for-each>
            </stopic>
          </xsl:for-each>
        </xsl:otherwise>

      </xsl:choose>
           
    </xsl:if>
  </xsl:template>

  <xsl:template name="AddOverloads">
    <xsl:param name="members" />
    <xsl:variable name="name" select="apidata/@name" />
    <xsl:variable name="subgroup" select="apidata/@subgroup" />
    <xsl:variable name="set" select="$members[apidata/@name=$name and apidata/@subgroup=$subgroup]" />
    <xsl:choose>
      <xsl:when test="count($set) &gt; 1">
        <xsl:if test="($set[1]/@id)=@id">
          <xsl:variable name="overloadId">
            <xsl:call-template name="overloadId">
              <xsl:with-param name="memberId" select="@id" />
            </xsl:call-template>
          </xsl:variable>
          <xsl:choose>
            <xsl:when test="$segregated">
              <stopic id="{@id}" project="{containers/library/@assembly}" file="{key('index',$overloadId)/file/@name}">
                <xsl:for-each select="$set">
                  <xsl:apply-templates select="." />
                </xsl:for-each>
              </stopic>
            </xsl:when>
            <xsl:otherwise>
              <topic id="{@id}" file="{key('index',$overloadId)/file/@name}">
                <xsl:for-each select="$set">
                  <xsl:apply-templates select="." />
                </xsl:for-each>
              </topic>
            </xsl:otherwise>
          </xsl:choose>
        </xsl:if>
      </xsl:when>
      <xsl:otherwise>
        <xsl:apply-templates select="." />
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

	<!-- for each member, create a leaf entry -->
	<xsl:template match="api[apidata/@group='member']">
    <xsl:choose>
      <xsl:when test="$segregated">
        <stopic id="{@id}" project="{containers/library/@assembly}" file="{file/@name}" />
      </xsl:when>
      <xsl:otherwise>
        <topic id="{@id}" file="{file/@name}" />
      </xsl:otherwise>
    </xsl:choose>
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
