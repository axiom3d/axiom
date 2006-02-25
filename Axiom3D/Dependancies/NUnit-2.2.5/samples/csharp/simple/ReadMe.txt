Due to an issue that has not been adequately addressed in the 
installation procedure you will have to refresh the reference
to the nunit.framework.dll. This problem presents itself by 
having the sample programs failing to compile. It will also be 
indicated visually by an yellow icon with an exclamation point. 


Steps:

1.) Remove the existing reference to nunit.framework.dll which has 
    the icon attached to it. 

2.) Right-click on the "References" element. Select 
    "Add Reference...".
    
3.) Hit the "Browse" button on the "Add Reference" dialog box. 

4.) Navigate to the C:\Program Files\NUnit V2.0\bin directory. 
    Select the nunit.framework.dll in this directory and close 
    the dialog box. 
    Note: This directory is the default installation directory 
    if you have chosen a different directory then navigate to it. 
    
5.) Recompile. 

This issue is being worked on and will be fixed in the release. 
