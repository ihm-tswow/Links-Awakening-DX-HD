using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using System;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjMermaid : GameObject
    {
        private readonly Animator _animator;
        private readonly AiComponent _aiComponent;
        private readonly BodyComponent _body;
        private readonly CSprite _sprite;
        private readonly BodyDrawComponent _drawComponent;
        private readonly AnimationComponent _animationComponent;
        private readonly BoxCollisionComponent _collisionComponent;

        private Vector2 _sitPosition;
        private int _sitDirection;

        private Vector2 _spawnPosition;

        private int _jumpCounter = 4;
        private bool _leave;

        public ObjMermaid() : base("mermaid") { }

        public ObjMermaid(Map.Map map, int posX, int posY, string spawnCondition) : base(map)
        {
            if (!string.IsNullOrEmpty(spawnCondition) && !SaveCondition.CheckCondition(spawnCondition))
            {
                IsDead = true;
                return;
            }

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _spawnPosition = EntityPosition.Position;

            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/npc_mermaid");
            _animator.Play("idle");

            _sprite = new CSprite(EntityPosition);
            _animationComponent = new AnimationComponent(_animator, _sprite, Vector2.Zero);

            _body = new BodyComponent(EntityPosition, -7, -10, 14, 10, 8)
            {
                Gravity = -0.075f,
            };

            _aiComponent = new AiComponent();

            var stateIdle = new AiState(UpdateIdle);
            var stateDive = new AiState(UpdateLeave) { Init = InitLeave };
            var stateHidden = new AiState();
            stateHidden.Trigger.Add(new AiTriggerCountdown(1000, null, EndHidden));
            var stateJump = new AiState(UpdateJump) { Init = InitJump };
            var stateJumpHidden = new AiState();
            stateJumpHidden.Trigger.Add(new AiTriggerCountdown(1000, null, EndJumpHidden));

            var stateSitHidden = new AiState(UpdateSitHidden) { Init = InitSitHidden };
            var stateSitJump = new AiState(UpdateSitJump) { Init = InitSitJump };
            var stateSit = new AiState(UpdateSit);

            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("dive", stateDive);
            _aiComponent.States.Add("hidden", stateHidden);
            _aiComponent.States.Add("jump", stateJump);
            _aiComponent.States.Add("jumpHidden", stateJumpHidden);

            _aiComponent.States.Add("sitHidden", stateSitHidden);
            _aiComponent.States.Add("sitJump", stateSitJump);
            _aiComponent.States.Add("sit", stateSit);

            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(CollisionComponent.Index, _collisionComponent = new BoxCollisionComponent(_body.BodyBox, Values.CollisionTypes.Enemy));
            AddComponent(InteractComponent.Index, new InteractComponent(_body.BodyBox, Interact));
            AddComponent(BaseAnimationComponent.Index, _animationComponent);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(DrawComponent.Index, _drawComponent = new BodyDrawComponent(_body, _sprite, Values.LayerPlayer));
            //AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, _sprite));

            if (Game1.GameManager.SaveManager.GetString("mermaid_state", "0") == "0")
                _aiComponent.ChangeState("idle");
            else
                _aiComponent.ChangeState("sitHidden");
        }

        private void InitSitHidden()
        {
            _drawComponent.IsActive = false;
        }

        private void UpdateSitHidden()
        {
            var playerDistance = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            if (playerDistance.Length() < 64)
                _aiComponent.ChangeState("sitJump");
        }

        private void InitSitJump()
        {
            _sitPosition = new Vector2(EntityPosition.X, EntityPosition.Y - 16);
            _drawComponent.IsActive = true;
            _body.Velocity.Z = 1.5f;
            _body.Gravity = -0.05f;

            Game1.GameManager.PlaySoundEffect("D360-14-0E");

            Splash();
            _animator.Play("stone_spawn");
        }

        private void UpdateSitJump()
        {
            // move upwards to the sitting position
            var newPosition = Vector2.Lerp(EntityPosition.Position, _sitPosition, 0.075f * Game1.TimeMultiplier);
            EntityPosition.Set(newPosition);

            if (_body.IsGrounded)
            {
                _sitDirection = MapManager.ObjLink.EntityPosition.X < EntityPosition.X ? -1 : 1;
                _animator.Play("sit_" + _sitDirection);

                _aiComponent.ChangeState("sit");
            }
        }

        private void UpdateSit()
        {
            var playerDistance = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            if (playerDistance.Length() < 64)
            {
                if (MapManager.ObjLink.EntityPosition.X < EntityPosition.X - _sitDirection * 4)
                {
                    _sitDirection = -1;
                    _animator.Play("sit_" + _sitDirection);
                }
                if (MapManager.ObjLink.EntityPosition.X > EntityPosition.X - _sitDirection * 4)
                {
                    _sitDirection = 1;
                    _animator.Play("sit_" + _sitDirection);
                }
            }
        }

        private void InitJump()
        {
            _leave = true;
            Splash();
            _drawComponent.IsActive = true;
            _collisionComponent.IsActive = true;

            _body.IsGrounded = false;
            _body.Velocity.Z = 1.25f;

            // find a target spot where there is water
            var tries = 10;
            var velocity = Vector2.Zero;
            var dirRadiant = Game1.RandomNumber.Next(0, 100) / 100f * MathF.PI * 2;

            // we first try a random direction and then go clockwise around until we find a spot where there is deep water
            while (tries > 0)
            {
                tries--;
                velocity = new Vector2(MathF.Sin(dirRadiant), MathF.Cos(dirRadiant));

                // is there water at the target position?
                if ((Map.GetFieldState(EntityPosition.Position + new Vector2(-3, -6) + velocity * 14) & MapStates.FieldStates.DeepWater) != 0 &&
                    (Map.GetFieldState(EntityPosition.Position + new Vector2(3, -6) + velocity * 14) & MapStates.FieldStates.DeepWater) != 0 &&
                    (Map.GetFieldState(EntityPosition.Position + new Vector2(-3, 0) + velocity * 14) & MapStates.FieldStates.DeepWater) != 0 &&
                    (Map.GetFieldState(EntityPosition.Position + new Vector2(3, 0) + velocity * 14) & MapStates.FieldStates.DeepWater) != 0)
                    break;

                dirRadiant += MathF.PI / 5;
            }
            _body.VelocityTarget = velocity * 0.45f;

            _animationComponent.MirroredH = velocity.X < 0;
            _animator.Play("jump");
            _jumpCounter--;

            Game1.GameManager.PlaySoundEffect("D360-36-24");
        }

        private void UpdateJump()
        {
            if (_body.IsGrounded)
            {
                _body.VelocityTarget = Vector2.Zero;
                _aiComponent.ChangeState("dive");
            }
        }

        private void EndJumpHidden()
        {
            _aiComponent.ChangeState("jump");
        }

        private void UpdateIdle()
        {
            var playerDistance = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            var distance = playerDistance.Length();
            if (distance < 128)
            {
                if (MathF.Abs(playerDistance.X) > 8)
                    _animationComponent.MirroredH = playerDistance.X > 0;
            }
            if (distance < 18)
            {
                if (MapManager.ObjLink.IsDiving() && !Game1.GameManager.DialogIsRunning())
                {
                    Game1.GameManager.StartDialogPath("npc_mermaid_dive");
                }
            }
        }

        private void InitLeave()
        {
            _animator.Play("leave");

            if (MapManager.ObjLink.IsDiving())
                MapManager.ObjLink.ShortenDive();
        }

        private void UpdateLeave()
        {
            if (!_animator.IsPlaying)
            {
                Splash();
                _drawComponent.IsActive = false;
                _collisionComponent.IsActive = false;

                if (!_leave)
                {
                    _aiComponent.ChangeState("hidden");

                    var posX = (_spawnPosition.X - EntityPosition.X) / 48;
                    var offsetX = ((posX + Game1.RandomNumber.Next(1, 3)) % 3) * 48;
                    EntityPosition.Set(new Vector2(_spawnPosition.X - offsetX, _spawnPosition.Y));
                }
                else
                {
                    if (0 < _jumpCounter)
                        _aiComponent.ChangeState("jumpHidden");
                    else
                        Map.Objects.DeleteObjects.Add(this);
                }
            }
        }

        private void EndHidden()
        {
            Splash();
            _animator.Play("idle");
            _drawComponent.IsActive = true;
            _collisionComponent.IsActive = true;
            _aiComponent.ChangeState("idle");
        }

        private void OnKeyChange()
        {
            var diveKey = Game1.GameManager.SaveManager.GetString("npc_mermaid_dive");
            if (!string.IsNullOrEmpty(diveKey) && diveKey == "1")
            {
                Game1.GameManager.SaveManager.RemoveString("npc_mermaid_dive");
                _aiComponent.ChangeState("dive");
            }

            var jumpKey = Game1.GameManager.SaveManager.GetString("npc_mermaid_leave");
            if (!string.IsNullOrEmpty(jumpKey) && jumpKey == "1")
            {
                Game1.GameManager.SaveManager.RemoveString("npc_mermaid_leave");
                _aiComponent.ChangeState("jump");
            }
        }

        private bool Interact()
        {
            Game1.GameManager.StartDialogPath("npc_mermaid");
            return true;
        }

        private void Splash()
        {
            var objSplash = new ObjAnimator(Map, (int)EntityPosition.X, (int)EntityPosition.Y, 0, 0, Values.LayerBottom, "Particles/fishingSplash", "idle", true);
            Map.Objects.SpawnObject(objSplash);
        }
    }
}