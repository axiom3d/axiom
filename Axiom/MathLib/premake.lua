package.name = "Axiom.MathLib"
package.language = "c#"
package.kind = "dll"
package.buildflags = { "unsafe" }
package.defines = { "TRACE" } 
package.config["Debug"].defines = { "DEBUG" }

-- output paths
package.config["Debug"].bindir = "bin/Debug"
package.config["Release"].bindir = "bin/Release"

package.target = "Axiom.MathLib"
package.links = { "System" }

package.files = { 
	matchfiles("*.cs"), 
	matchfiles("Collections/*.cs") 
}
