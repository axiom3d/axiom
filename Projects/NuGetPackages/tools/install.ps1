param($installPath, $toolsPath, $package, $project)

Get-ChildItem "$installPath\native\x86\*.dll" `
	|% { $project.ProjectItems.AddFromFile( $_ ); } `
	|% { $_.Properties.Item("CopyToOutputDirectory").Value = 2; }