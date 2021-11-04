using FontExtension;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoMSDF.Text;
using System.Diagnostics;

namespace MonoMSDF
{
	/// <summary>
	/// This is the main type for your game.
	/// </summary>
	public class Game1 : Game
	{
		private GraphicsDeviceManager graphics;
		private TextRenderer textRenderer;
		private TextRenderer segoescriptRenderer;
		FieldFont segoescriptFont;
		Stopwatch frameWatch;
		long frameTime = 0;
		long frameTicks = 0;
		float scale = 1;
		int scrolled = 0;
		//Camera
		Vector3 camTarget;
		Vector3 camPosition;
		Matrix projectionMatrix;
		Matrix viewMatrix;
		Matrix worldMatrix;

		public Game1()
		{
			this.graphics = new GraphicsDeviceManager(this)
			{
				PreferredBackBufferWidth = 1280,
				PreferredBackBufferHeight = 720,
				SynchronizeWithVerticalRetrace = false,
				GraphicsProfile = GraphicsProfile.HiDef
			};
			IsFixedTimeStep = false;
			Window.AllowUserResizing = true;
			IsMouseVisible = true;
			this.Content.RootDirectory = "Content";

			graphics.PreparingDeviceSettings += (sender, e) =>
			{
				float w = e.GraphicsDeviceInformation.PresentationParameters.BackBufferWidth;
				float h = e.GraphicsDeviceInformation.PresentationParameters.BackBufferHeight;
				float ratio = w / h;
				const float TARGETW = 1280;
				const float TARGETH = 720;
				const float TARGETRATIO = TARGETW / TARGETH;
				if (ratio < TARGETRATIO)
				{
					//taller
					float mh = TARGETW / ratio;
					float hAdjust = (mh - TARGETH) / 2;
					projectionMatrix = Matrix.CreateOrthographicOffCenter(0, TARGETW, -TARGETH - hAdjust, hAdjust, -200, 200);
				}
				else if (ratio > TARGETRATIO)
				{
					//wider
					float mw = TARGETH * ratio;
					float wAdjust = (mw - TARGETW) / 2;
					projectionMatrix = Matrix.CreateOrthographicOffCenter(-wAdjust, TARGETW + wAdjust, -TARGETH, 0, -200, 200);
				}
				else
				{
					projectionMatrix = Matrix.CreateOrthographicOffCenter(0, TARGETW, -TARGETH, 0, -200, 200);
				}
			};
		}

		protected override void Initialize()
		{
			base.Initialize();
			camTarget = new Vector3(0, 0, 0f);
			camPosition = new Vector3(0, 0, -100f);
			//projectionMatrix = Matrix.CreateOrthographicOffCenter(0, 1280, -720, 0, -200, 200);
			viewMatrix = Matrix.CreateLookAt(camPosition, camTarget, Vector3.Up);
			worldMatrix = Matrix.CreateWorld(camTarget, Vector3.Forward, Vector3.Down);
			frameWatch = new Stopwatch();
		}

		protected override void LoadContent()
		{
			var effect = this.Content.Load<Effect>("FieldFontEffect");
			var font = this.Content.Load<FieldFont>("consola");
			segoescriptFont = this.Content.Load<FieldFont>("segoescript");

			this.textRenderer = new TextRenderer(effect, font, this.GraphicsDevice)
			{
				//LineHeight = 1f,
				//OptimizeForTinyText = true
			};
			this.segoescriptRenderer = new TextRenderer(effect, segoescriptFont, this.GraphicsDevice)
			{
				PositiveYIsDown = true
			};
		}

		protected override void Update(GameTime gameTime)
		{
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
				|| Keyboard.GetState().IsKeyDown(Keys.Escape))
				Exit();

			int scroll = Mouse.GetState().ScrollWheelValue;
			if (scroll > scrolled)
			{
				scale += 0.1f;
			}
			else if (scroll < scrolled)
			{
				scale -= 0.1f;
			}
			scrolled = scroll;
			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			frameWatch.Restart();
			float totalTime = (float)gameTime.TotalGameTime.TotalSeconds;
			this.GraphicsDevice.Clear(Color.CornflowerBlue);

