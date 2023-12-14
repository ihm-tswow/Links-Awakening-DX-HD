using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjStoneSpawner : GameObject
    {
        private readonly ObjSprite[] _objSprites = new ObjSprite[4];
        private readonly DictAtlasEntry[] _stoneRectangleSource = new DictAtlasEntry[2];

        private bool _isActive = true;
        public override bool IsActive
        {
            set
            {
                for (var i = 0; i < _objSprites.Length; i++)
                    _objSprites[i].IsActive = value;
                _isActive = value;
            }
            get => _isActive;
        }

        private bool _hidden;
        public bool Hidden
        {
            set
            {
                for (var i = 0; i < _objSprites.Length; i++)
                    _objSprites[i].IsActive = !value;
                _hidden = value;
            }
            get => _hidden;
        }

        private int _holeX;
        private int _holeY;

        public ObjStoneSpawner() : base("small_stones") { }

        public ObjStoneSpawner(Map.Map map, int posX, int posY) : base(map)
        {
            _stoneRectangleSource[0] = Resources.GetSprite("small_stone_0");
            _stoneRectangleSource[1] = Resources.GetSprite("small_stone_1");

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            _holeX = posX / 16;
            _holeY = posY / 16;

            var position = new Point(posX, posY);

            // spawn the four stones
            // set the placement of the stones depending on the position of the spawner
            var stoneCount = (position.X + position.Y) / (Values.TileSize * 2) % 2 * 2 + 1;
            for (var i = 0; i < 4; i++)
            {
                // ...
                var stoneIndex = (stoneCount & 0b10) >> 1;
                stoneCount++;
                var fieldX = i % 2 * 8;
                var fieldY = i / 2 * 8;

                // deterministic offset of the stones depending on the position of the stone
                // i really do not know if this is a good way of doing this...
                var offsetIndex = (position.X + position.Y) / 8 + i;
                var offsetX = offsetIndex % (8 - _stoneRectangleSource[stoneIndex].SourceRectangle.Width);
                var offsetY = offsetIndex % (8 - _stoneRectangleSource[stoneIndex].SourceRectangle.Height);
                var stonePosition = new Vector2(fieldX + offsetX, fieldY + offsetY);
                var centerX = (int)(_stoneRectangleSource[stoneIndex].ScaledRectangle.Width / 2);
                var strStoneSprite = "small_stone_" + stoneIndex;

                _objSprites[i] = new ObjSprite(Map,
                    position.X + (int)stonePosition.X + centerX, position.Y + 5 + (int)stonePosition.Y,
                    strStoneSprite, Vector2.Zero, Values.LayerPlayer, strStoneSprite);

                Map.Objects.SpawnObject(_objSprites[i]);
            }

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        }

        private void Update()
        {
            // hide rocks when there has been dug at the tile
            if (!Hidden && Map.HoleMap != null &&
                Map.HoleMap.ArrayTileMap[_holeX, _holeY, 0] != -1)
                Hidden = true;
            if (Hidden && Map.HoleMap != null &&
                Map.HoleMap.ArrayTileMap[_holeX, _holeY, 0] == -1)
                Hidden = false;
        }
    }
}