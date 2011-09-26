using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Graphics;

#if SILVERLIGHT

namespace Microsoft.Xna.Framework.Graphics
{
}

namespace Microsoft.Xna.Framework.Graphics
{

    #region Interfaces

    public interface IGraphicsDeviceService
    {
        GraphicsDevice GraphicsDevice { get; }
        event EventHandler<EventArgs> DeviceDisposing;
        event EventHandler<EventArgs> DeviceReset;
        event EventHandler<EventArgs> DeviceResetting;
        event EventHandler<EventArgs> DeviceCreated;
    }

//    internal interface IDynamicGraphicsResource
//    {
//        bool IsContentLost { get; }
//        void SetContentLost( [MarshalAs( UnmanagedType.U1 )] bool isContentLost );
//        event EventHandler<EventArgs> ContentLost;
//    }

//    public interface IEffectMatrices
//    {
//        Matrix World { get; set; }
//        Matrix View { get; set; }
//        Matrix Projection { get; set; }
//    }

//    public interface IEffectLights
//    {
//        DirectionalLight DirectionalLight0 { get; }
//        DirectionalLight DirectionalLight1 { get; }
//        DirectionalLight DirectionalLight2 { get; }
//        Vector3 AmbientLightColor { get; set; }
//        bool LightingEnabled { get; set; }
//        void EnableDefaultLighting();
//    }

//    public interface IEffectFog
//    {
//        bool FogEnabled { get; set; }
//        float FogStart { get; set; }
//        float FogEnd { get; set; }
//        Vector3 FogColor { get; set; }
//    }

    public interface IContentProcessor
    {
        Type InputType { get; }
        Type OutputType { get; }
        object Process( object input, ContentProcessorContext context );
    }

    #endregion

    #region DisplayMode

    public class DisplayMode
    {
        public SurfaceFormat Format { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public float AspectRatio { get; set; }
        public Rectangle TitleSafeArea { get; set; }

        internal static readonly DisplayMode DefaultMode = new DisplayMode
                                                           {
                                                               // TODO
                                                               Width = 800,
                                                               Height = 600,
                                                               Format = SurfaceFormat.Color
                                                           };
    }

    #endregion

    #region DisplayModeCollection

    public class DisplayModeCollection : List<DisplayMode>
    {
        public IEnumerable<DisplayMode> this[ SurfaceFormat format ]
        {
            get
            {
                foreach ( var i in this )
                {
                    if ( i.Format == format )
                    {
                        yield return i;
                    }
                }
                ;
                yield break;
            }
        }
    }

    #endregion

    #region OcclusionQuery

    public class OcclusionQuery
    {
        public OcclusionQuery( GraphicsDevice graphicsDevice )
        {
        }

        public int PixelCount { get; set; }
        public bool IsComplete { get; set; }

        public void Begin()
        {
            throw new NotImplementedException();
        }

        public void End()
        {
            throw new NotImplementedException();
        }
    }

    #endregion

    #region Texture3D

    public class Texture3D : Texture2D
    {
        public Texture3D( GraphicsDevice graphicsDevice, int width, int height, int depth,
                          [MarshalAs( UnmanagedType.U1 )] bool mipMap, SurfaceFormat format )
            : base( graphicsDevice, width, height, mipMap, format )
        {
            Depth = depth;
        }

        public int Depth { get; set; }
    }

    #endregion

    #region ContentProcessorContext

    public abstract class ContentProcessorContext
    {
        public abstract ContentBuildLogger Logger { get; }
        public abstract OpaqueDataDictionary Parameters { get; }
        public abstract TargetPlatform TargetPlatform { get; }
        public abstract GraphicsProfile TargetProfile { get; }
        public abstract string BuildConfiguration { get; }
        public abstract string OutputFilename { get; }
        public abstract string OutputDirectory { get; }
        public abstract string IntermediateDirectory { get; }
        public abstract void AddDependency( string filename );
        public abstract void AddOutputFile( string filename );

