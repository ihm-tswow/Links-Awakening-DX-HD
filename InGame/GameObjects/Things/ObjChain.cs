using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjChain : GameObject
    {
        class Chain
        {
            public Vector2 StartPosition;
            public Vector2 EndPosition;
            public float Height;

            public Chain(Vector2 startPosition, Vector2 endPosition)
            {
                StartPosition = startPosition;
                EndPosition = endPosition;
            }
        }

        private const int ChainCount = 6;

        private ObjSprite[] _objChains = new ObjSprite[ChainCount - 1];
        private Chain[] _chains = new Chain[ChainCount];

        private float _chainLength;
        private float _chainLengthInit = 7.5f;
        private float _chainLengthEnd = 4f;

        public ObjChain(Map.Map map, Vector2 startPosition) : base(map)
        {
            // init the chain
            for (var i = 0; i < ChainCount; i++)
            {
                _chains[i] = new Chain(startPosition, startPosition);
            }

            for (var i = 0; i < ChainCount - 1; i++)
            {
                _objChains[i] = new ObjSprite(map, (int)startPosition.X, (int)startPosition.Y, "bowwow chain", Vector2.Zero, Values.LayerPlayer, null);
                map.Objects.SpawnObject(_objChains[i]);
            }
        }

        public void SetChainPosition(Vector2 position)
        {
            for (var i = 0; i < _chains.Length; i++)
            {
                _chains[i].StartPosition = position;
                _chains[i].EndPosition = position;
            }
        }

        public void UpdateChain(Vector3 startPosition, Vector3 endPosition)
        {
            var distance = (new Vector2(startPosition.X, startPosition.Y) - new Vector2(endPosition.X, endPosition.Y)).Length();
            if ((distance - _chainLengthEnd) > (_chains.Length - 1) * _chainLengthInit)
                _chainLength = (distance - _chainLengthEnd) / (_chains.Length - 1);
            else
                _chainLength = _chainLengthInit;

            BackwardPass(endPosition);
            ForwardPass(startPosition);
        }

        private void BackwardPass(Vector3 goalPosition)
        {
            _chains[_chains.Length - 1].EndPosition = new Vector2(goalPosition.X, goalPosition.Y);
            _chains[_chains.Length - 1].Height = goalPosition.Z;

            for (var i = _chains.Length - 1; i > 0; i--)
            {
                var direction = _chains[i].StartPosition - _chains[i].EndPosition;
                var chainLength = i < _chains.Length - 1 ? _chainLength : _chainLengthEnd;
                if (direction.Length() > chainLength)
                {
                    direction.Normalize();
                    direction *= chainLength;
                }
                _chains[i].StartPosition = _chains[i].EndPosition + direction;
                _chains[i - 1].EndPosition = _chains[i].StartPosition;

                if (_chains[i].Height > 1.5f)
                    _chains[i - 1].Height = _chains[i].Height - 1.5f;
                else
                    _chains[i - 1].Height = 0;
            }
        }

        private void ForwardPass(Vector3 startPosition)
        {
            _chains[0].StartPosition = new Vector2(startPosition.X, startPosition.Y);
            _chains[0].Height = startPosition.Z * 0.75f;

            for (var i = 0; i < _chains.Length; i++)
            {
                var direction = _chains[i].EndPosition - _chains[i].StartPosition;
                var chainLength = i < _chains.Length - 1 ? _chainLength : _chainLengthEnd;
                if (direction.Length() > chainLength)
                {
                    direction.Normalize();
                    direction *= chainLength;
                }

                _chains[i].EndPosition = _chains[i].StartPosition + direction;

                if (i < _objChains.Length)
                {
                    _objChains[i].EntityPosition.Set(new Vector2(_chains[i].EndPosition.X, _chains[i].EndPosition.Y + 3));
                    _objChains[i].EntityPosition.Z = _chains[i].Height;
                }

                if (i < _chains.Length - 1)
                {
                    _chains[i + 1].StartPosition = _chains[i].EndPosition;

                    if (_chains[i].Height > (i + 1) * 3f + 3f)
                        _chains[i + 1].Height = _chains[i].Height - ((i + 1) * 3f + 3f);
                    else
                        _chains[i + 1].Height = 0;
                }
            }
        }

        public Vector2 GetEndPosition()
        {
            return _chains[_chains.Length - 1].EndPosition;
        }
    }
}