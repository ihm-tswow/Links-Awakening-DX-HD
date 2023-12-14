using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Pools;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Base.Systems
{
    class SystemBody
    {
        public ComponentPool Pool;

        private readonly List<GameObject> _objectList = new List<GameObject>();
        private readonly List<GameObject> _holeList = new List<GameObject>();

        public void Update(int threadIndex, int threadCount)
        {
            if (Game1.TimeMultiplier <= 0)
                return;

            _objectList.Clear();
            Pool.GetComponentList(_objectList,
                (int)((MapManager.Camera.X - Game1.RenderWidth / 2) / MapManager.Camera.Scale),
                (int)((MapManager.Camera.Y - Game1.RenderHeight / 2) / MapManager.Camera.Scale),
                (int)(Game1.RenderWidth / MapManager.Camera.Scale),
                (int)(Game1.RenderHeight / MapManager.Camera.Scale), BodyComponent.Mask);

            foreach (var gameObject in _objectList)
            {
                if (!gameObject.IsActive)
                    continue;

                var component = gameObject.Components[BodyComponent.Index] as BodyComponent;
                if (component.IsActive)
                    UpdateBody(component);
            }
        }

        private void UpdateBody(BodyComponent body)
        {
            var collisionType = Values.BodyCollision.None;

            body.SpeedMultiply = 1;
            body.WasGrounded = body.IsGrounded;

            if (!Pool.Map.Is2dMap)
            {
                // z position update
                if (!body.IgnoresZ)
                    collisionType |= UpdateVelocityZ(body);

                // hole pulling
                if (!body.IgnoreHoles)
                    UpdateHole(body);
                else
                    body.HoleAbsorption = Vector2.Zero;
            }

            var velocityTargetMult = 1f;

            // the speed gets limited by the velocity and the hole absorption vector
            if (!body.DisableVelocityTargetMultiplier)
            {
                float velocityLength;
                if (Pool.Map.Is2dMap && !body.CurrentFieldState.HasFlag(MapStates.FieldStates.DeepWater))
                    velocityLength = Math.Abs(body.Velocity.X) * 2.5f;
                else
                    velocityLength = (new Vector2(body.Velocity.X, body.Velocity.Y).Length() + body.HoleAbsorption.Length()) * 1.5f;

                velocityTargetMult = MathHelper.Clamp(1f - velocityLength, 0, 1);
            }
            body.DisableVelocityTargetMultiplier = false;

            var velocityTarget = body.VelocityTarget * body.SpeedMultiply * velocityTargetMult;

            // AdditionalMovement should slide because the raft is using it to move
            var bodyOffset = velocityTarget + body.HoleAbsorption + body.AdditionalMovementVT;
            var slideOffset = body.SlideOffset;

            var velocityOffset = (new Vector2(body.Velocity.X, body.Velocity.Y) * (0.5f + body.SpeedMultiply * 0.5f)) * Game1.TimeMultiplier;

            body.LastVelocityTarget = body.VelocityTarget;
            body.LastAdditionalMovementVT = body.AdditionalMovementVT;

            if (body.RestAdditionalMovement)
                body.AdditionalMovementVT = Vector2.Zero;

            body.SlideOffset = Vector2.Zero;

            collisionType |= MoveBody(body, slideOffset + bodyOffset * Game1.TimeMultiplier, body.CollisionTypes | body.AvoidTypes,
                             body.IsPusher, body.IsSlider, false);

            // in 2d mode the velocity is also used to push, currently used for stomping goombas
            // if the player gets pushed onto a push trigger it should not get activated
            collisionType |= MoveBody(body, velocityOffset, body.CollisionTypes, Pool.Map.Is2dMap && body.IsPusher, false, true);

            // set IsGrounded in 2d mode
            if (Pool.Map.Is2dMap)
            {
                body.IsGrounded = (collisionType & Values.BodyCollision.Vertical) != 0 && body.Velocity.Y > 0;

                if (body.IsGrounded)
                {
                    // bounce of the ground
                    if (!body.WasGrounded && body.Velocity.Y * body.Bounciness2D > 0.4f)
                        body.Velocity.Y = -body.Velocity.Y * body.Bounciness2D;
                    else
                        body.Velocity.Y = 0;
                }

                if (!body.IgnoresZ && (body.CurrentFieldState & MapStates.FieldStates.Init) == 0)
                {
                    if (!body.IgnoresZ && (body.CurrentFieldState & MapStates.FieldStates.DeepWater) == 0)
                        body.Velocity.Y += body.Gravity2D * Game1.TimeMultiplier;
                    else if (!body.IgnoresZ && (body.CurrentFieldState & MapStates.FieldStates.DeepWater) != 0)
                        body.Velocity.Y += body.Gravity2DWater * Game1.TimeMultiplier;
                }
            }

            var drag = body.IsGrounded ? body.Drag : body.DragAir;
            if (body.CurrentFieldState.HasFlag(MapStates.FieldStates.DeepWater))
                drag = body.DragWater;

            // apply drag if the body is grounded
            body.Velocity.X *= (float)Math.Pow(drag, Game1.TimeMultiplier);
            if (Math.Abs(body.Velocity.X) < 0.01f * Game1.TimeMultiplier)
                body.Velocity.X = 0;

            if (!Pool.Map.Is2dMap || body.CurrentFieldState.HasFlag(MapStates.FieldStates.DeepWater))
            {
                body.Velocity.Y *= (float)Math.Pow(drag, Game1.TimeMultiplier);
                if (Math.Abs(body.Velocity.Y) < 0.01f * Game1.TimeMultiplier)
                    body.Velocity.Y = 0;
            }

            if (body.Position.HasChanged())
                body.Position.NotifyListeners();

            // get the current field the body is on
            var lastFieldState = body.CurrentFieldState;
            if (body.UpdateFieldState)
                body.CurrentFieldState = GetFieldState(body);

            if (body.Position.Z <= 0 && body.SplashEffect && lastFieldState != MapStates.FieldStates.Init && (body.CurrentFieldState & MapStates.FieldStates.DeepWater) != 0)
            {
                if (body.Owner.Map.Is2dMap && (lastFieldState & MapStates.FieldStates.DeepWater) == 0)
                {
                    body.Velocity.Y *= 0.25f;

                    Game1.GameManager.PlaySoundEffect("D360-14-0E");

                    // spawn splash animation
                    var splashAnimator = new ObjAnimator(body.Owner.Map, 0, 0, 0, 3, 1, "Particles/splash", "idle", true);
                    splashAnimator.EntityPosition.Set(new Vector2(body.Position.X, body.Position.Y - 9));
                    Game1.GameManager.MapManager.CurrentMap.Objects.SpawnObject(splashAnimator);
                }

                body.OnDeepWaterFunction?.Invoke();
            }

            // inform the listener of the collision
            body.VelocityCollision = collisionType;
            if (collisionType != Values.BodyCollision.None)
                body.MoveCollision?.Invoke(collisionType);

            body.LastVelocityCollision = collisionType;
        }

        public static MapStates.FieldStates GetFieldState(BodyComponent body)
        {
            var state = Game1.GameManager.MapManager.CurrentMap.GetFieldState(
                            new Vector2(body.BodyBox.Box.X + body.BodyBox.Box.Width / 2, body.BodyBox.Box.Front - 0.01f)) &
                        ~(MapStates.FieldStates.Water | MapStates.FieldStates.DeepWater | MapStates.FieldStates.Lava) |
                        Game1.GameManager.MapManager.CurrentMap.GetFieldState(
                            new Vector2(body.BodyBox.Box.X + body.BodyBox.Box.Width / 2, body.BodyBox.Box.Front + body.DeepWaterOffset)) &
                        (MapStates.FieldStates.Water | MapStates.FieldStates.DeepWater | MapStates.FieldStates.Lava);

            return state;
        }

        public static Values.BodyCollision MoveBody(BodyComponent body, Vector2 offset, Values.CollisionTypes collisionTypes, bool isPusher, bool slide, bool ignoreField)
        {
            var collisionType = Values.BodyCollision.None;

            // move body in one step without aligning it to colliding objects
            if (body.SimpleMovement)
            {
                var collidingBox = Box.Empty;
                var direction = AnimationHelper.GetDirection(offset);

                if (!Collision(body, body.Position.X + offset.X, body.Position.Y + offset.Y, direction, collisionTypes, ignoreField, ref collidingBox))
                {
                    body.Position.X += offset.X;
                    body.Position.Y += offset.Y;
                }
                else
                {
                    // the returned collision type is not as precise as with the other method
                    collisionType |= (direction % 2 == 0 ? Values.BodyCollision.Horizontal : Values.BodyCollision.Vertical);

                    if (direction == 0)
                        collisionType |= Values.BodyCollision.Left;
                    else if (direction == 1)
                        collisionType |= Values.BodyCollision.Top;
                    else if (direction == 2)
                        collisionType |= Values.BodyCollision.Right;
                    else if (direction == 3)
                        collisionType |= Values.BodyCollision.Bottom;
                }

                return collisionType;
            }

            if (offset.X != 0)
            {
                var collidingBox = Box.Empty;

                // move horizontally
                if (!Collision(body, body.Position.X + offset.X, body.Position.Y, offset.X < 0 ? 0 : 2, collisionTypes, ignoreField, ref collidingBox))
                {
                    body.Position.X += offset.X;
                }
                else
                {
                    var nullBox = Box.Empty;
                    collisionType |= offset.X < 0 ? Values.BodyCollision.Left : Values.BodyCollision.Right;
                    collisionType |= Values.BodyCollision.Horizontal;

                    // try to move around the object if there is space around it
                    if (slide)
                    {
                        var sliderOffset = Math.Abs(offset.X * 0.5f);

                        if (offset.Y >= 0 && !Collision(body, body.Position.X + offset.X,
                            body.Position.Y + body.MaxSlideDistance, offset.X < 0 ? 0 : 2, collisionTypes, ignoreField, ref nullBox))
                        {
                            body.SlideOffset.Y += sliderOffset;
                        }
                        else if (offset.Y <= 0 && !Collision(body, body.Position.X + offset.X,
                            body.Position.Y - body.MaxSlideDistance, offset.X < 0 ? 0 : 2, collisionTypes, ignoreField, ref nullBox))
                        {
                            body.SlideOffset.Y -= sliderOffset;
                        }
                    }

                    // align with the collided object
                    if (offset.X < 0 &&
                        Math.Abs(body.Position.X - collidingBox.Right + body.OffsetX) < Math.Abs(offset.X) &&
                        !Collision(body, collidingBox.Right - body.OffsetX, body.Position.Y, 0, collisionTypes, ignoreField, ref nullBox))
                    {
                        body.Position.X = collidingBox.Right - body.OffsetX;
                    }
                    else if (offset.X > 0 &&
                             Math.Abs(body.Position.X - (collidingBox.X - (body.Width + body.OffsetX))) < Math.Abs(offset.X) &&
                             !Collision(body, collidingBox.X - (body.Width + body.OffsetX), body.Position.Y, 2, collisionTypes, ignoreField, ref nullBox))
                    {
                        body.Position.X = collidingBox.X - (body.Width + body.OffsetX);
                    }

                    // try to push the colliding object
                    // if this is done before the alignment it can happen that the body walks into the object it is pushing
                    if (isPusher && Math.Abs(offset.X) > Math.Abs(offset.Y))
                    {
                        var pushRectangle = new Box(
                            body.Position.X + offset.X + body.OffsetX, body.Position.Y + body.OffsetY, body.Position.Z, body.Width, body.Height, body.Depth);
                        Game1.GameManager.MapManager.CurrentMap.Objects.PushObject(
                            pushRectangle, new Vector2(Math.Sign(offset.X), 0), PushableComponent.PushType.Continues);
                    }
                }
            }

            if (offset.Y != 0)
            {
                var collidingBox = Box.Empty;

                // move vertically
                if (!Collision(body, body.Position.X, body.Position.Y + offset.Y, offset.Y < 0 ? 1 : 3, collisionTypes, ignoreField, ref collidingBox))
                {
                    body.Position.Y += offset.Y;
                }
                else
                {
                    var nullBox = Box.Empty;
                    collisionType |= offset.Y < 0 ? Values.BodyCollision.Top : Values.BodyCollision.Bottom;
                    collisionType |= Values.BodyCollision.Vertical;

                    // try to move around the object if there is space around it
                    if (slide)
                    {
                        var sliderOffset = Math.Abs(offset.Y * 0.5f);

                        if (offset.X >= 0 && !Collision(body, body.Position.X + body.MaxSlideDistance,
                            body.Position.Y + offset.Y, offset.Y < 0 ? 1 : 3, collisionTypes, ignoreField, ref nullBox))
                        {
                            body.SlideOffset.X += sliderOffset;
                        }
                        else if (offset.X <= 0 && !Collision(body, body.Position.X - body.MaxSlideDistance,
                            body.Position.Y + offset.Y, offset.Y < 0 ? 1 : 3, collisionTypes, ignoreField, ref nullBox))
                        {
                            body.SlideOffset.X -= sliderOffset;
                        }
                    }

                    // align with the floor
                    if (offset.Y < 0 &&
                        Math.Abs(body.Position.Y - (collidingBox.Front - body.OffsetY)) < Math.Abs(offset.Y) &&
                        !Collision(body, body.Position.X, collidingBox.Front - body.OffsetY, 1, collisionTypes, ignoreField, ref nullBox))
                    {
                        body.Position.Y = collidingBox.Front - body.OffsetY;
                    }
                    else if (offset.Y > 0 &&
                             Math.Abs(body.Position.Y - (collidingBox.Y - (body.Height + body.OffsetY))) < Math.Abs(offset.Y) &&
                             !Collision(body, body.Position.X, collidingBox.Y - (body.Height + body.OffsetY), 3, collisionTypes, ignoreField, ref nullBox))
                    {
                        body.Position.Y = collidingBox.Y - (body.Height + body.OffsetY);
                    }

                    // try to push the colliding object
                    if (isPusher && Math.Abs(offset.X) < Math.Abs(offset.Y))
                    {
                        var pushRectangle = new Box(
                            body.Position.X + body.OffsetX, body.Position.Y + offset.Y + body.OffsetY, body.Position.Z, body.Width, body.Height, body.Depth);
                        Game1.GameManager.MapManager.CurrentMap.Objects.PushObject(
                            pushRectangle, new Vector2(0, Math.Sign(offset.Y)), PushableComponent.PushType.Continues);
                    }
                }
            }

            return collisionType;
        }

        private Values.BodyCollision UpdateVelocityZ(BodyComponent body)
        {
            var collision = Values.BodyCollision.None;

            if (body.IgnoresZ)
            {
                body.IsGrounded = false;
                return collision;
            }

            // let the body fall
            var floorHeight = 0f;
            if (!body.IgnoreHeight)
            {
                // get the position of the floor at the position of the body
                var depthBox = new Box(
                    body.Position.X + body.OffsetX, body.Position.Y + body.OffsetY,
                    body.Position.Z - body.Depth + 1,
                    body.Width, body.Height, body.Depth);
                floorHeight = Game1.GameManager.MapManager.CurrentMap.Objects.GetDepth(
                    depthBox, body.CollisionTypes, body.JumpStartHeight + body.MaxJumpHeight);
            }

            body.Velocity.Z += body.Gravity * Game1.TimeMultiplier;
            body.Velocity.Z = Math.Clamp(body.Velocity.Z, -6, 6);

            // move the body up or down as long as it is not hitting the floor
            if (body.Position.Z + body.Velocity.Z * Game1.TimeMultiplier > floorHeight &&
                (!body.IsGrounded || body.Velocity.Z >= 0 || Math.Abs(floorHeight - body.Position.Z) > 2))
            {
                // set jump height at beginning of the jump
                if (body.IsGrounded)
                    body.JumpStartHeight = body.Position.Z;

                body.Position.Z += body.Velocity.Z * Game1.TimeMultiplier;
                body.IsGrounded = false;
            }
            else
            {
                // spawn splash animation
                if (body.CurrentFieldState.HasFlag(MapStates.FieldStates.Water) && !body.IgnoreHeight && body.Velocity.Z < -0.5f)
                {
                    var splashAnimator = new ObjAnimator(body.Owner.Map, 0, 0, 0, 3, Values.LayerPlayer, "Particles/splash", "idle", true);
                    splashAnimator.EntityPosition.Set(new Vector2(
                        body.Position.X + body.OffsetX + body.Width / 2f,
                        body.Position.Y + body.OffsetY + body.Height - body.Position.Z - 3));
                    Game1.GameManager.MapManager.CurrentMap.Objects.SpawnObject(splashAnimator);
                }

                // bounce from the ground but not on the water
                if (body.Velocity.Z * body.Bounciness < -0.4f &&
                    !body.CurrentFieldState.HasFlag(MapStates.FieldStates.DeepWater))
                    body.Velocity.Z *= -body.Bounciness;
                else
                    body.Velocity.Z = 0;

                if (!body.IsGrounded)
                    collision |= Values.BodyCollision.Floor;

                // don't move the body on top of the object it is colliding
                if (body.Position.Z > floorHeight ||
                    Math.Abs(body.Position.Z - floorHeight) <= 3)
                    body.Position.Z = floorHeight;

                body.JumpStartHeight = body.Position.Z;
                body.IsGrounded = true;
            }

            return collision;
        }

        private void UpdateHole(BodyComponent body)
        {
            // check for collisions with holes
            if (body.Position.Z > 0)
            {
                body.WasHolePulled = false;
                return;
            }

            var bodyBox = body.BodyBox.Box;
            var bodyArea = bodyBox.Width * bodyBox.Height;
            var bodyBoxCenter = body.BodyBox.Box.Center;

            var holeCollisionCoM = Vector2.Zero;
            var holeCollisionArea = 0.0f;

            var noneCollisionCoM = bodyBoxCenter;
            var noneCollisionArea = bodyBox.Width * bodyBox.Height;

            _holeList.Clear();
            Game1.GameManager.MapManager.CurrentMap.Objects.GetComponentList(
                _holeList, (int)bodyBox.X, (int)bodyBox.Y, (int)bodyBox.Width, (int)bodyBox.Height, CollisionComponent.Mask);

            foreach (var hole in _holeList)
            {
                if (!hole.IsActive)
                    continue;

                var collisionObject = hole.Components[CollisionComponent.Index] as CollisionComponent;
                var collidingBox = Box.Empty;
                if ((collisionObject.CollisionType & Values.CollisionTypes.Hole) == 0 ||
                    !collisionObject.Collision(bodyBox, 0, 0, ref collidingBox))
                    continue;

                var collidingRec = bodyBox.Rectangle().GetIntersection(collidingBox.Rectangle());
                var collidingArea = collidingRec.Width * collidingRec.Height;

                // center of mass for the holes
                holeCollisionCoM =
                    holeCollisionCoM * (holeCollisionArea / (holeCollisionArea + collidingArea)) +
                    collidingRec.Center * (collidingArea / (holeCollisionArea + collidingArea));

                // this makes sure to not cancle out two holes pulling the body into different directions; otherwise the body would be able to walk between them if he is aligned with the
                if (collidingArea == holeCollisionArea && holeCollisionCoM.X == bodyBoxCenter.X && collidingRec.Width * 2 != bodyBox.Width)
                    holeCollisionCoM.X -= 4;
                if (collidingArea == holeCollisionArea && holeCollisionCoM.Y == bodyBoxCenter.Y && collidingRec.Height * 2 != bodyBox.Height)
                    holeCollisionCoM.Y += 4;

                holeCollisionArea += collidingArea;
            }

            // calculate the new centers of mass and collision/none collision areas
            noneCollisionCoM += (noneCollisionCoM - holeCollisionCoM) * (holeCollisionArea / noneCollisionArea);
            noneCollisionArea -= holeCollisionArea;

            body.SpeedMultiply = 1 - holeCollisionArea / bodyArea;

            // the direction of the force applied to the body goes from the CoM of the body rectangle that is not colliding
            // to the CoM of the body rectangle that is colliding
            var holeDirection = holeCollisionCoM - noneCollisionCoM;
            if (holeDirection != Vector2.Zero)
                holeDirection.Normalize();

            body.IsAbsorbed = false;

            var collisionAreaPercentage = holeCollisionArea / bodyArea;

            // the body is getting absorbed
            if (holeCollisionArea >= bodyArea * body.AbsorbPercentage)
            {
                // absorption gets set to zero if the body jumped into the hole
                // fixes a bug where the player can push an object on the other side of a hole while falling into it
                if (!body.WasHolePulled)
                    body.HoleAbsorption = Vector2.Zero;

                body.Velocity = Vector3.Zero;// *= (float)Math.Pow(0.85f, Game1.TimeMultiplier);
                body.HoleAbsorption *= (float)Math.Pow(0.85f, Game1.TimeMultiplier);
                body.HoleAbsorb?.Invoke();
                body.IsAbsorbed = true;
            }
            // body is getting pulled towards the hole
            else if (collisionAreaPercentage > body.AbsorbStop)
            {
                var holePull = new Vector2(holeDirection.X, holeDirection.Y) * collisionAreaPercentage * 0.5f;

                // calculate the new direction of the hole pull 
                var oldPercentage = (float)Math.Pow(0.8f, Game1.TimeMultiplier);
                body.HoleAbsorption = body.HoleAbsorption * oldPercentage +
                                      holePull * (1 - oldPercentage);

                body.HoleOnPull?.Invoke(holePull, collisionAreaPercentage);
                body.WasHolePulled = true;
            }
            // stop the absorption
            else if (body.HoleAbsorption != Vector2.Zero)
            {
                body.HoleAbsorption = Vector2.Zero;
                body.HoleOnPull?.Invoke(Vector2.Zero, collisionAreaPercentage);
                body.WasHolePulled = false;
            }
        }

        public static bool Collision(BodyComponent body, float posX, float posY, int direction,
            Values.CollisionTypes collisionTypes, bool ignoreField, ref Box collidingBox)
        {
            // the +2 is to allow the body to move onto objects that are up to 2 higher
            var box = new Box(posX + body.OffsetX, posY + body.OffsetY,
                Math.Min(body.JumpStartHeight + body.MaxJumpHeight, body.Position.Z + 2), body.Width, body.Height, body.Depth);
            var oldBox = new Box(body.Position.X + body.OffsetX, body.Position.Y + body.OffsetY,
                Math.Min(body.JumpStartHeight + body.MaxJumpHeight, body.Position.Z), body.Width, body.Height, body.Depth);

            // check if the body is inside his allowed field or if he already left it
            if (!ignoreField && body.FieldRectangle.Width > 0 &&
                !body.FieldRectangle.Contains(box.Rectangle()) && body.FieldRectangle.Contains(oldBox.Rectangle()))
                return true;

            var cBox = Box.Empty;
            if (Game1.GameManager.MapManager.CurrentMap.Objects.Collision(
                box, body.IgnoreInsideCollision ? oldBox : Box.Empty, collisionTypes, body.CollisionTypesIgnore, direction, body.Level, ref cBox))
            {
                collidingBox = cBox;
                return true;
            }

            return false;
        }
    }
}
