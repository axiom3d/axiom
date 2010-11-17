﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.1433
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

#pragma warning disable 1591

namespace Axiom.Demos.Configuration {
	
	
	/// <summary>
	///Represents a strongly typed in-memory cache of data.
	///</summary>
	[global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Data.Design.TypedDataSetGenerator", "2.0.0.0")]
	[global::System.Serializable()]
	[global::System.ComponentModel.DesignerCategoryAttribute("code")]
	[global::System.ComponentModel.ToolboxItem(true)]
	[global::System.Xml.Serialization.XmlSchemaProviderAttribute("GetTypedDataSetSchema")]
	[global::System.Xml.Serialization.XmlRootAttribute("EngineConfig")]
#if ( !(ANDROID || IPHONE))
	[global::System.ComponentModel.Design.HelpKeywordAttribute("vs.data.DataSet")]
#endif
	public partial class EngineConfig : global::System.Data.DataSet {
		
		private FilePathDataTable tableFilePath;
		
		private ConfigOptionDataTable tableConfigOption;
		
		private global::System.Data.SchemaSerializationMode _schemaSerializationMode = global::System.Data.SchemaSerializationMode.IncludeSchema;
		
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		public EngineConfig() {
			this.BeginInit();
			this.InitClass();
			global::System.ComponentModel.CollectionChangeEventHandler schemaChangedHandler = new global::System.ComponentModel.CollectionChangeEventHandler(this.SchemaChanged);
			base.Tables.CollectionChanged += schemaChangedHandler;
			base.Relations.CollectionChanged += schemaChangedHandler;
			this.EndInit();
		}
		
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		protected EngineConfig(global::System.Runtime.Serialization.SerializationInfo info, global::System.Runtime.Serialization.StreamingContext context) : 
				base(info, context, false) {
			if ((this.IsBinarySerialized(info, context) == true)) {
				this.InitVars(false);
				global::System.ComponentModel.CollectionChangeEventHandler schemaChangedHandler1 = new global::System.ComponentModel.CollectionChangeEventHandler(this.SchemaChanged);
				this.Tables.CollectionChanged += schemaChangedHandler1;
				this.Relations.CollectionChanged += schemaChangedHandler1;
				return;
			}
			string strSchema = ((string)(info.GetValue("XmlSchema", typeof(string))));
			if ((this.DetermineSchemaSerializationMode(info, context) == global::System.Data.SchemaSerializationMode.IncludeSchema)) {
				global::System.Data.DataSet ds = new global::System.Data.DataSet();
				ds.ReadXmlSchema(new global::System.Xml.XmlTextReader(new global::System.IO.StringReader(strSchema)));
				if ((ds.Tables["FilePath"] != null)) {
					base.Tables.Add(new FilePathDataTable(ds.Tables["FilePath"]));
				}
				if ((ds.Tables["ConfigOption"] != null)) {
					base.Tables.Add(new ConfigOptionDataTable(ds.Tables["ConfigOption"]));
				}
				this.DataSetName = ds.DataSetName;
				this.Prefix = ds.Prefix;
				this.Namespace = ds.Namespace;
				this.Locale = ds.Locale;
				this.CaseSensitive = ds.CaseSensitive;
				this.EnforceConstraints = ds.EnforceConstraints;
				this.Merge(ds, false, global::System.Data.MissingSchemaAction.Add);
				this.InitVars();
			}
			else {
				this.ReadXmlSchema(new global::System.Xml.XmlTextReader(new global::System.IO.StringReader(strSchema)));
			}
			this.GetSerializationData(info, context);
			global::System.ComponentModel.CollectionChangeEventHandler schemaChangedHandler = new global::System.ComponentModel.CollectionChangeEventHandler(this.SchemaChanged);
			base.Tables.CollectionChanged += schemaChangedHandler;
			this.Relations.CollectionChanged += schemaChangedHandler;
		}
		
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.ComponentModel.Browsable(false)]
		[global::System.ComponentModel.DesignerSerializationVisibility(global::System.ComponentModel.DesignerSerializationVisibility.Content)]
		public FilePathDataTable FilePath {
			get {
				return this.tableFilePath;
			}
		}
		
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.ComponentModel.Browsable(false)]
		[global::System.ComponentModel.DesignerSerializationVisibility(global::System.ComponentModel.DesignerSerializationVisibility.Content)]
		public ConfigOptionDataTable ConfigOption {
			get {
				return this.tableConfigOption;
			}
		}
		
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.ComponentModel.BrowsableAttribute(true)]
		[global::System.ComponentModel.DesignerSerializationVisibilityAttribute(global::System.ComponentModel.DesignerSerializationVisibility.Visible)]
		public override global::System.Data.SchemaSerializationMode SchemaSerializationMode {
			get {
				return this._schemaSerializationMode;
			}
			set {
				this._schemaSerializationMode = value;
			}
		}
		
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.ComponentModel.DesignerSerializationVisibilityAttribute(global::System.ComponentModel.DesignerSerializationVisibility.Hidden)]
		public new global::System.Data.DataTableCollection Tables {
			get {
				return base.Tables;
			}
		}
		
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.ComponentModel.DesignerSerializationVisibilityAttribute(global::System.ComponentModel.DesignerSerializationVisibility.Hidden)]
		public new global::System.Data.DataRelationCollection Relations {
			get {
				return base.Relations;
			}
		}
		
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		protected override void InitializeDerivedDataSet() {
			this.BeginInit();
			this.InitClass();
			this.EndInit();
		}
		
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		public override global::System.Data.DataSet Clone() {
			EngineConfig cln = ((EngineConfig)(base.Clone()));
			cln.InitVars();
			cln.SchemaSerializationMode = this.SchemaSerializationMode;
			return cln;
		}
		
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		protected override bool ShouldSerializeTables() {
			return false;
		}
		
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		protected override bool ShouldSerializeRelations() {
			return false;
		}
		
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		protected override void ReadXmlSerializable(global::System.Xml.XmlReader reader) {
			if ((this.DetermineSchemaSerializationMode(reader) == global::System.Data.SchemaSerializationMode.IncludeSchema)) {
				this.Reset();
				global::System.Data.DataSet ds = new global::System.Data.DataSet();
				ds.ReadXml(reader);
				if ((ds.Tables["FilePath"] != null)) {
					base.Tables.Add(new FilePathDataTable(ds.Tables["FilePath"]));
				}
				if ((ds.Tables["ConfigOption"] != null)) {
					base.Tables.Add(new ConfigOptionDataTable(ds.Tables["ConfigOption"]));
				}
				this.DataSetName = ds.DataSetName;
				this.Prefix = ds.Prefix;
				this.Namespace = ds.Namespace;
				this.Locale = ds.Locale;
				this.CaseSensitive = ds.CaseSensitive;
				this.EnforceConstraints = ds.EnforceConstraints;
				this.Merge(ds, false, global::System.Data.MissingSchemaAction.Add);
				this.InitVars();
			}
			else {
				this.ReadXml(reader);
				this.InitVars();
			}
		}
		
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		protected override global::System.Xml.Schema.XmlSchema GetSchemaSerializable() {
			global::System.IO.MemoryStream stream = new global::System.IO.MemoryStream();
			this.WriteXmlSchema(new global::System.Xml.XmlTextWriter(stream, null));
			stream.Position = 0;
			return global::System.Xml.Schema.XmlSchema.Read(new global::System.Xml.XmlTextReader(stream), null);
		}
		
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		internal void InitVars() {
			this.InitVars(true);
		}
		
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		internal void InitVars(bool initTable) {
			this.tableFilePath = ((FilePathDataTable)(base.Tables["FilePath"]));
			if ((initTable == true)) {
				if ((this.tableFilePath != null)) {
					this.tableFilePath.InitVars();
				}
			}
			this.tableConfigOption = ((ConfigOptionDataTable)(base.Tables["ConfigOption"]));
			if ((initTable == true)) {
				if ((this.tableConfigOption != null)) {
					this.tableConfigOption.InitVars();
				}
			}
		}
		
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		private void InitClass() {
			this.DataSetName = "EngineConfig";
			this.Prefix = "";
			this.Namespace = "http://tempuri.org/EngineConfig.xsd";
			this.EnforceConstraints = true;
			this.SchemaSerializationMode = global::System.Data.SchemaSerializationMode.IncludeSchema;
			this.tableFilePath = new FilePathDataTable();
			base.Tables.Add(this.tableFilePath);
			this.tableConfigOption = new ConfigOptionDataTable();
			base.Tables.Add(this.tableConfigOption);
		}
		
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		private bool ShouldSerializeFilePath() {
			return false;
		}
		
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		private bool ShouldSerializeConfigOption() {
			return false;
		}
		
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		private void SchemaChanged(object sender, global::System.ComponentModel.CollectionChangeEventArgs e) {
			if ((e.Action == global::System.ComponentModel.CollectionChangeAction.Remove)) {
				this.InitVars();
			}
		}
		
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		public static global::System.Xml.Schema.XmlSchemaComplexType GetTypedDataSetSchema(global::System.Xml.Schema.XmlSchemaSet xs) {
			EngineConfig ds = new EngineConfig();
			global::System.Xml.Schema.XmlSchemaComplexType type = new global::System.Xml.Schema.XmlSchemaComplexType();
			global::System.Xml.Schema.XmlSchemaSequence sequence = new global::System.Xml.Schema.XmlSchemaSequence();
			global::System.Xml.Schema.XmlSchemaAny any = new global::System.Xml.Schema.XmlSchemaAny();
			any.Namespace = ds.Namespace;
			sequence.Items.Add(any);
			type.Particle = sequence;
			global::System.Xml.Schema.XmlSchema dsSchema = ds.GetSchemaSerializable();
			if (xs.Contains(dsSchema.TargetNamespace)) {
				global::System.IO.MemoryStream s1 = new global::System.IO.MemoryStream();
				global::System.IO.MemoryStream s2 = new global::System.IO.MemoryStream();
				try {
					global::System.Xml.Schema.XmlSchema schema = null;
					dsSchema.Write(s1);
					for (global::System.Collections.IEnumerator schemas = xs.Schemas(dsSchema.TargetNamespace).GetEnumerator(); schemas.MoveNext(); ) {
						schema = ((global::System.Xml.Schema.XmlSchema)(schemas.Current));
						s2.SetLength(0);
						schema.Write(s2);
						if ((s1.Length == s2.Length)) {
							s1.Position = 0;
							s2.Position = 0;
							for (; ((s1.Position != s1.Length) 
										&& (s1.ReadByte() == s2.ReadByte())); ) {
								;
							}
							if ((s1.Position == s1.Length)) {
								return type;
							}
						}
					}
				}
				finally {
					if ((s1 != null)) {
						s1.Close();
					}
					if ((s2 != null)) {
						s2.Close();
					}
				}
			}
			xs.Add(dsSchema);
			return type;
		}
		
		public delegate void FilePathRowChangeEventHandler(object sender, FilePathRowChangeEvent e);
		
		public delegate void ConfigOptionRowChangeEventHandler(object sender, ConfigOptionRowChangeEvent e);
		
		/// <summary>
		///Represents the strongly named DataTable class.
		///</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Data.Design.TypedDataSetGenerator", "2.0.0.0")]
		[global::System.Serializable()]
		[global::System.Xml.Serialization.XmlSchemaProviderAttribute("GetTypedTableSchema")]
		public partial class FilePathDataTable : global::System.Data.DataTable, global::System.Collections.IEnumerable {
			
			private global::System.Data.DataColumn columngroup;
			
			private global::System.Data.DataColumn columnsrc;
			
			private global::System.Data.DataColumn columntype;
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public FilePathDataTable() {
				this.TableName = "FilePath";
				this.BeginInit();
				this.InitClass();
				this.EndInit();
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			internal FilePathDataTable(global::System.Data.DataTable table) {
				this.TableName = table.TableName;
				if ((table.CaseSensitive != table.DataSet.CaseSensitive)) {
					this.CaseSensitive = table.CaseSensitive;
				}
				if ((table.Locale.ToString() != table.DataSet.Locale.ToString())) {
					this.Locale = table.Locale;
				}
				if ((table.Namespace != table.DataSet.Namespace)) {
					this.Namespace = table.Namespace;
				}
				this.Prefix = table.Prefix;
				this.MinimumCapacity = table.MinimumCapacity;
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			protected FilePathDataTable(global::System.Runtime.Serialization.SerializationInfo info, global::System.Runtime.Serialization.StreamingContext context) : 
					base(info, context) {
				this.InitVars();
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public global::System.Data.DataColumn groupColumn {
				get {
					return this.columngroup;
				}
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public global::System.Data.DataColumn srcColumn {
				get {
					return this.columnsrc;
				}
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public global::System.Data.DataColumn typeColumn {
				get {
					return this.columntype;
				}
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			[global::System.ComponentModel.Browsable(false)]
			public int Count {
				get {
					return this.Rows.Count;
				}
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public FilePathRow this[int index] {
				get {
					return ((FilePathRow)(this.Rows[index]));
				}
			}
			
			public event FilePathRowChangeEventHandler FilePathRowChanging;
			
			public event FilePathRowChangeEventHandler FilePathRowChanged;
			
			public event FilePathRowChangeEventHandler FilePathRowDeleting;
			
			public event FilePathRowChangeEventHandler FilePathRowDeleted;
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public void AddFilePathRow(FilePathRow row) {
				this.Rows.Add(row);
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public FilePathRow AddFilePathRow(string group, string src, string type) {
				FilePathRow rowFilePathRow = ((FilePathRow)(this.NewRow()));
				object[] columnValuesArray = new object[] {
						group,
						src,
						type};
				rowFilePathRow.ItemArray = columnValuesArray;
				this.Rows.Add(rowFilePathRow);
				return rowFilePathRow;
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public virtual global::System.Collections.IEnumerator GetEnumerator() {
				return this.Rows.GetEnumerator();
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public override global::System.Data.DataTable Clone() {
				FilePathDataTable cln = ((FilePathDataTable)(base.Clone()));
				cln.InitVars();
				return cln;
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			protected override global::System.Data.DataTable CreateInstance() {
				return new FilePathDataTable();
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			internal void InitVars() {
				this.columngroup = base.Columns["group"];
				this.columnsrc = base.Columns["src"];
				this.columntype = base.Columns["type"];
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			private void InitClass() {
				this.columngroup = new global::System.Data.DataColumn("group", typeof(string), null, global::System.Data.MappingType.Attribute);
				base.Columns.Add(this.columngroup);
				this.columnsrc = new global::System.Data.DataColumn("src", typeof(string), null, global::System.Data.MappingType.Attribute);
				base.Columns.Add(this.columnsrc);
				this.columntype = new global::System.Data.DataColumn("type", typeof(string), null, global::System.Data.MappingType.Attribute);
				base.Columns.Add(this.columntype);
				this.columngroup.AllowDBNull = false;
				this.columngroup.Namespace = "";
				this.columngroup.DefaultValue = ((string)("General"));
				this.columnsrc.AllowDBNull = false;
				this.columnsrc.Namespace = "";
				this.columntype.AllowDBNull = false;
				this.columntype.Namespace = "";
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public FilePathRow NewFilePathRow() {
				return ((FilePathRow)(this.NewRow()));
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			protected override global::System.Data.DataRow NewRowFromBuilder(global::System.Data.DataRowBuilder builder) {
				return new FilePathRow(builder);
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			protected override global::System.Type GetRowType() {
				return typeof(FilePathRow);
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			protected override void OnRowChanged(global::System.Data.DataRowChangeEventArgs e) {
				base.OnRowChanged(e);
				if ((this.FilePathRowChanged != null)) {
					this.FilePathRowChanged(this, new FilePathRowChangeEvent(((FilePathRow)(e.Row)), e.Action));
				}
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			protected override void OnRowChanging(global::System.Data.DataRowChangeEventArgs e) {
				base.OnRowChanging(e);
				if ((this.FilePathRowChanging != null)) {
					this.FilePathRowChanging(this, new FilePathRowChangeEvent(((FilePathRow)(e.Row)), e.Action));
				}
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			protected override void OnRowDeleted(global::System.Data.DataRowChangeEventArgs e) {
				base.OnRowDeleted(e);
				if ((this.FilePathRowDeleted != null)) {
					this.FilePathRowDeleted(this, new FilePathRowChangeEvent(((FilePathRow)(e.Row)), e.Action));
				}
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			protected override void OnRowDeleting(global::System.Data.DataRowChangeEventArgs e) {
				base.OnRowDeleting(e);
				if ((this.FilePathRowDeleting != null)) {
					this.FilePathRowDeleting(this, new FilePathRowChangeEvent(((FilePathRow)(e.Row)), e.Action));
				}
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public void RemoveFilePathRow(FilePathRow row) {
				this.Rows.Remove(row);
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public static global::System.Xml.Schema.XmlSchemaComplexType GetTypedTableSchema(global::System.Xml.Schema.XmlSchemaSet xs) {
				global::System.Xml.Schema.XmlSchemaComplexType type = new global::System.Xml.Schema.XmlSchemaComplexType();
				global::System.Xml.Schema.XmlSchemaSequence sequence = new global::System.Xml.Schema.XmlSchemaSequence();
				EngineConfig ds = new EngineConfig();
				global::System.Xml.Schema.XmlSchemaAny any1 = new global::System.Xml.Schema.XmlSchemaAny();
				any1.Namespace = "http://www.w3.org/2001/XMLSchema";
				any1.MinOccurs = new decimal(0);
				any1.MaxOccurs = decimal.MaxValue;
				any1.ProcessContents = global::System.Xml.Schema.XmlSchemaContentProcessing.Lax;
				sequence.Items.Add(any1);
				global::System.Xml.Schema.XmlSchemaAny any2 = new global::System.Xml.Schema.XmlSchemaAny();
				any2.Namespace = "urn:schemas-microsoft-com:xml-diffgram-v1";
				any2.MinOccurs = new decimal(1);
				any2.ProcessContents = global::System.Xml.Schema.XmlSchemaContentProcessing.Lax;
				sequence.Items.Add(any2);
				global::System.Xml.Schema.XmlSchemaAttribute attribute1 = new global::System.Xml.Schema.XmlSchemaAttribute();
				attribute1.Name = "namespace";
				attribute1.FixedValue = ds.Namespace;
				type.Attributes.Add(attribute1);
				global::System.Xml.Schema.XmlSchemaAttribute attribute2 = new global::System.Xml.Schema.XmlSchemaAttribute();
				attribute2.Name = "tableTypeName";
				attribute2.FixedValue = "FilePathDataTable";
				type.Attributes.Add(attribute2);
				type.Particle = sequence;
				global::System.Xml.Schema.XmlSchema dsSchema = ds.GetSchemaSerializable();
				if (xs.Contains(dsSchema.TargetNamespace)) {
					global::System.IO.MemoryStream s1 = new global::System.IO.MemoryStream();
					global::System.IO.MemoryStream s2 = new global::System.IO.MemoryStream();
					try {
						global::System.Xml.Schema.XmlSchema schema = null;
						dsSchema.Write(s1);
						for (global::System.Collections.IEnumerator schemas = xs.Schemas(dsSchema.TargetNamespace).GetEnumerator(); schemas.MoveNext(); ) {
							schema = ((global::System.Xml.Schema.XmlSchema)(schemas.Current));
							s2.SetLength(0);
							schema.Write(s2);
							if ((s1.Length == s2.Length)) {
								s1.Position = 0;
								s2.Position = 0;
								for (; ((s1.Position != s1.Length) 
											&& (s1.ReadByte() == s2.ReadByte())); ) {
									;
								}
								if ((s1.Position == s1.Length)) {
									return type;
								}
							}
						}
					}
					finally {
						if ((s1 != null)) {
							s1.Close();
						}
						if ((s2 != null)) {
							s2.Close();
						}
					}
				}
				xs.Add(dsSchema);
				return type;
			}
		}
		
		/// <summary>
		///Represents the strongly named DataTable class.
		///</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Data.Design.TypedDataSetGenerator", "2.0.0.0")]
		[global::System.Serializable()]
		[global::System.Xml.Serialization.XmlSchemaProviderAttribute("GetTypedTableSchema")]
		public partial class ConfigOptionDataTable : global::System.Data.DataTable, global::System.Collections.IEnumerable {
			
			private global::System.Data.DataColumn columnRenderSystem;
			
			private global::System.Data.DataColumn columnName;
			
			private global::System.Data.DataColumn columnValue;
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public ConfigOptionDataTable() {
				this.TableName = "ConfigOption";
				this.BeginInit();
				this.InitClass();
				this.EndInit();
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			internal ConfigOptionDataTable(global::System.Data.DataTable table) {
				this.TableName = table.TableName;
				if ((table.CaseSensitive != table.DataSet.CaseSensitive)) {
					this.CaseSensitive = table.CaseSensitive;
				}
				if ((table.Locale.ToString() != table.DataSet.Locale.ToString())) {
					this.Locale = table.Locale;
				}
				if ((table.Namespace != table.DataSet.Namespace)) {
					this.Namespace = table.Namespace;
				}
				this.Prefix = table.Prefix;
				this.MinimumCapacity = table.MinimumCapacity;
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			protected ConfigOptionDataTable(global::System.Runtime.Serialization.SerializationInfo info, global::System.Runtime.Serialization.StreamingContext context) : 
					base(info, context) {
				this.InitVars();
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public global::System.Data.DataColumn RenderSystemColumn {
				get {
					return this.columnRenderSystem;
				}
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public global::System.Data.DataColumn NameColumn {
				get {
					return this.columnName;
				}
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public global::System.Data.DataColumn ValueColumn {
				get {
					return this.columnValue;
				}
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			[global::System.ComponentModel.Browsable(false)]
			public int Count {
				get {
					return this.Rows.Count;
				}
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public ConfigOptionRow this[int index] {
				get {
					return ((ConfigOptionRow)(this.Rows[index]));
				}
			}
			
			public event ConfigOptionRowChangeEventHandler ConfigOptionRowChanging;
			
			public event ConfigOptionRowChangeEventHandler ConfigOptionRowChanged;
			
			public event ConfigOptionRowChangeEventHandler ConfigOptionRowDeleting;
			
			public event ConfigOptionRowChangeEventHandler ConfigOptionRowDeleted;
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public void AddConfigOptionRow(ConfigOptionRow row) {
				this.Rows.Add(row);
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public ConfigOptionRow AddConfigOptionRow(string RenderSystem, string Name, string Value) {
				ConfigOptionRow rowConfigOptionRow = ((ConfigOptionRow)(this.NewRow()));
				object[] columnValuesArray = new object[] {
						RenderSystem,
						Name,
						Value};
				rowConfigOptionRow.ItemArray = columnValuesArray;
				this.Rows.Add(rowConfigOptionRow);
				return rowConfigOptionRow;
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public virtual global::System.Collections.IEnumerator GetEnumerator() {
				return this.Rows.GetEnumerator();
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public override global::System.Data.DataTable Clone() {
				ConfigOptionDataTable cln = ((ConfigOptionDataTable)(base.Clone()));
				cln.InitVars();
				return cln;
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			protected override global::System.Data.DataTable CreateInstance() {
				return new ConfigOptionDataTable();
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			internal void InitVars() {
				this.columnRenderSystem = base.Columns["RenderSystem"];
				this.columnName = base.Columns["Name"];
				this.columnValue = base.Columns["Value"];
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			private void InitClass() {
				this.columnRenderSystem = new global::System.Data.DataColumn("RenderSystem", typeof(string), null, global::System.Data.MappingType.Attribute);
				base.Columns.Add(this.columnRenderSystem);
				this.columnName = new global::System.Data.DataColumn("Name", typeof(string), null, global::System.Data.MappingType.Attribute);
				base.Columns.Add(this.columnName);
				this.columnValue = new global::System.Data.DataColumn("Value", typeof(string), null, global::System.Data.MappingType.Attribute);
				base.Columns.Add(this.columnValue);
				this.columnRenderSystem.AllowDBNull = false;
				this.columnRenderSystem.Namespace = "";
				this.columnName.AllowDBNull = false;
				this.columnName.Namespace = "";
				this.columnValue.Namespace = "";
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public ConfigOptionRow NewConfigOptionRow() {
				return ((ConfigOptionRow)(this.NewRow()));
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			protected override global::System.Data.DataRow NewRowFromBuilder(global::System.Data.DataRowBuilder builder) {
				return new ConfigOptionRow(builder);
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			protected override global::System.Type GetRowType() {
				return typeof(ConfigOptionRow);
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			protected override void OnRowChanged(global::System.Data.DataRowChangeEventArgs e) {
				base.OnRowChanged(e);
				if ((this.ConfigOptionRowChanged != null)) {
					this.ConfigOptionRowChanged(this, new ConfigOptionRowChangeEvent(((ConfigOptionRow)(e.Row)), e.Action));
				}
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			protected override void OnRowChanging(global::System.Data.DataRowChangeEventArgs e) {
				base.OnRowChanging(e);
				if ((this.ConfigOptionRowChanging != null)) {
					this.ConfigOptionRowChanging(this, new ConfigOptionRowChangeEvent(((ConfigOptionRow)(e.Row)), e.Action));
				}
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			protected override void OnRowDeleted(global::System.Data.DataRowChangeEventArgs e) {
				base.OnRowDeleted(e);
				if ((this.ConfigOptionRowDeleted != null)) {
					this.ConfigOptionRowDeleted(this, new ConfigOptionRowChangeEvent(((ConfigOptionRow)(e.Row)), e.Action));
				}
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			protected override void OnRowDeleting(global::System.Data.DataRowChangeEventArgs e) {
				base.OnRowDeleting(e);
				if ((this.ConfigOptionRowDeleting != null)) {
					this.ConfigOptionRowDeleting(this, new ConfigOptionRowChangeEvent(((ConfigOptionRow)(e.Row)), e.Action));
				}
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public void RemoveConfigOptionRow(ConfigOptionRow row) {
				this.Rows.Remove(row);
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public static global::System.Xml.Schema.XmlSchemaComplexType GetTypedTableSchema(global::System.Xml.Schema.XmlSchemaSet xs) {
				global::System.Xml.Schema.XmlSchemaComplexType type = new global::System.Xml.Schema.XmlSchemaComplexType();
				global::System.Xml.Schema.XmlSchemaSequence sequence = new global::System.Xml.Schema.XmlSchemaSequence();
				EngineConfig ds = new EngineConfig();
				global::System.Xml.Schema.XmlSchemaAny any1 = new global::System.Xml.Schema.XmlSchemaAny();
				any1.Namespace = "http://www.w3.org/2001/XMLSchema";
				any1.MinOccurs = new decimal(0);
				any1.MaxOccurs = decimal.MaxValue;
				any1.ProcessContents = global::System.Xml.Schema.XmlSchemaContentProcessing.Lax;
				sequence.Items.Add(any1);
				global::System.Xml.Schema.XmlSchemaAny any2 = new global::System.Xml.Schema.XmlSchemaAny();
				any2.Namespace = "urn:schemas-microsoft-com:xml-diffgram-v1";
				any2.MinOccurs = new decimal(1);
				any2.ProcessContents = global::System.Xml.Schema.XmlSchemaContentProcessing.Lax;
				sequence.Items.Add(any2);
				global::System.Xml.Schema.XmlSchemaAttribute attribute1 = new global::System.Xml.Schema.XmlSchemaAttribute();
				attribute1.Name = "namespace";
				attribute1.FixedValue = ds.Namespace;
				type.Attributes.Add(attribute1);
				global::System.Xml.Schema.XmlSchemaAttribute attribute2 = new global::System.Xml.Schema.XmlSchemaAttribute();
				attribute2.Name = "tableTypeName";
				attribute2.FixedValue = "ConfigOptionDataTable";
				type.Attributes.Add(attribute2);
				type.Particle = sequence;
				global::System.Xml.Schema.XmlSchema dsSchema = ds.GetSchemaSerializable();
				if (xs.Contains(dsSchema.TargetNamespace)) {
					global::System.IO.MemoryStream s1 = new global::System.IO.MemoryStream();
					global::System.IO.MemoryStream s2 = new global::System.IO.MemoryStream();
					try {
						global::System.Xml.Schema.XmlSchema schema = null;
						dsSchema.Write(s1);
						for (global::System.Collections.IEnumerator schemas = xs.Schemas(dsSchema.TargetNamespace).GetEnumerator(); schemas.MoveNext(); ) {
							schema = ((global::System.Xml.Schema.XmlSchema)(schemas.Current));
							s2.SetLength(0);
							schema.Write(s2);
							if ((s1.Length == s2.Length)) {
								s1.Position = 0;
								s2.Position = 0;
								for (; ((s1.Position != s1.Length) 
											&& (s1.ReadByte() == s2.ReadByte())); ) {
									;
								}
								if ((s1.Position == s1.Length)) {
									return type;
								}
							}
						}
					}
					finally {
						if ((s1 != null)) {
							s1.Close();
						}
						if ((s2 != null)) {
							s2.Close();
						}
					}
				}
				xs.Add(dsSchema);
				return type;
			}
		}
		
		/// <summary>
		///Represents strongly named DataRow class.
		///</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Data.Design.TypedDataSetGenerator", "2.0.0.0")]
		public partial class FilePathRow : global::System.Data.DataRow {
			
			private FilePathDataTable tableFilePath;
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			internal FilePathRow(global::System.Data.DataRowBuilder rb) : 
					base(rb) {
				this.tableFilePath = ((FilePathDataTable)(this.Table));
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public string group {
				get {
					return ((string)(this[this.tableFilePath.groupColumn]));
				}
				set {
					this[this.tableFilePath.groupColumn] = value;
				}
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public string src {
				get {
					return ((string)(this[this.tableFilePath.srcColumn]));
				}
				set {
					this[this.tableFilePath.srcColumn] = value;
				}
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public string type {
				get {
					return ((string)(this[this.tableFilePath.typeColumn]));
				}
				set {
					this[this.tableFilePath.typeColumn] = value;
				}
			}
		}
		
		/// <summary>
		///Represents strongly named DataRow class.
		///</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Data.Design.TypedDataSetGenerator", "2.0.0.0")]
		public partial class ConfigOptionRow : global::System.Data.DataRow {
			
			private ConfigOptionDataTable tableConfigOption;
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			internal ConfigOptionRow(global::System.Data.DataRowBuilder rb) : 
					base(rb) {
				this.tableConfigOption = ((ConfigOptionDataTable)(this.Table));
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public string RenderSystem {
				get {
					return ((string)(this[this.tableConfigOption.RenderSystemColumn]));
				}
				set {
					this[this.tableConfigOption.RenderSystemColumn] = value;
				}
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public string Name {
				get {
					return ((string)(this[this.tableConfigOption.NameColumn]));
				}
				set {
					this[this.tableConfigOption.NameColumn] = value;
				}
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public string Value {
				get {
					try {
						return ((string)(this[this.tableConfigOption.ValueColumn]));
					}
					catch (global::System.InvalidCastException e) {
						throw new global::System.Data.StrongTypingException("The value for column \'Value\' in table \'ConfigOption\' is DBNull.", e);
					}
				}
				set {
					this[this.tableConfigOption.ValueColumn] = value;
				}
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public bool IsValueNull() {
				return this.IsNull(this.tableConfigOption.ValueColumn);
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public void SetValueNull() {
				this[this.tableConfigOption.ValueColumn] = global::System.Convert.DBNull;
			}
		}
		
		/// <summary>
		///Row event argument class
		///</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Data.Design.TypedDataSetGenerator", "2.0.0.0")]
		public class FilePathRowChangeEvent : global::System.EventArgs {
			
			private FilePathRow eventRow;
			
			private global::System.Data.DataRowAction eventAction;
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public FilePathRowChangeEvent(FilePathRow row, global::System.Data.DataRowAction action) {
				this.eventRow = row;
				this.eventAction = action;
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public FilePathRow Row {
				get {
					return this.eventRow;
				}
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public global::System.Data.DataRowAction Action {
				get {
					return this.eventAction;
				}
			}
		}
		
		/// <summary>
		///Row event argument class
		///</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Data.Design.TypedDataSetGenerator", "2.0.0.0")]
		public class ConfigOptionRowChangeEvent : global::System.EventArgs {
			
			private ConfigOptionRow eventRow;
			
			private global::System.Data.DataRowAction eventAction;
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public ConfigOptionRowChangeEvent(ConfigOptionRow row, global::System.Data.DataRowAction action) {
				this.eventRow = row;
				this.eventAction = action;
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public ConfigOptionRow Row {
				get {
					return this.eventRow;
				}
			}
			
			[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public global::System.Data.DataRowAction Action {
				get {
					return this.eventAction;
				}
			}
		}
	}
}

#pragma warning restore 1591