using System.Linq;

using UnityEngine;

public sealed class TerrainTool : MonoBehaviour
{

    public Terrain targetTerrain;
    private float[,] virtualHeights;
    private float[,] virtualBaseHeights;
    private float[,] savedHeights;
    private float[,] savedBaseHeights;
    private float[,] alphas;
    public float terrainOffset = 0f;

    private bool hasBaseHeights, hasGroundHeights;

    private void Start()
    {
        targetTerrain = GameObject.FindObjectOfType<Terrain>();
        virtualHeights = new float[GetHeightmapResolution(), GetHeightmapResolution()];
        virtualBaseHeights = new float[GetHeightmapResolution(), GetHeightmapResolution()];
        savedHeights = new float[GetHeightmapResolution(), GetHeightmapResolution()];
        savedBaseHeights = new float[GetHeightmapResolution(), GetHeightmapResolution()];

        float terrainSizeY = GetTerrainSize().y;
        for (int y = 0; y < GetHeightmapResolution(); y++)
        {
            for (int x = 0; x < GetHeightmapResolution(); x++)
            {
                virtualHeights[y, x] = terrainOffset / terrainSizeY;
                virtualBaseHeights[y, x] = terrainOffset / terrainSizeY;
                savedHeights[y, x] = terrainOffset / terrainSizeY;
                savedBaseHeights[y, x] = terrainOffset / terrainSizeY;
            }
        }

        targetTerrain.terrainData.SetHeights(0, 0, virtualHeights);

        alphas = new float[GetHeightmapResolution(), GetHeightmapResolution()];
    }

    private TerrainData GetTerrainData() => targetTerrain.terrainData;

    private int GetHeightmapResolution() => GetTerrainData().heightmapResolution;

    private Vector3 GetTerrainSize() => GetTerrainData().size;

    public Vector3 WorldToTerrainPosition(Vector3 worldPosition)
    {
        var terrainPosition = worldPosition - targetTerrain.GetPosition();

        var terrainSize = GetTerrainSize();

        var heightmapResolution = GetHeightmapResolution();

        terrainPosition = new Vector3(terrainPosition.x / terrainSize.x, terrainPosition.y / terrainSize.y, terrainPosition.z / terrainSize.z);

        return new Vector3(terrainPosition.x * heightmapResolution, 0, terrainPosition.z * heightmapResolution);
    }

    public Vector2Int GetBrushPosition(Vector3 worldPosition, int brushWidth, int brushHeight)
    {
        var terrainPosition = WorldToTerrainPosition(worldPosition);

        var heightmapResolution = GetHeightmapResolution();

        return new Vector2Int((int)Mathf.Clamp(terrainPosition.x - brushWidth / 2.0f, 0.0f, heightmapResolution), (int)Mathf.Clamp(terrainPosition.z - brushHeight / 2.0f, 0.0f, heightmapResolution));
    }

    public Vector2Int GetSafeBrushSize(int brushX, int brushY, int brushWidth, int brushHeight)
    {
        var heightmapResolution = GetHeightmapResolution();

        while (heightmapResolution - (brushX + brushWidth) < 0) brushWidth--;

        while (heightmapResolution - (brushY + brushHeight) < 0) brushHeight--;

        return new Vector2Int(brushWidth, brushHeight);
    }

