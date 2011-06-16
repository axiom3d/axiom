﻿using System;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

namespace Axiom.Demos.Minimal._2010
{
    public sealed class DemoWindow 
    {
        private readonly Root _root;
        private SceneManager _scene;
        private PatchMesh _patch;
        private Entity _patchEntity;
        private Camera _camera;

        private struct PatchVertex
        {
            public float X, Y, Z;
            public float Nx, Ny, Nz;
            public float U, V;
        }

        public DemoWindow(Root root)
        {
            _root = root;
        }

        public void OnLoad()
        {
            // Create patch with positions, normals, and 1 set of texcoords
            var patchDeclaration = HardwareBufferManager.Instance.CreateVertexDeclaration();
            patchDeclaration.AddElement(0, 0, VertexElementType.Float3, VertexElementSemantic.Position);
            patchDeclaration.AddElement(0, 12, VertexElementType.Float3, VertexElementSemantic.Normal);
            patchDeclaration.AddElement(0, 24, VertexElementType.Float2, VertexElementSemantic.TexCoords, 0);

            var patchVertices = new PatchVertex[9];

            patchVertices[0].X = -500;
            patchVertices[0].Y = 200;
            patchVertices[0].Z = -500;
            patchVertices[0].Nx = -0.5f;
            patchVertices[0].Ny = 0.5f;
            patchVertices[0].Nz = 0;
            patchVertices[0].U = 0;
            patchVertices[0].V = 0;

            patchVertices[1].X = 0;
            patchVertices[1].Y = 500;
            patchVertices[1].Z = -750;
            patchVertices[1].Nx = 0;
            patchVertices[1].Ny = 0.5f;
            patchVertices[1].Nz = 0;
            patchVertices[1].U = 0.5f;
            patchVertices[1].V = 0;

            patchVertices[2].X = 500;
            patchVertices[2].Y = 1000;
            patchVertices[2].Z = -500;
            patchVertices[2].Nx = 0.5f;
            patchVertices[2].Ny = 0.5f;
            patchVertices[2].Nz = 0;
            patchVertices[2].U = 1;
            patchVertices[2].V = 0;

            patchVertices[3].X = -500;
            patchVertices[3].Y = 0;
            patchVertices[3].Z = 0;
            patchVertices[3].Nx = -0.5f;
            patchVertices[3].Ny = 0.5f;
            patchVertices[3].Nz = 0;
            patchVertices[3].U = 0;
            patchVertices[3].V = 0.5f;

            patchVertices[4].X = 0;
            patchVertices[4].Y = 500;
            patchVertices[4].Z = 0;
            patchVertices[4].Nx = 0;
            patchVertices[4].Ny = 0.5f;
            patchVertices[4].Nz = 0;
            patchVertices[4].U = 0.5f;
            patchVertices[4].V = 0.5f;

            patchVertices[5].X = 500;
            patchVertices[5].Y = -50;
            patchVertices[5].Z = 0;
            patchVertices[5].Nx = 0.5f;
            patchVertices[5].Ny = 0.5f;
            patchVertices[5].Nz = 0;
            patchVertices[5].U = 1;
            patchVertices[5].V = 0.5f;

            patchVertices[6].X = -500;
            patchVertices[6].Y = 0;
            patchVertices[6].Z = 500;
            patchVertices[6].Nx = -0.5f;
            patchVertices[6].Ny = 0.5f;
            patchVertices[6].Nz = 0;
            patchVertices[6].U = 0;
            patchVertices[6].V = 1;

            patchVertices[7].X = 0;
            patchVertices[7].Y = 500;
            patchVertices[7].Z = 500;
            patchVertices[7].Nx = 0;
            patchVertices[7].Ny = 0.5f;
            patchVertices[7].Nz = 0;
            patchVertices[7].U = 0.5f;
            patchVertices[7].V = 1;

            patchVertices[8].X = 500;
            patchVertices[8].Y = 200;
            patchVertices[8].Z = 800;
            patchVertices[8].Nx = 0.5f;
            patchVertices[8].Ny = 0.5f;
            patchVertices[8].Nz = 0;
            patchVertices[8].U = 1;
            patchVertices[8].V = 1;

            _patch = MeshManager.Instance.CreateBezierPatch("Bezier1", ResourceGroupManager.DefaultResourceGroupName, patchVertices, patchDeclaration, 3, 3, 5, 5, VisibleSide.Both, BufferUsage.StaticWriteOnly, BufferUsage.DynamicWriteOnly, true, true);
            _patch.Subdivision = 1.0f;

            _scene = _root.CreateSceneManager("DefaultSceneManager", "SLSharpInstance");
            _scene.ClearScene();

            _patchEntity = _scene.CreateEntity("Entity1", "Bezier1");
            _camera = _scene.CreateCamera("MainCamera");



            var mat = (Material)MaterialManager.Instance.Create("test", ResourceGroupManager.DefaultResourceGroupName);
            var pass = mat.GetTechnique(0).GetPass(0);
            pass.LightingEnabled = false;


            _patchEntity.MaterialName = mat.Name;

            _scene.RootSceneNode.AttachObject(_patchEntity);

            _camera.LookAt(Vector3.Zero);
            _camera.Near = 5;
            _camera.AutoAspectRatio = true;

            var vp = _root.AutoWindow.AddViewport(_camera, 0, 0, 1.0f, 1.0f, 100);
            vp.BackgroundColor = ColorEx.CornflowerBlue;

        }

        public void OnUnload()
        {
        }

        public void OnRenderFrame(object s, FrameEventArgs e)
        {
            var angle = Utility.DegreesToRadians((DateTime.Now.Millisecond / 1000.0f + DateTime.Now.Second) * 6 * 4);
            var cam = new Vector3((float)System.Math.Sin(angle), 0.0f, (float)System.Math.Cos(angle));
            var look = new Vector3(0, 300, 0);

            cam *= 1600.0f;
            cam.y += 100.0f;
            _camera.LookAt(look);
            _camera.Position = cam + look;
        }
      
    }
}
