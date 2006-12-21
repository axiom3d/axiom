using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;

using Axiom;
using Axiom.Core;
using Axiom.MathLib;

namespace Axiom.DotScene {

   /// <summary>
   /// Implements the necessary functionality to load an Axiom Scene from a DotScene file in the 0.2.0 format
   /// </summary>
   class DotSceneLoader020 : IDotSceneLoaderImplementor {

      #region Declarations

      private SceneManager _sceneManager;
      private RenderWindow _renderWindow;
      private bool _doMaterials;
      private bool _forceShadowBuffers;
      private string _groupName;

      #endregion

      #region IDotSceneLoaderImplementor Members

      /// <summary>
      /// Loads the specified scene based on the implementation provided
      /// </summary>
      /// <param name="sceneXmlDocument">XmlDocument containing scene data</param>
      /// <param name="sceneManager">Scene manager to build the scene</param>
      /// <param name="renderWindow">Window to render the scene to</param>
      /// <param name="groupName">Group name of the scene's resources</param>
      /// <param name="rootSceneNode">Root scene node</param>
      /// <param name="doMaterials">Boolean indicating whether materials should also be loaded</param>
      /// <param name="forceShadowBuffers">Boolean indicating whether ShadowBuffers should be forced</param>
      public void LoadScene( XmlDocument sceneXmlDocument, SceneManager sceneManager, RenderWindow renderWindow, string groupName, SceneNode rootSceneNode, bool doMaterials, bool forceShadowBuffers ) {
         
         // Sanity checks
         if( sceneXmlDocument == null ) {
            throw new Exception( "Error loading scene: XmlDocument null" );
         }
         XmlNode rootXmlNode = sceneXmlDocument.FirstChild;
         if( rootXmlNode == null ) {
            throw new Exception( "Error loading scene: Root node does not exist" );
         }

         // Set member values
         _sceneManager = sceneManager;
         _renderWindow = renderWindow;
         _doMaterials = doMaterials;
         _forceShadowBuffers = forceShadowBuffers;
         _groupName = groupName;

         if( rootSceneNode == null ) {
            rootSceneNode = _sceneManager.CreateSceneNode( "dsRootSceneNode" );
         }
          
         // Parse FirstChild nodes (nodes, environment, externals,)
         // (functions have to check if their node exists by themselves)
         ParseEnvironment( rootXmlNode.SelectSingleNode( "environment" ) );
         ParseExternals( rootXmlNode.SelectSingleNode( "externals" ) );
         ParseNodes( rootXmlNode.SelectSingleNode( "nodes" ), rootSceneNode );

      }

      #endregion


      #region Private Methods

      private void ParseEnvironment( XmlNode environmentNode ) {
        
         if (environmentNode == null) return;

         // Ambient color
         XmlNode ambientColorNode = environmentNode.SelectSingleNode( "colourAmbient" );
         _sceneManager.AmbientLight = DotSceneXmlUtility.RetrieveColor( ambientColorNode, ColorEx.Black );

         // Background color
         XmlNode backgroundColorNode = environmentNode.SelectSingleNode( "colourBackground" );
         ColorEx backgroundColor = DotSceneXmlUtility.RetrieveColor( backgroundColorNode, ColorEx.Black );

         for( int viewportIndex = 0; viewportIndex < _renderWindow.NumViewports; viewportIndex++ ) {
            _renderWindow.GetViewport( viewportIndex ).BackgroundColor = backgroundColor;
         }

         // SkyBox
         XmlNode skyBoxNode = environmentNode.SelectSingleNode( "skyBox" );
         if( skyBoxNode != null ) {
            ParseSkyBox( skyBoxNode );
         }

         // SkyDome
         XmlNode skyDomeNode = environmentNode.SelectSingleNode( "skyDome" );
         if( skyDomeNode != null ) {
            ParseSkyDome( skyDomeNode );
         }

         // SkyPlane
         XmlNode skyPlaneNode = environmentNode.SelectSingleNode( "skyPlane" );
         if( skyPlaneNode != null ) {
            ParseSkyPlane( skyPlaneNode );
         }

         // Fog
         XmlNode fogNode = environmentNode.SelectSingleNode( "fog" );
         if( fogNode != null ) {
            ParseFog( fogNode );
         }

      }



      private void ParseExternals(XmlNode externalsNode) {

          if (externalsNode == null) return;
          //TODO: Implement if necessary or leave a log message at least
          //LogManager.Instance.Write("The <{0}> xml node is not supported by DotSceneLoader020.", externalsNode.Name);
      }


