namespace Phoenix.Framework.Collisions;

/// <summary>
/// Describes the type of intersection a BoundingCylinder and an AxisAlignedBoundingBox had.
/// </summary>
public enum BoxCylinderIntersection
{
    /// <summary>The box touches the cylinder at an edge. Penetration is zero.</summary>
    Edge,
    /// <summary>The box touches the cylinder. Penetration is more than zero.</summary>
    Intersecting,
    /// <summary>The box and the cylinder do not intersect.</summary>
    None,
}
