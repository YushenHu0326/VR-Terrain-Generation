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
    private float maxColor, maxBaseColor;
    private int terrainType;

    public float[,] heights;
    public float[,] baseHeights;
    public float[,] alphas;
    public int xOffset, yOffset, range;
    public float terrainOffset;

    public bool hasGroundHeights;
    public bool hasBaseHeights;

    private Model model;
    public NNModel ONNXModel;

    private IWorker worker;


    private int count;

    public void LoadModel()
    {
        model = ModelLoader.Load(ONNXModel);

        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputeRef, model);
    }

    void GrabTerrainData()
    {
        terrain = Object.FindObjectOfType<Terrain>();
        terrainData = terrain.terrainData;

        maxHeight = terrainOffset;
        minHeight = terrainOffset;
        maxBaseHeight = terrainOffset;
        minBaseHeight = terrainOffset;

        if (hasGroundHeights)
        {
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

    float[] ProcessHeightmap(Texture2D tex, Texture2D aTex, bool based)
    {
        tex.Apply();
        RenderTexture rt = RenderTexture.GetTemporary(256, 256, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        RenderTexture.active = rt;

        Graphics.Blit(aTex, rt);
        aTex.Reinitialize(256, 256, aTex.format, true);
        aTex.filterMode = FilterMode.Bilinear;
        aTex.ReadPixels(new Rect(0.0f, 0.0f, 256, 256), 0, 0);
        aTex.Apply();

        Graphics.Blit(tex, rt);
        tex.Reinitialize(256, 256, tex.format, true);
        tex.filterMode = FilterMode.Bilinear;
        tex.ReadPixels(new Rect(0.0f, 0.0f, 256, 256), 0, 0);
        tex.Apply();

        float level = (maxHeight - minHeight) / 0.012f;
        if (based) 
            level = (maxBaseHeight - minBaseHeight) / 0.012f;

        for (int h = 0; h < 256; h++)
        {
            for (int w = 0; w < 256; w++)
            {
                float r = tex.GetPixel(w, h).r;

                float n = Mathf.PerlinNoise((float)w / 15f, (float)h / 15f);

                float a = aTex.GetPixel(w, h).r;
                if (!based) a = 1f - a;
                a *= 1f - 2f * Mathf.Abs(0.5f - r);

                //n *= 1f - terrainData.GetInterpolatedNormal((float)w / 256f, (float)h / 256f).y;

                r *= (n * a + 1f);

                r *= level;
                r = Mathf.Floor(r);
                r /= level * 1.5f;

                tex.SetPixel(w, h, new Color(r, r, r));
            }
        }

        //byte[] bytes = tex.EncodeToPNG();

        //System.IO.File.WriteAllBytes(Application.dataPath + "/h.png", bytes);

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

        UserStudy study = FindObjectOfType<UserStudy>();

        if (study != null)
        {
            count++;
            string name = "/UserStudy/user";
            name += study.userIndex.ToString();
            name += "-";
            name += count.ToString();
            name += ".png";
            System.IO.File.WriteAllBytes(Application.dataPath + name, bytes); 
        }

        return tex;
    }

    void WriteTerrainData(Texture2D tex, Texture2D baseTex)
    {
        /*Texture2D atex = new Texture2D(terrainData.heightmapResolution, terrainData.heightmapResolution, TextureFormat.ARGB32, false);
        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                atex.SetPixel(x, y, new Color(alphas[y, x], alphas[y, x], alphas[y, x], 1f));
            }
        }

        byte[] bytes = atex.EncodeToPNG();

        System.IO.File.WriteAllBytes(Application.dataPath + "/a.png", bytes);*/

        float[,] originHeights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        float[,] newHeights = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];

        float d = maxHeight - minHeight;
        float baseD = maxBaseHeight - minBaseHeight;

        if (hasBaseHeights && !hasGroundHeights)
        {
            maxBaseColor = 0f;

            for (int y = 0; y < terrainData.heightmapResolution; y++)
            {
                for (int x = 0; x < terrainData.heightmapResolution; x++)
                {
                    if (baseTex.GetPixel(x, y).r > maxBaseColor) maxBaseColor = baseTex.GetPixel(x, y).r;
                }
            }

            for (int y = 0; y < terrainData.heightmapResolution; y++)
            {
                for (int x = 0; x < terrainData.heightmapResolution; x++)
                {
                    newHeights[y, x] = baseTex.GetPixel(x, y).r / maxBaseColor * baseD + minBaseHeight;
                }
            }
        }
        else if (hasGroundHeights && !hasBaseHeights)
        {
            maxColor = 0f;

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
                    newHeights[y, x] = tex.GetPixel(x, y).r / maxColor * d + minHeight;
                }
            }
        }
        else
        {
            maxColor = 0f;
            maxBaseColor = 0f;

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
                    if (baseTex.GetPixel(x, y).r > maxBaseColor) maxBaseColor = baseTex.GetPixel(x, y).r;
                }
            }

            for (int y = 0; y < terrainData.heightmapResolution; y++)
            {
                for (int x = 0; x < terrainData.heightmapResolution; x++)
                {
                    float baseHeight = baseTex.GetPixel(x, y).r / maxBaseColor * baseD + minBaseHeight;
                    float groundHeight = tex.GetPixel(x, y).r / maxColor * d + minHeight;

                    float lerp = tex.GetPixel(x, y).r * 2f;

                    newHeights[y, x] = Mathf.Lerp(baseHeight, groundHeight, lerp);
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

        Texture2D alphaTex = new Texture2D(terrainData.heightmapResolution, terrainData.heightmapResolution, TextureFormat.ARGB32, false);
        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                alphaTex.SetPixel(x, y, new Color(alphas[y, x], alphas[y, x], alphas[y, x], 1f));
            }
        }

        if (hasGroundHeights)
        {
            Texture2D tex = RetrieveTerrainHeightmap();
            float[] values = ProcessHeightmap(tex, alphaTex, false);

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
            float[] values = ProcessHeightmap(tex, alphaTex, true);

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
