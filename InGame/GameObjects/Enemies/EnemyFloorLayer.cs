using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using ProjectZ.InGame.Controls;
using System.Collections.Generic;
using ProjectZ.InGame.GameObjects.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    // not sure why I called it an enemy...
    internal class EnemyFloorLayer : GameObject
    {
        private readonly List<GameObject> _deactivatedGameObjects = new List<GameObject>();
        private readonly List<EnemyFloorLayerFloor> _spawnedTiles = new List<EnemyFloorLayerFloor>();

        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;

        private Vector2 _spawnPosition;
        private Vector2 _startPosition;
        private Vector2 _endPosition;
        private Vector2 _moveDirection;

        private float _soundCounter;
        private float _moveCounter;
        private int _moveIndex;

        private string _fullKey;
        private int _minMoveCount;

        private const float MoveSpeed = 0.5f;

        public EnemyFloorLayer() : base("floor layer") { }

        public EnemyFloorLayer(Map.Map map, int posX, int posY, int count, string fullKey) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 14, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _minMoveCount = count;
            _fullKey = fullKey;

            _spawnPosition = EntityPosition.Position;

            _body = new BodyComponent(EntityPosition, -8, -14, 16, 14, 8)
            {
                IgnoreHoles = true,
                CollisionTypes =
                    Values.CollisionTypes.Normal |
                    Values.CollisionTypes.Player,
                FieldRectangle = map.GetField(posX, posY)
            };

            var sprite = new CSprite(EntityPosition);
            var animator = AnimatorSaveLoad.LoadAnimator("Enemies/floor layer");
            animator.Play("idle");

            var animatorComponent = new AnimationComponent(animator, sprite, new Vector2(0, 2));

            _aiComponent = new AiComponent();

            var stateIdle = new AiState() { Init = InitIdle };
            var stateMove = new AiState(UpdateMoving);
            var stateDead = new AiState(UpdateDead) { Init = InitDead };

            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("move", stateMove);
            _aiComponent.States.Add("dead", stateDead);

            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, animatorComponent);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(CollisionComponent.Index, new BodyCollisionComponent(_body, Values.CollisionTypes.Enemy));
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush) { InertiaTime = 250 });
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite) { Height = 1.0f, Rotation = 0.1f });

            _aiComponent.ChangeState("idle");
        }

        private void InitIdle()
        {
            SetActive(true);
        }

        private void InitDead()
        {
            SetActive(false);
        }

        private void SetActive(bool active)
        {
            ((BodyCollisionComponent)Components[CollisionComponent.Index]).IsActive = active;
            ((DrawComponent)Components[DrawComponent.Index]).IsActive = active;
            ((DrawShadowComponent)Components[DrawShadowComponent.Index]).IsActive = active;
        }

        private void UpdateDead()
        {
            // remove the floor when the player leaves the room and not all tiles where layed
            if (!_body.FieldRectangle.Contains(MapManager.ObjLink.EntityPosition.Position) && _moveIndex != _minMoveCount + 1)
                Reactivate();
        }

        private void Reactivate()
        {
            _moveIndex = 0;
            EntityPosition.Set(_spawnPosition);
            _aiComponent.ChangeState("idle");

            ((PushableComponent)Components[PushableComponent.Index]).IsActive = true;

            // despawn the tiles
            foreach (var tile in _spawnedTiles)
            {
                // reactivate holes
                tile.SetHoleState(true);
                Map.Objects.DeleteObjects.Add(tile);

                // spawn the explosion effect
                var splashAnimator = new ObjAnimator(Map, (int)tile.EntityPosition.X,
                    (int)tile.EntityPosition.Y, 0, 0, Values.LayerTop, "Particles/spawn", "run", true);
                Map.Objects.SpawnObject(splashAnimator);
            }
            _spawnedTiles.Clear();
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type != PushableComponent.PushType.Continues)
                return false;

            StartMoving(direction);

            return false;
        }

        private void StartMoving(Vector2 direction)
        {
            _aiComponent.ChangeState("move");

            ((PushableComponent)Components[PushableComponent.Index]).IsActive = false;

            _moveDirection = direction;
            _startPosition = EntityPosition.Position;
            _endPosition = EntityPosition.Position;

            SetNewPosition(false);
        }

        private void UpdateMoving()
        {
            MapManager.ObjLink.FreezePlayer();

            _soundCounter -= Game1.DeltaTime;
            if (_soundCounter < 0)
            {
                _soundCounter += 55;
                Game1.GameManager.PlaySoundEffect("D360-62-3E", false);
            }

            // get the new direction the player is pushing
            var vecDirection = ControlHandler.GetMoveVector2();
            if (vecDirection != Vector2.Zero)
            {
                var newDirection = AnimationHelper.GetDirection(vecDirection);
                _moveDirection = AnimationHelper.DirectionOffset[newDirection];
            }

            _moveCounter += Game1.DeltaTime;
            // finished moving to the next segment?
            var time = 16 / (60 * MoveSpeed) * 1000;
            if (_moveCounter > time)
            {
                _moveCounter -= time;

                SetNewPosition(true);
            }

            // calculate the new position
            var percentage = _moveCounter / time;
            var newPercentage = Vector2.Lerp(_startPosition, _endPosition, percentage);
            EntityPosition.Set(newPercentage);
        }

        private void SetNewPosition(bool spawnFloor)
        {
            _moveIndex++;
            _startPosition = _endPosition;
            _endPosition = _startPosition + _moveDirection * 16;

            // spawn the floor
            if (spawnFloor)
            {
                var objFloor = new EnemyFloorLayerFloor(Map, (int)_startPosition.X - 8, (int)_startPosition.Y - 14);
                Map.Objects.SpawnObject(objFloor);
                _spawnedTiles.Add(objFloor);
            }

            // stop moving if there is no hole at next position
            if (!CheckForHole((int)_endPosition.X - 8, (int)_endPosition.Y - 14))
            {
                Game1.GameManager.PlaySoundEffect("D360-47-2F");

                // spawn the explosion effect
                var splashAnimator = new ObjAnimator(Map, (int)EntityPosition.X - 8,
                    (int)EntityPosition.Y - 14, 0, 0, Values.LayerTop, "Particles/spawn", "run", true);
                Map.Objects.SpawnObject(splashAnimator);

                if (_moveIndex > _minMoveCount && !string.IsNullOrEmpty(_fullKey))
                    Game1.GameManager.SaveManager.SetString(_fullKey, "1");

                _aiComponent.ChangeState("dead");
            }
        }

        private bool CheckForHole(int posX, int posY)
        {
            _deactivatedGameObjects.Clear();
            Map.Objects.GetObjectsOfType(_deactivatedGameObjects, typeof(ObjHole), posX, posY, 16, 16);
            Map.Objects.GetObjectsOfType(_deactivatedGameObjects, typeof(ObjLava), posX, posY, 16, 16);
            foreach (var gameObject in _deactivatedGameObjects)
                if (gameObject.IsActive &&
                    gameObject.EntityPosition.Position == new Vector2(posX, posY))
                    return true;

            return false;
        }
    }
}