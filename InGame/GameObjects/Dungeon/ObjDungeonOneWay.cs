using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;
using ProjectZ.InGame.GameObjects.Bosses;

namespace ProjectZ.InGame.GameObjects.Dungeon
{
    internal class ObjDungeonOneWay : GameObject
    {
        private readonly ObjAnimator _animatorTop;
        private readonly ObjAnimator _animatorBottom;

        private Vector2 _startPosition;
        private Vector2 _endPosition;

        private bool _isRotation;

        public ObjDungeonOneWay() : base("dungeonOneWay") { }

        public ObjDungeonOneWay(Map.Map map, int posX, int posY) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);

            _animatorTop = new Things.ObjAnimator(map, posX, posY, Values.LayerBottom, "Objects/dOneWay", "idle2", false);
            Map.Objects.SpawnObject(_animatorTop);

            _animatorBottom = new Things.ObjAnimator(map, posX, posY + 16, Values.LayerBottom, "Objects/dOneWay", "idle", false);
            Map.Objects.SpawnObject(_animatorBottom);

            var pushBox = new CBox(posX + 6, posY + 16, 0, 4, 16, 16);
            AddComponent(PushableComponent.Index, new PushableComponent(pushBox, OnPush) { InertiaTime = 50 });

            var collisionBox = new CBox(posX, posY, 0, 16, 32, 16);
            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(collisionBox, Values.CollisionTypes.Normal));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type != PushableComponent.PushType.Continues || MapManager.ObjLink.Direction != 1)
                return false;

            StartRotation();
            return true;
        }

        private void Update()
        {
            if (!_isRotation)
                return;

            MapManager.ObjLink.CanWalk = false;

            if (_animatorBottom.Animator.CurrentFrameIndex < 4)
            {
                var frameTime = _animatorBottom.Animator.GetAnimationTime(0, _animatorBottom.Animator.CurrentFrameIndex) +
                                _animatorBottom.Animator.FrameCounter;
                var maxTime = _animatorBottom.Animator.GetAnimationTime(0, 4);

                var state = (float)frameTime / maxTime;
                var currentPosition = Vector2.Lerp(_startPosition, _endPosition, state);
                MapManager.ObjLink.SetPosition(currentPosition);
            }

            if (_animatorBottom.Animator.CurrentFrameIndex >= 4)
            {
                _isRotation = false;
                MapManager.ObjLink.SetPosition(_endPosition);
                MapManager.ObjLink.IsVisible = true;
            }
        }

        private void StartRotation()
        {
            // hide the player
            MapManager.ObjLink.IsVisible = false;

            Game1.GameManager.PlaySoundEffect("D360-12-0C");

            _isRotation = true;

            // the player will be moved between these two points while transitioning to the other side
            _startPosition = new Vector2(MapManager.ObjLink.EntityPosition.X, MapManager.ObjLink.EntityPosition.Y);
            _endPosition = new Vector2(
                EntityPosition.X + 8,
                EntityPosition.Y - MapManager.ObjLink._body.OffsetY - MapManager.ObjLink._body.Height);

            _animatorTop.Animator.Play("rotate2");
            _animatorBottom.Animator.Play("rotate");
        }
    }
}