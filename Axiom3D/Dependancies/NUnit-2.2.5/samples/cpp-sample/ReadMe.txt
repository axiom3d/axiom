Due to an issue that has not been adequately addressed in the 
installation procedure you will have to indicate the directory
where the nunit.framework.dll is located on your disk. 

This problem presents itself by having this program failing to
compile and link. 

Steps:

1.) Right-click on the "cpp-sample" element. Select "Properties" on 
    the context menu. 
    
2.) Select the "C/C++" element in the tree. 

3.) The field that needs to be updated is 
    "Resolve #using references". Update this field to the following
    directory: "C:\Program Files\NUnit V2.0\bin" 
    Note: This directory is the default installation directory 
    if you have chosen a different directory then navigate to it. 
    
5.) Recompile. 

This issue is being worked on and will be fixed in the release. 