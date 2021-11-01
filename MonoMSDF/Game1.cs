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
		Stopwatch frameWatch;
		long frameTime = 0;
		long frameTicks = 0;
		float scale = 1;
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
			var font = this.Content.Load<FieldFont>("marker");

			this.textRenderer = new TextRenderer(effect, font, this.GraphicsDevice)
			{
				PositionByTop = true,
				LineHeight = 1f,
				//OptimizeForTinyText = true
			};
		}

		protected override void Update(GameTime gameTime)
		{
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
				|| Keyboard.GetState().IsKeyDown(Keys.Escape))
				Exit();

			scale = 1 + Mouse.GetState().ScrollWheelValue / 100f;

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			frameWatch.Restart();
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
			textRenderer.ForegroundColor = Color.White;
			textRenderer.PositiveYIsDown = false;
			this.textRenderer.Render("→~!435&^%$", wvp);


			world = Matrix.CreateScale(0.01f) * Matrix.CreateRotationY((float)gameTime.TotalGameTime.TotalSeconds) * Matrix.CreateRotationZ(MathHelper.PiOver4);
			view = Matrix.CreateLookAt(Vector3.Backward, Vector3.Forward, Vector3.Up);
			projection = Matrix.CreatePerspectiveFieldOfView(
				MathHelper.PiOver2,
				viewport.Width / (float)viewport.Height,
				0.01f,
				1000.0f);

			wvp = world * view * projection;

			this.textRenderer.Render("          To Infinity And Beyond!", wvp);
			Matrix ortowvp = worldMatrix * viewMatrix * projectionMatrix;

			textRenderer.ForegroundColor = Color.Yellow;
			textRenderer.PositiveYIsDown = true;
			this.textRenderer.Render("ORTOGRAPHIC!", ortowvp, new Vector2(0, 0));
			textRenderer.ForegroundColor = Color.Red;
			this.textRenderer.Render("Text at 2x scale.", ortowvp, new Vector2(0, 32), 2f);
			textRenderer.ForegroundColor = Color.Green;
			this.textRenderer.Render("Text at half scale?", ortowvp, new Vector2(0, 96), 0.5f);
			textRenderer.ForegroundColor = Color.Gold;
			this.textRenderer.Render("ÑNCâarP", ortowvp, new Vector2(0, 112));
			this.textRenderer.Render("AWAY", ortowvp, new Vector2(0, 144));
			textRenderer.EnableKerning = false;
			this.textRenderer.Render("AWAY\n With You", ortowvp, new Vector2(0, 172));
			textRenderer.EnableKerning = true;
			this.textRenderer.Render($"Hære's something. Comma: ,", ortowvp, new Vector2(0, 720-128), 1.3f);
			this.textRenderer.Render($"Frame time: {frameTicks} ticks\nFrame time: {frameTime}ms\nThird line", ortowvp, new Vector2(0, 720-96), 2);
			//this.textRenderer.Render($"Frame time {frameTime}ms", ortowvp, new Vector2(0, 720-32-16), 2);
			this.textRenderer.Render($"Running for {gameTime.TotalGameTime.TotalSeconds} seconds", ortowvp, new Vector2(0, 720-32));
			this.textRenderer.Render("AWAY\nAAAA	AAAAA	A	AAӄA	A	A", ortowvp, Mouse.GetState().Position.ToVector2(), scale);
			frameTicks = frameWatch.ElapsedTicks;
			frameTime = frameWatch.ElapsedMilliseconds;
			frameWatch.Stop();
		}
	}
}
