using System.Numerics;

namespace Phoenix.Framework.Collisions
{
    public static class RayCasts
    {
        public static bool RayVsAABB(Ray ray, Vector3 aabbMin, Vector3 aabbMax,
            out float hitFraction, out Vector3 hitPoint, out Vector3 hitNormal, float maxDist = float.MaxValue)
        {
            hitFraction = 0f;
            hitPoint = Vector3.Zero;
            hitNormal = Vector3.Zero;

            var invD = new Vector3(1f / ray.Direction.X, 1f / ray.Direction.Y, 1f / ray.Direction.Z);
            var t0 = (aabbMin.X - ray.Position.X) * invD.X;
            var t1 = (aabbMax.X - ray.Position.X) * invD.X;
            var tmin = MathF.Min(t0, t1);
            var tmax = MathF.Max(t0, t1);

            for (int i = 1; i < 3; i++)
            {
                var v = i == 1 ? ray.Direction.Y : ray.Direction.Z;
                var p = i == 1 ? ray.Position.Y : ray.Position.Z;
                var bMin = i == 1 ? aabbMin.Y : aabbMin.Z;
                var bMax = i == 1 ? aabbMax.Y : aabbMax.Z;

                var invV = 1f / v;
                var ty0 = (bMin - p) * invV;
                var ty1 = (bMax - p) * invV;
                var tymin = MathF.Min(ty0, ty1);
                var tymax = MathF.Max(ty0, ty1);

                if (tymin > tmax || tymax < tmin)
                    return false;

                if (tymin > tmin) tmin = tymin;
                if (tymax < tmax) tmax = tymax;
            }

            if (tmin > maxDist || tmax < 0f)
                return false;

            hitFraction = tmin >= 0f ? tmin : tmax;
            hitPoint = ray.Position + ray.Direction * hitFraction;

            var eps = 1e-6f;
            var center = (aabbMin + aabbMax) * 0.5f;
            var rel = hitPoint - center;
            var halfExt = (aabbMax - aabbMin) * 0.5f;
            var dMin = halfExt.X - MathF.Abs(rel.X);
            var dX = MathF.Abs(rel.X + eps) - halfExt.X;
            if (dX > 0f) hitNormal = new Vector3(MathF.Sign(rel.X) * dX, 0, 0);
            hitNormal = Vector3.Normalize(hitNormal);
            return true;
        }

        public static bool RayVsOBB(Ray ray, Vector3 obbPos, Vector3 obbSize,
            Matrix4x4 orientation, out float hitFraction, out Vector3 hitPoint,
            out Vector3 hitNormal, float maxDist = float.MaxValue)
        {
            hitFraction = 0f;
            hitPoint = Vector3.Zero;
            hitNormal = Vector3.Zero;

            Matrix4x4.Invert(orientation, out var invOrient);
            var localOrigin = Vector3.Transform(ray.Position - obbPos, invOrient);
            var localDir = Vector3.TransformNormal(ray.Direction, invOrient);
            var localRay = new Ray(localOrigin, localDir);

            var half = obbSize * 0.5f;
            if (!RayVsAABB(localRay, -half, half, out var localFrac, out var localPt, out var localN, maxDist))
                return false;

            hitFraction = localFrac;
            hitPoint = ray.Position + ray.Direction * localFrac;
            hitNormal = Vector3.Normalize(Vector3.Transform(localN, orientation));
            return true;
        }

        public static bool RayVsSphere(Ray ray, Vector3 center, float radius,
            out float hitFraction, out Vector3 hitPoint, out Vector3 hitNormal)
        {
            hitFraction = 0f;
            hitPoint = Vector3.Zero;
            hitNormal = Vector3.Zero;

            var toSphere = center - ray.Position;
            var a = Vector3.Dot(ray.Direction, ray.Direction);
            var b = -2f * Vector3.Dot(ray.Direction, toSphere);
            var c = Vector3.Dot(toSphere, toSphere) - radius * radius;
            var disc = b * b - 4f * a * c;

            if (disc < 0f) return false;

            var sqrtDisc = MathF.Sqrt(disc);
            var t0 = (-b - sqrtDisc) / (2f * a);
            var t1 = (-b + sqrtDisc) / (2f * a);

            if (t0 < 0f) t0 = t1;
            if (t0 < 0f) return false;

            hitFraction = t0;
            hitPoint = ray.Position + ray.Direction * t0;
            hitNormal = Vector3.Normalize(hitPoint - center);
            return true;
        }