        public abstract ExternalReference<TOutput> BuildAsset<TInput, TOutput>( ExternalReference<TInput> sourceAsset,
                                                                                string processorName,
                                                                                OpaqueDataDictionary processorParameters,
                                                                                string importerName, string assetName );

        public ExternalReference<TOutput> BuildAsset<TInput, TOutput>( ExternalReference<TInput> sourceAsset,
                                                                       string processorName )
        {
            return BuildAsset<TInput, TOutput>( sourceAsset, processorName, Parameters, "", sourceAsset.Name );
        }

        public abstract TOutput BuildAndLoadAsset<TInput, TOutput>( ExternalReference<TInput> sourceAsset,
                                                                    string processorName,
                                                                    OpaqueDataDictionary processorParameters,
                                                                    string importerName );

        public TOutput BuildAndLoadAsset<TInput, TOutput>( ExternalReference<TInput> sourceAsset, string processorName )
        {
            return BuildAndLoadAsset<TInput, TOutput>( sourceAsset, processorName, Parameters, "" );
        }

        public abstract TOutput Convert<TInput, TOutput>( TInput input, string processorName,
                                                          OpaqueDataDictionary processorParameters );

        public TOutput Convert<TInput, TOutput>( TInput input, string processorName )
        {
            return Convert<TInput, TOutput>( input, processorName, Parameters );
        }
    }

    #endregion

    #region ContentProcessor<TInput, TOutput>

    public abstract class ContentProcessor<TInput, TOutput> : IContentProcessor
    {
        #region IContentProcessor Members

        public object Process( object input, ContentProcessorContext context )
        {
            return Process( (TInput)input, context );
        }

        public Type InputType
        {
            get
            {
                return typeof ( TInput );
            }
        }

        public Type OutputType
        {
            get
            {
                return typeof ( TOutput );
            }
        }

        #endregion

        public abstract TOutput Process( TInput input, ContentProcessorContext context );
    }

    #endregion

    #region Attributes

    [AttributeUsage( AttributeTargets.Class )]
    [Serializable]
    public class ContentProcessorAttribute : Attribute
    {
        public virtual string DisplayName { get; set; }
    }

    public class DisplayNameAttribute : Attribute
    {
        public DisplayNameAttribute( string displayName )
        {
        }
    }

    #endregion
}

namespace Microsoft.Xna.Framework.Content
{

    #region Attributes

    [AttributeUsage( AttributeTargets.Field | AttributeTargets.Property )]
    public sealed class ContentSerializerAttribute : Attribute
    {
        public string ElementName { get; set; }
        public bool FlattenContent { get; set; }
        public bool Optional { get; set; }
        public bool AllowNull { get; set; }
        public bool SharedResource { get; set; }
        public string CollectionItemName { get; set; }
        public bool HasCollectionItemName { get; private set; }

        public ContentSerializerAttribute Clone()
        {
            return new ContentSerializerAttribute
                   {
                       ElementName = ElementName,
                       FlattenContent = FlattenContent,
                       Optional = Optional,
                       AllowNull = AllowNull,
                       SharedResource = SharedResource,
                       CollectionItemName = CollectionItemName,
                       HasCollectionItemName = HasCollectionItemName,
                   };
        }
    }

    #endregion

    #region ContentTypeReaderManager

    public sealed class ContentTypeReaderManager
    {
        internal Dictionary<Type, ContentTypeReader> Readers = new Dictionary<Type, ContentTypeReader>();

        public ContentTypeReader GetTypeReader( Type type )
        {
            return Readers[ type ];
        }
    }

    #endregion

    #region ContentManager

    public class ContentManager : IDisposable
    {
        public ContentManager( IServiceProvider serviceProvider )
        {
            ServiceProvider = serviceProvider;
        }

        public ContentManager( IServiceProvider serviceProvider, string rootDirectory )
        {
            ServiceProvider = serviceProvider;
            RootDirectory = rootDirectory;
        }

        public IServiceProvider ServiceProvider { get; private set; }

        public string RootDirectory { get; set; }

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion

        public virtual void Unload()
        {
            throw new NotImplementedException();
        }

        public virtual T Load<T>( string assetName )
        {
            throw new NotImplementedException();
        }

