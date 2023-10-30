using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralTerrainPainter : MonoBehaviour
{
    public TerrainLayer grassLayer_1;
    public TerrainLayer grassLayer_2;
    public TerrainLayer snowLayer;
    public TerrainLayer cliffLayer_1;
    public TerrainLayer cliffLayer_2;

    private Terrain terrain;
    private TerrainData terrainData;

    private float[,,] alphas;

    // Start is called before the first frame update
    void Start()
    {
        InitializeTerrain();

        PaintTerrain();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InitializeTerrain()
    {
        terrain = Object.FindObjectOfType<Terrain>();
        terrainData = terrain.terrainData;

        terrainData.terrainLayers = new TerrainLayer[] { grassLayer_1,
                                                         grassLayer_2,
                                                         snowLayer,
                                                         cliffLayer_1,
                                                         cliffLayer_2 };

        alphas = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);
    }

    public void PaintTerrain()
    {
        InitializeTerrain();

        float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        float height = 0;
        float normal = 0f;
        //float noise = 0f;
        int layer = 0;

        //Texture2D tex = new Texture2D(terrainData.alphamapWidth, terrainData.alphamapHeight, TextureFormat.ARGB32, false);

        for (int y = 0; y < terrainData.alphamapWidth; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                height = heights[(int)((float)y / (float)terrainData.alphamapHeight * (float)terrainData.heightmapResolution),
                                 (int)((float)x / (float)terrainData.alphamapWidth * (float)terrainData.heightmapResolution)] * terrainData.size.y;
                normal = terrainData.GetInterpolatedNormal((float)(x) / (float)terrainData.alphamapWidth, (float)(y) / (float)terrainData.alphamapHeight).y;
                //noise = Mathf.PerlinNoise((float)x / 20f, (float)y / 20f);
                layer = (int)(height / 3f);

                //tex.SetPixel(x, y, new Color(normal, 0f, 0f, 1f));

                if (normal > 0.8f)
                {
                    if (height < 55f)
                    {
                        alphas[y, x, 0] = 1f;
                        alphas[y, x, 1] = 0f;
                        alphas[y, x, 2] = 0f;
                    }    
                    else if (height < 60f)
                    {
                        alphas[y, x, 0] = (60f - height) / 5f;
                        alphas[y, x, 1] = 0f;
                        alphas[y, x, 2] = (height - 55f) / 5f;
                    }
                    else
                    {
                        alphas[y, x, 0] = 0f;
                        alphas[y, x, 1] = 0f;
                        alphas[y, x, 2] = 1f;
                    }

                    alphas[y, x, 3] = 0f;
                    alphas[y, x, 4] = 0f;

                }
                else
                {
                    alphas[y, x, 0] = 0f;
                    alphas[y, x, 1] = 0f;
                    alphas[y, x, 2] = 0f;

                    if (layer % 2 == 0)
                    {
                        alphas[y, x, 3] = 1f;
                        alphas[y, x, 4] = 0.2f;
                    }
                    else
                    {
                        alphas[y, x, 3] = 0.2f;
                        alphas[y, x, 4] = 1f;
                    }
                }
            }
        }

        terrainData.SetAlphamaps(0, 0, alphas);

        //byte[] bytes = tex.EncodeToPNG();

        //System.IO.File.WriteAllBytes(Application.dataPath + "/n.png", bytes);
    }
}
