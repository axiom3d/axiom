using System;
using Axiom.Core;
using Math = Axiom.Math;

namespace Axiom.Samples.MousePicking
{
    public class SelectionRectangle : ManualObject
    {
        /// <summary>
        /// There are two ways to create your own mesh within Axiom.
        /// The first way is to subclass the SimpleRenderable object and provide it with the vertex and index buffers directly.
        /// This is the most direct way to create one, but it's also the most cryptic.
        /// The Generating A Mesh code snippet shows an example of this. To make things easier, 
        /// Axiom provides a much nicer interface called ManualObject, 
        /// which allows you to use some simple functions to define a mesh instead of writing raw data to the buffer objects.
        /// Instead of dropping the position, color, and so on into a buffer, you simply call the "position" and "colour" functions.
        /// In this tutorial we need to create a white rectangle to display when we are dragging the mouse to select objects. 
        /// There really isn't a class in Axiom we could use to display a 2D rectangle.
        /// We will have to come up with a way of doing it on our own. 
        /// We could use an Overlay and resize it to display the selection rectangle, but 
        /// the problem with doing it this way is that the image you use for the selection rectangle could
        /// get stretched out of shape and look awkward. Instead, we will generate a very 
        /// simple 2D mesh to act as our selection rectangle.
        /// </summary>
        /// <param name="name"></param>
        public SelectionRectangle(string name)
            : base(name)
        {
            /*
			 * When we create the selection rectangle, we have to create it such that it will render in 2D.
			 * We also have to be sure that it will render when Ogre's Overlays render so that it sits on top of all other 
			 * objects on screen. 
			 * 
			 * Doing this is actually very easy.
			 */
            RenderQueueGroup = RenderQueueGroupID.Overlay;
            UseIdentityProjection = true;
            UseIdentityView = true;
            QueryFlags = 0;
            /*
			 * The first function sets the render queue for the object to be the Overlay queue. 
			 * The next two functions sets the projection and view matrices to be the identity.
			 * Projection and view matrices are used by many rendering systems (such as OpenGL and DirectX) to define where 
			 * objects go in the world. 
			 * Since Axiom abstracts this away for us, we won't go into any detail about what these matrices actually 
			 * are or what they do. Instead what you need to know is that if you set the projection and view matrix 
			 * to be the identity, as we have here, we will basically create a 2D object. 
			 * When defining this object, the coordinate system changes a bit. 
			 * We no longer deal with the Z axis (if you are asked for the Z axis, set the value to -1). 
			 * Instead we have a new coordinate system with X and Y running from -1 to 1 inclusive. Lastly,
			 * we will set the query flags for the object to be 0, which will prevent prevent the selection rectangle 
			 * from being included in the query results.
			 */
        }

        /// <summary>
        /// Sets the corners of the SelectionRectangle.  Every parameter should be in the
        /// range [0, 1] representing a percentage of the screen the SelectionRectangle
        /// should take up.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        public void SetCorners(float left, float top, float right, float bottom)
        {
            /*
			 * Now that the object is set up, we need to actually build the rectangle. 
			 * We have one small snag before we get started. 
			 * We are going to be calling this function with mouse locations. 
			 * That is we will be given, a number between 0 and 1 for the x and y coordinates,
			 * yet we need to convert these to numbers in the range -1, 1. 
			 * To make matters slightly more complicated, the y coordinate is backwards too.
			 * The mouse cursor in CEGUI defines the top of the screen at 0, the bottom at 1.
			 * In our new coordinate system, the top of the screen is +1, the bottom is -1. Thankfully,
			 * a few quick conversions will take care of this problem.
			 */
            left = left * 2 - 1;
            right = right * 2 - 1;
            top = 1 - top * 2;
            bottom = 1 - bottom * 2;

            /*
			 * Now the positions are in the new coordinate system. 
			 * Next we need to actually build the object. To do this, we first call the begin method. 
			 * It takes in two parameters, the name of the material to use for this section of the object,
			 * and the render operation to use to draw it. Since we are not putting a texture on this,
			 * we will leave the material blank. The second parameter is the RenderOperation.
			 * We can render the mesh using points, lines, or triangles. 
			 * We would use triangles if we were rendering a full mesh, but since we want an empty rectangle,
			 * we will use the line strip. The line strip draws a line to each vertex from the previous vertex you defined.
			 * So to create our rectangle, we will define 5 points (the first and the last point are the same to connect the entire 
			 * rectangle)
			 */
            Clear();
            Begin("", Axiom.Graphics.OperationType.LineStrip);
            Position(left, top, -1);
            Position(right, top, -1);
            Position(right, bottom, -1);
            Position(left, bottom, -1);
            Position(left, top, -1);
            End();

            /*
			 * Note that since we will be calling this many times, we have added the clear call at the beginning 
			 * to remove the previous rectangle before redrawing it. 
			 * When defining a manual object, you may call begin/end multiple times to create multiple sub-meshes
			 * (which can have different materials/RenderOperations). Note we have also set the Z parameter to be -1,
			 * since we are trying to define a 2D object which will not use that axis. 
			 * Setting it to be -1 will ensure that we are not on top of, or behind, the camera when rendering.
			 * The last thing we need to do is set the bounding box for this object.
			 * Many SceneManagers cull objects which are off screen. Even though we've basically created a 2D object, 
			 * Axiom is still a 3D engine, and treats our 2D object as if it sits in 3D space.
			 * This means that if we create this object and attach it to a -SceneNode (as we will do in the next section), 
			 * it will disappear on us when we look away. To fix this we will set the bounding box of the object to be infinite,
			 * so that the camera will always be inside of it:
			 */
            BoundingBox.IsInfinite = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="topLeft"></param>
        /// <param name="bottomRight"></param>
        public void SetCorners(Math.Vector2 topLeft, Math.Vector2 bottomRight)
        {
            SetCorners(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
        }
    }
}