        protected T ReadAsset<T>( string assetName, Action<IDisposable> recordDisposableObject )
        {
            throw new NotImplementedException();
        }

        protected virtual Stream OpenStream( string assetName )
        {
            throw new NotImplementedException();
        }

        internal virtual Stream _OpenStream( string assetName )
        {
            return OpenStream( assetName );
        }
    }

    #endregion

    #region ContentReader

    public sealed class ContentReader : BinaryReader
    {
        public ContentReader( ContentManager contentManager, string assetName )
            : base( contentManager._OpenStream( assetName ) )
        {
            ContentManager = contentManager;
            AssetName = assetName;
        }

        public ContentManager ContentManager { get; private set; }

        public string AssetName { get; private set; }

        public T ReadObject<T>()
        {
            throw new NotImplementedException();
        }

        public T ReadObject<T>( T existingInstance )
        {
            throw new NotImplementedException();
        }

        public T ReadObject<T>( ContentTypeReader typeReader )
        {
            throw new NotImplementedException();
        }

        public T ReadObject<T>( ContentTypeReader typeReader, T existingInstance )
        {
            throw new NotImplementedException();
        }

        public T ReadRawObject<T>()
        {
            throw new NotImplementedException();
        }

        public T ReadRawObject<T>( T existingInstance )
        {
            throw new NotImplementedException();
        }

        public T ReadRawObject<T>( ContentTypeReader typeReader )
        {
            throw new NotImplementedException();
        }

        public T ReadRawObject<T>( ContentTypeReader typeReader, T existingInstance )
        {
            throw new NotImplementedException();
        }

        public void ReadSharedResource<T>( Action<T> fixup )
        {
            throw new NotImplementedException();
        }

        public T ReadExternalReference<T>()
        {
            throw new NotImplementedException();
        }

        public Vector2 ReadVector2()
        {
            throw new NotImplementedException();
        }

        public Vector3 ReadVector3()
        {
            throw new NotImplementedException();
        }

        public Vector4 ReadVector4()
        {
            throw new NotImplementedException();
        }

        public Matrix ReadMatrix()
        {
            throw new NotImplementedException();
        }

        public Quaternion ReadQuaternion()
        {
            throw new NotImplementedException();
        }

        public Color ReadColor()
        {
            throw new NotImplementedException();
        }

        public override float ReadSingle()
        {
            throw new NotImplementedException();
        }

        public override double ReadDouble()
        {
            throw new NotImplementedException();
        }
    }

    #endregion

    #region ContentTypeReader

    public abstract class ContentTypeReader
    {
        protected ContentTypeReader( Type targetType )
        {
            TargetType = targetType;
        }

        public Type TargetType { get; private set; }
        public virtual int TypeVersion { get; set; }
        public virtual bool CanDeserializeIntoExistingObject { get; set; }

        protected internal virtual void Initialize( ContentTypeReaderManager manager )
        {
            manager.Readers.Add( TargetType, this );
        }

        protected internal abstract object Read( ContentReader input, object existingInstance );
    }

    #endregion

    #region ContentTypeReader<T>

    public abstract class ContentTypeReader<T> : ContentTypeReader
    {
        protected ContentTypeReader()
            : base( typeof ( T ) )
        {
        }

        protected abstract T Read( ContentReader input, T existingInstance );

        protected internal override object Read( ContentReader input, object existingInstance )
        {
            return Read( input, (T)existingInstance );
        }
    }

    #endregion
}

namespace Microsoft.Xna.Framework.Content.Pipeline
{

    #region Enums

    public enum TargetPlatform
    {
        Windows,
        Xbox360,
        WindowsPhone,
    }

    #endregion

    #region Interfaces

    public interface IContentImporter
    {
        object Import( string filename, ContentImporterContext context );
    }

    #endregion

    #region ContentItem

    public class ContentItem
    {
        [ContentSerializer( Optional = true )]
        public string Name { get; set; }

        [ContentSerializer( Optional = true )]
        public ContentIdentity Identity { get; set; }

