using System;
using System.Collections.Generic;
using System.Text;

using Axiom.Math;
using Axiom.Overlays;

namespace Axiom.Core
{
	public class ObjectTextDisplay
	{
		protected MovableObject m_p;
		protected Camera m_c;
		protected bool m_enabled;
		protected Overlay m_pOverlay;
		protected OverlayElement m_pText;
		protected OverlayElementContainer m_pContainer;
		protected string m_text;

		public ObjectTextDisplay( MovableObject p, Camera c, string shapeName )
		{
			m_p = p;
			m_c = c;
			m_enabled = false;
			m_text = "";

			// create an overlay that we can use for later

			// = Ogre.OverlayManager.getSingleton().create("shapeName");
			m_pOverlay = (Overlay)OverlayManager.Instance.Create( "shapeName" );

			// (Ogre.OverlayContainer)(Ogre.OverlayManager.getSingleton().createOverlayElement("Panel", "container1"));
			m_pContainer = (OverlayElementContainer)( OverlayElementManager.Instance.CreateElement( "Panel", "container1", false ) );

			//m_pOverlay.add2D(m_pContainer);
			m_pOverlay.AddElement( m_pContainer );

			//m_pText = Ogre.OverlayManager.getSingleton().createOverlayElement("TextArea", "shapeNameText");
			m_pText = OverlayElementManager.Instance.CreateElement( "TextArea", shapeName, false );

			m_pText.SetDimensions( 1.0f, 1.0f );

			//m_pText.setMetricsMode(Ogre.GMM_PIXELS);
			m_pText.MetricsMode = MetricsMode.Pixels;


			m_pText.SetPosition( 1.0f, 1.0f );


			m_pText.SetParam( "font_name", "Arial" );
			m_pText.SetParam( "char_height", "25" );
			m_pText.SetParam( "horz_align", "center" );
			m_pText.Color = new ColorEx( 1.0f, 1.0f, 1.0f );
			//m_pText.setColour(Ogre.ColourValue(1.0, 1.0, 1.0));


			m_pContainer.AddChild( m_pText );

			m_pOverlay.Show();
		}



		public void enable( bool enable )
		{
			m_enabled = enable;
			if ( enable )
				m_pOverlay.Show();
			else
				m_pOverlay.Hide();

		}

		public void setText( string text )
		{

			m_text = text;

			m_pText.Text = m_text;
		}

		public void update()
		{
			if ( !m_enabled )
				return;

			// get the projection of the object's AABB into screen space
			AxisAlignedBox bbox = m_p.GetWorldBoundingBox( true );//new AxisAlignedBox(m_p.BoundingBox.Minimum, m_p.BoundingBox.Maximum);// GetWorldBoundingBox(true));


			//Ogre.Matrix4 mat = m_c.getViewMatrix();
			Matrix4 mat = m_c.ViewMatrix;
			//const Ogre.Vector3 corners = bbox.getAllCorners();
			Vector3[] corners = bbox.Corners;


			float min_x = 1.0f;
			float max_x = 0.0f;
			float min_y = 1.0f;
			float max_y = 0.0f;

			// expand the screen-space bounding-box so that it completely encloses
			// the object's AABB
			for ( int i = 0; i < 8; i++ )
			{
				Vector3 corner = corners[ i ];

				// multiply the AABB corner vertex by the view matrix to
				// get a camera-space vertex
				//corner = multiply(mat,corner);
				corner = mat * corner;

				// make 2D relative/normalized coords from the view-space vertex
				// by dividing out the Z (depth) factor -- this is an approximation
				float x = corner.x / corner.z + 0.5f;
				float y = corner.y / corner.z + 0.5f;

				if ( x < min_x )
					min_x = x;

				if ( x > max_x )
					max_x = x;

				if ( y < min_y )
					min_y = y;

				if ( y > max_y )
					max_y = y;
			}

			// we now have relative screen-space coords for the object's bounding box; here
			// we need to center the text above the BB on the top edge. The line that defines
			// this top edge is (min_x, min_y) to (max_x, min_y)

			//m_pContainer->setPosition(min_x, min_y);
			m_pContainer.SetPosition( 1 - max_x, min_y ); // Edited by alberts: This code works for me
			m_pContainer.SetDimensions( max_x - min_x, 0.1f ); // 0.1, just "because"
		}


	}


}
