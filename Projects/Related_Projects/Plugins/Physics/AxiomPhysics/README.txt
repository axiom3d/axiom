AxiomPhysics
------------

This is a first pack of sourcefiles, mediafiles and a modified ode wrapper to demonstrate
how physics could be done in axiom.

The main target was to build a ogretok like software interface for the creation of physical
objects and their simulation inside axiom. The main difference to ogretok is that it is based
on tokamak and this work is based on ode. So, some things are different.

Thanks go to the ode people. The simulations are a direct port of their simulations and 
you'll find some of their mediafiles here, too.

INSTALL
-------
This comes as a zip and not as a patch. This is because of the ode.dll and media files.
Extract the zip into a fresh axiom checkout. Link to the new ode.dll! Everywhere it is
used. This is important! I thing there are at least two such links in the last Axiom CVS
checkout! And now you should find a modified a one new physics demo in the demo browser.

DEMO
----
The demo browsers physics demo is changed to show some more physics effects: You'll 
see the well known boxes. New: You'll find a playground surrounded by walls, some static
boxes, a moveable sphere, a static ramp to have some jumps with your boxes, a box stack,
two meshes are falling from the sky and you can feel the different surface when moving
your boxes over the ground. There are some things you'll notify when playing with the
obstacles: Meshes fall through planes! There seem to be some problems with mesh to mesh
collision! This demo is more or less a "snapshot of a physics test area" than it is a 
demo ...

And the end of the demo browser you'll find one more physics demo. This is a port of the
ogretok demo with 10 simulations: Use key 1 to 0 to switch between the sims. Use left
mouse to fire bullet. Use F1/F2/F3/f4/F5 to change bullet mass, switch shadows and change
gravity. This is were you should start playing around! Notify the breakage message when
using the breakable sim (#7). AND THE RAGDOLLS ARE HERE, TOO!!!! That's it for now.

THINGS TO KNOW
--------------
- Breakable Joints are implemented as a first release. They look different than the 
  ogretok joints that implements "breakability". That's because the difference between 
  ode and tokamak.
- Damping is implemented, but I'm not sure if it behaves correct ...
- Joint limits are missing. So, demos using joints behave a little bit different than
  the corresponding ogretok sims.
- I sometimes get null pointer exceptions when closing my apps. :-(
- Callbacks are different.

ARCHITECTURAL THOUGHTS
----------------------
There's (at least) one part, where I dont't like the source code I produced and that should be worked over:
You'll get IPhysicalObjects back when creating static Objects, IJoints when creating Joints
and you'll get DynamicObjects back when creating dynamic objects. So, sometimes there will be
classes and sometimes interfaces returned. I guess that could be done better. But I think to 
be able to return an IDynamicObject there must be an IGameObject first to base the IDynamicObject
on. Leed's comment about that?

FEEDBACK IS APPRECIATED!!!

Hope, you'll get that stuff up and running. And I hope you'll have some fun with the sims!
Thanks to the ogretok guys again!