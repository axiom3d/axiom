@ECHO OFF

REM Step 2 - Transform the reflection output
"{@SandcastlePath}ProductionTools\XslTransform" /xsl:"{@SandcastlePath}ProductionTransforms\AddOverloads.xsl","{@SandcastlePath}ProductionTransforms\AddGuidFilenames.xsl" reflection.org /out:reflection.xml

REM Generate a topic manifest
"{@SandcastlePath}ProductionTools\XslTransform" /xsl:"{@SandcastlePath}ProductionTransforms\ReflectionToManifest.xsl" reflection.xml /out:manifest.xml
