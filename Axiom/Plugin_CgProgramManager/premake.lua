package = newpackage()
package.name = "Axiom.CgPrograms"
package.language = "c#"
package.kind = "dll"
package.target = "Axiom.CgPrograms"
package.defines = { "TRACE" } 
package.config["Debug"].defines = { "DEBUG" }

-- output paths
package.config["Debug"].bindir = "bin/Debug"
package.config["Release"].bindir = "bin/Release"

package.libpaths = { "../Solution Items" }

package.links = { 
	"System", 
	"Axiom.Engine", 
	"Tao.Cg" 
}

package.files = { matchfiles("*.cs") }