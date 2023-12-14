using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.SaveLoad;

namespace ProjectZ.InGame.Things
{
    internal class Resources
    {
        public class Texture
        {
            public string Name;
            public Texture2D SprTexture;

            public Texture(string name)
            {
                Name = name;
            }
        }

        public static Effect RoundedCornerEffect;

        public static Effect BlurEffect;
        public static Effect RoundedCornerBlurEffect;

        public static Effect BlurEffectV;
        public static Effect BlurEffectH;
        public static Effect BBlurEffectV;
        public static Effect BBlurEffectH;
        public static Effect BBlurMapping;
        public static Effect FullShadowEffect;
        public static Effect SaturationEffect;
        public static Effect WobbleEffect;
        public static Effect CircleShader;
        public static Effect LightShader;
        public static Effect LightFadeShader;
        public static Effect ThanosShader;

        // some sprites need different parameters set
        // we try to use as little different sprites effects as possible
        public static SpriteShader DamageSpriteShader0;
        public static SpriteShader DamageSpriteShader1;
        public static SpriteShader CloudShader;
        public static SpriteShader ThanosSpriteShader0;
        public static SpriteShader ThanosSpriteShader1;
        public static SpriteShader WindFishShader;
        public static SpriteShader ColorShader;

        public static SpriteShader ShockShader0;
        public static SpriteShader ShockShader1;

        public static SpriteFont EditorFont, EditorFontMonoSpace, EditorFontSmallMonoSpace;
        public static SpriteFont GameFont, GameHeaderFont;
        public static SpriteFont FontCredits, FontCreditsHeader;

        public static Texture2D EditorEyeOpen, EditorEyeClosed, EditorIconDelete;

        public static Texture2D SprWhite, SprTiledBlock, SprObjects, SprObjectsAnimated, SprItem, SprNpCs;
        public static Texture2D SprEnemies, SprMidBoss, SprNightmares, SprMiniMap;
        public static Texture2D SprShadow;
        public static Texture2D SprBlurTileset;
        public static Texture2D SprPhotos;
        public static Texture2D SprLink, SprLinkCloak;
        public static Texture2D SprGameSequences;
        public static Texture2D SprGameSequencesFinal;
        public static Texture2D SprFog;
        public static Texture2D SprLight;
        public static Texture2D SprLightRoomH;
        public static Texture2D SprLightRoomV;
        public static Texture2D NoiseTexture;

        public static Texture2D SprIconOptions, SprIconErase, SprIconCopy, EditorIconEdit, EditorIconSelect;

        public static List<Texture> TextureList = new List<Texture>();

        public static Dictionary<string, DictAtlasEntry> SpriteAtlas = new Dictionary<string, DictAtlasEntry>();
        public static Dictionary<string, int> TilesetSizes = new Dictionary<string, int>();

        public static Dictionary<string, SoundEffect> SoundEffects = new Dictionary<string, SoundEffect>();

        public static int GameFontHeight = 10;
        public static int EditorFontHeight;

        // resources needed to start showing the intro
        public static void LoadIntro(GraphicsDevice graphics, ContentManager content)
        {
            // TODO: make sure to only load the stuff needed; BlurEffect is not needed but needs changes to not load it here

            SprWhite = new Texture2D(graphics, 1, 1);
            SprWhite.SetData(new[] { Color.White });

            LoadTexturesFromFolder(Values.PathContentFolder + "/Intro/");

            BlurEffect = content.Load<Effect>("Shader/EffectBlur");
            RoundedCornerBlurEffect = content.Load<Effect>("Shader/RoundedCornerEffectBlur");

            AddSoundEffect(content, "D378-15-0F");
            AddSoundEffect(content, "D378-12-0C");
            AddSoundEffect(content, "D378-25-19");
        }
        
