using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using AnimationAux;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace SrinokanDreams
{
    /// <summary>
    /// This is the main class for your game
    /// </summary>
    public class GameStart : Microsoft.Xna.Framework.Game
    {
        #region Fields

        /// <summary>
        /// This graphics device we are drawing on in this program
        /// </summary>
        GraphicsDeviceManager graphics;
        SpriteFont sf;
        Dictionary<int, Texture2D> textures = new Dictionary<int, Texture2D>();
        SpriteBatch sb;
#if DEBUG
        int frameRate = 0;
        int frameCounter = 0;
        TimeSpan elapsedTime = TimeSpan.Zero;
#endif
        /// <summary>
        /// The camera we use
        /// </summary>
        private Camera camera;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public GameStart()
        {
            Globals.ThisPlayerID = Guid.NewGuid();
            // XNA startup
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            // Some basic setup for the display window
            this.IsMouseVisible = true;
            this.Window.AllowUserResizing = true;
            this.graphics.PreferredBackBufferWidth = 1920;
            this.graphics.PreferredBackBufferHeight = 1080;
            //this.graphics.IsFullScreen = true;
            // Create a simple mouse-based camera
            camera = new Camera(graphics);
            camera.Eye = new Vector3(1000, 1000, 1000);
            camera.Center = new Vector3(-20, 86, 159);
            Globals.StartReceiveClient();
            Globals.StartSendClient();
            Globals.MsgDispatcher = new MessageDispatcher();
            Globals.MsgReceiver = new MessageReceiver();
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            camera.Initialize();
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            sb = new SpriteBatch(graphics.GraphicsDevice);
            sf = Content.Load<SpriteFont>("NPCFont");
            Globals.MsgDispatcher.SendMessage(Message.MessageType.Start.ToString() + ":0;.");
            Globals.MsgReceiver.AskForReceive(ProcessStart);
        }

        public void ProcessStart(string response)
        {
            string[] responses = response.Split(new char[] { ':' });
            for (int i = 0; i < responses.Length; i += 2)
            {
                if (responses[i] == "0")
                {
                    string[] model = responses[i + 1].Split(new char[] { ';' });
                    Player currentPlayer = new Player(Content, model[0]);
                    Globals.ThisPlayer = currentPlayer;
                    currentPlayer.ModelPosition = new Vector3(float.Parse(model[1]), float.Parse(model[2]), float.Parse(model[3]));
                    currentPlayer.ModelRotation = float.Parse(model[4]);
                    currentPlayer.ModelVelocity = new Vector3(float.Parse(model[5]), float.Parse(model[6]), float.Parse(model[7]));
                }
            }
            Texture2D dummyTexture = new Texture2D(graphics.GraphicsDevice, 1, 1);
            dummyTexture.SetData(new Color[] { Color.Green });
            textures.Add(Globals.HPBarTexture, dummyTexture);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
            Globals.CloseSendClient();
            Globals.CloseReceiveClient();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
#if DEBUG
            elapsedTime += gameTime.ElapsedGameTime;

            if (elapsedTime > TimeSpan.FromSeconds(1))
            {
                elapsedTime -= TimeSpan.FromSeconds(1);
                frameRate = frameCounter;
                frameCounter = 0;
            }
#endif
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                this.Exit();
            if (Globals.ThisPlayer != null)
            {
                Globals.ThisPlayer.Update(gameTime, camera, graphics.GraphicsDevice.Viewport);
            }
            //camera.Update(graphics.GraphicsDevice, gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
#if DEBUG
            frameCounter++;
            string fps = string.Format("fps: {0}", frameRate);
#endif
            graphics.GraphicsDevice.Clear(Color.LightGray);
            if (Globals.ThisPlayer != null)
            {
                Globals.ThisPlayer.Draw(graphics.GraphicsDevice, camera, Matrix.Identity);
            }
            base.Draw(gameTime);
            sb.Begin();
            if (Globals.ThisPlayer != null)
            {
                Globals.ThisPlayer.Draw(sb, sf, textures);
            }
#if DEBUG
            sb.DrawString(sf, fps, new Vector2(33, 33), Color.Black);
#endif
            sb.End();
        }
    }
}
