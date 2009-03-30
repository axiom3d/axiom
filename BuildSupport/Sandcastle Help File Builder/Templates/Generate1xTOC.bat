@ECHO OFF

REM Step 4 - Generate a table of content for an HTML 1.x help file
if {%1} == {vs2005} (
"{@SandcastlePath}ProductionTools\XslTransform" /xsl:"{@SandcastlePath}ProductionTransforms\CreateVSToc.xsl" reflection.xml /out:toc.xml
) else (
"{@SandcastlePath}ProductionTools\XslTransform" /xsl:"{@SandcastlePath}ProductionTransforms\CreatePrototypeToc.xsl" reflection.xml /out:toc.xml
)

"{@SandcastlePath}ProductionTools\XslTransform" /xsl:"{@SandcastlePath}ProductionTransforms\TocToChmContents.xsl" toc.xml /out:"{@HTMLHelpName}.hhc"
