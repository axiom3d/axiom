@rem Performs a nightly build in debug mode and prepares a zip file for distribution
..\..\..\..\BuildSupport\nAnt\bin\NAnt.exe -D:project.release.type=nightly debug dist 
