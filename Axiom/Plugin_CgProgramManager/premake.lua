package = newpackage()
package.name = "Plugin_CgProgramManager"
package.language = "c#"
package.kind = "dll"
package.target = "Axiom.CgPrograms"
package.defines = { "TRACE" } 
package.config["Debug"].defines = { "DEBUG" }
package.libpaths = { "../Solution Items" }
package.links = { "System", "Engine", "Tao.Cg" }
package.files = { matchfiles("*.cs") }