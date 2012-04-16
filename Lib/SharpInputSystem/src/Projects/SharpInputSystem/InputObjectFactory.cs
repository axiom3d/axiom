using System;
using System.Collections.Generic;
using System.Text;

namespace SharpInputSystem
{
	/// <summary>
	/// Interface for creating devices - all devices ultimately get enumerated/created via a factory.
	/// A factory can create multiple types of objects.
	/// </summary>
	public interface InputObjectFactory
	{

		/// <summary>
		/// Return a list of all unused devices the factory maintains.
		/// </summary>
		/// <returns></returns>
		IEnumerable<KeyValuePair<Type, string>> FreeDevices
		{
			get;
		}

		/// <summary>
		/// Number of total devices of requested type
		/// </summary>
		/// <typeparam name="T">Type of devices to check</typeparam>
		/// <returns></returns>
		int DeviceCount<T>() where T : InputObject;

		/// <summary>
		/// Number of free devices of requested type
		/// </summary>
		/// <typeparam name="T">Type of devices to check</typeparam>
		/// <returns></returns>
		int FreeDeviceCount<T>() where T : InputObject;

		/// <summary>
		/// Does a Type exist with the given vendor name
		/// </summary>
		/// <typeparam name="T">Type to check</typeparam>
		/// <param name="vendor">Vendor name to test</param>
		/// <returns></returns>
		bool VendorExists<T>( string vendor ) where T : InputObject;

		/// <summary>
		/// Creates the InputObject
		/// </summary>
		/// <typeparam name="T">Type to create</typeparam>
		/// <param name="creator"></param>
		/// <param name="bufferMode">True to setup for buffered events</param>
		/// <param name="vendor">Create a device with the vendor name, "" means vendor name is unimportant</param>
		/// <returns></returns>
		InputObject CreateInputObject<T>( InputManager creator, bool bufferMode, string vendor ) where T : InputObject;

		/// <summary>
		/// Destroys an InputObject
		/// </summary>
		/// <param name="obj">the InputObject to destroy</param>
		void DestroyInputObject( InputObject obj );
	}
}
