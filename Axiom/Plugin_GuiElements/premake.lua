package = newpackage()
package.name = "Axiom.Gui.Elements"
package.language = "c#"
package.kind = "dll"
package.buildflags = { "unsafe" }
package.target = "Axiom.Gui.Elements"
package.defines = { "TRACE" } 
package.config["Debug"].defines = { "DEBUG" }

-- output paths
package.config["Debug"].bindir = "bin/Debug"
package.config["Release"].bindir = "bin/Release"

package.links = { 
	"System", 
	"Axiom.Engine", 
	"Axiom.MathLib" 
}

package.files = { matchfiles("*.cs") }