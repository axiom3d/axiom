using Microsoft.Xna.Framework.Graphics;

namespace Axiom.Core
{
#if !WINDOWS_PHONE
	public static class ExtensionMethods
	{
		public static PixelShader PixelShader( this Effect effect )
		{
			return CustomEffect.PixelShaderGet(effect);
		}

		public static void PixelShader( this Effect effect, PixelShader value )
		{
			CustomEffect.PixelShaderSet(effect, value);
		}

		public static VertexShader VertexShader( this Effect effect )
		{
			return CustomEffect.VertexShaderGet(effect);
		}

		public static void VertexShader( this Effect effect, VertexShader value )
		{
			CustomEffect.VertexShaderSet(effect, value);
		}
	}

	public class CustomEffect : Effect
	{
		internal static readonly Class<Effect>.Getter<PixelShader> PixelShaderGet =
			Class<Effect>.FieldGet<PixelShader>("PixelShader");

		internal static readonly Class<Effect>.Setter<PixelShader> PixelShaderSet =
			Class<Effect>.FieldSet<PixelShader>("PixelShader");

		internal static readonly Class<Effect>.Getter<VertexShader> VertexShaderGet =
			Class<Effect>.FieldGet<VertexShader>("VertexShader");

		internal static readonly Class<Effect>.Setter<VertexShader> VertexShaderSet =
			Class<Effect>.FieldSet<VertexShader>("VertexShader");

		public CustomEffect()
		{
		}

		public CustomEffect(params EffectTechnique[] techniques)
			: base(techniques)
		{
		}

		public PixelShader PixelShader
		{
			get
			{
				return PixelShaderGet(this);
			}
			set
			{
				PixelShaderSet(this, value);
			}
		}

		public VertexShader VertexShader
		{
			get
			{
				return VertexShaderGet(this);
			}
			set
			{
				VertexShaderSet(this, value);
			}
		}
	}
#endif
}