      private void ParseSkyBox( XmlNode skyBoxNode ) {

         // Material name
         string materialName = DotSceneXmlUtility.RetrieveXmlAttributeValue(skyBoxNode, "material", "BaseWhite");

         // Distance
         float distance = DotSceneXmlUtility.RetrieveXmlAttributeValue(skyBoxNode, "distance", 5000.0f);

         // Drawfirst
         bool drawFirst = DotSceneXmlUtility.RetrieveXmlAttributeValue(skyBoxNode, "drawFirst", true);

         // Rotation
         XmlNode rotationNode = skyBoxNode.Attributes["rotation"];
         Quaternion rotation = DotSceneXmlUtility.RetrieveQuaternion(rotationNode, Quaternion.Identity);

         // Set the sky
         _sceneManager.SetSkyBox( true, materialName, distance, drawFirst, rotation );

      }



      private void ParseSkyDome( XmlNode skyDomeNode ) {

         // Material name
         string materialName = DotSceneXmlUtility.RetrieveXmlAttributeValue(skyDomeNode, "material", "BaseWhite");

         // Distance
         float distance = DotSceneXmlUtility.RetrieveXmlAttributeValue(skyDomeNode, "distance", 4000.0f);
         

         // Drawfirst
         bool drawFirst = DotSceneXmlUtility.RetrieveXmlAttributeValue(skyDomeNode, "drawFirst", true);

         // Rotation
         XmlNode rotationNode = skyDomeNode.Attributes["rotation"];
         Quaternion rotation = DotSceneXmlUtility.RetrieveQuaternion( rotationNode, Quaternion.Identity );
        
         // Curvature
         float curvature = DotSceneXmlUtility.RetrieveXmlAttributeValue(skyDomeNode, "curvature", 10.0f);

         // Tiling
         float tiling = DotSceneXmlUtility.RetrieveXmlAttributeValue(skyDomeNode, "tiling", 8.0f);

         // Set the sky
         _sceneManager.SetSkyDome( true, materialName, curvature, tiling, distance, drawFirst, rotation );

      }



      private void ParseSkyPlane( XmlNode skyPlaneNode ) {

         // Material name
         string materialName = DotSceneXmlUtility.RetrieveXmlAttributeValue(skyPlaneNode, "material", "BaseWhite");

         // Drawfirst
         bool drawFirst = DotSceneXmlUtility.RetrieveXmlAttributeValue(skyPlaneNode, "drawFirst", false);

         // PlaneX
         float planeX = DotSceneXmlUtility.RetrieveXmlAttributeValue(skyPlaneNode, "planeX", 0.0f);

         // PlaneY
         float planeY = DotSceneXmlUtility.RetrieveXmlAttributeValue(skyPlaneNode, "planeY", -1.0f); 

         // PlaneZ
         float planeZ = DotSceneXmlUtility.RetrieveXmlAttributeValue(skyPlaneNode, "planeZ", -0.0f); 

         // PlaneD
         float planeD = DotSceneXmlUtility.RetrieveXmlAttributeValue(skyPlaneNode, "planeD", 500f); 

         // Scale
         float scale = DotSceneXmlUtility.RetrieveXmlAttributeValue(skyPlaneNode, "scale", 1000.0f); 

         // Tiling
         float tiling = DotSceneXmlUtility.RetrieveXmlAttributeValue(skyPlaneNode, "tiling", 10.0f); 

         // Bow
         float bow = DotSceneXmlUtility.RetrieveXmlAttributeValue(skyPlaneNode, "bow", 0.0f); 

         // Set the sky
         _sceneManager.SetSkyPlane( true, new Plane( new Vector3( planeX, planeY, planeZ ), planeD ), materialName, scale, tiling, drawFirst, bow, 1, 1 );

      }

      private void ParseFog( XmlNode fogNode ) {

         // Exponential density
         float exponentialDensity = DotSceneXmlUtility.RetrieveXmlAttributeValue(fogNode, "exponentialDensity", 0.0f); 

         // Linear start
         float linearStart = DotSceneXmlUtility.RetrieveXmlAttributeValue(fogNode, "linearStart", 0.0f);

         // Linear end
         float linearEnd = DotSceneXmlUtility.RetrieveXmlAttributeValue(fogNode, "linearEnd", 0.0f);

         // Diffuse color
         XmlNode diffuseColorNode = fogNode.SelectSingleNode( "colourDiffuse" );
         ColorEx diffuseColor = DotSceneXmlUtility.RetrieveColor( diffuseColorNode, ColorEx.White );
     
         // Fog mode
         FogMode fogMode;

          switch( DotSceneXmlUtility.RetrieveXmlAttributeValue(fogNode, "mode", String.Empty) ) {

             case "exp":
                  fogMode = FogMode.Exp;
                  break;
               case "exp2":
                  fogMode = FogMode.Exp2;
                  break;
               case "linear":
                  fogMode = FogMode.Linear;
                  break;
               default:
                  fogMode = FogMode.None;
                  break;
         }
         

         // Set the fog
         _sceneManager.SetFog( fogMode, diffuseColor, exponentialDensity, linearStart, linearEnd );

      }



