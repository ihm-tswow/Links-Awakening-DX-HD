using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Systems;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjLeverStone : GameObject
    {
        private readonly List<GameObject> _collidingObjects = new List<GameObject>();
        private readonly CBox _box;

        private readonly Vector2 _startPosition;
        private readonly Vector2 _endPosition;

        private readonly int _direction;

        public ObjLeverStone() : base("movestone_0") { }

        public ObjLeverStone(Map.Map map, int posX, int posY, int direction) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            _direction = direction;

            _startPosition = new Vector2(posX, posY);
            _endPosition = new Vector2(posX, posY) + AnimationHelper.DirectionOffset[direction] * 16;

            _box = new CBox(EntityPosition, 0, 0, 0, 16, 16, 8);

            // does not deal damage in the real game
            var damageBox = new CBox(EntityPosition, 1, 1, 0, 14, 14, 8);
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Object, 2));
            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(_box, Values.CollisionTypes.Normal));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawSpriteComponent("movestone_0", EntityPosition, Vector2.Zero, Values.LayerBottom));
        }

        private void Update()
        {
            UpdatePosition(ObjPullLever.LeverState);
        }

        private void UpdatePosition(float amount)
        {
            var lastBox = _box.Box;

            EntityPosition.Set(Vector2.Lerp(_startPosition, _endPosition, amount));

            // @HACK: this kind of stuff should be inside the movement system

            // check for colliding bodies and push them forward
            _collidingObjects.Clear();
            Map.Objects.GetComponentList(_collidingObjects,
                (int)EntityPosition.Position.X - 1, (int)EntityPosition.Position.Y - 1, 18, 18, BodyComponent.Mask);

            foreach (var collidingObject in _collidingObjects)
            {
                var body = (BodyComponent)collidingObject.Components[BodyComponent.Index];

                if (body.BodyBox.Box.Intersects(_box.Box) && !body.BodyBox.Box.Intersects(lastBox))
                {
                    var offset = Vector2.Zero;
                    if (_direction == 2)
                        offset.X = _box.Box.Left - body.BodyBox.Box.Right - 0.05f;
                    else if (_direction == 0)
                        offset.X = _box.Box.Right - body.BodyBox.Box.Left + 0.05f;
                    else if (_direction == 3)
                        offset.Y = _box.Box.Back - body.BodyBox.Box.Front - 0.05f;
                    else if (_direction == 1)
                        offset.Y = _box.Box.Front - body.BodyBox.Box.Back + 0.05f;

                    SystemBody.MoveBody(body, offset, body.CollisionTypes, false, false, false);
                    body.Position.NotifyListeners();
                }
            }
        }
    }
}