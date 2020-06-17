using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using AnimationAux;
using Microsoft.Xna.Framework.Graphics;
using System.Xml.Linq;
using Microsoft.Xna.Framework.Input;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework.Content;

namespace SrinokanDreams
{
    class Player
    {
        public Guid PlayerID { get; set; }
        public string ModelName { get; set; }
        public Dictionary<int, AnimatedModel> AnimatedModels { get; set; }
        public bool HasCastAnimation { get; set; }
        public int ShowAction { get; set; }
        public bool IsLiving { get; set; }
        public Vector3 ModelPosition { get; set; }
        public float ModelRotation { get; set; }
        public Vector3 ModelVelocity { get; set; }
        public string Name { get; set; }
        bool leftMouseButtonDown = false;
        Vector2 textPosition = Vector2.Zero;
        bool showNPCNamePlate = false;
        ContentManager contentManager;

        private void LoadAnimatedModel(int action, Microsoft.Xna.Framework.Content.ContentManager cm)
        {
            AnimatedModels.Add(action, new AnimatedModel(ModelName + "_" + Globals.ActionNames[action]));
            AnimatedModels[action].LoadContent(cm);
        }

        public void ChangeAction(int action)
        {
            ShowAction = action;
            AnimationClip clip = AnimatedModels[ShowAction].Clips[0];
            AnimationPlayer player = AnimatedModels[ShowAction].PlayClip(clip);
            player.Looping = true;
        }

        public Player(ContentManager cm, string modelName)
        {
            contentManager = cm;
            PlayerID = Globals.ThisPlayerID;
            AnimatedModels = new Dictionary<int, AnimatedModel>();
            ModelName = modelName;
            Globals.MsgDispatcher.SendMessage(Message.MessageType.PlayerPosition.ToString() + ":" + ModelPosition.X.ToString() + ";" + 
                ModelPosition.Y.ToString() + ";" + ModelPosition.Z.ToString() + ";." + Message.MessageType.PlayerRotation.ToString() + ":" + ModelRotation.ToString() + 
                ";." + Message.MessageType.PlayerVelocity.ToString() + ":" + ModelVelocity.X.ToString() + ";" + ModelVelocity.Y.ToString() + ";" + ModelVelocity.Z.ToString() +
                ";." + Message.MessageType.NPC.ToString() + ":" + modelName + ";.");
            Globals.MsgReceiver.AskForReceive(ProcessLoadPlayerMessage);
        }

        public void ProcessLoadPlayerMessage(string response)
        {
            string[] tmpstrs = response.Split(new char[] { '.' });
            string[] tmpstrs1 = tmpstrs[0].Split(new char[] { ':' });
            string[] tmpstrs3 = tmpstrs[1].Split(new char[] { ':' });
            string[] modelProperties = tmpstrs1[1].Split(new char[] { ';' });
            string[] actions = tmpstrs3[1].Split(new char[] { ';' });
            Name = modelProperties[0];
            HasCastAnimation = Convert.ToBoolean(modelProperties[1]);
            ShowAction = int.Parse(modelProperties[2]);
            IsLiving = bool.Parse(modelProperties[3]);
            foreach (string action in actions)
            {
                if (action != null && action.Length != 0)
                    LoadAnimatedModel(int.Parse(action), contentManager);
            }
            AnimationClip clip = AnimatedModels[ShowAction].Clips[0];
            AnimationPlayer player = AnimatedModels[ShowAction].PlayClip(clip);
            player.Looping = true;
        }

        internal void Update(GameTime gameTime, Camera camera, Viewport viewport)
        {
            KeyboardState ks = Keyboard.GetState();
            if (AnimatedModels.Keys.Contains(ShowAction))
            {
                AnimatedModels[ShowAction].Update(gameTime);
            }
            Vector3 modelVelocityAdd = Vector3.Zero;
            bool modelChanged = false;
            if (ks.IsKeyDown(Keys.A))
            {
                ModelRotation += 1 * 0.025f;
                modelChanged = true;
            }
            else if (ks.IsKeyDown(Keys.D))
            {
                ModelRotation -= 1 * 0.025f;
                modelChanged = true;
            }
            if (ks.IsKeyDown(Keys.W))
            {
                modelVelocityAdd.X = (float)Math.Sin(ModelRotation);
                modelVelocityAdd.Z = (float)Math.Cos(ModelRotation);
                modelVelocityAdd *= 2;
                ModelPosition += modelVelocityAdd;
                modelChanged = true;
            }
            if (ks.IsKeyDown(Keys.Escape))
            {
                ModelPosition = Vector3.Zero;
                ModelVelocity = Vector3.Zero;
                ModelRotation = 0.0f;
                modelChanged = true;
            }
            MouseState ms = Mouse.GetState();
            if (ms.LeftButton == ButtonState.Pressed)
            {
                leftMouseButtonDown = true;
            }
            if (leftMouseButtonDown && ms.LeftButton == ButtonState.Released)
            {
                leftMouseButtonDown = false;
                Vector3 nearPoint = new Vector3(ms.X, ms.Y, 0);
                Vector3 farPoint = new Vector3(ms.X, ms.Y, 1);

                nearPoint = viewport.Unproject(nearPoint, camera.Projection, camera.View, Matrix.Identity);
                farPoint = viewport.Unproject(farPoint, camera.Projection, camera.View, Matrix.Identity);

                Vector3 direction = farPoint - nearPoint;
                direction.Normalize();

                Ray ray = new Ray(nearPoint, direction);

                if (ray.Intersects(new BoundingBox(ModelPosition, new Vector3(ModelPosition.X + 50, ModelPosition.Y + 200, ModelPosition.Z + 100))) > 0)
                {
                    if (showNPCNamePlate)
                    {
                        showNPCNamePlate = false;
                    }
                    else
                    {
                        showNPCNamePlate = true;
                    }
                }
            }
            Vector3 screenSpace = viewport.Project(Vector3.Zero, camera.Projection, camera.View, Matrix.CreateTranslation(ModelPosition));
            textPosition.X = screenSpace.X;
            textPosition.Y = screenSpace.Y;
            if (modelChanged)
            {
                Globals.MsgDispatcher.SendMessage("PlayerPosition:" + ModelPosition.X.ToString() + ";" +
                    ModelPosition.Y.ToString() + ";" + ModelPosition.Z.ToString() + ";.PlayerRotation:" + ModelRotation.ToString() +
                    ";.PlayerVelocity:" + ModelVelocity.X.ToString() + ";" + ModelVelocity.Y.ToString() + ";" + ModelVelocity.Z.ToString() +
                    ";.");
            }
        }

        internal void Draw(SpriteBatch sb, SpriteFont sf, Dictionary<int, Texture2D> textures)
        {
            if (showNPCNamePlate)
            {
                Vector2 ms = sf.MeasureString(Name);
                sb.Draw(textures[Globals.HPBarTexture], new Rectangle((int)(textPosition.X - (ms.X / 2)), (int)(textPosition.Y - 150 - (ms.Y / 2)), 75, 5), Color.LightGreen);
                sb.DrawString(sf, Name, new Vector2(textPosition.X - (ms.X / 2), textPosition.Y - (ms.Y / 2) - 170), Color.Black);
            }
        }

        internal void Draw(GraphicsDevice graphicsDevice, Camera camera, Matrix identity)
        {
            if (AnimatedModels.Keys.Contains(ShowAction))
            {
                AnimatedModels[ShowAction].Draw(graphicsDevice, camera, ModelRotation, ModelPosition);
            }
        }
    }
}
