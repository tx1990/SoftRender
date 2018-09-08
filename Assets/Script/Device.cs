using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SoftRender
{
    public enum ShadingMode
    {
        Shaded,
        Wireframe,
    }

    public class ScanLineData
    {
        public Vector3 Pos { get; set; }
        public float NDotL { get; set; }
        public Vector2 Texcoord { get; set; }
    }

    public class Device
    {
        private readonly RawImage m_image;
        private readonly LightData m_light;
        private readonly MeshData[] m_meshData;
        private readonly VirtualCamera m_camera;
        private readonly Color[] m_frameBuffer;
        private readonly float[] m_depthBuffer;
        private readonly Texture2D m_frontBuffer;
        private readonly Color[] m_texture;

        private readonly int m_screenWidth;
        private readonly int m_screenHeight;

        private readonly int m_textureWidth;
        private readonly int m_textureHeight;

        private readonly Dictionary<int, Vector4> m_worldPoints;
        private readonly Dictionary<int, Vector3> m_screePoints;
        private readonly Dictionary<int, Vector3> m_worldNormals;

        public ShadingMode Mode { get; set; }

        public Device(RawImage image, LightData light, MeshData[] mesh, VirtualCamera camera, Texture2D texture, int width, int height)
        {
            m_image = image;
            m_light = light;
            m_meshData = mesh;
            m_camera = camera;
            if (texture)
            {
                m_texture = texture.GetPixels();
                m_textureWidth = texture.width;
                m_textureHeight = texture.height;
            }
            m_frontBuffer = new Texture2D(width, height);
            m_frameBuffer = new Color[width * height];
            m_depthBuffer = new float[width * height];

            m_screenWidth = width;
            m_screenHeight = height;

            m_worldPoints = new Dictionary<int, Vector4>();
            m_screePoints = new Dictionary<int, Vector3>();
            m_worldNormals = new Dictionary<int, Vector3>();
        }

        private bool m_finsh = true;

        public void Update()
        {
            //ClearBuffer();
            //for (int i = 0; i < m_meshData.Length; i++)
            //{
            //    RenderBuffer(m_meshData[i]);
            //}
            //m_frontBuffer.SetPixels(m_frameBuffer);
            //m_frontBuffer.Apply();
            //m_image.texture = m_frontBuffer;

            if (m_finsh)
            {
                m_finsh = false;
                Loom.RunAsync(() =>
                {
                    ClearBuffer();
                    foreach (var t in m_meshData)
                    {
                        RenderBuffer(t);
                    }
                    Loom.QueueOnMainThread(() =>
                    {
                        m_frontBuffer.SetPixels(m_frameBuffer);
                        m_frontBuffer.Apply();
                        m_image.texture = m_frontBuffer;
                        m_finsh = true;
                    });
                });
            }
        }

        private void ClearBuffer()
        {
            for (int i = 0; i < m_frameBuffer.Length; i++)
            {
                m_frameBuffer[i] = Color.black;
            }

            for (int i = 0; i < m_depthBuffer.Length; i++)
            {
                m_depthBuffer[i] = float.MaxValue;
            }
        }

        private bool Clip(float x, float y, float z, float w)
        {
            return x >= -w && x <= w && y >= -w && y <= w && z >= -w && z <= w;
        }

        private Vector4 GetWorldPoint(Matrix4x4 matrix, Vector4 vector, int index)
        {
            Vector4 point;
            if (!m_worldPoints.TryGetValue(index, out point))
            {
                point = matrix * vector;
                m_worldPoints.Add(index, point);
            }

            return point;
        }

        private Vector3 GetScreenPoint(Matrix4x4 matrix, Vector4 vector, int index)
        {
            Vector3 point;
            if (!m_screePoints.TryGetValue(index, out point))
            {
                var p = matrix * vector;
                var x = p.x / p.w;
                var y = p.y / p.w;

                x = (x + 1) * 0.5f * m_screenWidth;
                y = (y + 1) * 0.5f * m_screenHeight;
                point = new Vector3(x, y, p.z);
                m_screePoints.Add(index, point);
            }

            return point;
        }

        private Vector3 GetWorldNormal(Matrix4x4 matrix, Vector4 vector, int index)
        {
            Vector3 normal;
            if (!m_worldNormals.TryGetValue(index, out normal))
            {
                normal = matrix * vector;
                m_worldNormals.Add(index, normal);
            }

            return normal;
        }

        private void DrawPoint(int x, int y, Color color)
        {
            var index = y * m_screenWidth + x;
            if (index >= 0 && index < m_frameBuffer.Length)
            {
                m_frameBuffer[index] = color;
            }
        }

        private void DrawLine(int x0, int y0, int x1, int y1)
        {
            var dx = Mathf.Abs(x1 - x0);
            var dy = Mathf.Abs(y1 - y0);
            var sx = x0 < x1 ? 1 : -1;
            var sy = y0 < y1 ? 1 : -1;
            var err = dx - dy;

            while (true)
            {
                DrawPoint(x0, y0, Color.white);
                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                var e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        float ComputeNDotL(Vector3 vertex, Vector3 normal)
        {
            var lightDirection = m_light.Pos - vertex;

            normal.Normalize();
            lightDirection.Normalize();

            return Mathf.Max(0, Vector3.Dot(normal, lightDirection));
        }

        private void DrawTriangle(ScanLineData point0, ScanLineData point1, ScanLineData point2)
        {
            SortVectorByY(ref point0, ref point1);
            SortVectorByY(ref point1, ref point2);
            SortVectorByY(ref point0, ref point1);

            if ((int) point0.Pos.y == (int) point1.Pos.y)
            {
                if (point0.Pos.x > point1.Pos.x)
                {
                    for (int i = (int)point0.Pos.y, max = (int)point2.Pos.y; i <= max; i++)
                    {
                        ProcessScanLine(i, point1, point2, point0, point2);
                    }
                }
                else
                {
                    for (int i = (int)point0.Pos.y, max = (int)point2.Pos.y; i <= max; i++)
                    {
                        ProcessScanLine(i, point0, point2, point1, point2);
                    }
                }
            }
            else if ((int) point2.Pos.y == (int) point1.Pos.y)
            {
                if (point2.Pos.x > point1.Pos.x)
                {
                    for (int i = (int)point0.Pos.y, max = (int)point2.Pos.y; i <= max; i++)
                    {
                        ProcessScanLine(i, point0, point1, point0, point2);
                    }
                }
                else
                {
                    for (int i = (int)point0.Pos.y, max = (int)point2.Pos.y; i <= max; i++)
                    {
                        ProcessScanLine(i, point0, point2, point0, point1);
                    }
                }
            }
            else
            {
                var dP0P1 = CalculateSlope(point0, point1);
                var dP0P2 = CalculateSlope(point0, point2);

                if (dP0P1 > dP0P2)
                {
                    for (int i = (int) point0.Pos.y, max = (int) point2.Pos.y; i <= max; i++)
                    {
                        if (i < point1.Pos.y)
                        {
                            ProcessScanLine(i, point0, point2, point0, point1);
                        }
                        else
                        {
                            ProcessScanLine(i, point0, point2, point1, point2);
                        }
                    }
                }
                else
                {
                    for (int i = (int) point0.Pos.y, max = (int) point2.Pos.y; i <= max; i++)
                    {
                        if (i < point1.Pos.y)
                        {
                            ProcessScanLine(i, point0, point1, point0, point2);
                        }
                        else
                        {
                            ProcessScanLine(i, point1, point2, point0, point2);
                        }
                    }
                }
            }
        }

        private void ProcessScanLine(int y, ScanLineData a, ScanLineData b, ScanLineData c, ScanLineData d)
        {
            var gradient1 = (int) a.Pos.y != (int) b.Pos.y ? (y - a.Pos.y) / (b.Pos.y - a.Pos.y) : 1;
            var gradient2 = (int) c.Pos.y != (int) d.Pos.y ? (y - c.Pos.y) / (d.Pos.y - c.Pos.y) : 1;

            var sx = (int) Interpolate(a.Pos.x, b.Pos.x, gradient1);
            var ex = (int) Interpolate(c.Pos.x, d.Pos.x, gradient2);

            var z1 = Interpolate(a.Pos.z, b.Pos.z, gradient1);
            var z2 = Interpolate(c.Pos.z, d.Pos.z, gradient2);

            var snl = Interpolate(a.NDotL, b.NDotL, gradient1);
            var enl = Interpolate(c.NDotL, d.NDotL, gradient2);

            var su = Interpolate(a.Texcoord.x, b.Texcoord.x, gradient1);
            var eu = Interpolate(c.Texcoord.x, d.Texcoord.x, gradient2);
            var sv = Interpolate(a.Texcoord.y, b.Texcoord.y, gradient1);
            var ev = Interpolate(c.Texcoord.y, d.Texcoord.y, gradient2);

            for (int x = sx; x < ex; x++)
            {
                var gradient3 = (float) (x - sx) / (ex - sx);
                var z = Interpolate(z1, z2, gradient3);

                var index = y * m_screenWidth + x;
                if (index >= 0 && index < m_depthBuffer.Length)
                {
                    if (m_depthBuffer[index] > z)
                    {
                        m_depthBuffer[index] = z;
                        var ndotl = Interpolate(snl, enl, gradient3);
                        var color = m_light.LightColor * ndotl;
                        if (m_texture != null)
                        {
                            var u = Interpolate(su, eu, gradient3);
                            var v = Interpolate(sv, ev, gradient3);
                            color *= Map(u, v);
                        }
                        color.a = 1;
                        DrawPoint(x, y, color);
                    }
                }
            }
        }

        private Color Map(float u, float v)
        {
            var x = (int) Mathf.Abs(u * m_textureWidth % m_textureWidth);
            var y = (int) Mathf.Abs(v * m_textureHeight % m_textureHeight);
            return m_texture[y * m_textureWidth + x];
        }

        private float Interpolate(float min, float max, float gradient)
        {
            return min + (max - min) * Mathf.Clamp01(gradient);
        }

        private float CalculateSlope(ScanLineData a, ScanLineData b)
        {
            if (b.Pos.y > a.Pos.y)
            {
                return (b.Pos.x - a.Pos.x) / (b.Pos.y - a.Pos.y);
            }
            else
            {
                return 0;
            }
        }

        private void SortVectorByY(ref ScanLineData a, ref ScanLineData b)
        {
            if (a.Pos.y > b.Pos.y)
            {
                var temp = a;
                a = b;
                b = temp;
            }
        }

        private bool BackFaceCull(Vector3 p0, Vector3 p1, Vector3 p2)
        {
            var normal = Vector3.Cross(p1 - p0, p2 - p0);
            var pos = m_camera.Position;
            return Vector3.Dot(p0 - pos, normal) <= 0;
        }

        private void RenderBuffer(MeshData mesh)
        {
            var world = mesh.GetLocalToWorld();
            var view = m_camera.GetViewMatrix4X4(mesh.Position, Vector3.up);
            var projection = m_camera.GetProjectionMatrix4X4();
            var matrix = projection * view;
            m_worldPoints.Clear();
            m_screePoints.Clear();
            m_worldNormals.Clear();
            for (int i = 0, m = mesh.Triangle.GetLength(0); i < m; i++)
            {
                var i0 = mesh.Triangle[i, 0];
                var i1 = mesh.Triangle[i, 1];
                var i2 = mesh.Triangle[i, 2];

                var wP0 = GetWorldPoint(world, mesh.Vertex[i0].Pos, i0);
                var wP1 = GetWorldPoint(world, mesh.Vertex[i1].Pos, i1);
                var wP2 = GetWorldPoint(world, mesh.Vertex[i2].Pos, i2);

                if (Mode == ShadingMode.Shaded)
                {
                    if (!BackFaceCull(wP0, wP1, wP2))
                    {
                        var sP0 = GetScreenPoint(matrix, wP0, i0);
                        var sP1 = GetScreenPoint(matrix, wP1, i1);
                        var sP2 = GetScreenPoint(matrix, wP2, i2);

                        var wN0 = GetWorldNormal(world, mesh.Vertex[i0].Normal, i0);
                        var wN1 = GetWorldNormal(world, mesh.Vertex[i1].Normal, i1);
                        var wN2 = GetWorldNormal(world, mesh.Vertex[i2].Normal, i2);

                        var ndotl0 = ComputeNDotL(wP0, wN0);
                        var ndotl1 = ComputeNDotL(wP1, wN1);
                        var ndotl2 = ComputeNDotL(wP2, wN2);

                        var v0 = new ScanLineData {Pos = sP0, NDotL = ndotl0, Texcoord = mesh.Vertex[i0].Texcoord};
                        var v1 = new ScanLineData {Pos = sP1, NDotL = ndotl1, Texcoord = mesh.Vertex[i1].Texcoord};
                        var v2 = new ScanLineData {Pos = sP2, NDotL = ndotl2, Texcoord = mesh.Vertex[i2].Texcoord};

                        DrawTriangle(v0, v1, v2);
                    }
                }
                else
                {
                    var sP0 = GetScreenPoint(matrix, wP0, i0);
                    var sP1 = GetScreenPoint(matrix, wP1, i1);
                    var sP2 = GetScreenPoint(matrix, wP2, i2);

                    DrawLine((int)sP0.x, (int)sP0.y, (int)sP1.x, (int)sP1.y);
                    DrawLine((int)sP1.x, (int)sP1.y, (int)sP2.x, (int)sP2.y);
                    DrawLine((int)sP2.x, (int)sP2.y, (int)sP0.x, (int)sP0.y);
                }
            }

            mesh.Rotation += new Vector3(0, 1, 0);
        }
    }
}