        // Algorithm: Real-Time Collision Detection §5.1.9 and box3d b3RayCastCapsule
        public static bool RayVsCapsule(Ray ray, Vector3 pointA, Vector3 pointB, float radius,
            out float hitFraction, out Vector3 hitPoint, out Vector3 hitNormal, float maxDist = float.MaxValue)
        {
            hitFraction = 0f;
            hitPoint = Vector3.Zero;
            hitNormal = Vector3.Zero;

            if (maxDist <= 0f)
                return false;

            var d = pointB - pointA;
            var axisLengthSq = d.LengthSquared();

            const float tol = 1e-5f;
            if (axisLengthSq < tol * tol)
                return RayVsSphere(ray, (pointA + pointB) * 0.5f, radius, out hitFraction, out hitPoint, out hitNormal);

            var s = ray.Position - pointA;
            var axisLength = MathF.Sqrt(axisLengthSq);
            var axis = d / axisLength;
            var rr = radius * radius;

            var u = Vector3.Dot(s, axis);
            var closestAxis = axis * u;
            var sc = s - closestAxis;
            var sc2 = sc.LengthSquared();

            if (sc2 < rr)
            {
                var uClamped = Math.Clamp(u, 0f, axisLength);
                var cp = axis * uClamped;
                var scp = s - cp;
                var scp2 = scp.LengthSquared();

                if (scp2 < rr)
                {
                    hitPoint = ray.Position;
                    hitFraction = 0f;
                    var outLen = MathF.Sqrt(scp2);
                    hitNormal = outLen > 1e-12f ? scp / outLen : Vector3.UnitY;
                    return true;
                }

                Vector3 sphereCenter = u < 0f ? pointA : pointB;
                if (!RayVsSphere(ray, sphereCenter, radius, out float ts, out Vector3 pts, out Vector3 ns))
                    return false;
                if (ts > maxDist)
                    return false;
                hitFraction = ts;
                hitPoint = pts;
                hitNormal = ns;
                return true;
            }

            var dr = ray.Direction;
            var rayLength = dr.Length();
            if (rayLength < 1e-12f)
                return false;
            var rayAxis = dr / rayLength;

            var a12 = Vector3.Dot(axis, rayAxis);

            var hasFinite = !float.IsInfinity(maxDist);
            if (hasFinite && maxDist < float.MaxValue * 0.5f)
            {
                var extent = maxDist * Vector3.Dot(dr, axis);
                if (float.IsFinite(extent))
                {
                    var v = u + extent;
                    if ((u < -radius && v < -radius) || (axisLength + radius < u && axisLength + radius < v))
                        return false;
                }
            }
            else if (a12 == 0f && (u < -radius || axisLength + radius < u))
            {
                return false;
            }

            var det = 1f - a12 * a12;
            float tr;

            if (det < 1e-7f)
            {
                var perp = rayAxis - axis * a12;
                var perp2 = perp.LengthSquared();
                var beta = Vector3.Dot(sc, perp);
                var gamma = sc2 - rr;
                var disc = beta * beta - perp2 * gamma;

                if (beta >= 0f || disc < 0f)
                    return false;

                tr = gamma / (-beta + MathF.Sqrt(disc));
            }
            else
            {
                var invDet = 1f / det;
                var sa1 = u;
                var sa2 = Vector3.Dot(s, rayAxis);

                var t1 = (sa1 - a12 * sa2) * invDet;
                var t2 = (a12 * sa1 - sa2) * invDet;

                var p1 = axis * t1;
                var p2 = s + rayAxis * t2;
                var g = p2 - p1;
                var g2 = g.LengthSquared();

                if (g2 > rr)
                    return false;

                var h = MathF.Sqrt(MathF.Max(0f, (rr - g2) * invDet));
                tr = t2 - h;
            }

            var maxWorld = maxDist * rayLength;
            if (tr < 0f || (hasFinite && maxWorld < tr))
                return false;

            var tc = u + tr * a12;

            if (tc < 0f)
            {
                if (!RayVsSphere(ray, pointA, radius, out float ts, out Vector3 pts, out Vector3 ns))
                    return false;
                if (ts > maxDist)
                    return false;
                hitFraction = ts;
                hitPoint = pts;
                hitNormal = ns;
                return true;
            }

            if (axisLength < tc)
            {
                if (!RayVsSphere(ray, pointB, radius, out float ts, out Vector3 pts, out Vector3 ns))
                    return false;
                if (ts > maxDist)
                    return false;
                hitFraction = ts;
                hitPoint = pts;
                hitNormal = ns;
                return true;
            }

            hitFraction = tr / rayLength;
            hitPoint = pointA + s + rayAxis * tr;
            hitNormal = Vector3.Normalize(hitPoint - (pointA + axis * tc));
            return true;
        }

