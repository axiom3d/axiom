using System;
using Axiom.Core;
using Axiom.Graphics;

namespace Axiom.RenderSystems.OpenGL.GLSL {
	/// <summary>
	///		Factory class for GLSL programs.
	/// </summary>
	public class GLSLProgramFactory : IHighLevelGpuProgramFactory {
		protected static string languageName = "glsl";

		public GLSLProgramFactory() {
		}

		#region IHighLevelGpuProgramFactory Members

		public void Destroy(HighLevelGpuProgram program) {
			// TODO:  Add GLSLProgramFactory.Destroy implementation
		}

		/// <summary>
		///		Creates and returns a new GLSL program object.
		/// </summary>
		/// <param name="name">Name of the object.</param>
		/// <param name="type">Type of the object.</param>
		/// <returns>A newly created GLSL program object.</returns>
		public HighLevelGpuProgram Create(string name, Axiom.Graphics.GpuProgramType type) {
			return new GLSLProgram(name, type, languageName);
		}

		/// <summary>
		///		Returns the language code for this high level program manager.
		/// </summary>
		public string Language {
			get {
				return languageName;
			}
		}

		#endregion
	}
}
