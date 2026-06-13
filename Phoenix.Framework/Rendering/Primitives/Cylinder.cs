using Phoenix.Framework.Rendering.Geometry.Vertices;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Phoenix.Framework.Rendering.Primitives;

public class Cylinder : Primitive
{
    public InfoCylinder CylinderInfo;

    public static Cylinder Create(InfoCylinder cylinderInfo)
    {
        return PrimitiveHelper.Cylinder(cylinderInfo);
    }
    public static Cylinder Create()
    {
        return PrimitiveHelper.Cylinder();
    }
    internal Cylinder(InfoCylinder cylinderInfo)
    {
        CylinderInfo = cylinderInfo;
        _primitiveInfo = cylinderInfo;
        BuildMesh();
    }

    protected override void VertexIndexBufferPos(ref VertexBufferBuilder vbb, ref uint[] indices)
    {
        var lonSteps = Math.Max(CylinderInfo.SubDivisions, 3);
        var indexList = new List<uint>();

        for (var lon = 0; lon <= lonSteps; lon++)
        {
            var phi = 2f * MathF.PI * lon / lonSteps;
            vbb.Add(new Vector3(MathF.Cos(phi) * 0.5f, 0.5f, MathF.Sin(phi) * 0.5f));
        }
        for (var lon = 0; lon <= lonSteps; lon++)
        {
            var phi = 2f * MathF.PI * lon / lonSteps;
            vbb.Add(new Vector3(MathF.Cos(phi) * 0.5f, -0.5f, MathF.Sin(phi) * 0.5f));
        }

        var topOffset = 0u;
        var botOffset = (uint)(lonSteps + 1);

        for (var lon = 0; lon < lonSteps; lon++)
        {
            indexList.Add(topOffset + (uint)lon);
            indexList.Add(topOffset + (uint)(lon + 1));
        }
        for (var lon = 0; lon < lonSteps; lon++)
        {
            indexList.Add(botOffset + (uint)lon);
            indexList.Add(botOffset + (uint)(lon + 1));
        }
        for (var lon = 0; lon <= lonSteps; lon++)
        {
            indexList.Add(topOffset + (uint)lon);
            indexList.Add(botOffset + (uint)lon);
        }

        indices = indexList.ToArray();
    }

    protected override void VertexIndexBufferLines(ref VertexBufferBuilder vbb, ref uint[] indices)
    {
        var segments = Math.Max(6, CylinderInfo.SubDivisions);
        var indexList = new List<uint>();

        for (var i = 0; i < segments; i++)
        {
            var angle = 2f * MathF.PI * i / segments;
            vbb.Add(new Vector3(MathF.Cos(angle) * 0.5f, 0.5f, MathF.Sin(angle) * 0.5f));
        }
        for (var i = 0; i < segments; i++)
        {
            var angle = 2f * MathF.PI * i / segments;
            vbb.Add(new Vector3(MathF.Cos(angle) * 0.5f, 0f, MathF.Sin(angle) * 0.5f));
        }
        for (var i = 0; i < segments; i++)
        {
            var angle = 2f * MathF.PI * i / segments;
            vbb.Add(new Vector3(MathF.Cos(angle) * 0.5f, -0.5f, MathF.Sin(angle) * 0.5f));
        }

        for (var i = 0; i < segments; i++)
        {
            indexList.Add((uint)i);
            indexList.Add((uint)((i + 1) % segments));
        }
        var cOffset = (uint)segments;
        for (var i = 0; i < segments; i++)
        {
            indexList.Add(cOffset + (uint)i);
            indexList.Add(cOffset + (uint)((i + 1) % segments));
        }
        var bOffset = 2u * (uint)segments;
        for (var i = 0; i < segments; i++)
        {
            indexList.Add(bOffset + (uint)i);
            indexList.Add(bOffset + (uint)((i + 1) % segments));
        }

        for (var i = 0; i < 4; i++)
        {
            var angle = 2f * MathF.PI * i / 4;
            vbb.Add(new Vector3(MathF.Cos(angle) * 0.5f, 0.5f, MathF.Sin(angle) * 0.5f));
            vbb.Add(new Vector3(MathF.Cos(angle) * 0.5f, -0.5f, MathF.Sin(angle) * 0.5f));
        }
        var vOffset = 3u * (uint)segments;
        for (var i = 0; i < 4; i++)
        {
            indexList.Add(vOffset + 2u * (uint)i);
            indexList.Add(vOffset + 2u * (uint)i + 1);
        }

        indices = indexList.ToArray();
    }

