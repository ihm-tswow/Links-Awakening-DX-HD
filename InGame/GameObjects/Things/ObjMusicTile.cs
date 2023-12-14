using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    class ObjMusicTile : GameObject
    {
        private string[,] _musicData;
        private string _lastTrack;

        // @TODO: fade in/out
        public ObjMusicTile() : base("editor music") { }

        public ObjMusicTile(Map.Map map, int posX, int posY) : base(map)
        {
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));

            _musicData = DataMapSerializer.LoadData(Values.PathContentFolder + "musicOverworld.data");
        }

        private void Update()
        {
            var position = new Point(
                (int)(MapManager.ObjLink.PosX - Map.MapOffsetX * Values.TileSize) / 16,
                (int)(MapManager.ObjLink.PosY - Map.MapOffsetY * Values.TileSize) / 16);

            if (0 <= position.X && position.X < _musicData.GetLength(0) &&
                0 <= position.Y && position.Y < _musicData.GetLength(1))
            {
                var track = _musicData[position.X, position.Y];

                if (_lastTrack != track)
                {
                    _lastTrack = track;

                    if (int.TryParse(track, out var songNr))
                        Game1.GameManager.SetMusic(songNr, 0, false);
                }
            }
        }
    }
}
