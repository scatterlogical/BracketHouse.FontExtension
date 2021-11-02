using System.Collections.Generic;
using System.IO;
using System.Linq;
using FontExtension;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoMSDF.Text
{
	public sealed class TextRenderer
	{
		private const string LargeTextTechnique = "LargeText";
		private const string SmallTextTechnique = "SmallText";

		private readonly Effect Effect;
		private readonly FieldFont Font;
		private readonly Texture2D AtlasTexture;
		private readonly GraphicsDevice Device;

		public TextRenderer(Effect effect, FieldFont font, GraphicsDevice device)
		{
			this.Effect = effect;
			this.Font = font;
			this.Device = device;
			using (var stream = new MemoryStream(font.Bitmap))
			{
				this.AtlasTexture = Texture2D.FromStream(this.Device, stream);
			}

			this.EnableKerning = true;
			this.OptimizeForTinyText = false;
			this.PositiveYIsDown = false;
			this.PositionByBaseline = false;
		}

		public bool EnableKerning { get; set; }
		/// <summary>
		/// Disables text anti-aliasing which might cause blurry text when the text is rendered tiny
		/// </summary>
		public bool OptimizeForTinyText { get; set; }
		/// <summary>
		/// Flip the Y axis, so that positive Y is down. It is up to you to provide a wvp matrix where that makes sense.
		/// </summary>
		public bool PositiveYIsDown { get; set; }
		/// <summary>
		/// Position text by the baseline, instead of by the top.
		/// </summary>
		public bool PositionByBaseline { get; set; }
		/// <summary>
		/// Lineheight. If 0 or less, the lineheight from the font is used.
		/// </summary>
		public float LineHeight { get; set; } = 0;
		/// <summary>
		/// WorldViewProjection Matrix to use during rendering.
		/// </summary>
		public Matrix WorldViewProjection { get; set; }

		public void Render(string text, Vector2 position, Color color, float scale = 16)
		{
			if (string.IsNullOrEmpty(text))
			{
				return;
			}

			var textureWidth = AtlasTexture.Width;
			var textureHeight = AtlasTexture.Height;

			this.Effect.Parameters["WorldViewProjection"].SetValue(WorldViewProjection);
			this.Effect.Parameters["PxRange"].SetValue(this.Font.PxRange);
			this.Effect.Parameters["TextureSize"].SetValue(new Vector2(textureWidth, textureHeight));
			this.Effect.Parameters["GlyphTexture"].SetValue(AtlasTexture);
			this.Effect.CurrentTechnique.Passes[0].Apply();

			if (this.OptimizeForTinyText)
			{
				this.Effect.CurrentTechnique = this.Effect.Techniques[SmallTextTechnique];
			}
			else
			{
				this.Effect.CurrentTechnique = this.Effect.Techniques[LargeTextTechnique];
			}

			int yFlip = PositiveYIsDown ? -1 : 1;

			float scaledLineheight = this.LineHeight <= 0 ? Font.LineHeight * scale : this.LineHeight * scale;
			Vector2 penStart = position;
			if (!PositionByBaseline)
			{
				penStart.Y += scale * Font.Ascender * yFlip;
			}
			Vector2 pen = penStart;
			VertexPositionColorTexture[] verts = new VertexPositionColorTexture[text.Length * 4];
			int[] indices = new int[text.Length * 6];
			int glyphQuads = 0;
			verts[0].Position.X = 3;
			for (var i = 0; i < text.Length; i++)
			{
				FieldGlyph current = Font.GetGlyph(text[i]);

				if (!char.IsWhiteSpace(text[i]))
				{
					float left = pen.X + current.PlaneLeft * scale;
					float right = pen.X + current.PlaneRight * scale;
					float top = pen.Y - current.PlaneTop * scale * yFlip;
					float bottom = pen.Y - current.PlaneBottom * scale * yFlip;

					verts[glyphQuads * 4 + 0].Position.X = right;
					verts[glyphQuads * 4 + 0].Position.Y = bottom;

					verts[glyphQuads * 4 + 1].Position.X = left;
					verts[glyphQuads * 4 + 1].Position.Y = bottom;

					verts[glyphQuads * 4 + 2].Position.X = left;
					verts[glyphQuads * 4 + 2].Position.Y = top;

					verts[glyphQuads * 4 + 3].Position.X = right;
					verts[glyphQuads * 4 + 3].Position.Y = top;

					verts[glyphQuads * 4 + 0].TextureCoordinate.X = current.AtlasRight;
					verts[glyphQuads * 4 + 0].TextureCoordinate.Y = current.AtlasBottom;

					verts[glyphQuads * 4 + 1].TextureCoordinate.X = current.AtlasLeft;
					verts[glyphQuads * 4 + 1].TextureCoordinate.Y = current.AtlasBottom;

					verts[glyphQuads * 4 + 2].TextureCoordinate.X = current.AtlasLeft;
					verts[glyphQuads * 4 + 2].TextureCoordinate.Y = current.AtlasTop;

					verts[glyphQuads * 4 + 3].TextureCoordinate.X = current.AtlasRight;
					verts[glyphQuads * 4 + 3].TextureCoordinate.Y = current.AtlasTop;

					verts[glyphQuads * 4 + 0].Color = color;
					verts[glyphQuads * 4 + 1].Color = color;
					verts[glyphQuads * 4 + 2].Color = color;
					verts[glyphQuads * 4 + 3].Color = color;

					indices[glyphQuads * 6 + 0] = glyphQuads * 4 + 0;
					indices[glyphQuads * 6 + 1] = glyphQuads * 4 + 1;
					indices[glyphQuads * 6 + 2] = glyphQuads * 4 + 2;
					indices[glyphQuads * 6 + 3] = glyphQuads * 4 + 2;
					indices[glyphQuads * 6 + 4] = glyphQuads * 4 + 3;
					indices[glyphQuads * 6 + 5] = glyphQuads * 4 + 0;

					glyphQuads++;
				}

				pen.X += current.Advance * scale;

				if (this.EnableKerning && i < text.Length - 1)
				{
					if (Font.Kerning.TryGetValue((text[i], text[i + 1]), out float kern))
					{
						pen.X += kern * scale;
					}
				}
				if (text[i] == '\n')
				{
					pen.X = penStart.X;
					pen.Y -= scaledLineheight * yFlip;
				}
			}
			//DRAW
			Device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, verts, 0, glyphQuads * 4, indices, 0, glyphQuads * 2);
		}
	}
}
