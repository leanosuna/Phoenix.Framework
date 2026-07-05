using System.Numerics;

namespace Phoenix.Framework.Maths
{
    public static class MathEx
    {
        public static float Atan2(float y, float x)
        {
            if (x == 0f && y == 0f)
                return 0f;
            return MathF.Atan2(y, x);
        }

        public static (float Cos, float Sin) ComputeCosSin(float radians)
        {
            return (MathF.Cos(radians), MathF.Sin(radians));
        }

        public static Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 point)
        {
            var ab = b - a;
            var lenSq = ab.LengthSquared();
            if (lenSq < 1e-12f)
                return a;
            var t = Vector3.Dot(point - a, ab) / lenSq;
            t = Math.Clamp(t, 0f, 1f);
            return a + t * ab;
        }

        public static float PointToSegmentDistance(Vector3 a, Vector3 b, Vector3 point)
        {
            var closest = ClosestPointOnSegment(a, b, point);
            return Vector3.Distance(point, closest);
        }

        public static float PointToSegmentDistanceSq(Vector3 a, Vector3 b, Vector3 point)
        {
            var closest = ClosestPointOnSegment(a, b, point);
            return Vector3.DistanceSquared(point, closest);
        }

        public static (Vector3 P1, float T1, Vector3 P2, float T2) SegmentDistance(
            Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2)
        {
            var d1 = q1 - p1;
            var d2 = q2 - p2;
            var r = p1 - p2;

            var a = Vector3.Dot(d1, d1);
            var e = Vector3.Dot(d2, d2);
            var f = Vector3.Dot(d2, r);

            float t1, t2;

            if (a <= 1e-12f && e <= 1e-12f)
            {
                return (p1, 0f, p2, 0f);
            }

            if (a <= 1e-12f)
            {
                t1 = 0f;
                t2 = Math.Clamp(f / e, 0f, 1f);
            }
            else
            {
                var c = Vector3.Dot(d1, r);
                if (e <= 1e-12f)
                {
                    t2 = 0f;
                    t1 = Math.Clamp(-c / a, 0f, 1f);
                }
                else
                {
                    var b = Vector3.Dot(d1, d2);
                    var denom = a * e - b * b;

                    if (MathF.Abs(denom) > 1e-12f)
                    {
                        var numer = b * f - c * e;
                        t1 = Math.Clamp(numer / denom, 0f, 1f);
                    }
                    else
                    {
                        t1 = 0f;
                    }

                    t2 = (b * t1 + f) / e;

                    if (t2 < 0f)
                    {
                        t2 = 0f;
                        t1 = Math.Clamp(-c / a, 0f, 1f);
                    }
                    else if (t2 > 1f)
                    {
                        t2 = 1f;
                        t1 = Math.Clamp((b - c) / a, 0f, 1f);
                    }
                }
            }

            return (p1 + t1 * d1, t1, p2 + t2 * d2, t2);
        }

        public static (Vector3 P1, Vector3 P2) LineDistance(
            Vector3 p1, Vector3 d1, Vector3 p2, Vector3 d2)
        {
            var a = Vector3.Dot(d1, d1);
            var e = Vector3.Dot(d2, d2);
            var b = Vector3.Dot(d1, d2);
            var r = p1 - p2;
            var c = Vector3.Dot(d1, r);
            var f = Vector3.Dot(d2, r);

            var denom = a * e - b * b;
            if (MathF.Abs(denom) < 1e-12f)
            {
                return (p1, p2);
            }

            var t1 = (b * f - c * e) / denom;
            var t2 = (a * f - b * c) / denom;

            return (p1 + t1 * d1, p2 + t2 * d2);
        }

        public static Vector3 ClosestPointOnTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 point)
        {
            var ab = b - a;
            var ac = c - a;
            var ap = point - a;

            var d1 = Vector3.Dot(ab, ap);
            var d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0f && d2 <= 0f)
                return a;

            var bp = point - b;
            var d3 = Vector3.Dot(ab, bp);
            var d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0f && d4 <= d3)
                return b;

            var vc = d1 * d4 - d3 * d2;
            if (vc <= 0f && d1 >= 0f && d3 <= 0f)
            {
                var v = d1 / (d1 - d3);
                return a + v * ab;
            }

            var cp = point - c;
            var d5 = Vector3.Dot(ab, cp);
            var d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0f && d5 <= d6)
                return c;

            var vb = d5 * d2 - d1 * d6;
            if (vb <= 0f && d2 >= 0f && d6 <= 0f)
            {
                var w = d2 / (d2 - d6);
                return a + w * ac;
            }

            var va = d3 * d6 - d5 * d4;
            if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
            {
                var w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return b + w * (c - b);
            }

            var denom = 1f / (va + vb + vc);
            var v2 = vb * denom;
            var w2 = vc * denom;
            return a + ab * v2 + ac * w2;
        }
    }
}
