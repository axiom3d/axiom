#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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
#endregion

using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;

using Axiom.MathLib;
// This is coming from RealmForge.Utility
using Axiom.Core;

namespace Axiom
{
    /// <summary>
    ///    Class encapsulating the rendering properties of an object.
    /// </summary>
    /// <remarks>
    ///    The Material class encapsulates ALL aspects of the visual appearance,
    ///    of an object. It also includes other flags which 
    ///    might not be traditionally thought of as material properties such as 
    ///    culling modes and depth buffer settings, but these affect the 
    ///    appearance of the rendered object and are convenient to attach to the 
    ///    material since it keeps all the settings in one place. This is 
    ///    different to Direct3D which treats a material as just the color 
    ///    components (diffuse, specular) and not texture maps etc. This 
    ///    Material can be thought of as equivalent to a 'Shader'.
    ///    <p/>
    ///    A Material can be rendered in multiple different ways depending on the
    ///    hardware available. You may configure a Material to use high-complexity
    ///    fragment shaders, but these won't work on every card; therefore a Technique
    ///    is an approach to creating the visual effect you are looking for. You are advised
    ///    to create fallback techniques with lower hardware requirements if you decide to
    ///    use advanced features. In addition, you also might want lower-detail techniques
    ///    for distant geometry.
    ///    <p/>
    ///    Each technique can be made up of multiple passes. A fixed-function pass
    ///    may combine multiple texture layers using multi-texturing, but they can 
    ///    break that into multiple passes automatically if the active card cannot
    ///    handle that many simultaneous textures. Programmable passes, however, cannot
    ///    be split down automatically, so if the active graphics card cannot handle the
    ///    technique which contains these passes, the engine will try to find another technique
    ///    which the card can do. If, at the end of the day, the card cannot handle any of the
    ///    techniques which are listed for the material, the engine will render the 
    ///    geometry plain white, which should alert you to the problem.
    ///    <p/>
    ///    The engine comes configured with a number of default settings for a newly 
    ///    created material. These can be changed if you wish by retrieving the 
    ///    default material settings through 
    ///    SceneManager.DefaultMaterialSettings. Any changes you make to the 
    ///    Material returned from this method will apply to any materials created 
    ///    from this point onward.
    /// </summary>
    public class Material : Resource, IComparable
    {
        #region Member variables

        /// <summary>
        ///    A list of techniques that exist within this Material.
        /// </summary>
        protected TechniqueList techniques = new TechniqueList();
        /// <summary>
        ///    A list of the techniques of this material that are supported by the current hardware.
        /// </summary>
        protected TechniqueList supportedTechniques = new TechniqueList();
        /// <summary>
        ///    Flag noting whether or not this Material needs to be re-compiled.
        /// </summary>
        protected bool compilationRequired;
        /// <summary>
        ///		Should objects using this material receive shadows?
        /// </summary>
        protected bool receiveShadows;
        /// <summary>
        ///		Do transparent objects casts shadows?
        /// </summary>
        protected bool transparencyCastsShadows;
        /// <summary>
        ///		List of LOD distances specified for this material.
        /// </summary>
        protected FloatList lodDistances = new FloatList();
        /// <summary>
        ///		
        /// </summary>
        protected Hashtable bestTechniqueList = new Hashtable();

        /// <summary>
        ///    A reference to a precreated Material that contains all the default settings.
        /// </summary>
        static internal protected Material defaultSettings;
        /// <summary>
        ///    Auto incrementing number for creating unique names.
        /// </summary>
        static protected int autoNumber;

        #endregion

        #region Constructors

        /// <summary>
        ///    Constructor.  Creates an auto generated name for the material.
        /// </summary>
        /// <remarks>
        ///    Normally you create materials by calling the relevant SceneManager since that is responsible for
        ///    managing all scene state including materials.
        /// </remarks>
        public Material()
        {
            this.name = String.Format( "_Material{0}", autoNumber++ );
            compilationRequired = true;
            lodDistances.Add( 0.0f );
            receiveShadows = true;
        }

        /// <summary>
        ///    Contructor, taking the name of the material.
        /// </summary>
        /// <remarks>
        ///    Normally you create materials by calling the relevant SceneManager since that is responsible for
        ///    managing all scene state including materials.
        /// </remarks>
        /// <param name="name">Unique name of this material.</param>
        public Material( string name )
        {
            // apply default material settings to this new material
            ApplyDefaults();

            this.name = name;
            compilationRequired = true;
            isLoaded = false;
            receiveShadows = true;
        }