      private void ParseNodes( XmlNode nodesRootNode, SceneNode parentSceneNode ) {

         if (nodesRootNode == null) return;

         foreach( XmlNode childNode in nodesRootNode.ChildNodes ) {
            switch( childNode.Name ) {
               case "position":
                  parentSceneNode.Position = DotSceneXmlUtility.RetrieveVector3( childNode );
                  parentSceneNode.SetInitialState();
                  break;
               case "rotation":
                  parentSceneNode.Orientation = DotSceneXmlUtility.RetrieveQuaternion( childNode );
                  parentSceneNode.SetInitialState();
                  break;
               case "scale":
                  parentSceneNode.ScaleFactor = DotSceneXmlUtility.RetrieveVector3( childNode );
                  parentSceneNode.SetInitialState();
                  break;
               case "node":
                  ParseNode( parentSceneNode, childNode );
                  break;
            }
         }

      }

      private void ParseNode( SceneNode parentSceneNode, XmlNode nodeToParse ) {

         SceneNode newSceneNode = parentSceneNode.CreateChildSceneNode( DotSceneXmlUtility.RetrieveOrGenerateNodeName( nodeToParse ) );
         
         // Parse through PRS transformations first
         // List<XmlNode> stillToParseNodeList = new List<XmlNode>();
         foreach( XmlNode node in nodeToParse.ChildNodes ) {
            switch( node.Name ) {
               case "position":
                  newSceneNode.Position = DotSceneXmlUtility.RetrieveVector3( node );
                  break;
               case "rotation":
                  newSceneNode.Orientation = DotSceneXmlUtility.RetrieveQuaternion( node );
                  break;
               case "scale":
                  newSceneNode.ScaleFactor = DotSceneXmlUtility.RetrieveVector3( node );
                  break;
               case "entity":
                  ParseEntity( newSceneNode, node );
                  break;
               case "light":
                  ParseLight( newSceneNode, node );
                  break;
               case "camera":
                  ParseCamera( newSceneNode, node );
                  break;
               case "node":
                  // Note: Recursive call
                  ParseNode( newSceneNode, node );
                  break;
            }
         }

      }

      private void ParseEntity( SceneNode parentSceneNode, XmlNode entityNode ) {

         string entityNodeName = DotSceneXmlUtility.RetrieveOrGenerateNodeName( entityNode );

         XmlAttribute meshNameAttribute = entityNode.Attributes["meshFile"];

         if( meshNameAttribute != null ) {
            XmlNode vertexBufferNode = entityNode.SelectSingleNode( "vertexBuffer" );
            XmlNode indexBufferNode = entityNode.SelectSingleNode( "indexBuffer" );
            if( _forceShadowBuffers || ( vertexBufferNode == null && indexBufferNode == null )  ) {
               MeshManager.Instance.Load( meshNameAttribute.Value );
            }
            else {
               // TODO: Load the vertex and index buffers
            }
            Entity newEntity = _sceneManager.CreateEntity( entityNodeName, meshNameAttribute.Value );
            parentSceneNode.AttachObject( newEntity );
         }

      }

