using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using System.Collections.Generic;

namespace ProjectZ.InGame.GameObjects.Things
{
    public class ObjFinalBackground : GameObject
    {
        struct CloudPart
        {
            public Vector2 position;
            public Vector4 color0;
            public Vector4 color1;
            public float offset;
        }

        struct StarAnimation
        {
            public Vector2 position;
            public Animator animator;
            public Color color;
        }

        private List<CloudPart> _clouds = new List<CloudPart>();
        private List<StarAnimation> _stars = new List<StarAnimation>();

        private DictAtlasEntry _cloudSprite;
        private CPosition _spawnPosition;

        private string _moveStarKeys;
        private float _moveHeight;
        private float _positionTop;
        private float _movePosition;
        private bool _moveStars;

        public ObjFinalBackground() : base("final_cloud") { }

        public ObjFinalBackground(Map.Map map, int posX, int posY, string moveStarsKey) : base(map)
        {
            _spawnPosition = new CPosition(posX, posY, 0);
            _moveStarKeys = moveStarsKey;

            _cloudSprite = Resources.GetSprite("final_cloud");

            var colorRed0 = new Vector4(0.518f, 0.192f, 0.353f, 1.0f);
            var colorRed1 = new Vector4(0.835f, 0.196f, 0.541f, 1.0f);
            var colorBlue0 = new Vector4(0.290f, 0.255f, 0.996f, 1.0f);
            var colorBlue1 = new Vector4(0.510f, 0.388f, 0.898f, 1.0f);
            var colorBlue2 = new Vector4(0.259f, 0.741f, 0.776f, 1.0f);
            var colorBlue3 = new Vector4(0.259f, 0.482f, 0.518f, 1.0f);
            var colorWhite = new Vector4(0.969f, 0.990f, 0.910f, 1.0f);
            var colorGreen = new Vector4(0.188f, 0.769f, 0.353f, 1.0f);
            var colorYellow = new Vector4(0.975f, 0.675f, 0.031f, 1.0f);
            var colorLila0 = new Vector4(0.518f, 0.388f, 0.906f, 1.0f);
            var colorLila1 = new Vector4(0.710f, 0.322f, 0.808f, 1.0f);

            {
                var cloudX = posX - 80;
                var cloudY = posY + 56;
                var offset0 = 0.8f;
                var offset1 = 0.75f;

                _clouds.Add(new CloudPart() { position = new Vector2(cloudX, cloudY + 8), color0 = colorRed0, color1 = colorRed1, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 8, cloudY), color0 = colorRed0, color1 = colorRed1, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 16, cloudY + 8), color0 = colorRed0, color1 = colorRed1, offset = offset0 });
            }

