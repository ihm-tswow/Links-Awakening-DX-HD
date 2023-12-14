using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    public class ObjFence : GameObject
    {
        private DictAtlasEntry _sprite;

        private readonly List<CPosition> _positionList = new List<CPosition>();

        // could be replaced with a ObjSprite
        public ObjFence(Map.Map map, int posX, int posY, int placement) : base(map)
        {
            _sprite = Resources.GetSprite("fence");
            EditorIconSource = new Rectangle(0, 0, 16, 16);

            for (var i = 0; i < 4; i++)
            {
                if ((placement & 0x08) > 1)
                {
                    var fX = posX + 4 + (i % 2) * 8;
                    var fY = posY + 5 + (i / 2) * 8;

                    var position = new CPosition(fX, fY, 0);
                    _positionList.Add(position);

                    var fencePart = new ObjSprite(Map, (int)position.X, (int)position.Y,
                        "fence", Vector2.Zero, Values.LayerPlayer, "fence_shadow", new Rectangle(-3, -5, 6, 6), Values.CollisionTypes.Normal);

                    map.Objects.SpawnObject(fencePart);
                }

                placement <<= 1;
            }

            IsDead = true;
        }

        public override void DrawEditor(SpriteBatch spriteBatch, Vector2 drawPosition)
        {
            foreach (var position in _positionList)
                spriteBatch.Draw(_sprite.Texture, new Vector2(
                        position.X + drawPosition.X - _sprite.Origin.X,
                        position.Y + drawPosition.Y - _sprite.Origin.Y), _sprite.ScaledRectangle, Color.White, 0, Vector2.Zero, _sprite.Scale, SpriteEffects.None, 0);
        }
    }
}