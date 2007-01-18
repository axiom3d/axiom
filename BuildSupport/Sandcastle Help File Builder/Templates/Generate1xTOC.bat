@ECHO OFF

REM Step 4 - Generate a table of content for an HTML 1.x help file
"{@SandcastlePath}ProductionTools\XslTransform" /xsl:"{@SandcastlePath}ProductionTransforms\ReflectionToChmContents.xsl" reflection.xml /arg:html="Output\html" /out:"{@HTMLHelpName}.hhc"
