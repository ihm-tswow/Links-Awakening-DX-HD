using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjColorDungeonNPC : GameObject
    {
        private readonly Animator _animator;
        private readonly AnimationComponent _animationComponent;

        private readonly string _personId;
        private readonly bool _isRed;

        private string _npcState;
        private float _movementCounter;
        private float _moveTime;
        private bool _isMoving;

        private Vector2 _startPosition;
        private Vector2 _endPosition;
        private Vector2 _centerPosition;
        private Vector2 _sidePosition;

        public ObjColorDungeonNPC() : base("npc_color_dungeon") { }

        public ObjColorDungeonNPC(Map.Map map, int posX, int posY, string personId, bool isRed) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _personId = personId;
            _isRed = isRed;

            _centerPosition = new Vector2(EntityPosition.X + (_isRed ? -8 : 8), EntityPosition.Y);
            _sidePosition = new Vector2(EntityPosition.X + (_isRed ? 11 : -11), EntityPosition.Y);

            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/npc_color_dungeon_" + (isRed ? "red" : "blue"));
            _animator.Play("idle");

            // already moved?
            _npcState = Game1.GameManager.SaveManager.GetString(_personId + "_state");
            if (_npcState == "1")
                EntityPosition.Set(_centerPosition);
            else if (_npcState == "2")
                EntityPosition.Set(_sidePosition);

            var sprite = new CSprite(EntityPosition);
            _animationComponent = new AnimationComponent(_animator, sprite, Vector2.Zero);

            var body = new BodyComponent(EntityPosition, -8, -16, 16, 16, 8);

            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(KeyChanged));
            AddComponent(BodyComponent.Index, body);
            AddComponent(CollisionComponent.Index, new BodyCollisionComponent(body, Values.CollisionTypes.Normal));
            AddComponent(InteractComponent.Index, new InteractComponent(body.BodyBox, Interact));
            AddComponent(BaseAnimationComponent.Index, _animationComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite));
        }

        private void Update()
        {
            if (_isMoving)
            {
                // stop player from moving
                MapManager.ObjLink.FreezePlayer();

                _movementCounter += Game1.DeltaTime;
                var movePercentage = MathHelper.Clamp(_movementCounter / _moveTime, 0, 1);

                var newPosition = Vector2.Lerp(_startPosition, _endPosition, movePercentage);
                EntityPosition.Set(newPosition);

                // finished moving?
                if (movePercentage >= 1)
                {
                    _isMoving = false;
                    _animator.Play("idle");

                    // finished moving to the side => say something
                    if (_npcState == "2")
                        Game1.GameManager.StartDialogPath(_isRed ? "npc_color_3" : "npc_color_4");
                }
            }
        }

        private bool Interact()
        {
            Game1.GameManager.StartDialogPath(_personId);
            return true;
        }

        private void KeyChanged()
        {
            _npcState = Game1.GameManager.SaveManager.GetString(_personId + "_state");
            // start moving?
            var moveString = _personId + "_move";
            var moveValue = Game1.GameManager.SaveManager.GetString(moveString);

            if (moveValue != null)
            {
                _animator.Play("walk");

                _startPosition = EntityPosition.Position;
                if (_npcState == "1")
                    _endPosition = _centerPosition;
                else
                    _endPosition = _sidePosition;

                _animationComponent.MirroredH = _startPosition.X < _endPosition.X;

                // 0.375 move speed
                _moveTime = Math.Abs(_startPosition.X - _endPosition.X) / 0.375f / 60f * 1000;

                _isMoving = true;
                _movementCounter = 0;

                Game1.GameManager.SaveManager.RemoveString(moveString);
            }
        }
    }
}