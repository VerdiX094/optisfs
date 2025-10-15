using UnityEngine;

namespace OptiSFS
{
    public static class FastPolygon
    {
        public static bool FastIntersect(ConvexPolygon[] a, ConvexPolygon[] b, float overlapThreshold)
        {
            for (int i = 0; i < a.Length; i++)
            {
                var circleA = a[i].GetCircle();
                for (int j = 0; j < b.Length; j++)
                {
                    var circleB = b[j].GetCircle();

                    if (CirclesIntersect(circleA.Item1, circleA.Item2, circleB.Item1, circleB.Item2))
                    {
                        if (ConvexPolygon.Intersect(a[i], b[j], overlapThreshold)) 
                            return true;
                    }
                }
            }

            return false;
        }

        private static bool CirclesIntersect(Vector2 cenA, float rA, Vector2 cenB, float rB) => (cenB - cenA).sqrMagnitude <= (rA + rB) * (rA * rB);
    }
}