			this.GraphicsDevice.BlendState = BlendState.AlphaBlend;
			this.GraphicsDevice.DepthStencilState = DepthStencilState.None;
			this.GraphicsDevice.RasterizerState = RasterizerState.CullNone;

			var viewport = this.GraphicsDevice.Viewport;

			//var world = Matrix.CreateScale(0.01f) *  Matrix.CreateRotationY((float)gameTime.TotalGameTime.TotalSeconds);
			var world = Matrix.CreateScale(0.01f) * Matrix.Identity;
			var view = Matrix.CreateLookAt(Vector3.Backward, Vector3.Forward, Vector3.Up);
			var projection = Matrix.CreatePerspectiveFieldOfView(
				MathHelper.PiOver2,
				viewport.Width / (float)viewport.Height,
				0.01f,
				1000.0f);

			var wvp = world * view * projection;
			textRenderer.PositiveYIsDown = false;
			textRenderer.WorldViewProjection = wvp;
			this.textRenderer.ResetLayout();
			this.textRenderer.LayoutText("→~!435&^%$", Vector2.Zero, Color.White, 32);
			this.textRenderer.RenderLayoutedText();

			world = Matrix.CreateScale(0.01f) * Matrix.CreateRotationY(totalTime) * Matrix.CreateRotationZ(MathHelper.PiOver4);
			view = Matrix.CreateLookAt(Vector3.Backward, Vector3.Forward, Vector3.Up);
			projection = Matrix.CreatePerspectiveFieldOfView(
				MathHelper.PiOver2,
				viewport.Width / (float)viewport.Height,
				0.01f,
				1000.0f);

			wvp = world * view * projection;
			this.textRenderer.ResetLayout();
			this.textRenderer.LayoutText("          To Infinity And Beyond!", Vector2.Zero, Color.White, 32);
			this.textRenderer.RenderLayoutedText(wvp);
			
			Matrix ortowvp = worldMatrix * viewMatrix * projectionMatrix;

			textRenderer.PositiveYIsDown = true;
			textRenderer.WorldViewProjection = ortowvp;
			segoescriptRenderer.WorldViewProjection = ortowvp;
			this.textRenderer.ResetLayout();
			this.textRenderer.LayoutText("ORTOGRAPHIC!", new Vector2(0, 0), Color.Yellow, 32);
			this.textRenderer.LayoutText("Text at 2x scale.", new Vector2(0, 32), Color.Red, 64f);
			this.textRenderer.LayoutText("Text at half scale?", new Vector2(0, 96), Color.Green, 16f);
			this.textRenderer.LayoutText("ÑNCâarP", new Vector2(0, 112), Color.Gold, 32);
			this.textRenderer.LayoutText("Text with kerning: AWAY", new Vector2(0, 144), Color.Gold, 32);
			textRenderer.EnableKerning = false;
			this.textRenderer.LayoutText("Text without kerning: AWAY\n With You", new Vector2(0, 172), Color.Gold, 32);
			textRenderer.EnableKerning = true;
			this.textRenderer.LayoutText($"Hære's something. Comma: ,", new Vector2(0, 720-128), Color.Black, 32);
			this.textRenderer.LayoutText($"Frame time: {frameTicks} ticks\nFrame time: {frameTime}ms\nThird line", new Vector2(0, 720-256), Color.Gold, 64);
			this.textRenderer.LayoutText($"Running for {gameTime.TotalGameTime.TotalSeconds} seconds", new Vector2(0, 720-32), Color.Gold, 32);
			this.textRenderer.RenderLayoutedText();

			this.segoescriptRenderer.ResetLayout();
			string cursorText = $"AWAY\nThis is scale {scale}";
			Vector2 ctMeasure = segoescriptFont.MeasureString(cursorText) * scale * 32;
			this.segoescriptRenderer.LayoutText(cursorText, Mouse.GetState().Position.ToVector2() - ctMeasure / 2, Color.Black, scale * 32, totalTime, ctMeasure / 2);
			this.segoescriptRenderer.RenderLayoutedText();

			frameTicks = frameWatch.ElapsedTicks;
			frameTime = frameWatch.ElapsedMilliseconds;
			frameWatch.Stop();
		}
	}
}
