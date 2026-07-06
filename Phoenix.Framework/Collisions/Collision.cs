using Phoenix.Framework.Maths;
using System;
using System.Numerics;

namespace Phoenix.Framework.Collisions
{
    public static class Collision
    {
        public static bool SphereVsAABB(BoundingSphere sphere, AxisAlignedBoundingBox aabb,
            out Vector3 pushDir, out float depth)
        {
            pushDir = Vector3.Zero;
            depth = 0;

            var center = sphere.Center;
            var radius = sphere.Radius;
            var closest = Vector3.Clamp(center, aabb.Min, aabb.Max);
            var diff = center - closest;
            var distSq = diff.LengthSquared();

            if (distSq >= radius * radius)
                return false;

            if (distSq < 1e-8f)
            {
                var half = aabb.Size * 0.5f;
                var toCenter = center - aabb.Position;
                var dX = half.X - MathF.Abs(toCenter.X);
                var dY = half.Y - MathF.Abs(toCenter.Y);
                var dZ = half.Z - MathF.Abs(toCenter.Z);
                if (dX <= dY && dX <= dZ)
                { pushDir.X = MathF.Sign(toCenter.X); depth = radius - dX; }
                else if (dY <= dZ)
                { pushDir.Y = MathF.Sign(toCenter.Y); depth = radius - dY; }
                else
                { pushDir.Z = MathF.Sign(toCenter.Z); depth = radius - dZ; }
                return depth > 0;
            }

            var dist = MathF.Sqrt(distSq);
            pushDir = diff / dist;
            depth = radius - dist;
            return true;
        }

        public static bool SphereVsOBB(BoundingSphere sphere, OrientedBoundingBox obb,
            out Vector3 pushDir, out float depth)
        {
            pushDir = Vector3.Zero;
            depth = 0;

            var center = sphere.Center;
            var radius = sphere.Radius;

            Matrix4x4.Invert(obb.Orientation, out var invOrient);
            var localCenter = Vector3.Transform(center - obb.Position, invOrient);
            var half = obb.Size * 0.5f;
            var localClosest = Vector3.Clamp(localCenter, -half, half);
            var localDiff = localCenter - localClosest;
            var distSq = localDiff.LengthSquared();

            if (distSq >= radius * radius)
                return false;

            if (distSq < 1e-8f)
            {
                var dX = half.X - MathF.Abs(localCenter.X);
                var dY = half.Y - MathF.Abs(localCenter.Y);
                var dZ = half.Z - MathF.Abs(localCenter.Z);
                Vector3 localPush;
                float pen;
                if (dX <= dY && dX <= dZ)
                { localPush = new Vector3(MathF.Sign(localCenter.X), 0, 0); pen = dX; }
                else if (dY <= dZ)
                { localPush = new Vector3(0, MathF.Sign(localCenter.Y), 0); pen = dY; }
                else
                { localPush = new Vector3(0, 0, MathF.Sign(localCenter.Z)); pen = dZ; }
                pushDir = Vector3.Normalize(Vector3.Transform(localPush, obb.Orientation));
                depth = radius - pen;
                return depth > 0;
            }

            var dist = MathF.Sqrt(distSq);
            pushDir = Vector3.Normalize(Vector3.Transform(localDiff, obb.Orientation));
            depth = radius - dist;
            return true;
        }

        public static bool SphereVsCylinder(BoundingSphere sphere, BoundingCylinder cyl,
            out Vector3 pushDir, out float depth)
        {
            pushDir = Vector3.Zero;
            depth = 0;

            var center = sphere.Center;
            var radius = sphere.Radius;
            var halfH = cyl.HalfHeight;
            var dx = center.X - cyl.Position.X;
            var dz = center.Z - cyl.Position.Z;
            var horizDistSq = dx * dx + dz * dz;
            var horizDist = MathF.Sqrt(horizDistSq);
            var dy = center.Y - cyl.Position.Y;

            Vector3 closest;

            if (dy > halfH)
            {
                var topY = cyl.Position.Y + halfH;
                if (horizDist <= cyl.Radius)
                    closest = new Vector3(center.X, topY, center.Z);
                else
                {
                    var s = cyl.Radius / horizDist;
                    closest = new Vector3(cyl.Position.X + dx * s, topY, cyl.Position.Z + dz * s);
                }
            }
            else if (dy < -halfH)
            {
                var botY = cyl.Position.Y - halfH;
                if (horizDist <= cyl.Radius)
                    closest = new Vector3(center.X, botY, center.Z);
                else
                {
                    var s = cyl.Radius / horizDist;
                    closest = new Vector3(cyl.Position.X + dx * s, botY, cyl.Position.Z + dz * s);
                }
            }
            else
            {
                if (horizDistSq < 1e-12f)
                    closest = new Vector3(cyl.Position.X + cyl.Radius, center.Y, cyl.Position.Z);
                else
                {
                    var t = cyl.Radius / horizDist;
                    closest = new Vector3(cyl.Position.X + dx * t, center.Y, cyl.Position.Z + dz * t);
                }
            }

            var diff = center - closest;
            var distSq = diff.LengthSquared();

            if (distSq >= radius * radius)
                return false;

            if (distSq < 1e-8f)
            {
                pushDir = new Vector3(dx, dy, dz);
                if (pushDir == Vector3.Zero) pushDir = Vector3.UnitY;
                else pushDir = Vector3.Normalize(pushDir);
                depth = radius;
                return true;
            }

            var dist = MathF.Sqrt(distSq);
            pushDir = diff / dist;
            depth = radius - dist;
            return true;
        }

