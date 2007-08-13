
REM ********** Set path for .net framework2.0, sandcastle,hhc,hxcomp****************************

set PATH=c:\WINDOWS\Microsoft.NET\Framework\v2.0.50727;%CD%\..\..\ProductionTools;C:\Program Files\HTML Help Workshop;C:\Program Files\Microsoft Help 2.0 SDK;%PATH%

REM ********** Compile source files ****************************

csc /t:library /doc:comments.xml test.cs
::if there are more than one file, please use [ csc /t:library /doc:comments.xml *.cs ]

REM ********** Call MRefBuilder ****************************

MRefBuilder test.dll /out:reflection.org

REM ********** Apply Transforms ****************************

if {%1} == {vs2005} (
XslTransform /xsl:"..\..\ProductionTransforms\ApplyVSDocModel.xsl" reflection.org /xsl:"..\..\ProductionTransforms\AddFriendlyFilenames.xsl" /out:reflection.xml
 ) else (
 XslTransform /xsl:..\..\ProductionTransforms\ApplyPrototypeDocModel.xsl reflection.org /xsl:..\..\ProductionTransforms\AddGuidFilenames.xsl /out:reflection.xml
 )


XslTransform /xsl:..\..\ProductionTransforms\ReflectionToManifest.xsl  reflection.xml /out:manifest.xml

call ..\..\Presentation\%1\copyOutput.bat

REM ********** Call BuildAssembler ****************************
BuildAssembler /config:..\..\Presentation\%1\configuration\sandcastle.config manifest.xml

 
XslTransform /xsl:..\..\ProductionTransforms\ReflectionToChmProject.xsl reflection.xml /out:Output\test.hhp


REM **************Generate an intermediate Toc file that simulates the Whidbey TOC format.

if {%1} == {vs2005} (
XslTransform /xsl:..\..\ProductionTransforms\createvstoc.xsl reflection.xml /out:toc.xml 
) else (
XslTransform /xsl:..\..\ProductionTransforms\createPrototypetoc.xsl reflection.xml /out:toc.xml 
)

REM ************ Generate CHM help project ******************************

XslTransform /xsl:..\..\ProductionTransforms\TocToChmContents.xsl toc.xml /out:Output\test.hhc


XslTransform /xsl:..\..\ProductionTransforms\ReflectionToChmIndex.xsl reflection.xml /out:Output\test.hhk

hhc output\test.hhp

cd output

if {%1} == {vs2005} (
if exist test_vs2005.chm del test_vs2005.chm
ren test.chm test_vs2005.chm
)else (
if exist test_prototype.chm del test_prototype.chm
ren test.chm test_prototype.chm
)

cd ..


REM ************ Generate HxS help project **************************************

call ..\..\Presentation\%1\copyHavana.bat

XslTransform /xsl:..\..\ProductionTransforms\TocToHxSContents.xsl toc.xml /out:Output\test.HxT

:: If you need to generate hxs, please uncomment the following line. Make sure "Microsoft Help 2.0 SDK" is installed on your machine.
REM: hxcomp.exe -p output\test.hxc

