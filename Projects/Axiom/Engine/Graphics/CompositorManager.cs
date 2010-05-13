#region LGPL License

/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team
 
The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/
/*
 * Many thanks to the folks at Multiverse for providing the initial port for this class
 */

#endregion

#region SVN Version Information

// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text;

using Axiom.Core;
using Axiom.Configuration;
using Axiom.Scripting;
using Axiom.Media;

using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
    /// <summary>
    /// 
    /// </summary>
    public class CompositorManager : ResourceManager, ISingleton<CompositorManager>
    {
        /// <summary>
        /// 
        /// </summary>
        public struct TextureDef
        {
            /// <summary>
            /// 
            /// </summary>
            public int Width;

            /// <summary>
            /// 
            /// </summary>
            public int Height;

            /// <summary>
            /// 
            /// </summary>
            public PixelFormat Format;

            /// <summary>
            /// 
            /// </summary>
            public int FSAA;

            /// <summary>
            /// 
            /// </summary>
            public string FSAAHint;

            /// <summary>
            /// 
            /// </summary>
            public bool SRGBWrite;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="width"></param>
            /// <param name="height"></param>
            /// <param name="format"></param>
            /// <param name="aa"></param>
            /// <param name="aaHint"></param>
            /// <param name="srgb"></param>
            public TextureDef( int width, int height, PixelFormat format, int aa, string aaHint, bool srgb )
            {
                Width = width;
                Height = height;
                Format = format;
                FSAA = aa;
                FSAAHint = aaHint;
                SRGBWrite = srgb;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public struct TextureDefLess : IComparer<TextureDef>
        {
            public int Compare( TextureDef x, TextureDef y )
            {
                if ( x.Format < y.Format )
                {
                    return -1;
                }
                else if ( x.Format == y.Format )
                {
                    if ( x.Width < y.Width )
                    {
                        return -1;
                    }
                    else if ( x.Width == y.Width )
                    {
                        if ( x.FSAA < y.FSAA )
                        {
                            return -1;
                        }
                        else if ( x.FSAA == y.FSAA )
                        {
                            if ( x.FSAAHint != y.FSAAHint )
                            {
                                return -1;
                            }
                            else if ( !x.SRGBWrite && y.SRGBWrite )
                            {
                                return -1;
                            }
                        }
                    }
                }
                return 1;
            }

            //public static bool operator!=(TextureDef x, TextureDef y)
            //{

            //}
        }

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<Viewport, CompositorChain> _chains;

        /// <summary>
        /// 
        /// </summary>
        private Rectangle2D _rectangle;

        /// <summary>
        /// List of instances
        /// </summary>
        private List<CompositorInstance> _instances;

        /// <summary>
        /// Dictionary of registered compositor logics
        /// </summary>
        private Dictionary<string, ICompositorLogic> _compositorLogics;

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<string, ICustomCompositionPass> _customCompositionPasses;

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<TextureDef, List<Texture>> _texturesByDef;

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<Pair<string>, SortedDictionary<TextureDef, Texture>> _chainTexturesByRef = new Dictionary<Pair<string>, SortedDictionary<TextureDef, Texture>>();

        /// <summary>
        /// 
        /// </summary>
        public CompositorManager()
        {
            // Loading order (just after materials)
            base.LoadingOrder = 110.0f;
            // Resource type
            base.ResourceType = "Compositor";
            // Register with resource group manager
            ResourceGroupManager.Instance.RegisterResourceManager( base.ResourceType, this );

#if !AXIOM_USENEWCOMPILERS
            ScriptPatterns.Add( "*.compositor" );
            ResourceGroupManager.Instance.RegisterScriptLoader( this );
#endif // AXIOM_USENEWCOMPILERS
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="handle"></param>
        /// <param name="group"></param>
        /// <param name="isManual"></param>
        /// <param name="loader"></param>
        /// <param name="createParams"></param>
        /// <returns></returns>
        protected override Resource _create( string name, ulong handle, string group, bool isManual, IManualResourceLoader loader, Axiom.Collections.NameValuePairList createParams )
        {
            return new Compositor( this, name, handle, group, isManual, loader );
        }

        /// <summary>
        /// 
        /// </summary>
        public static CompositorManager Instance
        {
            get
            {
                return Singleton<CompositorManager>.Instance;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IRenderable TexturedRectangle2D
        {
            get
            {
                if ( _rectangle == null )
                {
                    _rectangle = new Rectangle2D( true );
                }

                RenderSystem rs = Root.Instance.RenderSystem;
                Viewport vp = rs.ActiveViewport;
                float hOffset = rs.HorizontalTexelOffset / ( 0.5f * vp.ActualWidth );
                float vOffset = rs.VerticalTexelOffset / ( 0.5f * vp.ActualHeight );
                _rectangle.SetCorners( -1 + hOffset, 1 - vOffset, 1 + hOffset, -1 - vOffset );
                return _rectangle;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Initialize( params object[] args )
        {
            _chains = new Dictionary<Viewport, CompositorChain>();
            _chainTexturesByRef = new Dictionary<Pair<string>, SortedDictionary<TextureDef, Texture>>();
            _compositorLogics = new Dictionary<string, ICompositorLogic>();
            _customCompositionPasses = new Dictionary<string, ICustomCompositionPass>();
            _instances = new List<CompositorInstance>();
            _texturesByDef = new Dictionary<TextureDef, List<Texture>>();

            Compositor scene = Create( "Axiom/Scene", ResourceGroupManager.InternalResourceGroupName ) as Compositor;
            if ( scene == null )
            {
                return false;
            }
            CompositionTechnique t = scene.CreateTechnique();
            CompositionTargetPass tp = t.OutputTarget;
            tp.VisibilityMask = 0xFFFFFFFF;
            CompositionPass pass = tp.CreatePass();
            pass.Type = CompositorPassType.Clear;

            pass = tp.CreatePass();
            pass.Type = CompositorPassType.RenderScene;
            /// Render everything, including skies
            pass.FirstRenderQueue = RenderQueueGroupID.Background;
            pass.LastRenderQueue = RenderQueueGroupID.SkiesLate;
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="groupName"></param>
        //public override void ParseScript( Stream data, string groupName, string file )
        //{
        //    Axiom.Scripting.Compiler.ScriptCompilerManager.Instance.ParseScript( data, groupName, file );
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vp"></param>
        /// <returns></returns>
        private CompositorChain GetCompositorChain( Viewport vp )
        {
            CompositorChain chain = null;
            if ( !_chains.TryGetValue( vp, out chain ) )
            {
                chain = new CompositorChain( vp );
                _chains.Add( vp, chain );
            }
            return chain;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vp"></param>
        /// <returns></returns>
        public bool HasCompositorChain( Viewport vp )
        {
            return _chains.ContainsKey( vp );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vp"></param>
        public void RemoveCompositorChain( Viewport vp )
        {
            if ( _chains.ContainsKey( vp ) )
            {
                _chains.Remove( vp );
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vp"></param>
        /// <param name="compositor"></param>
        /// <returns></returns>
        public CompositorInstance AddCompositor( Viewport vp, string compositor )
        {
            return AddCompositor( vp, compositor, -1 );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vp"></param>
        /// <param name="compositor"></param>
        /// <param name="addPosition"></param>
        /// <returns></returns>
        public CompositorInstance AddCompositor( Viewport vp, string compositor, int addPosition )
        {
            Compositor comp = GetByName( compositor ) as Compositor;
            if ( comp == null )
            {
                return null;
            }

            CompositorChain chain = GetCompositorChain( vp );
            return chain.AddCompositor( comp, addPosition == -1 ? CompositorChain.LastCompositor : addPosition );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vp"></param>
        /// <param name="compositor"></param>
        public void RemoveCompositor( Viewport vp, string compositor )
        {
            CompositorChain chain = GetCompositorChain( vp );
            for ( int pos = 0; pos < chain.CompositorCount; pos++ )
            {
                CompositorInstance instance = chain.GetCompositor( pos );
                if ( instance.Compositor.Name == compositor )
                {
                    chain.RemoveCompositor( pos );
                    break;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vp"></param>
        /// <param name="compositor"></param>
        /// <param name="value"></param>
        public void SetCompositorEnabled( Viewport vp, string compositor, bool value )
        {
            CompositorChain chain = GetCompositorChain( vp );
            for ( int pos = 0; pos < chain.CompositorCount; pos++ )
            {
                CompositorInstance instance = chain.GetCompositor( pos );
                if ( instance.Compositor.Name == compositor )
                {
                    chain.SetCompositorEnabled( pos, value );
                    break;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override void RemoveAll()
        {
            FreeChains();
            base.RemoveAll();
        }

        /// <summary>
        /// 
        /// </summary>
        public void ReconstructAllCompositorResources()
        {
            // In order to deal with shared resources, we have to disable *all* compositors
            // first, that way shared resources will get freed
            List<CompositorInstance> instancesToReenable = new List<CompositorInstance>();
            foreach ( CompositorChain chain in _chains.Values )
            {
                foreach ( CompositorInstance instIt in chain.Instances )
                {
                    if ( instIt.Enabled )
                    {
                        instIt.Enabled = false;
                        instancesToReenable.Add( instIt );
                    }
                }
            }
            foreach ( CompositorInstance inst in instancesToReenable )
            {
                inst.Enabled = true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="localName"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="format"></param>
        /// <param name="aa"></param>
        /// <param name="aaHint"></param>
        /// <param name="srgb"></param>
        /// <param name="textureAllreadyAssigned"></param>
        /// <param name="instance"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        public Texture GetPooledTexture( string name, string localName,
                                         int width, int height, PixelFormat format, int aa, string aaHint, bool srgb, ref List<Texture> textureAllreadyAssigned,
                                         CompositorInstance instance, CompositionTechnique.TextureScope scope )
        {
            if ( scope == CompositionTechnique.TextureScope.Global )
            {
                throw new Exception( "Global scope texture can not be pooled. CompositorManager.GetPooledTexture" );
            }

            TextureDef def = new TextureDef( width, height, format, aa, aaHint, srgb );
            if ( scope == CompositionTechnique.TextureScope.Chain )
            {
                Pair<string> pair = new Pair<string>( instance.Compositor.Name, localName );
                SortedDictionary<TextureDef, Texture> defMap = null;
                if ( _chainTexturesByRef.TryGetValue( pair, out defMap ) )
                {
                    Texture tex = null;
                    if ( defMap.TryGetValue( def, out tex ) )
                    {
                        return tex;
                    }
                }
                // ok, we need to create a new one
                if ( defMap == null )
                {
                    defMap = new SortedDictionary<TextureDef, Texture>( new TextureDefLess() );
                }

                Texture newTex = TextureManager.Instance.CreateManual(
                    name, ResourceGroupManager.InternalResourceGroupName, TextureType.TwoD,
                    width, height, 0, format, TextureUsage.RenderTarget, null, srgb, aa, aaHint );

                defMap.Add( def, newTex );

                if ( _chainTexturesByRef.ContainsKey( pair ) )
                {
                    _chainTexturesByRef[ pair ] = defMap;
                }
                else
                {
                    _chainTexturesByRef.Add( pair, defMap );
                }

                return newTex;
            } //end if scope

            List<Texture> i = null;
            if ( !_texturesByDef.TryGetValue( def, out i ) )
            {
                i = new List<Texture>();
                _texturesByDef.Add( def, i );
            }

            CompositorInstance previous = instance.Chain.GetPreviousInstance( instance );
            CompositorInstance next = instance.Chain.GetNextInstance( instance );

            Texture ret = null;
            // iterate over the existing textures and check if we can re-use
            foreach ( Texture tex in i )
            {
                // check not already used
                if ( !textureAllreadyAssigned.Contains( tex ) )
                {
                    bool allowReuse = true;
                    // ok, we didn't use this one already
                    // however, there is an edge case where if we re-use a texture
                    // which has an 'input previous' pass, and it is chained from another
                    // compositor, we can end up trying to use the same texture for both
                    // so, never allow a texture with an input previous pass to be
                    // shared with its immediate predecessor in the chain
                    if ( IsInputPreviousTarget( instance, localName ) )
                    {
                        // Check whether this is also an input to the output target of previous
                        // can't use CompositorInstance._previousInstance, only set up
                        // during compile
                        if ( previous != null && IsInputToOutputTarget( previous, tex ) )
                        {
                            allowReuse = false;
                        }
                    }
                    // now check the other way around since we don't know what order they're bound in
                    if ( IsInputToOutputTarget( instance, localName ) )
                    {
                        if ( next != null && IsInputPreviousTarget( next, tex ) )
                        {
                            allowReuse = false;
                        }
                    }
                    if ( allowReuse )
                    {
                        ret = tex;
                        break;
                    }
                }
            }
            if ( ret == null )
            {
                // ok, we need to create a new one
                ret = TextureManager.Instance.CreateManual(
                    name, ResourceGroupManager.InternalResourceGroupName, TextureType.TwoD,
                    width, height, 0, format, TextureUsage.RenderTarget, null, srgb, aa, aaHint );
                i.Add( ret );
                _texturesByDef[ def ] = i;
            }
            // record that we used this one in the requester's list
            textureAllreadyAssigned.Add( ret );

            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        public void FreePooledTextures()
        {
            FreePooledTextures( true );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="onlyIfUnreferenced"></param>
        public void FreePooledTextures( bool onlyIfUnreferenced )
        {
            if ( onlyIfUnreferenced )
            {
                foreach ( KeyValuePair<TextureDef, List<Texture>> i in _texturesByDef )
                {
                    List<Texture> texList = i.Value;
                    // if the resource system, plus this class, are the only ones to have a reference..
                    // NOTE: any material references will stop this texture getting freed (e.g. compositor demo)
                    // until this routine is called again after the material no longer references the texture
                    for ( int j = 0; j < texList.Count; j++ )
                    {
                        if ( texList[ j ].UseCount == ResourceGroupManager.ResourceSystemNumReferenceCount + 1 )
                        {
                            TextureManager.Instance.Remove( texList[ j ].Handle );
                            texList.Remove( texList[ j ] );
                        }
                    }
                }
                foreach ( KeyValuePair<Pair<string>, SortedDictionary<TextureDef, Texture>> i in _chainTexturesByRef )
                {
                    SortedDictionary<TextureDef, Texture> texMap = i.Value;
                    foreach ( KeyValuePair<TextureDef, Texture> j in texMap )
                    {
                        Texture tex = j.Value;
                        if ( tex.UseCount == ResourceGroupManager.ResourceSystemNumReferenceCount + 1 )
                        {
                            TextureManager.Instance.Remove( tex.Handle );
                            texMap.Remove( j.Key );
                        }
                    }
                }
            }
            else
            {
                // destroy all
                _texturesByDef.Clear();
                _chainTexturesByRef.Clear();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ICompositorLogic GetCompositorLogic( string name )
        {
            ICompositorLogic ret = null;
            if ( !_compositorLogics.TryGetValue( name, out ret ) )
            {
                throw new Exception( "Compositor logic '" + name + "' not registered, CompositorManager.RegisterCompositorLogic" );
            }
            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="logic"></param>
        public void RegisterCompositorLogic( string name, ICompositorLogic logic )
        {
            if ( string.IsNullOrEmpty( name ) )
            {
                throw new Exception( "Compositor logic name must not be empty, CompositorManager.RegisterCompositorLogic" );
            }
            if ( _compositorLogics.ContainsKey( name ) )
            {
                throw new Exception( "Compositor logic '" + name + "' allready exisits, CompositorManager.RegisterCompositorLogic" );
            }

            _compositorLogics.Add( name, logic );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="customPass"></param>
        public void RegisterCustomCompositionPass( string name, ICustomCompositionPass customPass )
        {
            if ( string.IsNullOrEmpty( name ) )
            {
                throw new Exception( "Custom composition pass name must not be empty, CompositorManager.RegisterCustomCompositionPass" );
            }
            if ( _compositorLogics.ContainsKey( name ) )
            {
                throw new Exception( "Custom composition pass '" + name + "' allready exisits, CompositorManager.RegisterCustomCompositionPass" );
            }

            _customCompositionPasses.Add( name, customPass );
        }

        /// <summary>
        /// Get's a custom composition pass by its name
        /// </summary>
        /// <param name="name">name of the custom pass</param>
        /// <returns>custom pass with the given name</returns>
        public ICustomCompositionPass GetCustomCompositionPass( string name )
        {
            ICustomCompositionPass ret = null;
            if ( !_customCompositionPasses.TryGetValue( name, out ret ) )
            {
                throw new Exception( "Custom composition pass '" + name + "' not registered, CompositorManager.GetCustomCompositionPass" );
            }
            return ret;
        }

        /// <summary>
        /// Clear composition chains for all viewports.
        /// </summary>
        private void FreeChains()
        {
            _chains.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="localName"></param>
        /// <returns></returns>
        private bool IsInputPreviousTarget( CompositorInstance instance, string localName )
        {
            foreach ( CompositionTargetPass tp in instance.Technique.TargetPasses )
            {
                if ( tp.InputMode == CompositorInputMode.Previous &&
                     tp.OutputName == localName )
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="texture"></param>
        /// <returns></returns>
        private bool IsInputPreviousTarget( CompositorInstance instance, Texture texture )
        {
            foreach ( CompositionTargetPass tp in instance.Technique.TargetPasses )
            {
                if ( tp.InputMode == CompositorInputMode.Previous )
                {
                    // Don't have to worry about an MRT, because no MRT can be input previous
                    Texture t = instance.GetTextureInstance( tp.OutputName, 0 );
                    if ( t != null && t == texture )
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="localName"></param>
        /// <returns></returns>
        private bool IsInputToOutputTarget( CompositorInstance instance, string localName )
        {
            CompositionTargetPass tp = instance.Technique.OutputTarget;
            foreach ( CompositionPass p in tp.Passes )
            {
                for ( int i = 0; i < p.NumInputs; i++ )
                {
                    if ( p.GetInput( i ).Name == localName )
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="texture"></param>
        /// <returns></returns>
        private bool IsInputToOutputTarget( CompositorInstance instance, Texture texture )
        {
            CompositionTargetPass tp = instance.Technique.OutputTarget;
            foreach ( CompositionPass p in tp.Passes )
            {
                for ( int i = 0; i < p.NumInputs; i++ )
                {
                    Texture t = instance.GetTextureInstance( p.GetInput( i ).Name, 0 );
                    if ( t != null && t == texture )
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        ///		Starts parsing an individual script file.
        /// </summary>
        /// <param name="data">Stream containing the script data.</param>
        public override void ParseScript(Stream data, string groupName, string fileName)
        {
            string file = ((FileStream)data).Name;
            string line = "";
            CompositorScriptContext context = new CompositorScriptContext();
            context.filename = file;
            context.lineNo = 0;

            StreamReader script = new StreamReader(data, System.Text.Encoding.ASCII);

            // parse through the data to the end
            while ((line = ParseHelper.ReadLine(script)) != null)
            {
                context.lineNo++;
                string[] splitCmd;
                string[] args;
                string arg;
                // ignore blank lines and comments
                if (!(line.Length == 0 || line.StartsWith("//")))
                {
                    context.line = line;
                    splitCmd = SplitByWhitespace(line, 2);
                    string token = splitCmd[0];
                    args = SplitArgs(splitCmd.Length == 2 ? splitCmd[1] : "");
                    arg = (args.Length > 0 ? args[0] : "");
                    if (context.section == CompositorScriptSection.None)
                    {
                        if (token != "compositor")
                        {
                            LogError(context, "First token is not 'compositor'!");
                            break; // Give up
                        }
                        string compositorName = RemoveQuotes(splitCmd[1].Trim());
                        context.compositor = (Compositor)this.Create(compositorName, groupName);
                        context.section = CompositorScriptSection.Compositor;
                        context.seenOpen = false;
                        continue; // next line
                    }
                    else
                    {
                        if (!context.seenOpen)
                        {
                            if (token == "{")
                                context.seenOpen = true;
                            else
                                LogError(context, "Expected open brace '{'; instead got {0}", token);
                            continue; // next line
                        }
                        switch (context.section)
                        {
                            case CompositorScriptSection.Compositor:
                                switch (token)
                                {
                                    case "technique":
                                        context.section = CompositorScriptSection.Technique;
                                        context.technique = context.compositor.CreateTechnique();
                                        context.seenOpen = false;
                                        continue; // next line
                                    case "}":
                                        context.section = CompositorScriptSection.None;
                                        context.seenOpen = false;
                                        if (context.technique == null)
                                        {
                                            LogError(context, "No 'technique' section in compositor");
                                            continue;
                                        }
                                        break;
                                    default:
                                        LogError(context,
                                                 "After opening brace '{' of compositor definition, expected 'technique', but got '{0}'",
                                                 token);
                                        continue; // next line
                                }
                                break;
                            case CompositorScriptSection.Technique:
                                switch (token)
                                {
                                    case "texture":
                                        ParseTextureLine(context, args);
                                        break;
                                    case "target":
                                        context.section = CompositorScriptSection.Target;
                                        context.target = context.technique.CreateTargetPass();
                                        context.target.OutputName = arg.Trim();
                                        context.seenOpen = false;
                                        break;
                                    case "target_output":
                                        context.section = CompositorScriptSection.Target;
                                        context.target = context.technique.OutputTarget;
                                        context.seenOpen = false;
                                        break;
                                    case "}":
                                        context.section = CompositorScriptSection.Compositor;
                                        context.seenOpen = true;
                                        break;
                                    default:
                                        LogIllegal(context, "technique", token);
                                        break;
                                }
                                break;
                            case CompositorScriptSection.Target:
                                switch (token)
                                {
                                    case "input":
                                        if (OptionCount(context, token, 1, args.Length))
                                        {
                                            arg = args[0];
                                            if (arg == "previous")
                                                context.target.InputMode = CompositorInputMode.Previous;
                                            else if (arg == "none")
                                                context.target.InputMode = CompositorInputMode.None;
                                            else
                                                LogError(context, "Illegal 'input' arg '{0}'", arg);
                                        }
                                        break;
                                    case "only_initial":
                                        context.target.OnlyInitial = OnOffArg(context, token, args);
                                        break;
                                    case "visibility_mask":
                                        if (!OptionCount(context, token, 1, args.Length))
                                            break;
                                        context.target.VisibilityMask = ParseUint(context, arg);
                                        break;
                                    case "lod_bias":
                                        if (!OptionCount(context, token, 1, args.Length))
                                            break;
                                        context.target.LodBias = ParseInt(context, arg);
                                        break;
                                    case "material_scheme":
                                        if (!OptionCount(context, token, 1, args.Length))
                                            break;
                                        context.target.MaterialScheme = arg.Trim();
                                        break;
                                    case "pass":
                                        context.section = CompositorScriptSection.Pass;
                                        context.pass = context.target.CreatePass();
                                        context.seenOpen = false;
                                        if (!OptionCount(context, token, 1, args.Length))
                                            break;
                                        arg = arg.Trim();
                                        switch (arg)
                                        {
                                            case "render_quad":
                                                context.pass.Type = CompositorPassType.RenderQuad;
                                                break;
                                            case "clear":
                                                context.pass.Type = CompositorPassType.Clear;
                                                break;
                                            case "stencil":
                                                context.pass.Type = CompositorPassType.Stencil;
                                                break;
                                            case "render_scene":
                                                context.pass.Type = CompositorPassType.RenderScene;
                                                break;
                                            default:
                                                LogError(context, "In line '{0}', unrecognized compositor pass type '{1}'", arg);
                                                break;
                                        }
                                        break;
                                    case "}":
                                        context.section = CompositorScriptSection.Technique;
                                        context.seenOpen = true;
                                        break;
                                    default:
                                        LogIllegal(context, "target", token);
                                        break;
                                }
                                break;
                            case CompositorScriptSection.Pass:
                                switch (token)
                                {
                                    case "first_render_queue":
                                        if (!OptionCount(context, token, 1, args.Length))
                                            break;
                                        context.pass.FirstRenderQueue = (RenderQueueGroupID)ParseInt(context, args[0]);
                                        break;
                                    case "last_render_queue":
                                        if (!OptionCount(context, token, 1, args.Length))
                                            break;
                                        context.pass.LastRenderQueue = (RenderQueueGroupID)ParseInt(context, args[0]);
                                        break;
                                    case "identifier":
                                        if (!OptionCount(context, token, 1, args.Length))
                                            break;
                                        context.pass.Identifier = ParseUint(context, args[0]);
                                        break;
                                    case "material":
                                        if (!OptionCount(context, token, 1, args.Length))
                                            break;
                                        context.pass.SetMaterialName(args[0].Trim());
                                        break;
                                    case "input":
                                        if (!OptionCount(context, token, 3, args.Length))
                                            break;
                                        int index = 0;
                                        if (args.Length == 3)
                                            index = ParseInt(context, args[2]);
                                        context.pass.SetInput(ParseInt(context, args[0]), args[1].Trim(), index);
                                        break;
                                    case "clear":
                                        context.section = CompositorScriptSection.Clear;
                                        context.seenOpen = false;
                                        break;
                                    case "stencil":
                                        context.section = CompositorScriptSection.Clear;
                                        context.seenOpen = false;
                                        break;
                                    case "}":
                                        context.section = CompositorScriptSection.Target;
                                        context.seenOpen = true;
                                        break;
                                    default:
                                        LogIllegal(context, "pass", token);
                                        break;
                                }
                                break;
                            case CompositorScriptSection.Clear:
                                switch (token)
                                {
                                    case "buffers":
                                        FrameBufferType fb = (FrameBufferType)0;
                                        foreach (string cb in args)
                                        {
                                            switch (cb)
                                            {
                                                case "colour":
                                                    fb |= FrameBufferType.Color;
                                                    break;
                                                case "color":
                                                    fb |= FrameBufferType.Color;
                                                    break;
                                                case "depth":
                                                    fb |= FrameBufferType.Depth;
                                                    break;
                                                case "stencil":
                                                    fb |= FrameBufferType.Stencil;
                                                    break;
                                                default:
                                                    LogError(context, "When parsing pass clear buffers options, illegal option '{0}'", cb);
                                                    break;
                                            }
                                        }
                                        break;
                                    case "colour":
                                        context.pass.ClearColor = ParseClearColor(context, args);
                                        break;
                                    case "color":
                                        context.pass.ClearColor = ParseClearColor(context, args);
                                        break;
                                    case "depth_value":
                                        if (!OptionCount(context, token, 1, args.Length))
                                            break;
                                        context.pass.ClearDepth = ParseFloat(context, args[0]);
                                        break;
                                    case "stencil_value":
                                        if (!OptionCount(context, token, 1, args.Length))
                                            break;
                                        context.pass.ClearDepth = ParseInt(context, args[0]);
                                        break;
                                    case "}":
                                        context.section = CompositorScriptSection.Pass;
                                        context.seenOpen = true;
                                        break;
                                    default:
                                        LogIllegal(context, "clear", token);
                                        break;
                                }
                                break;
                            case CompositorScriptSection.Stencil:
                                switch (token)
                                {
                                    case "check":
                                        context.pass.StencilCheck = OnOffArg(context, token, args);
                                        break;
                                    case "compare_func":
                                        if (!OptionCount(context, token, 1, args.Length))
                                            break;
                                        context.pass.StencilFunc = ParseCompareFunc(context, arg);
                                        break;
                                    case "ref_value":
                                        if (!OptionCount(context, token, 1, args.Length))
                                            break;
                                        context.pass.StencilRefValue = ParseInt(context, arg);
                                        break;
                                    case "mask":
                                        if (!OptionCount(context, token, 1, args.Length))
                                            break;
                                        context.pass.StencilMask = ParseInt(context, arg);
                                        break;
                                    case "fail_op":
                                        if (!OptionCount(context, token, 1, args.Length))
                                            break;
                                        context.pass.StencilFailOp = ParseStencilOperation(context, arg);
                                        break;
                                    case "depth_fail_op":
                                        if (!OptionCount(context, token, 1, args.Length))
                                            break;
                                        context.pass.StencilDepthFailOp = ParseStencilOperation(context, arg);
                                        break;
                                    case "pass_op":
                                        if (!OptionCount(context, token, 1, args.Length))
                                            break;
                                        context.pass.StencilPassOp = ParseStencilOperation(context, arg);
                                        break;
                                    case "two_sided":
                                        if (!OptionCount(context, token, 1, args.Length))
                                            break;
                                        context.pass.StencilTwoSidedOperation = OnOffArg(context, token, args);
                                        break;
                                    case "}":
                                        context.section = CompositorScriptSection.Pass;
                                        context.seenOpen = true;
                                        break;
                                    default:
                                        LogIllegal(context, "stencil", token);
                                        break;
                                }
                                break;
                            default:
                                LogError(context, "Internal compositor parser error: illegal context");
                                break;
                        }
                    } // if
                } // if
            } // while
            if (context.section != CompositorScriptSection.None)
                LogError(context, "At end of file, unterminated compositor script!");
        }
        protected static void LogError(CompositorScriptContext context, string error,
                                            params object[] substitutions)
        {
            StringBuilder errorBuilder = new StringBuilder();

            // log compositor name only if filename not specified
            if (context.filename == null && context.compositor != null)
            {
                errorBuilder.Append("Error in compositor ");
                errorBuilder.Append(context.compositor.Name);
                errorBuilder.Append(" : ");
                errorBuilder.AppendFormat("At line # {0}: '{1}'", context.lineNo, context.line);
                errorBuilder.AppendFormat(error, substitutions);
            }
            else
            {
                if (context.compositor != null)
                {
                    errorBuilder.Append("Error in compositor ");
                    errorBuilder.Append(context.compositor.Name);
                    errorBuilder.AppendFormat(" at line # {0}: '{1}'", context.lineNo, context.line);
                    errorBuilder.AppendFormat(" of {0}: ", context.filename);
                    errorBuilder.AppendFormat(error, (object[])substitutions);
                }
                else
                {
                    errorBuilder.AppendFormat("Error at line # {0}: '{1}'", context.lineNo, context.line);
                    errorBuilder.AppendFormat(" of {0}: ", context.filename);
                    errorBuilder.AppendFormat(error, substitutions);
                }
            }

            LogManager.Instance.Write(errorBuilder.ToString());
        }

        protected string[] SplitByWhitespace(string line, int count)
        {
            return StringConverter.Split(line, new char[] { ' ', '\t' }, count);
        }

        protected string[] SplitArgs(string args)
        {
            return args.Split(new char[] { ' ', '\t' });
        }

        protected string RemoveQuotes(string token)
        {
            if (token.Length >= 2 && token[0] == '\"')
                token = token.Substring(1);
            if (token[token.Length - 1] == '\"')
                token = token.Substring(0, token.Length - 1);
            return token;
        }

        protected bool OptionCount(CompositorScriptContext context, string introducer,
                                    int expectedCount, int count)
        {
            if (expectedCount < count)
            {
                LogError(context, "The '{0}' phrase requires {1} arguments", introducer, expectedCount);
                return false;
            }
            else
                return true;
        }

        protected bool OnOffArg(CompositorScriptContext context, string introducer, string[] args)
        {
            if (OptionCount(context, introducer, 1, args.Length))
            {
                string arg = args[0];
                if (arg == "on")
                    return true;
                else if (arg == "off")
                    return false;
                else
                {
                    LogError(context, "Illegal '{0}' arg '{1}'; should be 'on' or 'off'", introducer, arg);
                }
            }
            return false;
        }

        protected int ParseInt(CompositorScriptContext context, string s)
        {
            string n = s.Trim();
            try
            {
                return int.Parse(n);
            }
            catch (Exception e)
            {
                LogError(context, "Error converting string '{0}' to integer; error message is '{1}'",
                         n, e.Message);
                return 0;
            }
        }

        protected uint ParseUint(CompositorScriptContext context, string s)
        {
            string n = s.Trim();
            try
            {
                return uint.Parse(n);
            }
            catch (Exception e)
            {
                LogError(context, "Error converting string '{0}' to unsigned integer; error message is '{1}'",
                         n, e.Message);
                return 0;
            }
        }

        protected float ParseFloat(CompositorScriptContext context, string s)
        {
            string n = s.Trim();
            try
            {
                return float.Parse(n);
            }
            catch (Exception e)
            {
                LogError(context, "Error converting string '{0}' to float; error message is '{1}'",
                         n, e.Message);
                return 0.0f;
            }
        }

        protected ColorEx ParseClearColor(CompositorScriptContext context, string[] args)
        {
            if (args.Length != 4)
            {
                LogError(context, "A color value must consist of 4 floating point numbers");
                return ColorEx.Black;
            }
            else
            {
                float r = ParseFloat(context, args[0]);
                float g = ParseFloat(context, args[0]);
                float b = ParseFloat(context, args[0]);
                float a = ParseFloat(context, args[0]);

                return new ColorEx(a, r, g, b);
            }
        }

        protected void LogIllegal(CompositorScriptContext context, string category, string token)
        {
            LogError(context, "Illegal {0} attribute '{1}'", category, token);
        }

        protected void ParseTextureLine(CompositorScriptContext context, string[] args)
        {
            int widthPos = 1, heightPos = 2, formatPos = 3;
            if (args.Length == 4 || args.Length == 6)
            {
                if ( args.Length == 6)
                {
                    heightPos += 1;
                    formatPos += 2;
                }

                CompositionTechnique.TextureDefinition textureDef = context.technique.CreateTextureDefinition(args[0]);
                if (args[widthPos] == "target_width")
                {
                    textureDef.Width = 0;
                    textureDef.WidthFactor = 1.0f;
                }
                else if (args[widthPos] == "target_width_scaled")
                {
                    textureDef.Width = 0;
                    textureDef.WidthFactor = ParseFloat( context, args[ widthPos + 1 ] );
                }
                else
                {
                    textureDef.Width = ParseInt( context, args[ widthPos ] );
                    textureDef.WidthFactor = 1.0f;
                }

                if (args[heightPos] == "target_height")
                {
                    textureDef.Height = 0;
                    textureDef.HeightFactor = 1.0f;
                }
                else if (args[heightPos] == "target_height_scaled")
                {
                    textureDef.Height = 0;
                    textureDef.HeightFactor = ParseFloat( context, args[ heightPos + 1 ] );
                }
                else
                {
                    textureDef.Height = ParseInt( context, args[ heightPos ] );
                    textureDef.HeightFactor = 1.0f;
                }

                switch (args[formatPos])
                {
                    case "PF_A8R8G8B8":
                        textureDef.FormatList.Add( Axiom.Media.PixelFormat.A8R8G8B8 );
                        break;
                    case "PF_R8G8B8A8":
                        textureDef.FormatList.Add(Axiom.Media.PixelFormat.R8G8B8A8);
                        break;
                    case "PF_R8G8B8":
                        textureDef.FormatList.Add( Axiom.Media.PixelFormat.R8G8B8);
                        break;
                    case "PF_FLOAT16_RGBA":
                        textureDef.FormatList.Add( Axiom.Media.PixelFormat.FLOAT16_RGBA);
                        break;
                    case "PF_FLOAT16_RGB":
                        textureDef.FormatList.Add(Axiom.Media.PixelFormat.FLOAT16_RGB);
                        break;
                    case "PF_FLOAT32_RGBA":
                        textureDef.FormatList.Add(Axiom.Media.PixelFormat.FLOAT32_RGBA);
                        break;
                    case "PF_FLOAT16_R":
                        textureDef.FormatList.Add(Axiom.Media.PixelFormat.FLOAT16_R);
                        break;
                    case "PF_FLOAT32_R":
                        textureDef.FormatList.Add( Axiom.Media.PixelFormat.FLOAT32_R);
                        break;
                    default:
                        LogError(context, "Unsupported texture pixel format '{0}'", args[formatPos]);
                        break;
                }
            }
        }

        protected CompareFunction ParseCompareFunc(CompositorScriptContext context, string arg)
        {
            switch (arg.Trim())
            {
                case "always_fail":
                    return CompareFunction.AlwaysFail;
                case "always_pass":
                    return CompareFunction.AlwaysPass;
                case "less_equal":
                    return CompareFunction.LessEqual;
                case "less'":
                    return CompareFunction.Less;
                case "equal":
                    return CompareFunction.Equal;
                case "not_equal":
                    return CompareFunction.NotEqual;
                case "greater_equal":
                    return CompareFunction.GreaterEqual;
                case "greater":
                    return CompareFunction.Greater;
                default:
                    LogError(context, "Illegal stencil compare_func '{0}'", arg);
                    return CompareFunction.AlwaysPass;
            }
        }

        protected StencilOperation ParseStencilOperation(CompositorScriptContext context, string arg)
        {
            switch (arg.Trim())
            {
                case "keep":
                    return StencilOperation.Keep;
                case "zero":
                    return StencilOperation.Zero;
                case "replace":
                    return StencilOperation.Replace;
                case "increment_wrap":
                    return StencilOperation.IncrementWrap;
                case "increment":
                    return StencilOperation.Increment;
                case "decrement_wrap":
                    return StencilOperation.DecrementWrap;
                case "decrement":
                    return StencilOperation.Decrement;
                case "invert":
                    return StencilOperation.Invert;
                default:
                    LogError(context, "Illegal stencil_operation '{0}'", arg);
                    return StencilOperation.Keep;
            }
        }

    }

    /// <summary>
    ///		Enum to identify compositor sections.
    /// </summary>
    public enum CompositorScriptSection
    {
        None,
        Compositor,
        Technique,
        Target,
        Pass,
        Clear,
        Stencil
    }

    /// <summary>
    ///		Struct for holding the script context while parsing.
    /// </summary>
    public class CompositorScriptContext
    {
        public CompositorScriptSection section = CompositorScriptSection.None;
        public Compositor compositor = null;
        public CompositionTechnique technique = null;
        public CompositionPass pass = null;
        public CompositionTargetPass target = null;
        public bool seenOpen = false;
        // Error reporting state
        public int lineNo;
        public string line;
        public string filename;
    }
}