        [ContentSerializer( Optional = true )]
        public OpaqueDataDictionary OpaqueData { get; set; }
    }

    #endregion

    #region ContentIdentity

    [Serializable]
    public class ContentIdentity
    {
        public ContentIdentity()
        {
        }

        public ContentIdentity( string sourceFilename )
        {
            SourceFilename = sourceFilename;
        }

        public ContentIdentity( string sourceFilename, string sourceTool )
        {
            SourceFilename = sourceFilename;
            SourceTool = sourceTool;
        }

        public ContentIdentity( string sourceFilename, string sourceTool, string fragmentIdentifier )
        {
            SourceFilename = sourceFilename;
            SourceTool = sourceTool;
            FragmentIdentifier = fragmentIdentifier;
        }

        [ContentSerializer( Optional = true )]
        public string SourceFilename { get; set; }

        [ContentSerializer( Optional = true )]
        public string SourceTool { get; set; }

        [ContentSerializer( Optional = true )]
        public string FragmentIdentifier { get; set; }
    }

    #endregion

    #region ExternalReference<T>

    public sealed class ExternalReference<T> : ContentItem
    {
        public ExternalReference()
        {
        }

        public ExternalReference( string filename )
        {
            Filename = filename;
        }

        public ExternalReference( string filename, ContentIdentity relativeToContent )
        {
            Filename = filename;
        }

        public string Filename { get; set; }
    }

    #endregion

    #region ContentImporter<T>

    public abstract class ContentImporter<T> : IContentImporter
    {
        #region IContentImporter Members

        object IContentImporter.Import( string filename, ContentImporterContext context )
        {
            return Import( filename, context );
        }

        #endregion

        public abstract T Import( string filename, ContentImporterContext context );
    }

    #endregion

    #region ContentBuildLogger

    public abstract class ContentBuildLogger
    {
        public string LoggerRootDirectory { get; set; }
        public abstract void LogMessage( string message, params object[] messageArgs );
        public abstract void LogImportantMessage( string message, params object[] messageArgs );

        public abstract void LogWarning( string helpLink, ContentIdentity contentIdentity, string message,
                                         params object[] messageArgs );

        public void PushFile( string filename )
        {
        }

        public void PopFile()
        {
        }

        protected string GetCurrentFilename( ContentIdentity contentIdentity )
        {
            throw new NotImplementedException();
        }
    }

    #endregion

    #region ContentImporterContext

    public abstract class ContentImporterContext
    {
        public abstract ContentBuildLogger Logger { get; }
        public abstract string OutputDirectory { get; }
        public abstract string IntermediateDirectory { get; }
        public abstract void AddDependency( string filename );
    }

    #endregion

    #region OpaqueDataDictionary

    //[ContentSerializerCollectionItemName("Data")]
    public sealed class OpaqueDataDictionary : Dictionary<string, object>
    {
        //protected internal override Type DefaultSerializerType { get; }
        //public T GetValue<T>(string key, T defaultValue);
        //public string GetContentAsXml();
        //protected override void AddItem(string key, object value);
        //protected override void ClearItems();
        //protected override bool RemoveItem(string key);
        //protected override void SetItem(string key, object value);
    }

    #endregion

    #region InvalidContentException

    [Serializable]
    public class InvalidContentException : Exception
    {
        public InvalidContentException()
        {
        }

        public InvalidContentException( string message )
            : base( message )
        {
        }

        public InvalidContentException( string message, Exception innerException )
            : base( message, innerException )
        {
        }

        public InvalidContentException( string message, ContentIdentity contentIdentity )
            : base( message )
        {
            ContentIdentity = contentIdentity;
        }

        public InvalidContentException( string message, ContentIdentity contentIdentity, Exception innerException )
            : base( message, innerException )
        {
            ContentIdentity = contentIdentity;
        }

        public ContentIdentity ContentIdentity { get; set; }
    }

    #endregion

    #region Attributes

    [AttributeUsage( AttributeTargets.Class )]
    [Serializable]
    public class ContentImporterAttribute : Attribute
    {
        public ContentImporterAttribute( string fileExtension )
        {
            FileExtensions = new[]
                             {
                                 fileExtension
                             };
        }

