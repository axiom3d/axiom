@ECHO OFF

REM Step 4 - Generate a packaged table of content for an HTML 2.x help file
"{@SandcastlePath}ProductionTools\XslTransform" /xsl:"{@SandcastlePath}ProductionTransforms\ReflectionToHxSContents.xsl" reflection.xml /out:"{@HTMLHelpName}.HxT"
