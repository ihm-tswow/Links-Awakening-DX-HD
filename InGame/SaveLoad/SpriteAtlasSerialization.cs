using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ProjectZ.InGame.SaveLoad
{
    class SpriteAtlasSerialization
    {
        public class SpriteAtlas
        {
            public int Scale = 1;
            public List<AtlasEntry> Data = new List<AtlasEntry>();
        }

        public class AtlasEntry
        {
            public string EntryId;
            public Rectangle SourceRectangle;
            public Vector2 Origin;

            public override string ToString()
            {
                return EntryId;
            }
        }

        public static void SaveSpriteAtlas(string filePath, SpriteAtlas spriteAtlas)
        {
            using var writer = new StreamWriter(filePath);

            // version
            writer.WriteLine("1");
            writer.WriteLine(spriteAtlas.Scale);

            // this scales the source rectangle because that is the easy thing to do to support scaling in the editor
            // this makes it possible to upscale the image by x and just change the scale value in the .atlas file
            for (var i = 0; i < spriteAtlas.Data.Count; i++)
            {
                var rectangle = spriteAtlas.Data[i].SourceRectangle;
                var origin = spriteAtlas.Data[i].Origin;
                writer.WriteLine($"{spriteAtlas.Data[i].EntryId}:" +
                                 $"{rectangle.X / spriteAtlas.Scale}," +
                                 $"{rectangle.Y / spriteAtlas.Scale}," +
                                 $"{rectangle.Width / spriteAtlas.Scale}," +
                                 $"{rectangle.Height / spriteAtlas.Scale}," +
                                 $"{origin.X / spriteAtlas.Scale}," +
                                 $"{origin.Y / spriteAtlas.Scale}");
            }
        }

        public static bool LoadSpriteAtlas(string filePath, SpriteAtlas spriteAtlas)
        {
            if (!File.Exists(filePath))
                return false;

            using var reader = new StreamReader(filePath);

            // version is currently not used
            reader.ReadLine();

            // will crash if the data does not contain integer numbers
            spriteAtlas.Scale = int.Parse(reader.ReadLine());

            while (!reader.EndOfStream)
            {
                var strLine = reader.ReadLine();
                var split = strLine.Split(':');
                if (split.Length == 2)
                {
                    var newEntry = new AtlasEntry();
                    newEntry.EntryId = split[0];

                    var rectangleData = split[1].Split(",");
                    if (rectangleData.Length >= 4)
                        newEntry.SourceRectangle = new Rectangle(
                            int.Parse(rectangleData[0]), int.Parse(rectangleData[1]),
                            int.Parse(rectangleData[2]), int.Parse(rectangleData[3]));
                    if (rectangleData.Length >= 6)
                        newEntry.Origin = new Vector2(int.Parse(rectangleData[4]), int.Parse(rectangleData[5]));

                    spriteAtlas.Data.Add(newEntry);
                }
            }

            return true;
        }

        public static void LoadSourceDictionary(Texture2D texture, string fileName, Dictionary<string, DictAtlasEntry> dictionary)
        {
            var spriteAtlas = new SpriteAtlas();

            if (!LoadSpriteAtlas(fileName, spriteAtlas))
                return;

            for (var i = 0; i < spriteAtlas.Data.Count; i++)
            {
                var newEntry = new DictAtlasEntry(texture, spriteAtlas.Data[i].SourceRectangle, spriteAtlas.Data[i].Origin, spriteAtlas.Scale);
                dictionary.TryAdd(spriteAtlas.Data[i].EntryId, newEntry);
            }
        }
    }
}
