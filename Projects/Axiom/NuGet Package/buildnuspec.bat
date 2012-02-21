@echo off
SET PATH=..\..\..\Tools\Nuget\;%PATH%
path
mkdir ..\..\dist\nuspec

nuget pack Axiom.nuspec -OutputDirectory ..\..\dist\nuspec
nuget pack Axiom.Platforms.Win32.nuspec -OutputDirectory ..\..\dist\nuspec
nuget pack Axiom.RenderSystems.DirectX9.nuspec -OutputDirectory ..\..\dist\nuspec
nuget pack Axiom.RenderSystems.Xna.nuspec -OutputDirectory ..\..\dist\nuspec
nuget pack Axiom.Plugins.FreeImageCodecs.nuspec -OutputDirectory ..\..\dist\nuspec
nuget pack Axiom.Plugins.ParticleFX.nuspec -OutputDirectory ..\..\dist\nuspec
nuget pack Axiom.Plugins.SystemDrawingCodecs.nuspec -OutputDirectory ..\..\dist\nuspec
nuget pack Axiom.SceneManagers.Octree.nuspec -OutputDirectory ..\..\dist\nuspec
nuget pack Axiom.SceneManagers.PortalConnected.nuspec -OutputDirectory ..\..\dist\nuspec
nuget pack Axiom.SceneManagers.Bsp.nuspec -OutputDirectory ..\..\dist\nuspec

pause
