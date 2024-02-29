using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Presentation : MonoBehaviour
{
    public Terrain terrain;
    public Texture2D height;
    // Start is called before the first frame update
    void Start()
    {
        if (terrain != null)
        {
            TerrainData terrainData = terrain.terrainData;
            float[,] newHeights = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];

            for (int y = 0; y < terrainData.heightmapResolution; y++)
            {
                for (int x = 0; x < terrainData.heightmapResolution; x++)
                {
                    newHeights[y, x] = height.GetPixel((int)((float)x * (float)height.width / (float)terrainData.heightmapResolution), (int)((float)y * (float)height.height / (float)terrainData.heightmapResolution)).r / 5f;
                }
            }

            terrainData.SetHeights(0, 0, newHeights);

            GetComponent<ProceduralTerrainPainter>().PaintTerrain();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
