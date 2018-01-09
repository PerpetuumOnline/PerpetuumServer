using System;

namespace Perpetuum.PathFinders
{
    public class Heuristic
    {
        public readonly static Heuristic None      = new Heuristic((dx, dy) => 0);
        public readonly static Heuristic Manhattan = new Heuristic((dx, dy) => dx + dy);
        public readonly static Heuristic Euclidean = new Heuristic((dx, dy) => (int) Math.Sqrt(dx * dx + dy * dy));
        public readonly static Heuristic Chebyshev = new Heuristic(Math.Max);

        public delegate int HeuristicCalculator(int dx, int dy);

        private readonly HeuristicCalculator _calculator;

        public Heuristic(HeuristicCalculator calculator)
        {   
            _calculator = calculator;
        }

        public int Calculate(int currentX,int currentY,int endX,int endY)
        {
            var dx = Math.Abs(currentX - endX);
            var dy = Math.Abs(currentY - endY);
            return _calculator(dx, dy);
        }
    }
}