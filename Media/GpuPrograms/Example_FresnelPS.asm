ps.1.4 
  // conversion from Cg generated ARB_fragment_program to ps.1.4 by NFZ 
  // c0 : distortionRange 
  // c1 : tintColour 
  // testure 0 : noiseMap 
  // texture 1 : reflectMap 
  // texture 2 : refractMap 
  // v0.x : fresnel 
  // t0.xyz : noiseCoord 
  // t1.xyw : projectionCoord 


  // sample noise map using noiseCoord in TEX unit 0 
texld r0, t0 

  // get projected texture coordinates from TEX coord 1 
  // will be used in phase 2 

texcrd r1.xy, t1_dw.xyw 

  // for ps.1.4 have to re-arrange things a bit to perturb projected texture coordinates 

mov r0.yz, r0.x  // work around for OpenGL DDs 3d volume texture 
mad r0.xy, r0_bx2, c0.x, r1 

phase 

  // sampe reflectMap using dependant read : texunit 1 
texld r1, r0 

  // sample refractMap : texunit 2 

texld r2, r0 

  // adding tintColour that is in global c1 

add r2, r2, c1 

  // use linear interp to fake fresnel effect 
lrp r0, v0.x, r1, r2 







































































