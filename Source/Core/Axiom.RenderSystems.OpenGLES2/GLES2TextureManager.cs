using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Core;
using GL = OpenTK.Graphics.ES20.GL;
using GLenum = OpenTK.Graphics.ES20.All;
using Axiom.Media;

namespace Axiom.RenderSystems.OpenGLES2
{
    class GLES2TextureManager : TextureManager
    {
        GLES2Support glSupport;
        int warningTextureID;

        public GLES2TextureManager(GLES2Support support)
        {
            this.glSupport = support;
            this.warningTextureID = 0;
            //Register with group manager
            ResourceGroupManager.Instance.RegisterResourceManager(ResourceType, this);

            CreateWarningTexture();
        }

        protected override void dispose(bool disposeManagedResources)
        {
            //Unregister with group manager
            ResourceGroupManager.Instance.UnregisterResourceManager(ResourceType);

            //Delte warning texture
            GL.DeleteTextures(1, ref warningTextureID);
            base.dispose(disposeManagedResources);
        }
        public override bool IsHardwareFilteringSupported(Graphics.TextureType ttype, Media.PixelFormat format, Graphics.TextureUsage usage, bool preciseFormatOnly)
        {
            if (format == PixelFormat.Unknown)
            {
                return false;
            }

            //Check native format
            PixelFormat nativeFormat = GetNativeFormat(ttype, format, usage);
            if (preciseFormatOnly && format != nativeFormat)
                return false;

            //Assume non-floaitng point is supported always
            if (!PixelUtil.IsFloatingPoint(nativeFormat))
                return true;

            return false;
        }

        protected override Resource _create(string name, ulong handle, string group, bool isManual, IManualResourceLoader loader, Collections.NameValuePairList createParams)
        {
            return new GLES2Texture(this, name, handle, group, isManual, loader, glSupport);
        }
        public override Media.PixelFormat GetNativeFormat(Graphics.TextureType ttype, Media.PixelFormat format, Graphics.TextureUsage usage)
        {
            var caps = Root.Instance.RenderSystem.Capabilities;

            //Check compressed texture support
            //if a compressed formt not supported, rever to PixelFormat.A8R8G8B8
            if (PixelUtil.IsCompressed(format) &&
                !caps.HasCapability(Graphics.Capabilities.TextureCompressionDXT) && !caps.HasCapability(Graphics.Capabilities.TextureCompressionPVRTC))
            {
                return PixelFormat.A8R8G8B8;
            }
            //if floating point texture not supported, rever to PixelFormat.A8R8G8B8
            if (PixelUtil.IsFloatingPoint(format) &&
                !caps.HasCapability(Graphics.Capabilities.TextureFloat))
            {
                return PixelFormat.A8R8G8B8;
            }

            //Check if this is a valid rendertarget format
            if ((usage & Graphics.TextureUsage.RenderTarget) == Graphics.TextureUsage.RenderTarget)
            {
                //Get closest supported alternative
                //if format is supported it's returned
                return GLES2RTTManager.Instance.GetSupportedAlternative(format);
            }

            //Supported
            return format;
        }

        /// <summary>
        /// Internal method to create a warning texture (bound when a texture unit is blank)
        /// </summary>
        protected void CreateWarningTexture()
        {
            //Generate warning texture
            int width = 8, height = 8;
            int[] data = new int[width & height];

            //Yellow/black stripes
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    data[y * width + x] = (((x + y) % 8) < 4) ? 0x000000 : 0xFFFF00;
                }
            }

            //Create GL resource
            GL.GenTextures(1, ref warningTextureID);
            GL.BindTexture(OpenTK.Graphics.ES20.All.Texture2D, warningTextureID);
            GL.TexImage2D(OpenTK.Graphics.ES20.All.Texture2D, 0, (int)GLenum.Rgb, width, height, 0, GLenum.Rgb, GLenum.UnsignedShort565, data);

            data = null;
        }
        public int WarningTextureID
        {
            get { return warningTextureID; }
        }
    }
}
