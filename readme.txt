Axiom 3D Rendering Engine
-------------------------

Table of Contents
1) What Is Axiom
2) Installation

1) What is Axiom
   Axiom is a fully managed rendering engine written in C#. It abstracts the underlying operating system rending API's like DirectX, XNA and OpenGL into a common resuable extesible API. Although not a complete game engine, it does include a few subsystems like Scene Management, Particle Systems, Overlays ( GUI ), Lighting and Shadows.

2) Installation
   Installing the Axiom SDK is very simple. Just unzip the archive into a directory of your choice. 
   
   a) Depending upon your operating system and managed framework, there are a few additional dependencies you will need.
   
      On Windows 
        using DirectX
          SlimDX - http://www.slimdx.org
        Using Xna
          Xna Game Studio - http://creators.xna.com
      
      On Linux 
        You will need to use your appropriate distributions software installation tool to install the following dependencies:
        mono - http://mono-project.com
        lib-devil - http://openil.sourceforge.net/
        NVidia Cg Toolkit - http://developer.nvidia.com/object/cg_toolkit.html        
    
   b) unzip the package to a directory.
   c) run the Axiom.Demos.Browser.Winforms.exe
      On Windows, double click the file in Windows Explorer under Samples/Win32
      On Linux Open a Terminal Window, cd to the directory you unzipped the package + Samples/linux and run 'mono Axiom.Demos.Browser.Winforms.exe'