      private void ParseLight( SceneNode parentSceneNode, XmlNode lightNode ) {

         string lightNodeName = DotSceneXmlUtility.RetrieveOrGenerateNodeName( lightNode );
         Light newLight = _sceneManager.CreateLight( lightNodeName );
         parentSceneNode.AttachObject( newLight );

         // Visibility
         newLight.IsVisible = DotSceneXmlUtility.RetrieveXmlAttributeValue(lightNode, "visible", true);

         // Diffuse color
         XmlNode diffuseColorNode = lightNode.SelectSingleNode( "colourDiffuse" );
         newLight.Diffuse = DotSceneXmlUtility.RetrieveColor( diffuseColorNode, newLight.Diffuse );

         // Specular color
         XmlNode specularColorNode = lightNode.SelectSingleNode( "colourSpecular" );
         newLight.Specular = DotSceneXmlUtility.RetrieveColor( specularColorNode, newLight.Specular );

         // Light type
         string lightType = DotSceneXmlUtility.RetrieveXmlAttributeValue(lightNode, "type", "point");

         switch( lightType ) {
            case "point":
               newLight.Type = LightType.Point;
               break;
            case "directional":
            case "targetDirectional":
               newLight.Type = LightType.Directional;
               break;
            case "spot":
            case "targetSpot":
               newLight.Type = LightType.Spotlight;
               break;
         }

         // Attenuation
         XmlNode attenuationNode = lightNode.SelectSingleNode( "lightAttenuation" );
         if( attenuationNode != null ) {
            float attenuationRange = DotSceneXmlUtility.RetrieveXmlAttributeValue(attenuationNode, "range", 0.0f);
            float attenuationConstant = DotSceneXmlUtility.RetrieveXmlAttributeValue(attenuationNode, "constant", 0.0f);
            float attenuationLinear = DotSceneXmlUtility.RetrieveXmlAttributeValue(attenuationNode, "linear", 0.0f);
            float attenuationQuadratic = DotSceneXmlUtility.RetrieveXmlAttributeValue(attenuationNode, "quadratic", 0.0f);
            newLight.SetAttenuation( attenuationRange, attenuationConstant, attenuationLinear, attenuationQuadratic );
         }

         // Range
         XmlNode rangeNode = lightNode.SelectSingleNode( "lightRange" );
         if( rangeNode != null ) {
            float inner = DotSceneXmlUtility.RetrieveXmlAttributeValue(rangeNode, "inner", 0.0f);
            float outer = DotSceneXmlUtility.RetrieveXmlAttributeValue(rangeNode, "outer", 0.0f);
            float falloff = DotSceneXmlUtility.RetrieveXmlAttributeValue(rangeNode, "falloff", 0.0f);
            newLight.SetSpotlightRange( inner, outer, falloff );
         }

         // Direction/normal
         XmlNode normalNode = lightNode.SelectSingleNode( "normal" );
         if( normalNode != null ) {
            newLight.Direction = DotSceneXmlUtility.RetrieveVector3( normalNode );
         }

         // Position
         XmlNode positionNode = lightNode.SelectSingleNode( "position" );
         if( positionNode != null ) {
            newLight.Position = DotSceneXmlUtility.RetrieveVector3( positionNode );
         }

      }

      private void ParseCamera( SceneNode parentSceneNode, XmlNode cameraNode ) {

         string cameraName = DotSceneXmlUtility.RetrieveOrGenerateNodeName( cameraNode );
         Camera newCamera = _sceneManager.CreateCamera( cameraName );
         parentSceneNode.AttachObject( newCamera );

         // Clipping
         XmlNode clippingNode = cameraNode.SelectSingleNode( "clipping" );
         if( clippingNode != null ) {
            newCamera.Near = DotSceneXmlUtility.RetrieveXmlAttributeValue(clippingNode, "near", newCamera.Near);
            newCamera.Far = DotSceneXmlUtility.RetrieveXmlAttributeValue(cameraNode, "far", newCamera.Far);
         }

         // FOV
         XmlAttribute fovAttribute = cameraNode.Attributes["fov"];
         if( fovAttribute != null ) {
             newCamera.FOV = DotSceneXmlUtility.RetrieveXmlAttributeValue(cameraNode, "fov", newCamera.FOV);
         }

         // AspectRatio
         newCamera.AspectRatio = DotSceneXmlUtility.RetrieveXmlAttributeValue(cameraNode, "aspectRatio", newCamera.AspectRatio);

         // Projection type
         string type = DotSceneXmlUtility.RetrieveXmlAttributeValue(cameraNode, "projectionType", string.Empty);
         if (type == "perspective")
         {
             newCamera.ProjectionType = Projection.Perspective;
         }
         else
         {
             newCamera.ProjectionType = Projection.Orthographic;
         }

         // Direction/normal
         XmlNode normalNode = cameraNode.SelectSingleNode( "normal" );
         if( normalNode != null ) {
            newCamera.Direction = DotSceneXmlUtility.RetrieveVector3( normalNode );
         }

         // Position
         XmlNode positionNode = cameraNode.SelectSingleNode( "position" );
         if( positionNode != null ) {
            newCamera.Position = DotSceneXmlUtility.RetrieveVector3( positionNode );
         }

      }


      #endregion

   }

}