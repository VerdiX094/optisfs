using System.Collections.Generic;

namespace OptiSFS
{
    public static class FastPolygon
    {
        public static bool FastIntersect(ConvexPolygon[] a, ConvexPolygon[] b, float overlapThreshold)
        {
            if (a.Length == 0 || b.Length == 0) return false;
            
            Circle[] bCache = new Circle[b.Length];
            
            for (int i = 0; i < a.Length; i++)
            {
                var circleA = a[i].GetCircle();
                for (int j = 0; j < b.Length; j++)
                {
                    if (i == 0) // first pass, cache needs to be built
                        bCache[j] = b[j].GetCircle();
                    
                    if (CirclesIntersect_WithLocal(circleA, bCache[j]))
                    {
                        if (ConvexPolygon.Intersect(a[i], b[j], overlapThreshold)) 
                            return true;
                    }
                }
            }

            return false;
        }

        private static bool CirclesIntersect(Circle a, Circle b) => (b.center - a.center).sqrMagnitude <= (a.radius + b.radius) * (a.radius + b.radius);

        private static bool CirclesIntersect_WithLocal(in Circle a, in Circle b)
        {
            float dx = a.center.x - b.center.x;
            float dy = a.center.y - b.center.y;

            float r = a.radius + b.radius;
            return dx * dx + dy * dy <= r * r;
        }
    }
}