        public static bool SphereVsSphere(BoundingSphere a, BoundingSphere b,
            out Vector3 pushDir, out float depth)
        {
            pushDir = Vector3.Zero;
            depth = 0;

            var diff = a.Center - b.Center;
            var distSq = diff.LengthSquared();
            var sumRadius = a.Radius + b.Radius;

            if (distSq >= sumRadius * sumRadius)
                return false;

            if (distSq < 1e-8f)
            {
                pushDir = Vector3.UnitY;
                depth = sumRadius;
                return true;
            }

            var dist = MathF.Sqrt(distSq);
            pushDir = diff / dist;
            depth = sumRadius - dist;
            return true;
        }

        public static bool CapsuleVsAABB(Capsule cap, AxisAlignedBoundingBox aabb,
            out Vector3 pushDir, out float depth)
        {
            pushDir = Vector3.Zero;
            depth = 0;

            var capMin = Vector3.Min(cap.PointA, cap.PointB) - new Vector3(cap.Radius);
            var capMax = Vector3.Max(cap.PointA, cap.PointB) + new Vector3(cap.Radius);

            var ovX = MathF.Min(capMax.X, aabb.Max.X) - MathF.Max(capMin.X, aabb.Min.X);
            var ovY = MathF.Min(capMax.Y, aabb.Max.Y) - MathF.Max(capMin.Y, aabb.Min.Y);
            var ovZ = MathF.Min(capMax.Z, aabb.Max.Z) - MathF.Max(capMin.Z, aabb.Min.Z);

            if (ovX <= 0 || ovY <= 0 || ovZ <= 0)
                return false;

            var capMid = (cap.PointA + cap.PointB) * 0.5f;
            var aabbCenter = aabb.Position;

            if (ovX <= ovY && ovX <= ovZ)
            { pushDir.X = capMid.X >= aabbCenter.X ? 1f : -1f; depth = ovX; }
            else if (ovY <= ovZ)
            { pushDir.Y = capMid.Y >= aabbCenter.Y ? 1f : -1f; depth = ovY; }
            else
            { pushDir.Z = capMid.Z >= aabbCenter.Z ? 1f : -1f; depth = ovZ; }

            return true;
        }

        public static bool CapsuleVsOBB(Capsule cap, OrientedBoundingBox obb,
            out Vector3 pushDir, out float depth)
        {
            pushDir = Vector3.Zero;
            depth = 0;

            var halfO = obb.Size * 0.5f;

            Matrix4x4.Invert(obb.Orientation, out var invOrient);
            var localA = Vector3.Transform(cap.PointA - obb.Position, invOrient);
            var localB = Vector3.Transform(cap.PointB - obb.Position, invOrient);

            var capMin = Vector3.Min(localA, localB) - new Vector3(cap.Radius);
            var capMax = Vector3.Max(localA, localB) + new Vector3(cap.Radius);

            var ovX = MathF.Min(capMax.X, halfO.X) - MathF.Max(capMin.X, -halfO.X);
            var ovY = MathF.Min(capMax.Y, halfO.Y) - MathF.Max(capMin.Y, -halfO.Y);
            var ovZ = MathF.Min(capMax.Z, halfO.Z) - MathF.Max(capMin.Z, -halfO.Z);

            if (ovX <= 0 || ovY <= 0 || ovZ <= 0)
                return false;

            var localMid = (localA + localB) * 0.5f;

            Vector3 localPush;
            float pen;
            if (ovX <= ovY && ovX <= ovZ)
            { localPush = new Vector3(localMid.X >= 0 ? 1f : -1f, 0, 0); pen = ovX; }
            else if (ovY <= ovZ)
            { localPush = new Vector3(0, localMid.Y >= 0 ? 1f : -1f, 0); pen = ovY; }
            else
            { localPush = new Vector3(0, 0, localMid.Z >= 0 ? 1f : -1f); pen = ovZ; }

            pushDir = Vector3.Transform(localPush, obb.Orientation);
            if (pushDir != Vector3.Zero)
                pushDir = Vector3.Normalize(pushDir);
            depth = pen;
            return true;
        }

