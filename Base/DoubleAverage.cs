using System.Linq;

namespace ProjectZ.Base
{
    public class DoubleAverage
    {
        public double Average;

        private readonly double[] _timeCounts;
        private int _currentIndex;

        public DoubleAverage(int size)
        {
            _timeCounts = new double[size];
        }

        public void AddValue(double value)
        {
            _timeCounts[_currentIndex] = value;

            _currentIndex++;
            if (_currentIndex >= _timeCounts.Length)
                _currentIndex = 0;

            Average = _timeCounts.Average();
        }
    }
}