        public static void LoadTextures(GraphicsDevice graphics, ContentManager content)
        {
            // load the tileset sizes
            LoadTilesetSizes();

            LoadTexture(out SprGameSequences, Values.PathContentFolder + "Sequences/game sequences.png");
            LoadTexture(out SprGameSequencesFinal, Values.PathContentFolder + "Sequences/end sequence.png");
            LoadTexture(out SprPhotos, Values.PathContentFolder + "Photo Mode/photos.png");
            LoadTexture(out var bigEditorIcons, Values.PathContentFolder + "Editor/editorIcons4x.png");
            LoadTexture(out var SprUI, Values.PathContentFolder + "ui.png");

            LoadTexturesFromFolder(Values.PathContentFolder + "/Sequences/");
            LoadTexturesFromFolder(Values.PathContentFolder + "/Light/");

            // load all the tileset textures
            LoadTexturesFromFolder(Values.PathTilesetFolder);

            LoadTexturesFromFolder(Values.PathMapObjectFolder);

            SprMiniMap = GetTexture("minimap.png");
            SprItem = GetTexture("items.png");

            SprLink = GetTexture("link0.png");
            SprLinkCloak = GetTexture("link_cloak.png");
            SprEnemies = GetTexture("enemies.png");
            SprNpCs = GetTexture("npcs.png");
            SprObjects = GetTexture("objects.png");
            SprObjectsAnimated = GetTexture("objects animated.png");
            SprMidBoss = GetTexture("midboss.png");
            SprNightmares = GetTexture("nightmares.png");
            SprBlurTileset = GetTexture("blur tileset.png");

            // load fonts
            EditorFont = content.Load<SpriteFont>("Fonts/editor font");
            EditorFontHeight = (int)EditorFont.MeasureString("H").Y;

            EditorFontMonoSpace = content.Load<SpriteFont>("Fonts/editor mono font");

            EditorFontSmallMonoSpace = content.Load<SpriteFont>("Fonts/editor small mono font");

            GameFont = content.Load<SpriteFont>("Fonts/smallFont");
            GameFont.LineSpacing = GameFontHeight;

            GameHeaderFont = content.Load<SpriteFont>("Fonts/newHeaderFont");

            FontCredits = content.Load<SpriteFont>("Fonts/credits font");
            FontCreditsHeader = content.Load<SpriteFont>("Fonts/credits header font");

            // load textures
            SprTiledBlock = new Texture2D(graphics, 2, 2);
            SprTiledBlock.SetData(new[] { Color.White, Color.LightGray, Color.LightGray, Color.White });

            EditorEyeOpen = content.Load<Texture2D>("Editor/eye_open");
            EditorEyeClosed = content.Load<Texture2D>("Editor/eye_closed");
            EditorIconDelete = content.Load<Texture2D>("Editor/delete");
            EditorIconEdit = content.Load<Texture2D>("Editor/edit");
            EditorIconSelect = content.Load<Texture2D>("Editor/select");

            SprLight = content.Load<Texture2D>("Light/light");
            SprLightRoomH = content.Load<Texture2D>("Light/ligth room");
            SprLightRoomV = content.Load<Texture2D>("Light/ligth room vertical");

            SprShadow = content.Load<Texture2D>("Light/shadow");
            LoadContentTextureWithAtlas(content, "Light/doorLight");

            SprIconOptions = content.Load<Texture2D>("Menu/gearIcon");
            SprIconErase = content.Load<Texture2D>("Menu/trashIcon");
            SprIconCopy = content.Load<Texture2D>("Menu/copyIcon");

            // need to have pre multiplied alpha
            SprFog = content.Load<Texture2D>("Objects/fog");

            // load shader
            RoundedCornerEffect = content.Load<Effect>("Shader/RoundedCorner");
            BlurEffectH = content.Load<Effect>("Shader/BlurH");
            BlurEffectV = content.Load<Effect>("Shader/BlurV");
            BBlurEffectH = content.Load<Effect>("Shader/BBlurH");
            BBlurEffectV = content.Load<Effect>("Shader/BBlurV");
            FullShadowEffect = content.Load<Effect>("Shader/FullShadowEffect");
            // used in the inventory
            SaturationEffect = content.Load<Effect>("Shader/SaturationFilter");
            WobbleEffect = content.Load<Effect>("Shader/WobbleShader");
            CircleShader = content.Load<Effect>("Shader/CircleShader");
            LightShader = content.Load<Effect>("Shader/LightShader");
            LightFadeShader = content.Load<Effect>("Shader/LightFadeShader");

            var cloudShader = content.Load<Effect>("Shader/ColorCloud");
            CloudShader = new SpriteShader(cloudShader);
            CloudShader.FloatParameter.Add("scaleX", 1);
            CloudShader.FloatParameter.Add("scaleY", 1);

            NoiseTexture = GetTexture("thanos noise.png");
            ThanosShader = content.Load<Effect>("Shader/ThanosShader");
            ThanosShader.Parameters["NoiceTexture"].SetValue(NoiseTexture);
            // only works for sprites using the sequence sprite
            ThanosShader.Parameters["Scale"].SetValue(new Vector2(
                    (float)SprGameSequencesFinal.Width / NoiseTexture.Width,
                    (float)SprGameSequencesFinal.Height / NoiseTexture.Height));

            ThanosSpriteShader0 = new SpriteShader(ThanosShader);
            ThanosSpriteShader0.FloatParameter.Add("Percentage", 0);
            ThanosSpriteShader1 = new SpriteShader(ThanosShader);
            ThanosSpriteShader1.FloatParameter.Add("Percentage", 0);

            WindFishShader = new SpriteShader(content.Load<Effect>("Shader/WaleShader"));
            WindFishShader.FloatParameter.Add("Offset", 0);
            WindFishShader.FloatParameter.Add("Period", 0);

            ColorShader = new SpriteShader(content.Load<Effect>("Shader/ColorShader"));

            var damageShader = content.Load<Effect>("Shader/DamageShader");

            // crow needs mark1 to have a value bigger than 0.605333
            DamageSpriteShader0 = new SpriteShader(damageShader);
            DamageSpriteShader0.FloatParameter.Add("mark0", 0.1f);
            DamageSpriteShader0.FloatParameter.Add("mark1", 0.725f);

            // stone hinox needs mark1 to be below 0.553
            DamageSpriteShader1 = new SpriteShader(damageShader);
            DamageSpriteShader1.FloatParameter.Add("mark0", 0.1f);
            DamageSpriteShader1.FloatParameter.Add("mark1", 0.55f);

            var shockShader = content.Load<Effect>("Shader/ShockEffect");

            ShockShader0 = new SpriteShader(shockShader);
            ShockShader0.FloatParameter.Add("mark0", 0.0f);
            ShockShader0.FloatParameter.Add("mark1", 0.2675f);
            ShockShader0.FloatParameter.Add("mark2", 0.725f);

            ShockShader1 = new SpriteShader(shockShader);
            ShockShader1.FloatParameter.Add("mark0", 0.0f);
            ShockShader1.FloatParameter.Add("mark1", 0.35f);
            ShockShader1.FloatParameter.Add("mark2", 0.625f);
        }

