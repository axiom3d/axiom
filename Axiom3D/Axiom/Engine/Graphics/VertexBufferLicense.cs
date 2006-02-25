using System;

namespace Axiom
{
    /// <summary>
    ///		Structure holding details of a license to use a temporary shared buffer.
    /// </summary>
    public class VertexBufferLicense
    {
        #region Fields

        public HardwareVertexBuffer originalBuffer;
        public BufferLicenseRelease licenseType;
        public HardwareVertexBuffer buffer;
        public IHardwareBufferLicensee licensee;

        #endregion Fields

        #region Constructor

        /// <summary>
        /// 
        /// </summary>
        public VertexBufferLicense( HardwareVertexBuffer originalBuffer, BufferLicenseRelease licenseType,
            HardwareVertexBuffer buffer, IHardwareBufferLicensee licensee )
        {

            this.originalBuffer = originalBuffer;
            this.licenseType = licenseType;
            this.buffer = buffer;
            this.licensee = licensee;
        }

        #endregion Constructor
    }
}