    protected override void VertexIndexBufferPosUv(ref VertexBufferBuilder vbb, ref uint[] indices)
    {
        var lonSteps = Math.Max(CylinderInfo.SubDivisions, 3);
        var indexList = new List<uint>();

        // Top cap center
        vbb.Add(new Vector3(0f, 0.5f, 0f), new Vector2(0.5f, 0.5f));
        var topCenter = 0u;
        var topRingStart = 1u;
        for (var lon = 0; lon < lonSteps; lon++)
        {
            var phi = 2f * MathF.PI * lon / lonSteps;
            var x = MathF.Cos(phi);
            var z = MathF.Sin(phi);
            vbb.Add(new Vector3(x * 0.5f, 0.5f, z * 0.5f), new Vector2(x * 0.5f + 0.5f, z * 0.5f + 0.5f));
        }

        // Bottom cap center
        vbb.Add(new Vector3(0f, -0.5f, 0f), new Vector2(0.5f, 0.5f));
        var botCenter = (uint)(1 + lonSteps);
        var botRingStart = botCenter + 1;
        for (var lon = 0; lon < lonSteps; lon++)
        {
            var phi = 2f * MathF.PI * lon / lonSteps;
            var x = MathF.Cos(phi);
            var z = MathF.Sin(phi);
            vbb.Add(new Vector3(x * 0.5f, -0.5f, z * 0.5f), new Vector2(x * 0.5f + 0.5f, z * 0.5f + 0.5f));
        }

        // Top cap triangles
        for (var lon = 0; lon < lonSteps; lon++)
        {
            var next = (uint)((lon + 1) % lonSteps);
            indexList.Add(topCenter);
            indexList.Add(topRingStart + next);
            indexList.Add(topRingStart + (uint)lon);
        }

        // Bottom cap triangles
        for (var lon = 0; lon < lonSteps; lon++)
        {
            var next = (uint)((lon + 1) % lonSteps);
            indexList.Add(botCenter);
            indexList.Add(botRingStart + (uint)lon);
            indexList.Add(botRingStart + next);
        }

        // Side
        var sideBotStart = (uint)(2 + 2 * lonSteps);
        var sideTopStart = sideBotStart + (uint)(lonSteps + 1);
        for (var lon = 0; lon <= lonSteps; lon++)
        {
            var phi = 2f * MathF.PI * lon / lonSteps;
            var x = MathF.Cos(phi);
            var z = MathF.Sin(phi);
            var u = (float)lon / lonSteps;
            vbb.Add(new Vector3(x * 0.5f, -0.5f, z * 0.5f), new Vector2(u, 0));
        }
        for (var lon = 0; lon <= lonSteps; lon++)
        {
            var phi = 2f * MathF.PI * lon / lonSteps;
            var x = MathF.Cos(phi);
            var z = MathF.Sin(phi);
            var u = (float)lon / lonSteps;
            vbb.Add(new Vector3(x * 0.5f, 0.5f, z * 0.5f), new Vector2(u, 1));
        }

        for (var lon = 0; lon < lonSteps; lon++)
        {
            indexList.Add(sideBotStart + (uint)lon);
            indexList.Add(sideTopStart + (uint)lon);
            indexList.Add(sideBotStart + (uint)(lon + 1));

            indexList.Add(sideTopStart + (uint)lon);
            indexList.Add(sideTopStart + (uint)(lon + 1));
            indexList.Add(sideBotStart + (uint)(lon + 1));
        }

        indices = indexList.ToArray();
    }

