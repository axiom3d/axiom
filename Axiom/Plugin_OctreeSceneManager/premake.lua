package = newpackage()
package.name = "Axiom.SceneManagers.Octree"
package.language = "c#"
package.kind = "dll"
package.target = "Axiom.SceneManagers.Octree"
package.buildflags = { "unsafe" }
package.defines = { "TRACE" } 
package.config["Debug"].defines = { "DEBUG" }

-- output paths
package.config["Debug"].bindir = "bin/Debug"
package.config["Release"].bindir = "bin/Release"

package.libpaths = { "../Solution Items" }

package.links = { 
	"System", 
	"System.Data", 
	"System.Xml", 
	"Axiom.Engine", 
	"Axiom.MathLib" 
}

package.files = { matchfiles("*.cs") }