        public ContentImporterAttribute( params string[] fileExtensions )
        {
            FileExtensions = fileExtensions;
        }

        public IEnumerable<string> FileExtensions { get; private set; }
        public bool CacheImportedData { get; set; }
        public virtual string DisplayName { get; set; }
        public string DefaultProcessor { get; set; }
    }

    #endregion
}

namespace Microsoft.Xna.Framework.Content.Pipeline.Graphics
{

    #region EffectContent

    public class EffectContent : ContentItem
    {
        public string EffectCode { get; set; }
    }

    #endregion
}

namespace Microsoft.Xna.Framework.Content.Pipeline.Processors
{

    #region Enums

    public enum EffectProcessorDebugMode
    {
        Auto,
        Debug,
        Optimize,
    }

    #endregion

    #region CompiledEffectContent

    public class CompiledEffectContent : ContentItem
    {
        private readonly byte[] effectCode;

        public CompiledEffectContent( byte[] effectCode )
        {
            this.effectCode = effectCode;
        }

        public byte[] GetEffectCode()
        {
            return effectCode;
        }
    }

    #endregion

    #region ContentProcessor<EffectContent, CompiledEffectContent>

    public class EffectProcessor : ContentProcessor<EffectContent, CompiledEffectContent>
    {
        [DefaultValue( 0 )]
        public virtual EffectProcessorDebugMode DebugMode { get; set; }

        [DefaultValue( null )]
        public virtual string Defines { get; set; }

        public override CompiledEffectContent Process( EffectContent input, ContentProcessorContext context )
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}

namespace Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler
{

    #region ContentCompiler

    public sealed class ContentCompiler
    {
        internal Dictionary<Type, ContentTypeWriter> Writers = new Dictionary<Type, ContentTypeWriter>();

        public ContentTypeWriter GetTypeWriter( Type type )
        {
            return Writers[ type ];
        }
    }

    #endregion

    #region ContentWriter

    public sealed class ContentWriter : BinaryWriter
    {
        public TargetPlatform TargetPlatform { get; set; }
        public GraphicsProfile TargetProfile { get; set; }

        public void WriteObject<T>( T value )
        {
        }

        public void WriteObject<T>( T value, ContentTypeWriter typeWriter )
        {
        }

        public void WriteRawObject<T>( T value )
        {
        }

        public void WriteRawObject<T>( T value, ContentTypeWriter typeWriter )
        {
        }

        public void WriteSharedResource<T>( T value )
        {
        }

        public void WriteExternalReference<T>( ExternalReference<T> reference )
        {
        }

        public void Write( Vector2 value )
        {
        }

        public void Write( Vector3 value )
        {
        }

        public void Write( Vector4 value )
        {
        }

        public void Write( Matrix value )
        {
        }

        public void Write( Quaternion value )
        {
        }

        public void Write( Color value )
        {
        }
    }

    #endregion

    #region ContentTypeWriter

    public abstract class ContentTypeWriter
    {
        protected ContentTypeWriter( Type targetType )
        {
            TargetType = targetType;
        }

        public Type TargetType { get; private set; }
        public virtual int TypeVersion { get; set; }
        public virtual bool CanDeserializeIntoExistingObject { get; set; }

        protected virtual void Initialize( ContentCompiler compiler )
        {
            compiler.Writers.Add( TargetType, this );
        }

        protected internal abstract void Write( ContentWriter output, object value );

        public virtual string GetRuntimeType( TargetPlatform targetPlatform )
        {
            return TargetType.Name;
        }

        public abstract string GetRuntimeReader( TargetPlatform targetPlatform );

        protected internal virtual bool ShouldCompressContent( TargetPlatform targetPlatform, object value )
        {
            return false;
        }
    }

    #endregion

    #region ContentTypeWriter<T>

    public abstract class ContentTypeWriter<T> : ContentTypeWriter
    {
        protected ContentTypeWriter()
            : base( typeof ( T ) )
        {
        }

