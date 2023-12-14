using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;
using System;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjHoneycomb : GameObject
    {
        private readonly CSprite _sprite;

        private ObjBees[] _objBee = new ObjBees[6];
        private ObjPersonNew _objFollowerTarget;

        private bool _spawnBees;
        private int _spawnIndex;
        private double _spawnCounter;
        private const int SpawnTime = 175;

        private bool _fallDown;
        private double _fallCounter;
        private const double FallTime = 250;

        public ObjHoneycomb() : base("trade5Map") { }

        public ObjHoneycomb(Map.Map map, int posX, int posY, string saveKey) : base(map)
        {
            EntityPosition = new CPosition(posX, posY + 24, 0);
            EntitySize = new Rectangle(0, -32, 16, 32);

            if (!string.IsNullOrEmpty(saveKey) &&
                Game1.GameManager.SaveManager.GetString(saveKey) == "1")
            {
                SpawnItem();
                IsDead = true;
                return;
            }

            var body = new BodyComponent(EntityPosition, 0, 0, 14, 14, 8);
            _sprite = new CSprite("trade5Map", EntityPosition, new Vector2(0, -24));

            AddComponent(BodyComponent.Index, body);
            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerPlayer));
        }

        public override void Init()
        {
            // get tarin to parent the stick to
            var objTarin = Map.Objects.GetObjectOfType((int)EntityPosition.X, (int)EntityPosition.Y, 32, 32, typeof(ObjPersonNew));
            if (objTarin != null)
            {
                var objStick = new ObjPersonNew(Map, (int)objTarin.EntityPosition.X, (int)objTarin.EntityPosition.Y, null, "tarin stick", "tarinStick", "pHidden", new Rectangle(0, 0, 8, 8));
                ((BodyCollisionComponent)objStick.Components[CollisionComponent.Index]).IsActive = false;
                objStick.EntityPosition.SetParent(objTarin.EntityPosition, Vector2.Zero, true);
                Map.Objects.SpawnObject(objStick);
            }

            _objFollowerTarget = new ObjPersonNew(Map, (int)EntityPosition.X, (int)EntityPosition.Y - 24, null, "bee target", "beeTarget", null, new Rectangle(0, 0, 8, 8));
            Map.Objects.SpawnObject(_objFollowerTarget);

            SpawnBee(0);
        }

        private void OnKeyChange()
        {
            var honeycombFall = Game1.GameManager.SaveManager.GetString("honeycombFall");
            if (honeycombFall != null && honeycombFall == "1")
            {
                _fallDown = true;
                Game1.GameManager.SaveManager.RemoveString("honeycombFall");
            }
            var honeycombHit = Game1.GameManager.SaveManager.GetString("honeycombHit");
            if (honeycombHit != null && honeycombHit == "1")
            {
                _sprite.DrawOffset.X -= 1;
                Game1.GameManager.SaveManager.RemoveString("honeycombHit");
            }
            var honeycombReset = Game1.GameManager.SaveManager.GetString("honeycombReset");
            if (honeycombReset != null && honeycombReset == "1")
            {
                if (!_spawnBees)
                {
                    _objFollowerTarget.EntityPosition.Set(new Vector2(EntityPosition.X - 12, EntityPosition.Y - 0));
                    _spawnBees = true;
                    _spawnCounter = SpawnTime;
                    _spawnIndex = 1;
                    _objBee[0].SetAngryMode();
                }
                _sprite.DrawOffset.X += 1;
                Game1.GameManager.SaveManager.RemoveString("honeycombReset");
            }
            var honeycombAttack = Game1.GameManager.SaveManager.GetString("honeycombAttack");
            if (honeycombAttack != null && honeycombAttack == "1")
            {
                for (var i = 0; i < _objBee.Length; i++)
                    StartFollowing(i);
                Game1.GameManager.SaveManager.RemoveString("honeycombAttack");
            }
            var honeycombFade = Game1.GameManager.SaveManager.GetString("honeycombFade");
            if (!string.IsNullOrEmpty(honeycombFade))
            {
                var fadeTime = int.Parse(honeycombFade);
                for (var i = 0; i < _objBee.Length; i++)
                    _objBee[i].FadeAway(fadeTime);
                Game1.GameManager.SaveManager.RemoveString("honeycombFade");
            }
        }

        public void Update()
        {
            if (_fallDown)
            {
                _fallCounter += Game1.DeltaTime;
                _sprite.DrawOffset.X = -MathF.Sin((float)(_fallCounter / FallTime) * MathF.PI * 2);

                if (_fallCounter > FallTime)
                {
                    SpawnItem();
                    Map.Objects.DeleteObjects.Add(this);
                }
            }

            // look at tarin
            if (_spawnBees && _spawnCounter > 2350)
            {
                var playerDirection = new Vector2(_objFollowerTarget.EntityPosition.X, _objFollowerTarget.EntityPosition.Y + 16) - MapManager.ObjLink.EntityPosition.Position;
                if (playerDirection != Vector2.Zero)
                    playerDirection.Normalize();

                var playerDir = AnimationHelper.GetDirection(playerDirection, MathF.PI * 1.175f);
                MapManager.ObjLink.SetWalkingDirection(playerDir);
            }

            if (_spawnBees)
                _spawnCounter += Game1.DeltaTime;
            if (_spawnCounter < SpawnTime || _spawnIndex >= 6)
                return;

            SpawnBee(_spawnIndex);
            _objBee[_spawnIndex].SetAngryMode();

            _spawnCounter -= SpawnTime;
            _spawnIndex++;
        }

        private void SpawnBee(int index)
        {
            _objBee[index] = new ObjBees(Map, new Vector2((int)EntityPosition.X + 8, (int)EntityPosition.Y - 12), _objFollowerTarget, index == 0);
            Map.Objects.SpawnObject(_objBee[index]);
        }

        private void StartFollowing(int index)
        {
            var radiants = index / 6f * MathF.PI * 2;
            var offset = new Vector2(MathF.Sin(radiants), MathF.Cos(radiants)) * Game1.RandomNumber.Next(4, 7);
            _objBee[index].SetFollowMode(offset);
        }

        private void SpawnItem()
        {
            var objItem = new ObjItem(Map, (int)EntityPosition.X, (int)EntityPosition.Y, null, "ow_honeycomb", "trade5", null);
            if (!objItem.IsDead)
            {
                objItem.EntityPosition.Set(new Vector3(EntityPosition.X + 8, EntityPosition.Y + 8, 16));
                ((BodyComponent)objItem.Components[BodyComponent.Index]).Bounciness = 0;
                Map.Objects.SpawnObject(objItem);
            }
        }
    }
}