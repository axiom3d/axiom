package.name = "PlatformManager_Win32"
package.language = "c#"
package.kind = "dll"
package.target = "Axiom.Platforms.Win32"
package.links = { "System", "System.Drawing", "System.Windows.Forms", "Engine" }

-- get the target since we need to do something special for SharpDevelop
local i, target = next(options.target, nil)

if(target == "sd") then
	tinsert(package.links, "Microsoft.DirectX, Version=1.0.1901.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")
	tinsert(package.links, "Microsoft.DirectX.DirectInput, Version=1.0.1901.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")
else
	tinsert(package.links, "Microsoft.DirectX")
	tinsert(package.links, "Microsoft.DirectX.DirectInput")
end

package.files = { matchfiles("*.cs") }