    protected override void VertexIndexBufferPosUvNorm(ref VertexBufferBuilder vbb, ref uint[] indices)
    {
        var lonSteps = Math.Max(CylinderInfo.SubDivisions, 3);
        var indexList = new List<uint>();

        var nTop = new Vector3(0, 1, 0);
        var nBot = new Vector3(0, -1, 0);

        // Top cap center
        vbb.Add(new Vector3(0f, 0.5f, 0f), new Vector2(0.5f, 0.5f), nTop);
        var topCenter = 0u;
        var topRingStart = 1u;
        for (var lon = 0; lon < lonSteps; lon++)
        {
            var phi = 2f * MathF.PI * lon / lonSteps;
            var x = MathF.Cos(phi);
            var z = MathF.Sin(phi);
            vbb.Add(new Vector3(x * 0.5f, 0.5f, z * 0.5f), new Vector2(x * 0.5f + 0.5f, z * 0.5f + 0.5f), nTop);
        }

        // Bottom cap center
        vbb.Add(new Vector3(0f, -0.5f, 0f), new Vector2(0.5f, 0.5f), nBot);
        var botCenter = (uint)(1 + lonSteps);
        var botRingStart = botCenter + 1;
        for (var lon = 0; lon < lonSteps; lon++)
        {
            var phi = 2f * MathF.PI * lon / lonSteps;
            var x = MathF.Cos(phi);
            var z = MathF.Sin(phi);
            vbb.Add(new Vector3(x * 0.5f, -0.5f, z * 0.5f), new Vector2(x * 0.5f + 0.5f, z * 0.5f + 0.5f), nBot);
        }

        for (var lon = 0; lon < lonSteps; lon++)
        {
            var next = (uint)((lon + 1) % lonSteps);
            indexList.Add(topCenter);
            indexList.Add(topRingStart + next);
            indexList.Add(topRingStart + (uint)lon);
        }

        for (var lon = 0; lon < lonSteps; lon++)
        {
            var next = (uint)((lon + 1) % lonSteps);
            indexList.Add(botCenter);
            indexList.Add(botRingStart + (uint)lon);
            indexList.Add(botRingStart + next);
        }

        // Side
        var sideBotStart = (uint)(2 + 2 * lonSteps);
        var sideTopStart = sideBotStart + (uint)(lonSteps + 1);
        for (var lon = 0; lon <= lonSteps; lon++)
        {
            var phi = 2f * MathF.PI * lon / lonSteps;
            var x = MathF.Cos(phi);
            var z = MathF.Sin(phi);
            var n = new Vector3(x, 0, z);
            vbb.Add(new Vector3(x * 0.5f, -0.5f, z * 0.5f), new Vector2((float)lon / lonSteps, 0), n);
        }
        for (var lon = 0; lon <= lonSteps; lon++)
        {
            var phi = 2f * MathF.PI * lon / lonSteps;
            var x = MathF.Cos(phi);
            var z = MathF.Sin(phi);
            var n = new Vector3(x, 0, z);
            vbb.Add(new Vector3(x * 0.5f, 0.5f, z * 0.5f), new Vector2((float)lon / lonSteps, 1), n);
        }

        for (var lon = 0; lon < lonSteps; lon++)
        {
            indexList.Add(sideBotStart + (uint)lon);
            indexList.Add(sideTopStart + (uint)lon);
            indexList.Add(sideBotStart + (uint)(lon + 1));

            indexList.Add(sideTopStart + (uint)lon);
            indexList.Add(sideTopStart + (uint)(lon + 1));
            indexList.Add(sideBotStart + (uint)(lon + 1));
        }

        indices = indexList.ToArray();
    }

