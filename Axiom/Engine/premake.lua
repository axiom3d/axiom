package.name = "Engine"
package.language = "c#"
package.kind = "dll"
package.buildflags = { "unsafe" }
package.defines = { "TRACE" } 
package.config["Debug"].target = { "DEBUG" }
package.target = "Axiom"
package.libpaths = { "../Solution Items" }
package.links = { "System", "System.Data", "System.Xml", "System.Drawing", "System.Windows.Forms", "System.XML", "MathLib", "ICSharpCode.SharpZipLib" }

-- insert all the various folders into the file list
tinsert(package.files, matchfiles("*.cs"))
tinsert(package.files, matchfiles("Animating/*.cs"))
tinsert(package.files, matchfiles("Audio/*.cs"))
tinsert(package.files, matchfiles("Collections/*.cs"))
tinsert(package.files, matchfiles("Configuration/*.cs"))
tinsert(package.files, matchfiles("Controllers/*.cs"))
tinsert(package.files, matchfiles("Controllers/Canned/*.cs"))
tinsert(package.files, matchfiles("Core/*.cs"))
tinsert(package.files, matchfiles("EventSystem/*.cs"))
tinsert(package.files, matchfiles("Exceptions/*.cs"))
tinsert(package.files, matchfiles("FileSystem/*.cs"))
tinsert(package.files, matchfiles("Fonts/*.cs"))
tinsert(package.files, matchfiles("Graphics/*.cs"))
tinsert(package.files, matchfiles("Gui/*.cs"))
tinsert(package.files, matchfiles("Input/*.cs"))
tinsert(package.files, matchfiles("Media/*.cs"))
tinsert(package.files, matchfiles("ParticleSystems/*.cs"))
tinsert(package.files, matchfiles("Physics/*.cs"))
tinsert(package.files, matchfiles("Scripting/*.cs"))
tinsert(package.files, matchfiles("Serialization/*.cs"))
tinsert(package.files, matchfiles("Text/*.cs"))
tinsert(package.files, matchfiles("Utility/*.cs"))