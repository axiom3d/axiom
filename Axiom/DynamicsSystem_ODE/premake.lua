package.name = "Axiom.Dynamics.ODE"
package.language = "c#"
package.kind = "dll"
package.target = "Axiom.Dynamics.ODE"
package.defines = { "TRACE" } 
package.config["Debug"].defines = { "DEBUG" }

-- output paths
package.config["Debug"].bindir = "bin/Debug"
package.config["Release"].bindir = "bin/Release"

package.links = { 
	"System", 
	"Axiom.Engine", 
	"Axiom.MathLib", 
	"ode" 
}

package.libpaths = { "../Solution Items" }
package.files = { matchfiles("*.cs"), matchfiles("Collections/*.cs") }