    public void RaiseTerrain(Vector3 worldPosition, float height, float baseBrushSize, float leftBrushSize, float rightBrushSize, float leftBrushCurve, float rightBrushCurve, Vector3 derivative, Vector3 leftSlope, Vector3 rightSlope, Vector3 start, Vector3 end)
    {
        if (!hasGroundHeights) hasGroundHeights = true;

        int maxBrushSize = (int)Mathf.Max(baseBrushSize * leftBrushSize, baseBrushSize * rightBrushSize);

        var brushPosition = GetBrushPosition(worldPosition, maxBrushSize, maxBrushSize);

        var brushSize = GetSafeBrushSize(brushPosition.x, brushPosition.y, maxBrushSize, maxBrushSize);

        var terrainData = GetTerrainData();

        Vector3 direction = new Vector3(derivative.x, 0f, derivative.z);
        Vector3 deviation = new Vector3(0f, 0f, 0f);

        Vector3 startDirection = (worldPosition - start).normalized;
        Vector3 endDirection = (worldPosition - end).normalized;
        Vector3 leftDirection = (worldPosition - leftSlope).normalized;
        Vector3 rightDirection = (worldPosition - rightSlope).normalized;

        float startSize = Mathf.Max(leftBrushSize, rightBrushSize);
        float endSize = startSize;
        float frontSize = startSize;
        float backSize = startSize;

        if (startDirection.y != 0f)
        {
            startSize = Mathf.Sqrt(startDirection.x * startDirection.x + startDirection.z * startDirection.z) / Mathf.Abs(startDirection.y);
        }

        if (endDirection.y != 0f)
        {
            endSize = Mathf.Sqrt(endDirection.x * endDirection.x + endDirection.z * endDirection.z) / Mathf.Abs(endDirection.y);
        }

        if (startDirection.y != 0f)
        {
            frontSize = Mathf.Sqrt(leftDirection.x * leftDirection.x + leftDirection.z * leftDirection.z) / Mathf.Abs(leftDirection.y);
        }

        if (endDirection.y != 0f)
        {
            backSize = Mathf.Sqrt(rightDirection.x * rightDirection.x + rightDirection.z * rightDirection.z) / Mathf.Abs(rightDirection.y);
        }

        float angle = 0f;
        float angleTemp;

        for (var y = 0; y < brushSize.y; y++)
        {
            for (var x = 0; x < brushSize.x; x++)
            {
                deviation.x = (float)(x - brushSize.x / 2);
                deviation.z = (float)(y - brushSize.y / 2);

                angle = Vector3.Angle(direction.normalized, deviation.normalized) * 
                                      Mathf.Sign(direction.normalized.x * deviation.normalized.z - direction.normalized.z * deviation.normalized.x);
                angle /= 180f;
                angleTemp = angle;

                if (angle < -0.5f) angle = 1f - (-angle - 0.5f);
                else if (angle < 0.5f && angle >= -0.5f) angle = 1f - (angle + 0.5f);
                else angle = angle - 0.5f;

                float distance = Mathf.Sqrt(Mathf.Pow((float)x - (float)brushSize.x / 2f, 2f) +
                                            Mathf.Pow((float)y - (float)brushSize.y / 2f, 2f));

                float adjustedSize;

                if (leftBrushSize < rightBrushSize)
                {
                    adjustedSize = Mathf.Lerp(leftBrushSize * (float)brushSize.x, (float)brushSize.x, angle);
                }
                else
                {
                    adjustedSize = Mathf.Lerp((float)brushSize.x, rightBrushSize * (float)brushSize.x, angle);
                }

                if (Mathf.Min(startSize, endSize, frontSize, backSize) < Mathf.Max(leftBrushSize, rightBrushSize))
                {
                    adjustedSize *= Mathf.Min(startSize, endSize, frontSize, backSize);
                }

                distance /= adjustedSize / 2f;
                distance = Mathf.Clamp(distance, 0f, 1f);
                distance = 1f - distance;

                if (angleTemp < 0f) distance = Mathf.Pow(distance, Mathf.Lerp(rightBrushCurve, 1f, Mathf.Abs(angleTemp + 0.5f) * 2f));
                else distance = Mathf.Pow(distance, Mathf.Lerp(leftBrushCurve, 1f, Mathf.Abs(angleTemp - 0.5f) * 2f));

                if (virtualHeights[y + brushPosition.y, x + brushPosition.x] < distance * (height - terrainOffset) / terrainData.size.y + terrainOffset / terrainData.size.y)
                    virtualHeights[y + brushPosition.y, x + brushPosition.x] = distance * (height - terrainOffset) / terrainData.size.y + terrainOffset / terrainData.size.y;
            }
        }
    }

