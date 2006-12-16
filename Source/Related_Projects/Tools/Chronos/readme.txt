Patching Axiom
---------------
See the patches directory for .patch files to apply to the Axiom codebase.

Common problems
---------------
- You might get errors about loading duplicate materials. This is a known issue, and the material management is under overhaul. You can try to resolve the issue yourself (the .pat file contains a partial fix that should work for most people), or wait for us to do so. :)

- You get an error loading a .ttf file. This is caused by Ogre overlay files not parsing correctly in Axiom. Remove your overlay files from your media directory for the time being.

License
---------------
Unless otherwise noted, all files in this project are distributed under the LGPL. You should have a copy of the LGPL in the license.txt file in the root of the project directory.

Patches, plugins, bugfixes, and SVN
--------------------------
If you would like to submit patches or enhancements, by all means, please feel free! You can submit a patch file by sending it (and a description of what it does!) to chronos@digitalsentience.com, or post in the Axiom or Ogre message boards, and I'll most likely pick it up. If you write your own plugin and would like it included with the source tree, email me at chronos@digitalsentience.com and we'll see about getting you set up with SVN write access for your plugin.
