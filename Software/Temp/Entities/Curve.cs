using System.Collections.Generic;

namespace Temp.Entities
{
    public class Curve
    {
        public Curve() {
            points = new List<Point>();
        }
        public string Name;
        public List<Point> points;
    }
}