        #endregion

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public ColorEx Ambient
        {
            set
            {
                for ( int i = 0; i < techniques.Count; i++ )
                {
                    ( (Technique)techniques[i] ).Ambient = value;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public CullingMode CullingMode
        {
            set
            {
                for ( int i = 0; i < techniques.Count; i++ )
                {
                    ( (Technique)techniques[i] ).CullingMode = value;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool DepthCheck
        {
            set
            {
                for ( int i = 0; i < techniques.Count; i++ )
                {
                    ( (Technique)techniques[i] ).DepthCheck = value;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool DepthWrite
        {
            set
            {
                for ( int i = 0; i < techniques.Count; i++ )
                {
                    ( (Technique)techniques[i] ).DepthWrite = value;
                }
            }
        }

        public ColorEx Diffuse
        {
            set
            {
                for ( int i = 0; i < techniques.Count; i++ )
                {
                    ( (Technique)techniques[i] ).Diffuse = value;
                }
            }
        }

        /// <summary>
        ///		Determines if the material has any transparency with the rest of the scene (derived from 
        ///    whether any Techniques say they involve transparency).
        /// </summary>
        public bool IsTransparent
        {
            get
            {
                // check each technique to see if it is transparent
                for ( int i = 0; i < techniques.Count; i++ )
                {
                    if ( ( (Technique)techniques[i] ).IsTransparent )
                    {
                        return true;
                    }
                }

                // if we got this far, there are no transparent techniques
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Lighting
        {
            set
            {
                for ( int i = 0; i < techniques.Count; i++ )
                {
                    ( (Technique)techniques[i] ).Lighting = value;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ManualCullingMode ManualCullMode
        {
            set
            {
                for ( int i = 0; i < techniques.Count; i++ )
                {
                    ( (Technique)techniques[i] ).ManualCullMode = value;
                }
            }
        }

        /// <summary>
        ///    Gets the number of techniques within this Material.
        /// </summary>
        public int NumTechniques
        {
            get
            {
                return techniques.Count;
            }
        }

        /// <summary>
        ///		Gets the number of levels-of-detail this material has.
        /// </summary>
        public int NumLodLevels
        {
            get
            {
                return bestTechniqueList.Count;
            }
        }

        public TextureFiltering TextureFiltering
        {
            set
            {
                for ( int i = 0; i < techniques.Count; i++ )
                {
                    ( (Technique)techniques[i] ).TextureFiltering = value;
                }
            }
        }

        /// <summary>
        ///		Sets whether objects using this material will receive shadows.
        /// </summary>
        /// <remarks>
        ///		This method allows a material to opt out of receiving shadows, if
        ///		it would otherwise do so. Shadows will not be cast on any objects
        ///		unless the scene is set up to support shadows and not all techniques 
        ///		cast shadows on all objects. In any case, if you have a need to prevent
        ///		shadows being received by material, this is the method you call to do it.
        ///		Note: Transparent materials never receive shadows despite this setting. 
        ///		The default is to receive shadows.
        ///		<seealso cref="SceneManager.ShadowTechnique"/>
        /// </remarks>
        public bool ReceiveShadows
        {
            get
            {
                return receiveShadows;
            }
            set
            {
                receiveShadows = value;
            }
        }

        /// <summary>
        ///		Gets/Sets whether objects using this material be classified as opaque to the shadow caster system.
        /// </summary>
        /// <remarks>
        ///		This method allows a material to cast a shadow, even if it is transparent.
        ///		By default, transparent materials neither cast nor receive shadows. Shadows
        ///		will not be cast on any objects unless the scene is set up to support shadows 
        ///		<seealso cref="SceneManager.ShadowTechnique"/>, and not all techniques cast
        ///		shadows on all objects.
        /// </remarks>
        public bool TransparencyCastsShadows
        {
            get
            {
                return transparencyCastsShadows;
            }
            set
            {
                transparencyCastsShadows = value;
            }
        }

        #endregion

        #region Implementation of IComparable

        /// <summary>
        ///		Used for comparing 2 Material objects.
        /// </summary>
        /// <remarks>
        ///		This comparison will be used in RenderQueue group sorting of Materials materials.
        ///		If this object is transparent and the object being compared is not, this is greater that obj.
        ///		If this object is not transparent and the object being compared is, obj is greater than this.
        /// </remarks>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo( object obj )
        {
            Debug.Assert( obj is Material, "Materials cannot be compared to objects of type '" + obj.GetType().Name );

            Material material = obj as Material;

            // compare this Material with the incoming object to compare to.
            if ( this.IsTransparent && !material.IsTransparent )
                return -1;
            else if ( !this.IsTransparent && material.IsTransparent )
                return 1;
            else
                return 0;
        }

        #endregion

        #region Implementation of Resource

        /// <summary>
        ///		Overridden from Resource.
        /// </summary>
        /// <remarks>
        ///		By default, Materials are not loaded, and adding additional textures etc do not cause those
        ///		textures to be loaded. When the <code>Load</code> method is called, all textures are loaded (if they
        ///		are not already), GPU programs are created if applicable, and Controllers are instantiated.
        ///		Once a material has been loaded, all changes made to it are immediately loaded too
        /// </remarks>
        public override void Load()
        {
            if ( !isLoaded )
            {
                // compile if needed
                if ( compilationRequired )
                {
                    Compile();
                }

                // load all the supported techniques
                for ( int i = 0; i < supportedTechniques.Count; i++ )
                {
                    ( (Technique)supportedTechniques[i] ).Load();
                }

                isLoaded = true;
            }
        }

        /// <summary>
        ///		Unloads the material, frees resources etc.
        ///		<see cref="Resource"/>
        /// </summary>
        public override void Unload()
        {
            if ( isLoaded )
            {
                // unload unsupported techniques
                for ( int i = 0; i < supportedTechniques.Count; i++ )
                {
                    ( (Technique)supportedTechniques[i] ).Unload();
                }

                isLoaded = false;
            }
        }

        /// <summary>
        ///	    Disposes of any resources used by this object.	
        /// </summary>
        public override void Dispose()
        {
            if ( isLoaded )
            {
                Unload();
            }
        }

        /// <summary>
        ///    Overridden to ensure a recompile occurs if needed before use.
        /// </summary>
        public override void Touch()
        {
            if ( compilationRequired )
            {
                Compile();
            }

            // call base class
            base.Touch();
        }

        #endregion

        #region Methods

        /// <summary>
        ///		Only to be used by MaterialManager.Init.
        /// </summary>
        /// <param name="name"></param>
        internal void SetName( string name )
        {
            this.name = name;
        }

        /// <summary>
        ///    'Compiles' this Material.
        /// </summary>
        /// <remarks>
        ///    Compiling a material involves determining which Techniques are supported on the
        ///    card on which the engine is currently running, and for fixed-function Passes within those
        ///    Techniques, splitting the passes down where they contain more TextureUnitState 
        ///    instances than the curren card has texture units.
        ///    <p/>
        ///    This process is automatically done when the Material is loaded, but may be
        ///    repeated if you make some procedural changes.
        ///    <p/>
        ///    By default, the engine will automatically split texture unit operations into multiple
        ///    passes when the target hardware does not have enough texture units.
        /// </remarks>
        public void Compile()
        {
            Compile( true );
        }

        /// <summary>
        ///    'Compiles' this Material.
        /// </summary>
        /// <remarks>
        ///    Compiling a material involves determining which Techniques are supported on the
        ///    card on which the engine is currently running, and for fixed-function Passes within those
        ///    Techniques, splitting the passes down where they contain more TextureUnitState 
        ///    instances than the curren card has texture units.
        ///    <p/>
        ///    This process is automatically done when the Material is loaded, but may be
        ///    repeated if you make some procedural changes.
        /// </remarks>
        /// <param name="autoManageTextureUnits">
        ///    If true, when a fixed function pass has too many TextureUnitState
        ///    entries than the card has texture units, the Pass in question will be split into
        ///    more than one Pass in order to emulate the Pass. If you set this to false and
        ///    this situation arises, an Exception will be thrown.
        /// </param>
        public void Compile( bool autoManageTextureUnits )
        {
            // clear current list of supported techniques
            supportedTechniques.Clear();
            bestTechniqueList.Clear();

            // compile each technique, adding supported ones to the list of supported techniques
            for ( int i = 0; i < techniques.Count; i++ )
            {
                Technique t = (Technique)techniques[i];

                // compile the technique, splitting texture passes if required
                t.Compile( autoManageTextureUnits );

                // if supported, add it to the list
                if ( t.IsSupported )
                {
                    supportedTechniques.Add( t );

                    // don't wanna insert if it is already present
                    if ( bestTechniqueList[t.LodIndex] == null )
                    {
                        bestTechniqueList[t.LodIndex] = t;
                    }
                }
            }

            // TODO Order best techniques

            compilationRequired = false;

            // Did we find any?
            if ( supportedTechniques.Count == 0 )
            {
                LogManager.Instance.Write( "Warning: Material '{0}' has no supportable Techniques on this hardware.  Will be rendered blank.", name );
            }
        }

        /// <summary>
        ///    Creates a new Technique for this Material.
        /// </summary>
        /// <remarks>
        ///    A Technique is a single way of rendering geometry in order to achieve the effect
        ///    you are intending in a material. There are many reason why you would want more than
        ///    one - the main one being to handle variable graphics card abilities; you might have
        ///    one technique which is impressive but only runs on 4th-generation graphics cards, 
        ///    for example. In this case you will want to create at least one fallback Technique.
        ///    The engine will work out which Techniques a card can support and pick the best one.
        ///    <p/>    
        ///    If multiple Techniques are available, the order in which they are created is 
        ///    important - the engine will consider lower-indexed Techniques to be preferable
        ///    to higher-indexed Techniques, ie when asked for the 'best' technique it will
        ///    return the first one in the technique list which is supported by the hardware.
        /// </remarks>
        /// <returns></returns>
        public Technique CreateTechnique()
        {
            Technique t = new Technique( this );
            techniques.Add( t );
            compilationRequired = true;
            return t;
        }

        public Technique GetBestTechnique()
        {
            return GetBestTechnique( 0 );
        }

        /// <summary>
        ///    Gets the best supported technique. 
        /// </summary>
        /// <remarks>
        ///    This method returns the lowest-index supported Technique in this material
        ///    (since lower-indexed Techniques are considered to be better than higher-indexed
        ///    ones).
        ///    <p/>
        ///    The best supported technique is only available after this material has been compiled,
        ///    which typically happens on loading the material. Therefore, if this method returns
        ///    null, try calling Material.Load.
        /// </remarks>
        /// </summary>
        public Technique GetBestTechnique( int lodIndex )
        {
            if ( supportedTechniques.Count > 0 )
            {
                if ( bestTechniqueList[lodIndex] == null )
                {
                    throw new AxiomException( "Lod index {0} not found for material '{1}'", lodIndex, name );
                }
                else
                {
                    return (Technique)bestTechniqueList[lodIndex];
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        ///		Gets the LOD index to use at the given distance.
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public int GetLodIndex( float distance )
        {
            return GetLodIndexSquaredDepth( distance * distance );
        }

        /// <summary>
        ///		Gets the LOD index to use at the given squared distance.
        /// </summary>
        /// <param name="squaredDistance"></param>
        /// <returns></returns>
        public int GetLodIndexSquaredDepth( float squaredDistance )
        {
            for ( int i = 0; i < lodDistances.Count; i++ )
            {
                float val = (float)lodDistances[i];

                if ( val > squaredDistance )
                {
                    return i - 1;
                }
            }

            // if we fall all the way through, use the highest value
            return lodDistances.Count - 1;
        }

        /// <summary>
        ///    Gets the technique at the specified index.
        /// </summary>
        /// <param name="index">Index of the technique to return.</param>
        /// <returns></returns>
        public Technique GetTechnique( int index )
        {
            Debug.Assert( index < techniques.Count, "index < techniques.Count" );

            return (Technique)techniques[index];
        }

        /// <summary>
        ///    Tells the material that it needs recompilation.
        /// </summary>
        internal void NotifyNeedsRecompile()
        {
            compilationRequired = true;

            // force reload of any new resources
            isLoaded = false;
        }

        /// <summary>
        ///    Removes the specified Technique from this material.
        /// </summary>
        /// <param name="t">A reference to the technique to remove</param>
        public void RemoveTechnique( Technique t )
        {
            Debug.Assert( t != null, "t != null" );

            // remove from the list, and force a rebuild of supported techniques
            techniques.Remove( t );
            supportedTechniques.Clear();
            bestTechniqueList.Clear();
            compilationRequired = true;
        }

        /// <summary>
        ///		Removes all techniques from this material.
        /// </summary>
        public void RemoveAllTechniques()
        {
            techniques.Clear();
            supportedTechniques.Clear();
            bestTechniqueList.Clear();
            compilationRequired = true;
        }

        /// <summary>
        ///		Sets the distance at which level-of-detail (LOD) levels come into effect.
        /// </summary>
        /// <remarks>
        ///		You should only use this if you have assigned LOD indexes to the Technique
        ///		instances attached to this Material. If you have done so, you should call this
        ///		method to determine the distance at which the lowe levels of detail kick in.
        ///		The decision about what distance is actually used is a combination of this
        ///		and the LOD bias applied to both the current Camera and the current Entity.
        /// </remarks>
        /// <param name="lodDistances">
        ///		A list of floats which indicate the distance at which to 
        ///		switch to lower details. They are listed in LOD index order, starting at index
        ///		1 (ie the first level down from the highest level 0, which automatically applies
        ///		from a distance of 0).
        /// </param>
        public void SetLodLevels( FloatList lodDistanceList )
        {
            // clear and add the 0 distance entry
            lodDistances.Clear();
            lodDistances.Add( 0.0f );

            for ( int i = 0; i < lodDistanceList.Count; i++ )
            {
                float val = (float)lodDistanceList[i];

                // squared distance
                lodDistances.Add( val * val );
            }
        }

        public void SetSceneBlending( SceneBlendType blendType )
        {
            // load each technique
            for ( int i = 0; i < techniques.Count; i++ )
            {
                ( (Technique)techniques[i] ).SetSceneBlending( blendType );
            }
        }

        public void SetSceneBlending( SceneBlendFactor src, SceneBlendFactor dest )
        {
            // load each technique
            for ( int i = 0; i < techniques.Count; i++ )
            {
                ( (Technique)techniques[i] ).SetSceneBlending( src, dest );
            }
        }

        /// <summary>
        ///    Creates a copy of this Material with the specified name (must be unique).
        /// </summary>
        /// <param name="newName">The name that the cloned material will be known as.</param>
        /// <returns></returns>
        public Material Clone( string newName )
        {
            Material newMaterial = (Material)MaterialManager.Instance.Create( newName );

            int handle = newMaterial.handle;

            CopyTo( newMaterial );

            newMaterial.isLoaded = isLoaded;
            newMaterial.name = newName;
            newMaterial.handle = handle;

            return newMaterial;
        }


        /// <summary>
        ///		Copies the details of this material into another, preserving the target's handle and name
        ///		(unlike operator=) but copying everything else.
        /// </summary>
        /// <param name="target">Material which will receive this material's settings.</param>
        public void CopyTo( Material target )
        {
            CopyTo( target, true );
        }

        /// <summary>
        ///		Copies the details of this material into another, preserving the target's handle and name
        ///		(unlike operator=) but copying everything else.
        /// </summary>
        /// <param name="target">Material which will receive this material's settings.</param>
        public void CopyTo( Material target, bool copyUniqueInfo )
        {

            if ( copyUniqueInfo )
            {
                target.name = name;
                target.handle = handle;
                target.isLoaded = isLoaded;
            }
            // copy basic data
            target.size = size;
            target.lastAccessed = lastAccessed;
            target.receiveShadows = receiveShadows;

            target.RemoveAllTechniques();

            // clone a copy of all the techniques
            for ( int i = 0; i < techniques.Count; i++ )
            {
                Technique technique = (Technique)techniques[i];
                Technique newTechnique = target.CreateTechnique();
                technique.CopyTo( newTechnique );

                // only add this technique to supported techniques if its...well....supported :-)
                if ( newTechnique.IsSupported )
                {
                    target.supportedTechniques.Add( newTechnique );

                    if ( target.bestTechniqueList[technique.LodIndex] == null )
                    {
                        target.bestTechniqueList[technique.LodIndex] = newTechnique;
                    }
                }
            }

            // clear LOD distances
            target.lodDistances.Clear();

            // copy LOD distances
            for ( int i = 0; i < lodDistances.Count; i++ )
            {
                target.lodDistances.Add( lodDistances[i] );
            }

            target.compilationRequired = compilationRequired;
        }

        public void ApplyDefaults()
        {
            // copy properties from the default materials
            //defaultSettings.CopyTo(this);
            CreateTechnique().CreatePass();

            //compilationRequired = true;
        }

        #endregion

        #region Object overloads

        /// <summary>
        ///    Overridden to give Materials a meaningful hash code.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        /// <summary>
        ///    Overridden.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return name;
        }

        #endregion Object overloads
    }
}