            {
                var cloudX = posX + 80;
                var cloudY = posY - 32;
                var offset0 = 0.8f;
                var offset1 = 0.75f;

                _clouds.Add(new CloudPart() { position = new Vector2(cloudX, cloudY + 8), color0 = colorGreen, color1 = colorWhite, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 8, cloudY), color0 = colorGreen, color1 = colorWhite, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 16, cloudY + 8), color0 = colorGreen, color1 = colorWhite, offset = offset0 });
            }

            {
                var cloudX = posX + 88;
                var cloudY = posY + 40;
                var offset0 = 0.8f;
                var offset1 = 0.75f;

                _clouds.Add(new CloudPart() { position = new Vector2(cloudX, cloudY + 8), color0 = colorLila0, color1 = colorWhite, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 8, cloudY), color0 = colorLila0, color1 = colorWhite, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 16, cloudY + 8), color0 = colorLila0, color1 = colorWhite, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 24, cloudY), color0 = colorLila0, color1 = colorWhite, offset = offset0 });
            }

            {
                var cloudX = posX - 40;
                var cloudY = posY - 0;
                var offset0 = 0.8f;
                var offset1 = 0.75f;

                _clouds.Add(new CloudPart() { position = new Vector2(cloudX, cloudY + 16), color0 = colorRed0, color1 = colorRed1, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 8, cloudY + 8), color0 = colorRed0, color1 = colorRed1, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 16, cloudY + 16), color0 = colorRed0, color1 = colorRed1, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 16, cloudY + 8), color0 = colorRed0, color1 = colorRed1, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 16, cloudY), color0 = colorRed0, color1 = colorRed1, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 24, cloudY + 8), color0 = colorRed0, color1 = colorRed1, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 32, cloudY), color0 = colorRed0, color1 = colorRed1, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 40, cloudY + 8), color0 = colorRed0, color1 = colorRed1, offset = offset1 });
            }

            {
                var cloudX = posX + 32;
                var cloudY = posY;
                var offset0 = 1;
                var offset1 = 0.925f;

                _clouds.Add(new CloudPart() { position = new Vector2(cloudX, cloudY + 16), color0 = colorGreen, color1 = colorWhite, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 8, cloudY + 8), color0 = colorGreen, color1 = colorWhite, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 16, cloudY + 16), color0 = colorGreen, color1 = colorWhite, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 24, cloudY + 8), color0 = colorBlue2, color1 = colorBlue3, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 24, cloudY + 16), color0 = colorBlue2, color1 = colorBlue3, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 32, cloudY + 16), color0 = colorBlue2, color1 = colorBlue3, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 24, cloudY + 24), color0 = colorBlue2, color1 = colorBlue3, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 40, cloudY + 8), color0 = colorBlue2, color1 = colorWhite, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 48, cloudY + 8), color0 = colorBlue2, color1 = colorWhite, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 56, cloudY + 16), color0 = colorBlue2, color1 = colorWhite, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 32, cloudY), color0 = colorBlue2, color1 = colorBlue3, offset = offset1 });
            }

            {
                var cloudX = posX - 32;
                var cloudY = posY + 32;
                var offset0 = 0.65f;
                var offset1 = 0.7f;

                _clouds.Add(new CloudPart() { position = new Vector2(cloudX, cloudY + 16), color0 = colorBlue2, color1 = colorBlue3, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 8, cloudY + 8), color0 = colorBlue2, color1 = colorBlue3, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 16, cloudY + 16), color0 = colorBlue2, color1 = colorBlue3, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 16, cloudY), color0 = colorBlue2, color1 = colorBlue3, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 24, cloudY + 8), color0 = colorWhite, color1 = colorYellow, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 32, cloudY), color0 = colorWhite, color1 = colorYellow, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 40, cloudY + 8), color0 = colorWhite, color1 = colorYellow, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 16, cloudY + 32), color0 = colorGreen, color1 = colorWhite, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 24, cloudY + 24), color0 = colorGreen, color1 = colorWhite, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 32, cloudY + 32), color0 = colorGreen, color1 = colorWhite, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 32, cloudY + 16), color0 = colorGreen, color1 = colorWhite, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 40, cloudY + 24), color0 = colorWhite, color1 = colorYellow, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 48, cloudY + 16), color0 = colorWhite, color1 = colorYellow, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 56, cloudY + 24), color0 = colorWhite, color1 = colorYellow, offset = offset1 });
            }

            {
                var cloudX = posX - 136;
                var cloudY = posY + 8;
                var offset = 0.9f;
                var offset1 = 0.925f;

                _clouds.Add(new CloudPart() { position = new Vector2(cloudX, cloudY + 16), color0 = colorBlue2, color1 = colorBlue3, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 8, cloudY + 8), color0 = colorBlue2, color1 = colorBlue3, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 16, cloudY + 16), color0 = colorBlue2, color1 = colorBlue3, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 16, cloudY), color0 = colorBlue2, color1 = colorBlue3, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 24, cloudY + 8), color0 = colorWhite, color1 = colorLila0, offset = offset });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 40, cloudY + 8), color0 = colorWhite, color1 = colorLila0, offset = offset });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 32, cloudY + 16), color0 = colorBlue2, color1 = colorWhite, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 40, cloudY + 24), color0 = colorBlue2, color1 = colorWhite, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 48, cloudY + 16), color0 = colorBlue2, color1 = colorWhite, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 56, cloudY + 24), color0 = colorBlue2, color1 = colorWhite, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 24, cloudY + 24), color0 = colorWhite, color1 = colorLila1, offset = offset });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 32, cloudY + 32), color0 = colorWhite, color1 = colorLila1, offset = offset });
            }

            {
                var cloudX = posX + 16;
                var cloudY = posY - 24;
                var offset0 = 0.85f;
                var offset1 = 0.825f;
                var offset2 = 0.8f;

                _clouds.Add(new CloudPart() { position = new Vector2(cloudX, cloudY), color0 = colorRed0, color1 = colorRed1, offset = offset2 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 16, cloudY), color0 = colorGreen, color1 = colorWhite, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 16, cloudY - 16), color0 = colorBlue0, color1 = colorBlue1, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 24, cloudY - 8), color0 = colorGreen, color1 = colorWhite, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 8, cloudY - 8), color0 = colorRed0, color1 = colorRed1, offset = offset2 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 32, cloudY), color0 = colorBlue2, color1 = colorBlue3, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 32, cloudY - 16), color0 = colorBlue2, color1 = colorBlue3, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 40, cloudY - 8), color0 = colorBlue2, color1 = colorBlue3, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 48, cloudY - 16), color0 = colorBlue2, color1 = colorBlue3, offset = offset1 });
            }

            {
                var cloudX = posX - 88;
                var cloudY = posY - 24;
                var offset0 = 0.85f;
                var offset1 = 0.8f;

                _clouds.Add(new CloudPart() { position = new Vector2(cloudX, cloudY), color0 = colorBlue0, color1 = colorBlue1, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 8, cloudY - 8), color0 = colorBlue0, color1 = colorBlue1, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 8, cloudY + 24), color0 = colorBlue0, color1 = colorBlue1, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 32, cloudY), color0 = colorRed0, color1 = colorRed1, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 16, cloudY + 16), color0 = colorBlue0, color1 = colorBlue1, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 24, cloudY - 8), color0 = colorRed0, color1 = colorRed1, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 40, cloudY), color0 = colorYellow, color1 = colorWhite, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 24, cloudY + 8), color0 = colorRed0, color1 = colorRed1, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 24, cloudY + 24), color0 = colorRed0, color1 = colorRed1, offset = offset1 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 16, cloudY), color0 = colorBlue0, color1 = colorBlue1, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 32, cloudY + 16), color0 = colorGreen, color1 = colorWhite, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 36, cloudY + 8), color0 = colorGreen, color1 = colorWhite, offset = offset0 });
                _clouds.Add(new CloudPart() { position = new Vector2(cloudX + 40, cloudY + 16), color0 = colorYellow, color1 = colorWhite, offset = offset0 });
            }

            var randomDist = 16;
            var halfOffset = 24;
            _moveHeight = halfOffset * 2 * 20;
            _positionTop = posY - halfOffset * 2 * 10;

            for (int x = 0; x < 20; x++)
            {
                for (int y = 0; y < 20; y++)
                {
                    if (Game1.RandomNumber.Next(0, 6) == 0)
                        continue;

                    var position = new Vector2(posX + (x - 10) * halfOffset * 2 + (y % 2) * halfOffset, posY + (y - 10) * halfOffset * 2);

                    _stars.Add(new StarAnimation()
                    {
                        position = position + new Vector2(
                            Game1.RandomNumber.Next(0, randomDist * 2) - randomDist,
                            Game1.RandomNumber.Next(0, randomDist * 2) - randomDist),
                        animator = AnimatorSaveLoad.LoadAnimator("Sequences/final star"),
                        color = Color.White * (Game1.RandomNumber.Next(50, 75) / 100f)
                    });
                }
            }

            foreach (var star in _stars)
            {
                star.animator.Play("idle");
                star.animator.SetFrame(Game1.RandomNumber.Next(0, 4));
            }

            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerBackground, new CPosition(posX, posY - 512, 0)));
        }

        private void OnKeyChange()
        {
            if (!_moveStars && Game1.GameManager.SaveManager.GetString(_moveStarKeys) == "1")
                _moveStars = true;
        }

        private void Update()
        {
            foreach (var star in _stars)
            {
                star.animator.Update();
            }

            if (_moveStars)
            {
                _movePosition += Game1.TimeMultiplier * 8;
            }
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            // we offset the background objects in relation to the camera so that the move slower than the actuall objects
            // draw the start
            var starOffset = -(_spawnPosition.Position - (new Vector2(MapManager.Camera.X, MapManager.Camera.Y) / MapManager.Camera.Scale)) * new Vector2(1.0f, 0.75f);
            foreach (var star in _stars)
            {
                var offsetPosition = star.position;
                offsetPosition.Y += _movePosition;
                offsetPosition.Y = (offsetPosition.Y - _positionTop) % _moveHeight + _positionTop;
                star.animator.Draw(spriteBatch, offsetPosition + starOffset, star.color);
            }

            // draw the clouds
            // this is all way too complicated but I dont know how this can be done simpler
            // looking right on resize makes hard
            // not so sure how the position value in the shader works
            var shaderOffset = (new Vector2(MapManager.Camera.Location.X + 4000, MapManager.Camera.Location.Y) * 0.85f - new Vector2(Game1.RenderWidth, Game1.RenderHeight) / 2);
            Resources.CloudShader.Effect.Parameters["offset"].SetValue(shaderOffset);
            Resources.CloudShader.FloatParameter["scale"] = MapManager.Camera.Scale;
            Resources.CloudShader.FloatParameter["scaleX"] = MapManager.Camera.Scale;
            Resources.CloudShader.FloatParameter["scaleY"] = MapManager.Camera.Scale;

            foreach (var cloud in _clouds)
            {
                spriteBatch.End();
                ObjectManager.SpriteBatchBegin(spriteBatch, Resources.CloudShader);
                Resources.CloudShader.Effect.Parameters["color0"].SetValue(cloud.color0);
                Resources.CloudShader.Effect.Parameters["color1"].SetValue(cloud.color1);

                var offset = -(_spawnPosition.Position - (new Vector2(MapManager.Camera.X, MapManager.Camera.Y) / MapManager.Camera.Scale)) * new Vector2(1.0f, 0.35f) * cloud.offset;
                var position = cloud.position + offset;
                position.Y += _movePosition;
                var cameraView = MapManager.Camera.GetGameView();
                if (position.Y < cameraView.Bottom)
                    DrawHelper.DrawNormalized(spriteBatch, _cloudSprite, position, Color.White);

                spriteBatch.End();
                ObjectManager.SpriteBatchBegin(spriteBatch, null);
            }
        }
    }
}
