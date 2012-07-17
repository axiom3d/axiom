Examples - Readme
=================

The examples describe integration scenarios with various project types.

To run any of them, open a command prompt and change directory into the 
folder that contains the example.  Then run the "Build.bat" file
located in the root of the MetaBuild distribution.

eg. > cd MetaBuild\examples\Solution
    > ..\..\Build.bat    

Each example will then produce output in the MetaBuild\build folder
(by default).  You may control other settings by passing options to
Build.bat.

eg. > ..\..\Build.bat /builddir c:\Temp

To view other options type:

eg. > ..\..\Build.bat /?

Note: All examples require .Net 3.5 to be installed since that is a
      prerequisite for MetaBuld.  Others may require additional components
      and will not work otherwise.

Summary
-------

* Solution:
  A simple module that demonstrates how to build a solution and
  gather its output in one place using the Solution and File items.