        protected abstract void Write( ContentWriter output, T value );

        protected internal override void Write( ContentWriter output, object value )
        {
            Write( output, (T)value );
        }
    }

    #endregion

    #region Attributes

    [AttributeUsage( AttributeTargets.Class )]
    public sealed class ContentTypeWriterAttribute : Attribute
    {
    }

    #endregion
}

namespace Microsoft.Xna.Framework.GamerServices
{

    #region GamerServicesDispatcher

    public static class GamerServicesDispatcher
    {
        public static bool IsInitialized { get; private set; }

        public static IntPtr WindowHandle { get; set; }

        public static void Initialize( IServiceProvider serviceProvider )
        {
            IsInitialized = true;
        }

        public static void Update()
        {
        }

        public static event EventHandler<EventArgs> InstallingTitleUpdate;
    }

    #endregion
}

#endif

namespace Axiom.Core
{

    #region XnaLogger

    public class XnaLogger : ContentBuildLogger
    {
        public override void LogMessage( string message, params object[] messageArgs )
        {
            LogManager.Instance.Write( LogMessageLevel.Normal, false, message + "\n" + messageArgs );
        }

        public override void LogImportantMessage( string message, params object[] messageArgs )
        {
            LogManager.Instance.Write( LogMessageLevel.Critical, false, message + "\n" + messageArgs );
        }

        public override void LogWarning( string helpLink, ContentIdentity contentIdentity, string message,
                                         params object[] messageArgs )
        {
            LogManager.Instance.Write( LogMessageLevel.Trivial, false,
                                       helpLink + "\n" + contentIdentity + "\n" + message + "\n" + messageArgs );
        }
    }

    #endregion

    #region XnaImporterContext

    public class XnaImporterContext : ContentImporterContext
    {
        public override string IntermediateDirectory
        {
            get
            {
                return string.Empty;
            }
        }

        public override string OutputDirectory
        {
            get
            {
                return string.Empty;
            }
        }

        public override ContentBuildLogger Logger
        {
            get
            {
                return logger;
            }
        }

        private readonly ContentBuildLogger logger = new XnaLogger();

        public override void AddDependency( string filename )
        {
        }
    }

    #endregion

    #region XnaProcessorContext

    public class XnaProcessorContext : ContentProcessorContext
    {
        public override TargetPlatform TargetPlatform
        {
            get
            {
                return TargetPlatform.Windows;
            }
        }

        public override GraphicsProfile TargetProfile
        {
            get
            {
                return GraphicsProfile.Reach;
            }
        }

        public override string BuildConfiguration
        {
            get
            {
                return string.Empty;
            }
        }

        public override string IntermediateDirectory
        {
            get
            {
                return string.Empty;
            }
        }

        public override string OutputDirectory
        {
            get
            {
                return string.Empty;
            }
        }

        public override string OutputFilename
        {
            get
            {
                return string.Empty;
            }
        }

        public override OpaqueDataDictionary Parameters
        {
            get
            {
                return parameters;
            }
        }

        private readonly OpaqueDataDictionary parameters = new OpaqueDataDictionary();

        public override ContentBuildLogger Logger
        {
            get
            {
                return logger;
            }
        }

        private readonly ContentBuildLogger logger = new XnaLogger();

        public override void AddDependency( string filename )
        {
        }

        public override void AddOutputFile( string filename )
        {
        }

        public override TOutput Convert<TInput, TOutput>( TInput input, string processorName,
                                                          OpaqueDataDictionary processorParameters )
        {
            throw new NotImplementedException();
        }

        public override TOutput BuildAndLoadAsset<TInput, TOutput>( ExternalReference<TInput> sourceAsset,
                                                                    string processorName,
                                                                    OpaqueDataDictionary processorParameters,
                                                                    string importerName )
        {
            throw new NotImplementedException();
        }

        public override ExternalReference<TOutput> BuildAsset<TInput, TOutput>( ExternalReference<TInput> sourceAsset,
                                                                                string processorName,
                                                                                OpaqueDataDictionary processorParameters,
                                                                                string importerName, string assetName )
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}