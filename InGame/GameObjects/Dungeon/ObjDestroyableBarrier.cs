using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Dungeon
{
    internal class ObjDestroyableBarrier : GameObject
    {
        public readonly string SaveKey;
        private readonly string _pushString;
        private readonly bool _playSound;

        public ObjDestroyableBarrier(Map.Map map, int posX, int posY, Rectangle sourceRectangle, string saveId, int rotation, bool playSound, string pushString) : base(map)
        {
            EditorIconSource = sourceRectangle;
            SprEditorImage = Resources.SprObjects;

            EntityPosition = new CPosition(
                posX + sourceRectangle.Width / 2f,
                posY + sourceRectangle.Height / 2f, 0);
            EntitySize = new Rectangle(-sourceRectangle.Width / 2, -sourceRectangle.Height / 2, sourceRectangle.Width, sourceRectangle.Height);

            // don't spawn the wall if it was already destroyed
            if (!string.IsNullOrEmpty(saveId) &&
                Game1.GameManager.SaveManager.GetString(saveId) == "1")
            {
                IsDead = true;
                return;
            }

            SaveKey = saveId;
            _playSound = playSound;
            _pushString = pushString;

            var box = new CBox(EntityPosition.X + EntitySize.X, EntityPosition.Y + EntitySize.Y, 0, sourceRectangle.Width, sourceRectangle.Height, 16);
            var sprite = new CSprite(Resources.SprObjects, EntityPosition, EditorIconSource, new Vector2(0, 0));
            sprite.Center = new Vector2(sourceRectangle.Width / 2f, sourceRectangle.Height / 2f);
            sprite.Rotation = rotation * MathF.PI / 2f;

            if (!string.IsNullOrEmpty(saveId))
                AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
            if (!string.IsNullOrEmpty(_pushString))
                AddComponent(PushableComponent.Index, new PushableComponent(box, OnPush));
            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(box, Values.CollisionTypes.Normal | Values.CollisionTypes.Destroyable));
            AddComponent(HittableComponent.Index, new HittableComponent(box, OnHit));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(sprite, Values.LayerBottom));
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType pushType)
        {
            Game1.GameManager.StartDialogPath(_pushString);
            return false;
        }

        private void OnKeyChange()
        {
            if (Game1.GameManager.SaveManager.GetString(SaveKey) == "1")
            {
                SpawnParticleStones();
                Map.Objects.DeleteObjects.Add(this);
            }
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            // gets destroyed by a bomb
            if (damageType == HitType.Bomb)
            {
                Map.Objects.DeleteObjects.Add(this);

                if (_playSound)
                    Game1.GameManager.PlaySoundEffect("D360-02-02");
                    
                Game1.GameManager.PlaySoundEffect("D378-09-09");

                if (!string.IsNullOrEmpty(SaveKey))
                    Game1.GameManager.SaveManager.SetString(SaveKey, "1");

                SpawnParticleStones();

                return Values.HitCollision.Blocking;
            }

            return Values.HitCollision.None;
        }

        private void SpawnParticleStones()
        {
            var rndMin = 15;
            var rndMax = 25;

            var vector0 = new Vector3(-1, -1, 0) * Game1.RandomNumber.Next(rndMin, rndMax) / 100f;
            var vector1 = new Vector3(-1, 0, 0) * Game1.RandomNumber.Next(rndMin, rndMax) / 100f;
            var vector2 = new Vector3(1, -1, 0) * Game1.RandomNumber.Next(rndMin, rndMax) / 100f;
            var vector3 = new Vector3(1, 0, 0) * Game1.RandomNumber.Next(rndMin, rndMax) / 100f;

            vector0.Z = 1.25f;
            vector1.Z = 1.25f;
            vector2.Z = 1.25f;
            vector3.Z = 1.25f;

            var stone0 = new ObjSmallStone(Map, (int)EntityPosition.X - 2, (int)EntityPosition.Y - 6, 0, vector0, true);
            var stone1 = new ObjSmallStone(Map, (int)EntityPosition.X - 2, (int)EntityPosition.Y - 1, 0, vector1, true);
            var stone2 = new ObjSmallStone(Map, (int)EntityPosition.X + 3, (int)EntityPosition.Y - 6, 0, vector2, false);
            var stone3 = new ObjSmallStone(Map, (int)EntityPosition.X + 3, (int)EntityPosition.Y - 1, 0, vector3, false);

            Map.Objects.SpawnObject(stone0);
            Map.Objects.SpawnObject(stone1);
            Map.Objects.SpawnObject(stone2);
            Map.Objects.SpawnObject(stone3);
        }
    }
}
