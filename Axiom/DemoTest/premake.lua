package = newpackage()
package.name = "Demos"
package.language = "c#"
package.kind = "exe"
package.buildflags = { "unsafe" }
package.defines = { "TRACE" } 
package.config["Debug"].defines = { "DEBUG" }

-- output paths
package.config["Debug"].bindir = "bin"
package.config["Release"].bindir = "bin"

package.target = "Demos"

package.links = { 
	"System", 
	"System.Windows.Forms", 
	"System.Drawing",
	"System.Data",
	"System.Xml",
	"Axiom.Engine", 
	"Axiom.MathLib",
	"Axiom.Dynamics.ODE",
	"Axiom.CgPrograms",
	"Axiom.RenderSystems.OpenGL",
	"Axiom.RenderSystems.DirectX9",
	"Axiom.SceneManagers.Octree",
	"Axiom.Platforms.Win32",
	"Axiom.Gui.Elements",
	"Axiom.ParticleFX"
}

package.files = { 
	matchfiles("*.cs"),
	matchfiles("Demos/*.cs"),
	"app.config"
}