    public void LowerTerrain(Vector3 worldPosition, float height, float baseBrushSize, float leftBrushSize, float rightBrushSize, float leftBrushCurve, float rightBrushCurve, Vector3 derivative)
    {
        int maxBrushSize = (int)Mathf.Max(baseBrushSize * leftBrushSize, baseBrushSize * rightBrushSize);

        var brushPosition = GetBrushPosition(worldPosition, maxBrushSize, maxBrushSize);

        var brushSize = GetSafeBrushSize(brushPosition.x, brushPosition.y, maxBrushSize, maxBrushSize);

        var terrainData = GetTerrainData();

        float brushHeight = (worldPosition.y - targetTerrain.transform.position.y) / terrainData.size.y;
        float originHeight = (height - targetTerrain.transform.position.y) / terrainData.size.y;

        Vector3 direction = new Vector3(derivative.x, 0f, derivative.z);
        Vector3 deviation = new Vector3(0f, 0f, 0f);

        float angle = 0f;
        float angleTemp;

        for (var y = 0; y < brushSize.y; y++)
        {
            for (var x = 0; x < brushSize.x; x++)
            {
                deviation.x = (float)(x - brushSize.x / 2);
                deviation.z = (float)(y - brushSize.y / 2);

                angle = Vector3.Angle(direction.normalized, deviation.normalized) *
                                      Mathf.Sign(direction.normalized.x * deviation.normalized.z - direction.normalized.z * deviation.normalized.x);
                angle /= 180f;
                angleTemp = angle;

                if (angle < -0.5f) angle = 1f - (-angle - 0.5f);
                else if (angle < 0.5f && angle >= -0.5f) angle = 1f - (angle + 0.5f);
                else angle = angle - 0.5f;

                float distance = Mathf.Sqrt(Mathf.Pow((float)x - (float)brushSize.x / 2f, 2f) +
                                            Mathf.Pow((float)y - (float)brushSize.y / 2f, 2f));

                float adjustedSize;

                if (leftBrushSize < rightBrushSize)
                {
                    adjustedSize = Mathf.Lerp(leftBrushSize * (float)brushSize.x, (float)brushSize.x, angle);
                }
                else
                {
                    adjustedSize = Mathf.Lerp((float)brushSize.x, rightBrushSize * (float)brushSize.x, angle);
                }

                distance /= adjustedSize / 2f;
                distance = Mathf.Clamp(distance, 0f, 1f);

                if (angleTemp < 0f) distance = Mathf.Pow(distance, Mathf.Lerp(rightBrushCurve, 1f, Mathf.Abs(angleTemp + 0.5f) * 2f));
                else distance = Mathf.Pow(distance, Mathf.Lerp(leftBrushCurve, 1f, Mathf.Abs(angleTemp - 0.5f) * 2f));

                float desireHeight = Mathf.Lerp(brushHeight, originHeight, distance);

                if (desireHeight > terrainOffset / terrainData.size.y)
                {
                    if (!hasBaseHeights) hasBaseHeights = true;

                    if (virtualBaseHeights[y + brushPosition.y, x + brushPosition.x] > desireHeight)
                        virtualBaseHeights[y + brushPosition.y, x + brushPosition.x] = desireHeight;
                }
                else
                {
                    if (virtualHeights[y + brushPosition.y, x + brushPosition.x] < desireHeight)
                        virtualHeights[y + brushPosition.y, x + brushPosition.x] = desireHeight;
                }
            }
        }
    }