        public static bool CapsuleVsCylinder(Capsule cap, BoundingCylinder cyl,
            out Vector3 pushDir, out float depth)
        {
            var dx = cap.PointB.X - cap.PointA.X;
            var dz = cap.PointB.Z - cap.PointA.Z;
            var xzLenSq = dx * dx + dz * dz;
            float t;
            if (xzLenSq < 1e-12f)
                t = 0.5f;
            else
                t = Math.Clamp(((cyl.Position.X - cap.PointA.X) * dx + (cyl.Position.Z - cap.PointA.Z) * dz) / xzLenSq, 0f, 1f);
            var closestOnCap = cap.PointA + t * (cap.PointB - cap.PointA);

            var sphere = new BoundingSphere(closestOnCap, cap.Radius);
            return SphereVsCylinder(sphere, cyl, out pushDir, out depth);
        }

        public static bool CapsuleVsSphere(Capsule cap, BoundingSphere sphere,
            out Vector3 pushDir, out float depth)
        {
            var closestOnCap = MathEx.ClosestPointOnSegment(cap.PointA, cap.PointB, sphere.Center);
            var capSphere = new BoundingSphere(closestOnCap, cap.Radius);
            return SphereVsSphere(capSphere, sphere, out pushDir, out depth);
        }

        public static bool CapsuleVsCapsule(Capsule a, Capsule b,
            out Vector3 pushDir, out float depth)
        {
            pushDir = Vector3.Zero;
            depth = 0;

            var (p1, _, p2, _) = MathEx.SegmentDistance(a.PointA, a.PointB, b.PointA, b.PointB);
            var sumR = a.Radius + b.Radius;
            var diff = p1 - p2;
            var distSq = diff.LengthSquared();

            if (distSq >= sumR * sumR)
                return false;

            if (distSq < 1e-8f)
            {
                pushDir = Vector3.UnitY;
                depth = sumR;
                return true;
            }

            var dist = MathF.Sqrt(distSq);
            pushDir = diff / dist;
            depth = sumR - dist;
            return true;
        }

        public static bool CapsuleVsVolume(Capsule cap, SerializableVolume vol,
            out Vector3 pushDir, out float depth)
        {
            switch (vol)
            {
                case SerializableAABB aabb:
                    return CapsuleVsAABB(cap, aabb, out pushDir, out depth);
                case SerializableOBB obb:
                    return CapsuleVsOBB(cap, obb, out pushDir, out depth);
                case SerializableCylinder cyl:
                    return CapsuleVsCylinder(cap, cyl, out pushDir, out depth);
                case SerializableSphere sphere:
                    return CapsuleVsSphere(cap, sphere, out pushDir, out depth);
                case SerializableCapsule other:
                    return CapsuleVsCapsule(cap, other, out pushDir, out depth);
            }
            pushDir = Vector3.Zero;
            depth = 0;
            return false;
        }
        public static bool RayVsVolume(Ray ray, SerializableVolume vol,
            out float hitFraction, out Vector3 hitPoint, out Vector3 hitNormal)
        {
            switch (vol)
            {
                case SerializableAABB aabb:
                {
                    AxisAlignedBoundingBox box = aabb;
                    return RayCasts.RayVsAABB(ray, box.Min, box.Max, out hitFraction, out hitPoint, out hitNormal);
                }
                case SerializableOBB obb:
                {
                    OrientedBoundingBox box = obb;
                    return RayCasts.RayVsOBB(ray, box.Position, box.Size, box.Orientation, out hitFraction, out hitPoint, out hitNormal);
                }
                case SerializableCylinder cyl:
                    return RayCasts.RayVsCylinder(ray, cyl.Position, cyl.Radius, cyl.Height * 0.5f, out hitFraction, out hitPoint, out hitNormal);
                case SerializableSphere sphere:
                {
                    BoundingSphere s = sphere;
                    return RayCasts.RayVsSphere(ray, s.Center, s.Radius, out hitFraction, out hitPoint, out hitNormal);
                }
                case SerializableCapsule cap:
                {
                    Capsule c = cap;
                    return RayCasts.RayVsCapsule(ray, c.PointA, c.PointB, c.Radius, out hitFraction, out hitPoint, out hitNormal);
                }
            }
            hitFraction = 0;
            hitPoint = Vector3.Zero;
            hitNormal = Vector3.Zero;
            return false;
        }
    }
}
