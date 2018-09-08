using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SoftRender;
using UnityEngine;
using UnityEngine.UI;

public class Demo : MonoBehaviour
{
    public class RenderJsonData
    {
        public List<MeshJsonData> meshes;
    }

    [Serializable]
    public class MeshJsonData
    {
        public List<float> vertices;
        public List<int> indices;
        public int uvCount;
    }

    public RawImage Image;
    public ShadingMode Mode;
    public Color Color;

    private Device m_device;

	void Start ()
	{
	    var camera = new VirtualCamera(new Vector3(0, 0, -10), 60, 16f / 9f, 1000, 0.3f);

	    LoadJsonFile();
	    var texture = LoadTexture("Suzanne");

        var mesh = LoadJsonFile();
	    var lightData = new LightData {Pos = new Vector3(0, 10, -10), LightColor = Color};
	    m_device = new Device(Image, lightData, mesh, camera, texture, 1280, 720);
	    m_device.Mode = Mode;

	    //Application.targetFrameRate = 60;
	}

    //private MeshData[] GetCubeData()
    //{
    //    var vertex = new[]
    //    {
    //        new Vector4(-1, 1, 1, 1),
    //        new Vector4(1, 1, 1, 1),
    //        new Vector4(-1, -1, 1, 1),
    //        new Vector4(1, -1, 1, 1),
    //        new Vector4(-1, 1, -1, 1),
    //        new Vector4(1, 1, -1, 1),
    //        new Vector4(1, -1, -1, 1),
    //        new Vector4(-1, -1, -1, 1),
    //    };
    //    var triangle = new[,]
    //    {
    //        {0, 1, 2},
    //        {1, 2, 3},
    //        {1, 3, 6},
    //        {1, 5, 6},
    //        {0, 1, 4},
    //        {1, 4, 5},
    //        {2, 3, 7},
    //        {3, 6, 7},
    //        {0, 2, 7},
    //        {0, 4, 7},
    //        {4, 5, 6},
    //        {4, 6, 7}
    //    };
    //    var colors = new[]
    //    {
    //        Color.white,
    //        Color.white,
    //        Color.blue,
    //        Color.blue,
    //        Color.cyan,
    //        Color.cyan,
    //        Color.magenta,
    //        Color.magenta,
    //        Color.yellow,
    //        Color.yellow,
    //        Color.red,
    //        Color.red,
    //    };
    //    var mesh = new MeshData(vertex, Vector3.zero, Vector3.zero, triangle, colors);
    //    return new[] {mesh};
    //}

    private MeshData[] LoadJsonFile()
    {
        var path = Application.dataPath + "/monkey.babylon";
        var text = LoadFileText(path);
        var json = JsonUtility.FromJson<RenderJsonData>(text);

        var meshes = new MeshData[json.meshes.Count];
        for (int i = 0; i < json.meshes.Count; i++)
        {
            var verticesArrary = json.meshes[i].vertices;
            var indicesArrary = json.meshes[i].indices;

            var uvCount = json.meshes[i].uvCount; 
            var verticesStep = 1;

            switch (uvCount)
            {
                case 0:
                    verticesStep = 6;
                    break;
                case 1:
                    verticesStep = 8;
                    break;
                case 2:
                    verticesStep = 10;
                    break;
                default:
                    break;
            }

            var verticesCount = verticesArrary.Count / verticesStep;
            var triangelCount = indicesArrary.Count / 3;

            var vertices = new Vertex[verticesCount];
            var triangles = new int[triangelCount, 3];
            var colors = new Color[triangelCount];
            for (int j = 0; j < verticesCount; j++)
            {
                vertices[j] = new Vertex
                {
                    Pos = new Vector4(verticesArrary[j * verticesStep], verticesArrary[j * verticesStep + 1],
                        verticesArrary[j * verticesStep + 2], 1),
                    Normal = new Vector4(verticesArrary[j * verticesStep + 3], verticesArrary[j * verticesStep + 4],
                        verticesArrary[j * verticesStep + 5], 1)
                };

                if (uvCount > 0)
                {
                    var u = verticesArrary[j * verticesStep + 6];
                    var v = 1 - verticesArrary[j * verticesStep + 7];
                    vertices[j].Texcoord = new Vector2(u, v);
                }
            }

            for (int j = 0; j < triangelCount; j++)
            {
                triangles[j, 0] = indicesArrary[j * 3];
                triangles[j, 1] = indicesArrary[j * 3 + 1];
                triangles[j, 2] = indicesArrary[j * 3 + 2];
                colors[j] = new Color(1f/triangelCount*j, 0, 0);
            }
            meshes[i] = new MeshData(vertices, Vector3.zero, Vector3.zero, triangles, colors);
        }

        return meshes;
    }

    private string LoadFileText(string path)
    {
        if (File.Exists(path))
        {
            FileStream fs = new FileStream(path, FileMode.Open);
            StreamReader sr = new StreamReader(fs);
            string text = sr.ReadToEnd();
            sr.Close();
            fs.Close();
            return text;
        }
        else
        {
            Debug.LogError("no file " + path);
            return string.Empty;
        }
    }

    private Texture2D LoadTexture(string name)
    {
        var texture = Resources.Load<Texture2D>(name);
        return texture;
    }

    void Update ()
    {
        m_device.Mode = Mode;
		m_device.Update();
	}
}
