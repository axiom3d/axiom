<?xml version="1.0" encoding="utf-8"?>
<configurationSectionModel xmlns:dm0="http://schemas.microsoft.com/VisualStudio/2008/DslTools/Core" dslVersion="1.0.0.0" Id="78d1ad54-68bb-4adf-aa48-dc087c665fd6" namespace="Axiom.Framework.Configuration" xmlSchemaNamespace="axiomConfigurationSection" xmlns="http://schemas.microsoft.com/dsltools/ConfigurationSectionDesigner">
  <typeDefinitions>
    <externalType name="String" namespace="System" />
    <externalType name="Boolean" namespace="System" />
    <externalType name="Int32" namespace="System" />
    <externalType name="Int64" namespace="System" />
    <externalType name="Single" namespace="System" />
    <externalType name="Double" namespace="System" />
    <externalType name="DateTime" namespace="System" />
    <externalType name="TimeSpan" namespace="System" />
  </typeDefinitions>
  <configurationElements>
    <configurationSection name="AxiomConfigurationSection" namespace="Axiom.Framework.Configuration" codeGenOptions="Singleton, XmlnsProperty" xmlSectionName="axiomConfigurationSection">
      <attributeProperties>
        <attributeProperty name="LogFilename" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="logFilename" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/78d1ad54-68bb-4adf-aa48-dc087c665fd6/String" />
          </type>
        </attributeProperty>
      </attributeProperties>
      <elementProperties>
        <elementProperty name="Plugins" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="plugins" isReadOnly="false">
          <type>
            <configurationElementCollectionMoniker name="/78d1ad54-68bb-4adf-aa48-dc087c665fd6/PluginsElementCollection" />
          </type>
        </elementProperty>
        <elementProperty name="ResourceLocations" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="resourceLocations" isReadOnly="false">
          <type>
            <configurationElementCollectionMoniker name="/78d1ad54-68bb-4adf-aa48-dc087c665fd6/ResourceLocationElementCollection" />
          </type>
        </elementProperty>
        <elementProperty name="RenderSystems" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="renderSystems" isReadOnly="false">
          <type>
            <configurationElementCollectionMoniker name="/78d1ad54-68bb-4adf-aa48-dc087c665fd6/RenderSystemElementCollection" />
          </type>
        </elementProperty>
      </elementProperties>
    </configurationSection>
    <configurationElement name="PluginElement" namespace="Axiom.Framework.Configuration">
      <attributeProperties>
        <attributeProperty name="Path" isRequired="true" isKey="true" isDefaultCollection="false" xmlName="path" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/78d1ad54-68bb-4adf-aa48-dc087c665fd6/String" />
          </type>
        </attributeProperty>
        <attributeProperty name="Enabled" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="enabled" isReadOnly="false" defaultValue="true">
          <type>
            <externalTypeMoniker name="/78d1ad54-68bb-4adf-aa48-dc087c665fd6/Boolean" />
          </type>
        </attributeProperty>
      </attributeProperties>
    </configurationElement>
    <configurationElementCollection name="PluginsElementCollection" namespace="Axiom.Framework.Configuration" xmlItemName="pluginElement" codeGenOptions="Indexer, AddMethod, RemoveMethod">
      <attributeProperties>
        <attributeProperty name="AutoScan" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="autoScan" isReadOnly="false" defaultValue="true">
          <type>
            <externalTypeMoniker name="/78d1ad54-68bb-4adf-aa48-dc087c665fd6/Boolean" />
          </type>
        </attributeProperty>
        <attributeProperty name="Path" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="path" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/78d1ad54-68bb-4adf-aa48-dc087c665fd6/String" />
          </type>
        </attributeProperty>
      </attributeProperties>
      <itemType>
        <configurationElementMoniker name="/78d1ad54-68bb-4adf-aa48-dc087c665fd6/PluginElement" />
      </itemType>
    </configurationElementCollection>
    <configurationElementCollection name="ResourceLocationElementCollection" namespace="Axiom.Framework.Configuration" xmlItemName="resourceLocation" codeGenOptions="Indexer, AddMethod, RemoveMethod">
      <itemType>
        <configurationElementMoniker name="/78d1ad54-68bb-4adf-aa48-dc087c665fd6/ResourceLocationElement" />
      </itemType>
    </configurationElementCollection>
    <configurationElementCollection name="RenderSystemElementCollection" namespace="Axiom.Framework.Configuration" xmlItemName="renderSystem" codeGenOptions="Indexer, AddMethod, RemoveMethod">
      <attributeProperties>
        <attributeProperty name="DefaultRenderSystem" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="defaultRenderSystem" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/78d1ad54-68bb-4adf-aa48-dc087c665fd6/String" />
          </type>
        </attributeProperty>
      </attributeProperties>
      <itemType>
        <configurationElementMoniker name="/78d1ad54-68bb-4adf-aa48-dc087c665fd6/RenderSystem" />
      </itemType>
    </configurationElementCollection>
    <configurationElement name="ResourceLocationElement" namespace="Axiom.Framework.Configuration">
      <attributeProperties>
        <attributeProperty name="Path" isRequired="true" isKey="true" isDefaultCollection="false" xmlName="path" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/78d1ad54-68bb-4adf-aa48-dc087c665fd6/String" />
          </type>
        </attributeProperty>
        <attributeProperty name="Type" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="type" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/78d1ad54-68bb-4adf-aa48-dc087c665fd6/String" />
          </type>
        </attributeProperty>
        <attributeProperty name="Recurse" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="recurse" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/78d1ad54-68bb-4adf-aa48-dc087c665fd6/String" />
          </type>
        </attributeProperty>
        <attributeProperty name="Group" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="group" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/78d1ad54-68bb-4adf-aa48-dc087c665fd6/String" />
          </type>
        </attributeProperty>
      </attributeProperties>
    </configurationElement>
    <configurationElement name="RenderSystem" namespace="Axiom.Framework.Configuration">
      <attributeProperties>
        <attributeProperty name="Name" isRequired="true" isKey="true" isDefaultCollection="false" xmlName="name" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/78d1ad54-68bb-4adf-aa48-dc087c665fd6/String" />
          </type>
        </attributeProperty>
      </attributeProperties>
      <elementProperties>
        <elementProperty name="Options" isRequired="false" isKey="false" isDefaultCollection="true" xmlName="options" isReadOnly="false">
          <type>
            <configurationElementCollectionMoniker name="/78d1ad54-68bb-4adf-aa48-dc087c665fd6/RenderSystemOptionElementCollection" />
          </type>
        </elementProperty>
      </elementProperties>
    </configurationElement>
    <configurationElementCollection name="RenderSystemOptionElementCollection" namespace="Axiom.Framework.Configuration" xmlItemName="option" codeGenOptions="Indexer, AddMethod, RemoveMethod">
      <itemType>
        <configurationElementMoniker name="/78d1ad54-68bb-4adf-aa48-dc087c665fd6/RenderSystemOption" />
      </itemType>
    </configurationElementCollection>
    <configurationElement name="RenderSystemOption" namespace="Axiom.Framework.Configuration">
      <attributeProperties>
        <attributeProperty name="Name" isRequired="true" isKey="true" isDefaultCollection="false" xmlName="name" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/78d1ad54-68bb-4adf-aa48-dc087c665fd6/String" />
          </type>
        </attributeProperty>
        <attributeProperty name="Value" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="value" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/78d1ad54-68bb-4adf-aa48-dc087c665fd6/String" />
          </type>
        </attributeProperty>
      </attributeProperties>
    </configurationElement>
  </configurationElements>
  <propertyValidators>
    <validators />
  </propertyValidators>
</configurationSectionModel>