using System;
using Microsoft.DirectX.Direct3D;
using D3D = Microsoft.DirectX.Direct3D;
using Axiom.SubSystems.Rendering;

namespace RenderSystem_DirectX9
{
	/// <summary>
	/// 	Summary description for D3DVertexDeclaration.
	/// </summary>
	public class D3DVertexDeclaration : Axiom.SubSystems.Rendering.VertexDeclaration
	{
		#region Member variables

			protected D3D.Device device;
			protected D3D.VertexDeclaration d3dVertexDecl;
			protected bool needsRebuild;

		#endregion
		
		#region Constructors
		
		public D3DVertexDeclaration(D3D.Device device)
		{
			this.device = device;
		}
		
		#endregion
		
		#region Methods

		public override void AddElement(Axiom.SubSystems.Rendering.VertexElement element)
		{
			base.AddElement (element);

			needsRebuild = true;
		}

		#endregion
		
		#region Properties

		/// <summary>
		/// 
		/// </summary>
		/// DOC
		public D3D.VertexDeclaration D3DVertexDecl
		{
			get 
			{
				// rebuild declaration if things have changed
				if(needsRebuild)
				{
					if(d3dVertexDecl != null)
						d3dVertexDecl.Dispose();

					// create elements array
					D3D.VertexElement[] d3dElements = new D3D.VertexElement[elements.Count + 1];
	
					// loop through and configure each element for D3D
					for(int i = 0; i < elements.Count; i++)
					{
						Axiom.SubSystems.Rendering.VertexElement element = 
							(Axiom.SubSystems.Rendering.VertexElement)elements[i];

						d3dElements[i].DeclarationMethod = D3D.DeclarationMethod.Default;
						d3dElements[i].Offset = (short)element.Offset;
						d3dElements[i].Stream = (short)element.Source;
						d3dElements[i].DeclarationType = D3DHelper.ConvertEnum(element.Type);
						d3dElements[i].DeclarationUsage = D3DHelper.ConvertEnum(element.Semantic);

						// set usage index explicitly for diffuse and specular, use index for the rest (i.e. texture coord sets)
						switch(element.Semantic)
						{
							case VertexElementSemantic.Diffuse:
								d3dElements[i].UsageIndex = 0;
								break;

							case VertexElementSemantic.Specular:
								d3dElements[i].UsageIndex = 1;
								break;

							default:
								d3dElements[i].UsageIndex = (byte)element.Index;
								break;
						} //  switch

					} // for

					// configure the last element to be the end
					d3dElements[elements.Count] = D3D.VertexElement.VertexDeclarationEnd;

					// create the new declaration
					d3dVertexDecl = new D3D.VertexDeclaration(device, d3dElements);

					// reset the flag
					needsRebuild = false;

				}

				return d3dVertexDecl; 
			}
		}
		
		#endregion

	}
}
