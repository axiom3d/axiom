package = newpackage()
package.name = "Axiom.RenderSystems.OpenGL"
package.language = "c#"
package.kind = "dll"
package.buildflags = { "unsafe" }
package.defines = { "TRACE" } 
package.config["Debug"].defines = { "DEBUG" }
package.target = "Axiom.RenderSystems.OpenGL"
package.libpaths = { "../Solution Items" }

-- output paths
package.config["Debug"].bindir = "bin/Debug"
package.config["Release"].bindir = "bin/Release"

package.links = { 
	"System", 
	"System.Data", 
	"System.Xml", 
	"System.Drawing", 
	"System.Windows.Forms", 
	"Axiom.Engine", 
	"Axiom.MathLib", 
	"Tao.OpenGL", 
	"Tao.Platform.Windows" 
}

package.files = { 
	matchfiles("*.cs"),
	matchfiles("Nvidia/*.cs"),
	matchfiles("ATI/*.cs")
}