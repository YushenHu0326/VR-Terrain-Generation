using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Barracuda;

public class TerrainModifier : MonoBehaviour
{
    private Terrain terrain;
    private TerrainData terrainData;
    private float maxHeight, minHeight;
    private float maxBaseHeight, minBaseHeight;
    private float maxColor;
    private int terrainType;

    public float[,] heights;
    public float[,] baseHeights;
    public float[,] alphas;
    public int xOffset, yOffset, range;

    public bool hasGroundHeights;
    public bool hasBaseHeights;

    private Model model;
    public NNModel ONNXModel;

    private IWorker worker;

    public void LoadModel()
    {
        model = ModelLoader.Load(ONNXModel);

        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputeRef, model);
    }

    void GrabTerrainData()
    {
        terrain = Object.FindObjectOfType<Terrain>();
        terrainData = terrain.terrainData;

        if (hasGroundHeights)
        {
            maxHeight = 0f;
            minHeight = 1.01f;

            for (int y = 0; y < terrainData.heightmapResolution; y++)
            {
                for (int x = 0; x < terrainData.heightmapResolution; x++)
                {
                    if (heights[y + yOffset, x + xOffset] > maxHeight)
                        maxHeight = heights[y + yOffset, x + xOffset];
                    if (heights[y + yOffset, x + xOffset] < minHeight)
                        minHeight = heights[y + yOffset, x + xOffset];
                }
            }
        }

        if (hasBaseHeights)
        {
            maxBaseHeight = 0f;
            minBaseHeight = 1.01f;

            for (int y = 0; y < terrainData.heightmapResolution; y++)
            {
                for (int x = 0; x < terrainData.heightmapResolution; x++)
                {
                    if (baseHeights[y + yOffset, x + xOffset] > maxBaseHeight)
                        maxBaseHeight = baseHeights[y + yOffset, x + xOffset];
                    if (baseHeights[y + yOffset, x + xOffset] < minBaseHeight)
                        minBaseHeight = baseHeights[y + yOffset, x + xOffset];
                }
            }

            if (minBaseHeight < 0f) minBaseHeight = 0f;
        }
    }

    Texture2D RetrieveTerrainHeightmap()
    {
        Texture2D tex = new Texture2D(terrainData.heightmapResolution, terrainData.heightmapResolution, TextureFormat.ARGB32, false);

        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                tex.SetPixel(x, y, new Color((heights[y + yOffset, x + xOffset] - minHeight) / (maxHeight - minHeight),
                                             0f, 0f, 1));
            }
        }

        return tex;
    }

    Texture2D RetrieveBaseTerrainHeightmap()
    {
        Texture2D tex = new Texture2D(terrainData.heightmapResolution, terrainData.heightmapResolution, TextureFormat.ARGB32, false);

        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                tex.SetPixel(x, y, new Color((baseHeights[y + yOffset, x + xOffset] - minBaseHeight) / (maxBaseHeight - minBaseHeight),
                                             0f, 0f, 1));
            }
        }

        return tex;
    }

    Texture2D RetrieveTerrainStroke()
    {
        Texture2D tex = new Texture2D(terrainData.heightmapResolution, terrainData.heightmapResolution, TextureFormat.ARGB32, false);

        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                tex.SetPixel(x, y, new Color(alphas[y + yOffset, x + xOffset],
                                             0f, 0f, 1));
            }
        }

        return tex;
    }

    float[] ProcessHeightmap(Texture2D tex)
    {
        tex.Apply();
        RenderTexture rt = RenderTexture.GetTemporary(256, 256, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        RenderTexture.active = rt;

        Graphics.Blit(tex, rt);
        tex.Reinitialize(256, 256, tex.format, true);
        tex.filterMode = FilterMode.Bilinear;
        tex.ReadPixels(new Rect(0.0f, 0.0f, 256, 256), 0, 0);
        tex.Apply();

        for (int h = 0; h < 256; h++)
        {
            for (int w = 0; w < 256; w++)
            {
                float r = tex.GetPixel(w, h).r;

                float n = Mathf.PerlinNoise((float)w / 30f, (float)h / 30f);

                r *= ((1f - r) * n * 0.2f + 1f);

                r *= 10f;
                r = Mathf.Floor(r);
                r /= 12f;

                tex.SetPixel(w, h, new Color(r, r, r));
            }
        }

        byte[] bytes = tex.EncodeToPNG();

        System.IO.File.WriteAllBytes(Application.dataPath + "/h.png", bytes);

        float[] values = new float[196608];

        for (int h = 0; h < 256; h++)
        {
            for (int w = 0; w < 256; w++)
            {
                values[w * 3 + h * 256 * 3] = tex.GetPixel(w, h).r * 2f - 1f;
                values[w * 3 + h * 256 * 3 + 1] = tex.GetPixel(w, h).g * 2f - 1f;
                values[w * 3 + h * 256 * 3 + 2] = tex.GetPixel(w, h).b * 2f - 1f;
            }
        }

        return values;
    }

    Texture2D RestoreHeightmap(Texture2D tex)
    {
        tex.Apply();
        RenderTexture rt = RenderTexture.GetTemporary(terrainData.heightmapResolution, terrainData.heightmapResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        RenderTexture.active = rt;

        Graphics.Blit(tex, rt);
        tex.Reinitialize(terrainData.heightmapResolution, terrainData.heightmapResolution, tex.format, true);
        tex.filterMode = FilterMode.Bilinear;
        tex.ReadPixels(new Rect(0.0f, 0.0f, terrainData.heightmapResolution, terrainData.heightmapResolution), 0, 0);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();

        System.IO.File.WriteAllBytes(Application.dataPath + "/h0.png", bytes);

        return tex;
    }

    void WriteTerrainData(Texture2D tex, Texture2D baseTex)
    {
        Texture2D atex = new Texture2D(terrainData.heightmapResolution, terrainData.heightmapResolution, TextureFormat.ARGB32, false);
        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                atex.SetPixel(x, y, new Color(alphas[y, x], alphas[y, x], alphas[y, x], 1f));
            }
        }

        byte[] bytes = atex.EncodeToPNG();

        System.IO.File.WriteAllBytes(Application.dataPath + "/a.png", bytes);

        float[,] originHeights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        float[,] newHeights = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];

        maxColor = 0f;

        if (hasBaseHeights)
        {
            for (int y = 0; y < terrainData.heightmapResolution; y++)
            {
                for (int x = 0; x < terrainData.heightmapResolution; x++)
                {
                    if (baseTex.GetPixel(x, y).r > maxColor) maxColor = baseTex.GetPixel(x, y).r;
                }
            }

            for (int y = 0; y < terrainData.heightmapResolution; y++)
            {
                for (int x = 0; x < terrainData.heightmapResolution; x++)
                {
                    newHeights[y, x] = baseTex.GetPixel(x, y).r / maxColor * (maxBaseHeight - minBaseHeight) + minBaseHeight;
                }
            }
        }

        maxColor = 0f;

        if (hasGroundHeights)
        {
            for (int y = 0; y < terrainData.heightmapResolution; y++)
            {
                for (int x = 0; x < terrainData.heightmapResolution; x++)
                {
                    if (tex.GetPixel(x, y).r > maxColor) maxColor = tex.GetPixel(x, y).r;
                }
            }

            for (int y = 0; y < terrainData.heightmapResolution; y++)
            {
                for (int x = 0; x < terrainData.heightmapResolution; x++)
                {
                    if (hasBaseHeights)
                        newHeights[y, x] += tex.GetPixel(x, y).r / maxColor * (maxHeight - minHeight);
                    else
                        newHeights[y, x] = tex.GetPixel(x, y).r / maxColor * (maxHeight - minHeight) + minHeight;
                }
            }
        }

        terrainData.SetHeights(xOffset, yOffset, newHeights);
    }

    public void ModifyTerrain()
    {
        LoadModel();
        GrabTerrainData();

        Texture2D modifiedTex = new Texture2D(256, 256, TextureFormat.ARGB32, false);
        Texture2D modifiedBaseTex = new Texture2D(256, 256, TextureFormat.ARGB32, false);

        if (hasGroundHeights)
        {
            Texture2D tex = RetrieveTerrainHeightmap();
            float[] values = ProcessHeightmap(tex);

            Tensor input = new Tensor(1, 256, 256, 3, values);
            Tensor output = worker.Execute(input).PeekOutput();
            float[] newValues = output.AsFloats();
            input.Dispose();

            Texture2D newTex = new Texture2D(256, 256, TextureFormat.ARGB32, false);

            for (int h = 0; h < 256; h++)
            {
                for (int w = 0; w < 256; w++)
                {
                    newTex.SetPixel(w, h, new Color(newValues[w * 3 + h * 256 * 3] * 0.5f + 0.5f,
                                                    newValues[w * 3 + h * 256 * 3 + 1] * 0.5f + 0.5f,
                                                    newValues[w * 3 + h * 256 * 3 + 2] * 0.5f + 0.5f));
                }
            }

            modifiedTex = RestoreHeightmap(newTex);
        }

        if (hasBaseHeights)
        {
            Texture2D tex = RetrieveBaseTerrainHeightmap();
            float[] values = ProcessHeightmap(tex);

            Tensor input = new Tensor(1, 256, 256, 3, values);
            Tensor output = worker.Execute(input).PeekOutput();
            float[] newValues = output.AsFloats();
            input.Dispose();

            Texture2D newTex = new Texture2D(256, 256, TextureFormat.ARGB32, false);

            for (int h = 0; h < 256; h++)
            {
                for (int w = 0; w < 256; w++)
                {
                    newTex.SetPixel(w, h, new Color(newValues[w * 3 + h * 256 * 3] * 0.5f + 0.5f,
                                                    newValues[w * 3 + h * 256 * 3 + 1] * 0.5f + 0.5f,
                                                    newValues[w * 3 + h * 256 * 3 + 2] * 0.5f + 0.5f));
                }
            }

            modifiedBaseTex = RestoreHeightmap(newTex);
        }

        worker?.Dispose();

        WriteTerrainData(modifiedTex, modifiedBaseTex);

        PaintTerrain();
    }

    public void PaintTerrain()
    {
        GetComponent<ProceduralTerrainPainter>().PaintTerrain();
    }

    public void OnDestroy()
    {
        worker?.Dispose();
    }
}