        public static void LoadSounds(ContentManager content)
        {
            // load all the sound effects
            var soundEffectFiles = Directory.GetFiles(content.RootDirectory + "/SoundEffects").ToList();
            foreach (var path in soundEffectFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(path);
                AddSoundEffect(content, fileName);
            }
        }

        public static void AddSoundEffect(ContentManager content, string fileName)
        {
            var soundEffect = content.Load<SoundEffect>("SoundEffects/" + fileName);

            // try add is used because some files may already be loaded for the intro sequence
            SoundEffects.TryAdd(fileName, soundEffect);
        }

        public static void LoadContentTextureWithAtlas(ContentManager content, string filePath)
        {
            var texture = content.Load<Texture2D>(filePath);

            // load the sprite atlas
            var atlasPath = Values.PathContentFolder + filePath + ".atlas";
            SpriteAtlasSerialization.LoadSourceDictionary(texture, atlasPath, SpriteAtlas);
        }

        public static void LoadTexturesFromFolder(string path)
        {
            var texturePaths = Directory.GetFiles(path).ToList();

            foreach (var filePath in texturePaths)
            {
                if (!filePath.Contains(".png"))
                    continue;

                var newTexture = new Texture(Path.GetFileName(filePath));
                LoadTexture(out newTexture.SprTexture, filePath);
                TextureList.Add(newTexture);
            }
        }

        public static void LoadTexture(out Texture2D texture, string strFilePath)
        {
            using Stream stream = File.Open(strFilePath, FileMode.Open);

            texture = Texture2D.FromStream(Game1.Graphics.GraphicsDevice, stream);

            // load the sprite atlas
            var atlasFileName = strFilePath.Replace(".png", ".atlas");
            SpriteAtlasSerialization.LoadSourceDictionary(texture, atlasFileName, SpriteAtlas);
        }

        public static Texture2D GetTexture(string name)
        {
            for (var i = 0; i < TextureList.Count; i++)
            {
                if (TextureList[i].Name == name)
                    return TextureList[i].SprTexture;
            }

            return null;
        }

        public static Rectangle SourceRectangle(string id)
        {
            return SpriteAtlas.ContainsKey(id) ? SpriteAtlas[id].ScaledRectangle : Rectangle.Empty;
        }

        public static DictAtlasEntry GetSprite(string id)
        {
            return SpriteAtlas.ContainsKey(id) ? SpriteAtlas[id] : null;
        }

        public static void LoadTilesetSizes()
        {
            var fileName = Values.PathTilesetFolder + "tileset size.txt";

            if (File.Exists(fileName))
            {
                using var reader = new StreamReader(fileName);

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();

                    // comment?
                    if (line.StartsWith("//"))
                        continue;

                    var split = line.Split(':');
                    if (split.Length != 2)
                        continue;

                    if (int.TryParse(split[1], out var value))
                        TilesetSizes.Add(split[0], value);
                }
            }
        }
    }
}
