using System;

namespace Axiom.SubSystems.Rendering
{
	/// <summary>
	/// 	This serves as a way to query information about the capabilies of a 3D API and the
	/// 	users hardware configuration.  A RenderSystem should create and initialize an instance
	/// 	of this class during startup so that it will be available for use ASAP for checking caps.
	/// </summary>
	public class HardwareCaps
	{
		#region Member variables
		
		private Capabilities caps;
		private int numTextureUnits;
		private float pixelMaterialVersion;
		private float vertexMaterialVersion;
		private int numIndexedMatrices;
		private string vendor;

		#endregion
		
		#region Constructors
		
		public HardwareCaps()
		{
		}
		
		#endregion
		
		#region Properties

		/// <summary>
		///		Reports on the number of texture units the graphics hardware has available.
		/// </summary>
		public int NumTextureUnits
		{
			get { return numTextureUnits; }
			set { numTextureUnits = value; }
		}

		/// <summary>
		///		Gets/Sets the vendor of the current video card.
		/// </summary>
		public string Vendor
		{
			get { return vendor; }
			set { vendor = value; }
		}

		#endregion

		#region Methods

		public bool CheckCap(Capabilities cap)
		{
			return (caps & cap) > 0;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cap"></param>
		public void SetCap(Capabilities cap)
		{
			caps |= cap;

			// write out to the debug console
			System.Diagnostics.Debug.WriteLine(String.Format("Hardware Cap: {0}", cap.ToString()));
		}

		#endregion

	}
}