        public static bool RayVsTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2,
            out float hitFraction, out Vector3 hitPoint, out Vector3 hitNormal)
        {
            hitFraction = 0f;
            hitPoint = Vector3.Zero;
            hitNormal = Vector3.Zero;

            var edge1 = v1 - v0;
            var edge2 = v2 - v0;
            var h = Vector3.Cross(ray.Direction, edge2);
            var a = Vector3.Dot(edge1, h);

            if (MathF.Abs(a) < 1e-8f)
                return false;

            var f = 1f / a;
            var s = ray.Position - v0;
            var u = f * Vector3.Dot(s, h);

            if (u < 0f || u > 1f)
                return false;

            var q = Vector3.Cross(s, edge1);
            var v = f * Vector3.Dot(ray.Direction, q);

            if (v < 0f || u + v > 1f)
                return false;

            var t = f * Vector3.Dot(edge2, q);
            if (t < 1e-8f)
                return false;

            hitFraction = t;
            hitPoint = ray.Position + ray.Direction * t;
            hitNormal = Vector3.Normalize(Vector3.Cross(edge1, edge2));
            return true;
        }

        public static bool RayVsCylinder(Ray ray, Vector3 center, float radius, float halfHeight,
            out float hitFraction, out Vector3 hitPoint, out Vector3 hitNormal)
        {
            hitFraction = float.MaxValue;
            hitPoint = Vector3.Zero;
            hitNormal = Vector3.Zero;
            var hitAny = false;

            var dp = ray.Position - center;
            var ax = ray.Direction.X;
            var az = ray.Direction.Z;
            var px = dp.X;
            var pz = dp.Z;

            var a = ax * ax + az * az;
            if (a > 1e-12f)
            {
                var b = 2f * (px * ax + pz * az);
                var c = px * px + pz * pz - radius * radius;
                var disc = b * b - 4f * a * c;

                if (disc >= 0f)
                {
                    var sqrtDisc = MathF.Sqrt(disc);
                    var inv2a = 0.5f / a;
                    var t0 = (-b - sqrtDisc) * inv2a;
                    var t1 = (-b + sqrtDisc) * inv2a;

                    foreach (var t in new[] { t0, t1 })
                    {
                        if (t < 1e-6f) continue;
                        var y = ray.Position.Y + ray.Direction.Y * t;
                        if (y >= center.Y - halfHeight && y <= center.Y + halfHeight && t < hitFraction)
                        {
                            hitFraction = t;
                            hitPoint = ray.Position + ray.Direction * t;
                            var horizPt = new Vector3(hitPoint.X, center.Y, hitPoint.Z);
                            hitNormal = Vector3.Normalize(hitPoint - horizPt);
                            hitAny = true;
                        }
                    }
                }
            }

            if (MathF.Abs(ray.Direction.Y) >= 1e-12f)
            {
                var tTop = (center.Y + halfHeight - ray.Position.Y) / ray.Direction.Y;
                if (tTop >= 1e-6f && tTop < hitFraction)
                {
                    var p = ray.Position + ray.Direction * tTop;
                    if ((p.X - center.X) * (p.X - center.X) + (p.Z - center.Z) * (p.Z - center.Z) <= radius * radius)
                    { hitFraction = tTop; hitPoint = p; hitNormal = Vector3.UnitY; hitAny = true; }
                }
                var tBot = (center.Y - halfHeight - ray.Position.Y) / ray.Direction.Y;
                if (tBot >= 1e-6f && tBot < hitFraction)
                {
                    var p = ray.Position + ray.Direction * tBot;
                    if ((p.X - center.X) * (p.X - center.X) + (p.Z - center.Z) * (p.Z - center.Z) <= radius * radius)
                    { hitFraction = tBot; hitPoint = p; hitNormal = -Vector3.UnitY; hitAny = true; }
                }
            }

            return hitAny;
        }
    }
}
