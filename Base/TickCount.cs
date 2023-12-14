using System.Linq;

namespace ProjectZ.Base
{
    public class TickCounter
    {
        public int AverageTime;

        private readonly int[] _timeCounts;
        private int _currentIndex;

        public TickCounter(int average)
        {
            _timeCounts = new int[average];
        }

        public void AddTick(long tick)
        {
            _timeCounts[_currentIndex] = (int)tick;

            _currentIndex++;
            if (_currentIndex >= _timeCounts.Length)
                _currentIndex = 0;

            AverageTime = (int)_timeCounts.Average();
        }
    }
}
