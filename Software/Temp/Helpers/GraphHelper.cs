using System;
using System.Collections.Generic;
using System.Linq;
using Temp.Entities;

namespace Temp.Helpers
{
    public class GraphHelper
    {
        public GraphHelper()
        {
            IdealCurve = new List<double>();
            oldIdealCurve = new List<double>();
        }

        public void graphGeneration(Curve curve, double startingTemp)
        {
            wantedCurve = curve;

            IdealCurve = new List<double>
            {
                startingTemp
            };

            totalTime = 0;

            for (int i = 0; i < curve.points.Count;i++)
            {
                double actualAngle = IdealCurve.Last();
                double nextAngle = curve.points[i].TempValue;

                if (curve.points[i].TimeValue == 0)
                {
                    curve.points[i].TimeValue = 1;
                }

                double delta = nextAngle - actualAngle;
                double increment = delta / Math.Abs(curve.points[i].TimeValue);

                if (increment == (Math.Abs(nextAngle) - Math.Abs(actualAngle)))
                {
                    IdealCurve.Add(Math.Round((IdealCurve.Last() + increment), 2));
                }
                else
                {
                    for (int j = IdealCurve.Count; j < (curve.points[i].TimeValue + totalTime); j++)
                    {
                        IdealCurve.Add(Math.Round((IdealCurve.Last() + increment), 2));
                    }
                }
                totalTime += curve.points[i].TimeValue;
            }
        }

        public List<double> IdealCurve;

        public List<double> oldIdealCurve;

        public Curve wantedCurve;

        public int totalTime;
    }
}
