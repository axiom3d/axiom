package = newpackage()
package.name = "RenderSystem_OpenGL"
package.language = "c#"
package.kind = "dll"
package.buildflags = { "unsafe" }
package.defines = { "TRACE" } 
package.config["Debug"].target = { "DEBUG" }
package.target = "Axiom.RenderSystems.OpenGL"
package.libpaths = { "../Solution Items" }

package.links = { 
	"System", 
	"System.Data", 
	"System.Xml", 
	"System.Drawing", 
	"System.Windows.Forms", 
	"Engine", 
	"MathLib", 
	"Tao.OpenGL", 
	"Tao.Platform.Windows" 
}

package.files = { 
	matchfiles("*.cs"),
	matchfiles("Nvidia/*.cs"),
	matchfiles("ATI/*.cs")
}