    public void FillTerrain(Vector3 worldPosition, Vector3 initialPosition, int brushWidth, int brushHeight)
    {
        var brushPosition = GetBrushPosition(worldPosition, brushWidth, brushHeight);
        var initBrushPosition = GetBrushPosition(initialPosition, brushWidth, brushHeight);

        int worldPositionX = brushPosition.x + (int)((float)brushWidth / 2f);
        int worldPositionY = brushPosition.y + (int)((float)brushWidth / 2f);
        int initPositionX = initBrushPosition.x + (int)((float)brushWidth / 2f);
        int initPositionY = initBrushPosition.y + (int)((float)brushWidth / 2f);

        var brushSize = GetSafeBrushSize(brushPosition.x, brushPosition.y, brushWidth, brushHeight);

        var terrainData = GetTerrainData();

        float currentHeight = (worldPosition.y - targetTerrain.transform.position.y) / terrainData.size.y;
        float originHeight = (initialPosition.y - targetTerrain.transform.position.y) / terrainData.size.y;
        float height = 0f;

        int start, end;

        if (worldPositionX != initPositionX)
        {
            start = (int)Mathf.Min(worldPositionX, initPositionX);
            end = (int)Mathf.Max(worldPositionX, initPositionX);

            int y;

            for (var x = start; x < end; x++)
            {
                y = initPositionY + (int)((float)(x - initPositionX) / (float)(worldPositionX - initPositionX) * (worldPositionY - initPositionY));
                height = originHeight + (currentHeight - originHeight) * (float)(x - initPositionX) / (float)(worldPositionX - initPositionX);

                for (var yy = 0; yy < 16; yy++)
                {
                    for (var xx = 0; xx < 16; xx++)
                    {
                        if (height > terrainOffset / terrainData.size.y)
                            virtualHeights[yy - 8 + y, xx - 8 + x] = height;
                        else
                            virtualBaseHeights[yy - 8 + y, xx - 8 + x] = height;
                    }
                }
            }
        }
        else
        {
            start = (int)Mathf.Min(worldPositionY, initPositionY);
            end = (int)Mathf.Max(worldPositionY, initPositionY);

            int x;

            for (var y = start; y < end; y++)
            {
                x = (int)initPositionX + (int)((float)(y - initPositionY) / (float)(worldPositionY - initPositionY) * (worldPositionX - initPositionX));
                height = originHeight + (currentHeight - originHeight) * (float)(y - initPositionY) / (float)(worldPositionY - initPositionY);

                for (var yy = 0; yy < 16; yy++)
                {
                    for (var xx = 0; xx < 16; xx++)
                    {
                        if (height > terrainOffset / terrainData.size.y)
                            virtualHeights[yy - 8 + y, xx - 8 + x] = height;
                        else
                            virtualBaseHeights[yy - 8 + y, xx - 8 + x] = height;
                    }
                }
            }
        }
    }


    public void ApplyTerrain()
    {
        var terrainData = GetTerrainData();

        int r = terrainData.heightmapResolution;

        for (var y = 0; y < r; y++)
        {
            for (var x = 0; x < r; x++)
            {
                if (savedHeights[y, x] > virtualHeights[y, x])  
                    virtualHeights[y, x] = savedHeights[y, x];
                if (savedBaseHeights[y, x] < virtualBaseHeights[y, x]) 
                    virtualBaseHeights[y, x] = savedBaseHeights[y, x];
            }
        }

        TerrainModifier modifier = Object.FindObjectOfType<TerrainModifier>();
        modifier.heights = virtualHeights;
        modifier.baseHeights = virtualBaseHeights;
        modifier.hasGroundHeights = hasGroundHeights;
        modifier.hasBaseHeights = hasBaseHeights;
        modifier.alphas = alphas;
        modifier.terrainOffset = terrainOffset / terrainData.size.y;

        modifier.ModifyTerrain();
        //GetTerrainData().SetHeights(0, 0, virtualHeights);
    }

    public void SaveTerrain()
    {
        var terrainData = GetTerrainData();

        int r = terrainData.heightmapResolution;

        for (var y = 0; y < r; y++)
        {
            for (var x = 0; x < r; x++)
            {
                if (virtualHeights[y, x] > savedHeights[y, x])  
                    savedHeights[y, x] = virtualHeights[y, x];
                if (virtualBaseHeights[y, x] < savedBaseHeights[y, x]) 
                    savedBaseHeights[y, x] = virtualBaseHeights[y, x];
            }
        }
    }
    

    public void ClearTerrain(bool eraseHistory)
    {
        hasBaseHeights = false;
        hasGroundHeights = false;

        var terrainData = GetTerrainData();

        int r = terrainData.heightmapResolution;

        for (var y = 0; y < r; y++)
        {
            for (var x = 0; x < r; x++)
            {
                virtualHeights[y, x] = terrainOffset / terrainData.size.y;
                virtualBaseHeights[y, x] = terrainOffset / terrainData.size.y;
                if (eraseHistory)
                {
                    savedHeights[y, x] = terrainOffset / terrainData.size.y;
                    savedBaseHeights[y, x] = terrainOffset / terrainData.size.y;
                }
                alphas[y, x] = 0f;
            }
        }

        terrainData.SetHeights(0, 0, virtualHeights);

        TerrainModifier modifier = Object.FindObjectOfType<TerrainModifier>();
        modifier.PaintTerrain();
    }
}