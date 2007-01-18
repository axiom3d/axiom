<?xml version="1.0"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.1" xmlns:msxsl="urn:schemas-microsoft-com:xslt">

	<xsl:output indent="yes" encoding="UTF-8" />

	<xsl:key name="typeIndex" match="/reflection/types/type" use="@id" />
	<xsl:key name="memberIndex" match="/reflection/members/member" use="@id" />

	<xsl:variable name="attached">
		<xsl:call-template name="getAttached" />
	</xsl:variable>

	<xsl:template match="/">
		<reflection>

			<!-- assemblies and namespaces get copied undisturbed -->
			<xsl:copy-of select="/reflection/assemblies" />
			<xsl:copy-of select="/reflection/namespaces" />

			<!-- types, with attached members appended to element lists -->
			<types>
				<xsl:for-each select="/reflection/types/type" >
					<xsl:variable name="typeId" select="@id" />
					<type id="{$typeId}">
						<xsl:copy-of select="*[local-name()!='elements']" />
						<elements>
							<xsl:for-each select="elements/element" >
								<xsl:copy-of select="." />
							</xsl:for-each>
							<xsl:for-each select="msxsl:node-set($attached)/member[memberdata/@type=$typeId]">
								<element member="{@id}" />
							</xsl:for-each>
						</elements>
					</type>
				</xsl:for-each>
			</types>

			<!-- members, with attached members appended -->
			<members>
				<xsl:for-each select="/reflection/members/member">
					<xsl:copy-of select="." />
				</xsl:for-each>
				<xsl:for-each select="msxsl:node-set($attached)/member">
					<xsl:copy-of select="." />
				</xsl:for-each>
			</members>

		</reflection>
	</xsl:template>

	<xsl:template name="getAttached" >

		<xsl:for-each select="/reflection/members/member" >

			<!-- find a static field of type System.Windows.DependencyProperty -->
			<xsl:if test="apidata/@subgroup='field' and memberdata/@static='true' and value/@type='T:System.Windows.DependencyProperty'">
				<xsl:variable name="fieldName" select="apidata/@name" />
				<xsl:variable name="fieldNameLength" select="string-length($fieldName)" />

				<!-- see if the name ends in Property -->
				<xsl:if test="$fieldNameLength>8 and substring($fieldName,number($fieldNameLength)-7)='Property'">
					<xsl:variable name="propertyName" select="substring($fieldName,1,number($fieldNameLength)-8)" />
					<xsl:variable name="typeName" select="substring(memberdata/@type,3)" />

					<!-- make sure the type doesn't already define this property -->
					<xsl:if test="not(boolean(key('memberIndex',concat('P:',$typeName,'.',$propertyName))))" >

						<!-- look for getter and setter -->
						<xsl:variable name="getter" select="/reflection/members/member[apidata/@name=concat('Get',$propertyName) and apidata/@subgroup='method' and memberdata/@type=concat('T:',$typeName) and memberdata/@static='true' and count(parameters/parameter)=1]" />
						<xsl:variable name="setter" select="/reflection/members/member[apidata/@name=concat('Set',$propertyName) and apidata/@subgroup='method' and memberdata/@type=concat('T:',$typeName) and memberdata/@static='true' and count(parameters/parameter)=2]" />

						<xsl:if test="boolean($getter) or boolean($setter)">
							<xsl:variable name="value" select="$getter/value/@type" />
							<member id="{concat('P:',$typeName,'.',$propertyName)}">
								<apidata name="{$propertyName}" group="member" subgroup="property" subsubgroup="attachedProperty" />
								<memberdata type="{concat('T:',$typeName)}" visibility="public" static="false" special="false" abstract="false" virtual="false" final="false" />
								<propertydata>
									<xsl:if test="boolean($getter)">
										<xsl:attribute name="getter"><xsl:value-of select="$getter/@id" /></xsl:attribute>
									</xsl:if>
									<xsl:if test="boolean($setter)">
										<xsl:attribute name="setter"><xsl:value-of select="$setter/@id" /></xsl:attribute>
									</xsl:if>
								</propertydata>
								<value type="{$value}" />
							</member>
						</xsl:if>

					</xsl:if>

				</xsl:if>
			</xsl:if>

			<xsl:if test="apidata/@subgroup='field' and memberdata/@static='true' and value/@type='T:System.Windows.RoutedEvent'">
				<xsl:variable name="fieldName" select="apidata/@name" />
				<xsl:variable name="fieldNameLength" select="string-length($fieldName)" />

				<!-- see if the name ends in event -->
				<xsl:if test="$fieldNameLength>5 and substring($fieldName,number($fieldNameLength)-4)='Event'">
					<xsl:variable name="eventName" select="substring($fieldName,1,number($fieldNameLength)-5)" />
					<xsl:variable name="typeName" select="substring(memberdata/@type,3)" />

					<!-- make sure the type doesn't already define this event -->
					<xsl:if test="not(boolean(key('memberIndex',concat('E:',$typeName,'.',$eventName))))" >

						<!-- look for the adder and remover -->
						<xsl:variable name="adder" select="/reflection/members/member[apidata/@name=concat('Add',$eventName,'Handler') and apidata/@subgroup='method' and memberdata/@type=concat('T:',$typeName) and memberdata/@static='true' and count(parameters/parameter)=2]" />
						<xsl:variable name="remover" select="/reflection/members/member[apidata/@name=concat('Remove',$eventName,'Handler') and apidata/@subgroup='method' and memberdata/@type=concat('T:',$typeName) and memberdata/@static='true' and count(parameters/parameter)=2]" />

						<!-- get event data from the adder and remover -->
						<xsl:variable name="handlerId" select="$adder/parameters/parameter[2]/@type" />
						
						<xsl:if test="boolean($adder) and boolean($remover)">
							<member id="{concat('E:',$typeName,'.',$eventName)}" >
								<apidata name="{$eventName}" group="member" subgroup="event" subsubgroup="attachedEvent" />
								<memberdata type="{concat('T:',$typeName)}" visibility="public" static="false" sepcial="false" abstract="false" virtual="false" final="false" />
								<eventdata handler="{$handlerId}" adder="{$adder/@id}" remover="{$remover/@id}" />
							</member>
						</xsl:if>

					</xsl:if>
				</xsl:if>

			</xsl:if>

		</xsl:for-each>

	</xsl:template>

</xsl:stylesheet>
