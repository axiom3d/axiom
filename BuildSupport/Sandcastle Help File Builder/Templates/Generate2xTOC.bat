@ECHO OFF

REM Step 4 - Generate a packaged table of content for an HTML 2.x help file
if {%1} == {vs2005} (
"{@SandcastlePath}ProductionTools\XslTransform" /xsl:"{@SandcastlePath}ProductionTransforms\CreateVSToc.xsl" reflection.xml /out:toc.xml
) else (
"{@SandcastlePath}ProductionTools\XslTransform" /xsl:"{@SandcastlePath}ProductionTransforms\CreatePrototypeToc.xsl" reflection.xml /out:toc.xml
)

"{@SandcastlePath}ProductionTools\XslTransform" /xsl:"{@SandcastlePath}ProductionTransforms\TocToHxSContents.xsl" toc.xml /out:"{@HTMLHelpName}.HxT"
