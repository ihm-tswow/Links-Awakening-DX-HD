using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ProjectZ.InGame.GameObjects.Base.CObjects
{
    public class CPosition
    {
        public delegate void PositionChanged(CPosition newPosition);
        public Dictionary<Type, PositionChanged> PositionChangedDict = new Dictionary<Type, PositionChanged>();

        private Vector2 _parentOffset;

        private Vector2 _position;
        public Vector2 Position => _position;

        public Vector2 LastPosition { get; set; }
        public float _lastZ;

        public float X
        {
            get => _position.X;
            set => _position.X = value;
        }
        public float Y
        {
            get => _position.Y;
            set => _position.Y = value;
        }
        public float Z;

        public CPosition(float x, float y, float z)
        {
            _position = new Vector2(x, y);
            Z = z;
        }

        public CPosition(Vector3 position)
        {
            _position = new Vector2(position.X, position.Y);
            Z = position.Z;
        }

        public void AddPositionListener(Type type, PositionChanged positionChanged)
        {
            if (PositionChangedDict.ContainsKey(type))
                PositionChangedDict[type] += positionChanged;
            else
                PositionChangedDict.Add(type, positionChanged);
        }

        public void RemovePositionListener(Type type)
        {
            if (PositionChangedDict.ContainsKey(type))
                PositionChangedDict.Remove(type);
        }

        public void Set(Vector2 position)
        {
            _position = position;
            NotifyListeners();
        }

        public void Set(Vector3 position)
        {
            _position.X = position.X;
            _position.Y = position.Y;
            Z = position.Z;
            NotifyListeners();
        }

        public void SetZ(float posZ)
        {
            Z = posZ;
            NotifyListeners();
        }

        public void Set(CPosition position)
        {
            _position.X = position.X;
            _position.Y = position.Y;
            Z = position.Z;
            NotifyListeners();
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }

        public void Move(Vector2 velocity)
        {
            _position += velocity * Game1.TimeMultiplier;
            NotifyListeners();
        }

        public void Offset(Vector2 offset)
        {
            _position += offset;
            NotifyListeners();
        }

        public void NotifyListeners()
        {
            foreach (var listener in PositionChangedDict)
                listener.Value(this);

            LastPosition = Position;
            _lastZ = Z;
        }

        public void UpdateParent(CPosition position)
        {
            _position.X = position.X + _parentOffset.X;
            _position.Y = position.Y + _parentOffset.Y;
            Z = position.Z;
            NotifyListeners();
        }

        public void UpdateParentOffsetZ(CPosition position)
        {
            _position.X = position.X + _parentOffset.X;
            _position.Y = position.Y + _parentOffset.Y - position.Z;
            NotifyListeners();
        }

        public void SetParent(CPosition position, Vector2 offset, bool offsetZ = false)
        {
            _parentOffset = offset;

            if (!offsetZ)
            {
                UpdateParent(position);
                position.AddPositionListener(typeof(CPosition), UpdateParent);
            }
            else
            {
                UpdateParentOffsetZ(position);
                position.AddPositionListener(typeof(CPosition), UpdateParentOffsetZ);
            }
        }

        public bool HasChanged()
        {
            return _position != LastPosition || Z != _lastZ;
        }
    }
}