    protected override void VertexIndexBufferPosUvNormTaBt(ref VertexBufferBuilder vbb, ref uint[] indices)
    {
        var lonSteps = Math.Max(CylinderInfo.SubDivisions, 3);
        var indexList = new List<uint>();

        var nTop = new Vector3(0, 1, 0);
        var tTop = new Vector3(1, 0, 0);
        var btTop = new Vector3(0, 0, -1);
        var nBot = new Vector3(0, -1, 0);
        var tBot = new Vector3(1, 0, 0);
        var btBot = new Vector3(0, 0, 1);

        // Top cap center
        vbb.Add(new Vector3(0f, 0.5f, 0f), new Vector2(0.5f, 0.5f), nTop, tTop, btTop);
        var topCenter = 0u;
        var topRingStart = 1u;
        for (var lon = 0; lon < lonSteps; lon++)
        {
            var phi = 2f * MathF.PI * lon / lonSteps;
            var x = MathF.Cos(phi);
            var z = MathF.Sin(phi);
            vbb.Add(new Vector3(x * 0.5f, 0.5f, z * 0.5f), new Vector2(x * 0.5f + 0.5f, z * 0.5f + 0.5f), nTop, tTop, btTop);
        }

        // Bottom cap center
        vbb.Add(new Vector3(0f, -0.5f, 0f), new Vector2(0.5f, 0.5f), nBot, tBot, btBot);
        var botCenter = (uint)(1 + lonSteps);
        var botRingStart = botCenter + 1;
        for (var lon = 0; lon < lonSteps; lon++)
        {
            var phi = 2f * MathF.PI * lon / lonSteps;
            var x = MathF.Cos(phi);
            var z = MathF.Sin(phi);
            vbb.Add(new Vector3(x * 0.5f, -0.5f, z * 0.5f), new Vector2(x * 0.5f + 0.5f, z * 0.5f + 0.5f), nBot, tBot, btBot);
        }

        for (var lon = 0; lon < lonSteps; lon++)
        {
            var next = (uint)((lon + 1) % lonSteps);
            indexList.Add(topCenter);
            indexList.Add(topRingStart + next);
            indexList.Add(topRingStart + (uint)lon);
        }

        for (var lon = 0; lon < lonSteps; lon++)
        {
            var next = (uint)((lon + 1) % lonSteps);
            indexList.Add(botCenter);
            indexList.Add(botRingStart + (uint)lon);
            indexList.Add(botRingStart + next);
        }

        // Side
        var sideBotStart = (uint)(2 + 2 * lonSteps);
        var sideTopStart = sideBotStart + (uint)(lonSteps + 1);
        for (var lon = 0; lon <= lonSteps; lon++)
        {
            var phi = 2f * MathF.PI * lon / lonSteps;
            var x = MathF.Cos(phi);
            var z = MathF.Sin(phi);
            var n = new Vector3(x, 0, z);
            var t = new Vector3(-z, 0, x);
            var bt = new Vector3(0, 1, 0);
            vbb.Add(new Vector3(x * 0.5f, -0.5f, z * 0.5f), new Vector2((float)lon / lonSteps, 0), n, t, bt);
        }
        for (var lon = 0; lon <= lonSteps; lon++)
        {
            var phi = 2f * MathF.PI * lon / lonSteps;
            var x = MathF.Cos(phi);
            var z = MathF.Sin(phi);
            var n = new Vector3(x, 0, z);
            var t = new Vector3(-z, 0, x);
            var bt = new Vector3(0, 1, 0);
            vbb.Add(new Vector3(x * 0.5f, 0.5f, z * 0.5f), new Vector2((float)lon / lonSteps, 1), n, t, bt);
        }

        for (var lon = 0; lon < lonSteps; lon++)
        {
            indexList.Add(sideBotStart + (uint)lon);
            indexList.Add(sideTopStart + (uint)lon);
            indexList.Add(sideBotStart + (uint)(lon + 1));

            indexList.Add(sideTopStart + (uint)lon);
            indexList.Add(sideTopStart + (uint)(lon + 1));
            indexList.Add(sideBotStart + (uint)(lon + 1));
        }

        indices = indexList.ToArray();
    }
}
