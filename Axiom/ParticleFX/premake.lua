package = newpackage()
package.name = "Axiom.ParticleFX"
package.language = "c#"
package.kind = "dll"
package.defines = { "TRACE" } 
package.config["Debug"].defines = { "DEBUG" }

-- output paths
package.config["Debug"].bindir = "bin/Debug"
package.config["Release"].bindir = "bin/Release"

package.target = "Axiom.ParticleFX"

package.links = { 
	"System", 
	"Axiom.Engine", 
	"Axiom.MathLib" 
}

package.files = { 
	matchfiles("*.cs"),
	matchfiles("Factories/*.cs")
}