package = newpackage()
package.name = "RenderSystem_DirectX9"
package.language = "c#"
package.kind = "dll"
package.target = "Axiom.RenderSystems.DirectX9"
package.defines = { "TRACE" } 
package.config["Debug"].target = { "DEBUG" }

-- get the target since we need to do something special for SharpDevelop
local i, target = next(options.target, nil)

package.links = { "System", "System.Data", "System.Drawing", "System.Xml", "System.Windows.Forms", "Engine", "MathLib" }

if(target == "sd") then
	tinsert(package.links, "Microsoft.DirectX, Version=1.0.1901.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")
	tinsert(package.links, "Microsoft.DirectX.Direct3D, Version=1.0.1901.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")
	tinsert(package.links, "Microsoft.DirectX.Direct3DX, Version=1.0.1901.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")
else
	tinsert(package.links, "Microsoft.DirectX")
	tinsert(package.links, "Microsoft.DirectX.Direct3D")
	tinsert(package.links, "Microsoft.DirectX.Direct3DX")
end
	
package.files = { 
	matchfiles("*.cs"),
	matchfiles("HLSL/*.cs") 
}