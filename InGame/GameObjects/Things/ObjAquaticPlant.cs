using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjAquaticPlant : GameObject
    {
        private readonly CSprite _topPlant;
        private readonly CSprite _bottomPlant;

        private double _counter;

        private readonly float _topLeafDivider;
        private readonly float _bottomLeafDivider;

        public ObjAquaticPlant() : base("aquatic_plant") { }

        public ObjAquaticPlant(Map.Map map, int posX, int posY) : base(map)
        {
            var sourceTop = Resources.SourceRectangle("aquatic_plant_top");
            var sourceBottom = Resources.SourceRectangle("aquatic_plant_bottom");

            EntityPosition = new CPosition(posX, posY + 16, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            _topPlant = new CSprite(Resources.SprObjects, new CPosition(posX + 1, posY, 0), sourceTop, Vector2.Zero);
            _bottomPlant = new CSprite(Resources.SprObjects, new CPosition(posX + 7, posY + 10, 0), sourceBottom, Vector2.Zero);

            // so not all leafs move in parallel
            _counter = Game1.RandomNumber.Next(0, 450);
            _topLeafDivider = Game1.RandomNumber.Next(350, 450);
            _bottomLeafDivider = Game1.RandomNumber.Next(250, 350);

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerBottom, EntityPosition));
        }

        public void Update()
        {
            _counter += Game1.DeltaTime;

            _topPlant.DrawOffset.X = (float)Math.Sin(_counter / _topLeafDivider);
            _bottomPlant.DrawOffset.X = (float)Math.Sin(_counter / _bottomLeafDivider);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // draw top and bottom leafs
            _topPlant.Draw(spriteBatch);
            _bottomPlant.Draw(spriteBatch);
        }
    }
}