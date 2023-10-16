using System.Linq;

using UnityEngine;

public sealed class TerrainTool : MonoBehaviour
{
    public enum TerrainModificationAction
    {
        Raise,
        Lower,
        Flatten,
        Sample,
        SampleAverage,
    }

    public int brushWidth = 200;
    public int brushHeight = 200;

    public TerrainModificationAction modificationAction;

    public Terrain _targetTerrain;
    private float[,] virtualHeights;
    private float[,] alphas;
    public float terrainOffset = 50f;

    private float _sampledHeight;

    public void OnActionChange(int value)
    {
        if (value == 0) modificationAction = TerrainModificationAction.Raise;
        else if (value == 1) modificationAction = TerrainModificationAction.Lower;
        else if (value == 2) modificationAction = TerrainModificationAction.Flatten;
        else if (value == 3) modificationAction = TerrainModificationAction.Sample;
        else if (value == 4) modificationAction = TerrainModificationAction.SampleAverage;
    }

    public void OnSizeChange(float size)
    {
        brushWidth = (int) (500f * size);
        brushHeight = (int) (500f * size);
    }

    private void Start()
    {
        _targetTerrain = GameObject.FindObjectOfType<Terrain>();
        virtualHeights = new float[GetHeightmapResolution(), GetHeightmapResolution()];

        float terrainSizeY = GetTerrainSize().y;
        for (int y = 0; y < GetHeightmapResolution(); y++)
        {
            for (int x = 0; x < GetHeightmapResolution(); x++)
            {
                virtualHeights[y, x] = terrainOffset / terrainSizeY;
            }
        }

        _targetTerrain.terrainData.SetHeights(0, 0, virtualHeights);

        alphas = new float[GetHeightmapResolution(), GetHeightmapResolution()];
    }

    private TerrainData GetTerrainData() => _targetTerrain.terrainData;

    private int GetHeightmapResolution() => GetTerrainData().heightmapResolution;

    private Vector3 GetTerrainSize() => GetTerrainData().size;

    public Vector3 WorldToTerrainPosition(Vector3 worldPosition)
    {
        var terrainPosition = worldPosition - _targetTerrain.GetPosition();

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

    public void RaiseTerrain(Vector3 worldPosition, float height, float baseBrushSize, float leftBrushSize, float rightBrushSize, Vector3 derivative)
    {
        int maxBrushSize = (int)Mathf.Max(baseBrushSize * leftBrushSize, baseBrushSize * rightBrushSize);

        var brushPosition = GetBrushPosition(worldPosition, maxBrushSize, maxBrushSize);

        var brushSize = GetSafeBrushSize(brushPosition.x, brushPosition.y, maxBrushSize, maxBrushSize);

        var terrainData = GetTerrainData();

        Vector3 direction = new Vector3(derivative.x, 0f, derivative.z);
        Vector3 deviation = new Vector3(0f, 0f, 0f);
        float angle = 0f;

        for (var y = 0; y < brushSize.y; y++)
        {
            for (var x = 0; x < brushSize.x; x++)
            {
                deviation.x = (float)(x - brushSize.x / 2);
                deviation.z = (float)(y - brushSize.y / 2);

                angle = Vector3.Angle(direction.normalized, deviation.normalized) * 
                                      Mathf.Sign(direction.normalized.x * deviation.normalized.z - direction.normalized.z * deviation.normalized.x);
                angle /= 180f;

                if (angle < -0.5f) angle = 1f - (-angle - 0.5f);
                else if (angle < 0.5f && angle >= -0.5f) angle = 1f - (angle + 0.5f);
                else angle = angle - 0.5f;

                float distance = Mathf.Sqrt(Mathf.Pow((float)x - (float)brushSize.x / 2f, 2f) +
                                            Mathf.Pow((float)y - (float)brushSize.y / 2f, 2f));
                distance /= (float)brushSize.x / 2f;
                distance = Mathf.Clamp(distance, 0f, 1f);
                distance = 1f - distance;

                if (leftBrushSize < rightBrushSize)
                {
                    distance = Mathf.Lerp(distance * leftBrushSize / rightBrushSize, distance, angle);
                }
                else
                {
                    distance = Mathf.Lerp(distance * rightBrushSize / leftBrushSize, distance, angle);
                }

                if (virtualHeights[y + brushPosition.y, x + brushPosition.x] - terrainOffset / terrainData.size.y < distance * (height - terrainOffset) / terrainData.size.y)
                    virtualHeights[y + brushPosition.y, x + brushPosition.x] = distance * (height - terrainOffset) / terrainData.size.y + terrainOffset / terrainData.size.y;

                if (alphas[y + brushPosition.y, x + brushPosition.x] < 1f)
                    alphas[y + brushPosition.y, x + brushPosition.x] = 0.3f;
            }
        }
    }

    public void PaintStroke(Vector3 worldPosition, float height, float baseBrushSize, float leftBrushSize, float rightBrushSize, Vector3 derivative, int width)
    {
        int maxBrushSize = (int)Mathf.Max(baseBrushSize * leftBrushSize, baseBrushSize * rightBrushSize);

        var brushPosition = GetBrushPosition(worldPosition, maxBrushSize, maxBrushSize);

        var brushSize = GetSafeBrushSize(brushPosition.x, brushPosition.y, maxBrushSize, maxBrushSize);

        var terrainData = GetTerrainData();

        for (var y = 0; y < brushSize.y; y++)
        {
            for (var x = 0; x < brushSize.x; x++)
            {
                if (Mathf.Abs(derivative.normalized.y) < 0.15f)
                {
                    if (y >= brushSize.y / 2 - width && y <= brushSize.y / 2 + width)
                        if (x >= brushSize.x / 2 - width && x <= brushSize.x / 2 + width)
                            alphas[y + brushPosition.y, x + brushPosition.x] = 1f;
                }
                else if (Mathf.Abs(derivative.normalized.y) > 0.8f)
                {
                    if (y >= brushSize.y / 2 - width * 3 && y <= brushSize.y / 2 + width * 3)
                        if (x >= brushSize.x / 2 - width * 3 && x <= brushSize.x / 2 + width * 3)
                            alphas[y + brushPosition.y, x + brushPosition.x] = 0.6f;
                }
            }
        }
    }


    public void LowerTerrain(Vector3 worldPosition, float height, float baseBrushSize, float leftBrushSize, float rightBrushSize, Vector3 derivative)
    {
        int maxBrushSize = (int)Mathf.Max(baseBrushSize * leftBrushSize, baseBrushSize * rightBrushSize);

        var brushPosition = GetBrushPosition(worldPosition, maxBrushSize, maxBrushSize);

        var brushSize = GetSafeBrushSize(brushPosition.x, brushPosition.y, maxBrushSize, maxBrushSize);

        var terrainData = GetTerrainData();

        Vector3 direction = new Vector3(derivative.x, 0f, derivative.z);
        Vector3 deviation = new Vector3(0f, 0f, 0f);
        float angle = 0f;

        for (var y = 0; y < brushSize.y; y++)
        {
            for (var x = 0; x < brushSize.x; x++)
            {
                deviation.x = (float)(x - brushSize.x / 2);
                deviation.z = (float)(y - brushSize.y / 2);

                angle = Vector3.Angle(direction.normalized, deviation.normalized) *
                                      Mathf.Sign(direction.normalized.x * deviation.normalized.z - direction.normalized.z * deviation.normalized.x);
                angle /= 180f;

                if (angle < -0.5f) angle = 1f - (-angle - 0.5f);
                else if (angle < 0.5f && angle >= -0.5f) angle = 1f - (angle + 0.5f);
                else angle = angle - 0.5f;

                float distance = Mathf.Sqrt(Mathf.Pow((float)x - (float)brushSize.x / 2f, 2f) +
                                            Mathf.Pow((float)y - (float)brushSize.y / 2f, 2f));
                distance /= (float)brushSize.x / 2f;
                distance *= distance;
                distance = Mathf.Clamp(distance, 0f, 1f);
                distance = 1f - distance;

                if (leftBrushSize < rightBrushSize)
                {
                    distance = Mathf.Lerp(distance * leftBrushSize / rightBrushSize, distance, angle);
                }
                else
                {
                    distance = Mathf.Lerp(distance * rightBrushSize / leftBrushSize, distance, angle);
                }
                
                if (virtualHeights[y + brushPosition.y, x + brushPosition.x] > (terrainOffset - distance * (terrainOffset - height)) / terrainData.size.y)
                    virtualHeights[y + brushPosition.y, x + brushPosition.x] = (terrainOffset - distance * (terrainOffset - height)) / terrainData.size.y;

                if (angle < 0.7f && angle > 0.3f)
                    alphas[y + brushPosition.y, x + brushPosition.x] = 0.6f;
            }
        }
    }

    public void FillTerrain(Vector3 worldPosition, Vector3 initialPosition, float height, int brushWidth, int brushHeight)
    {
        var brushPosition = GetBrushPosition(worldPosition, brushWidth, brushHeight);
        var initBrushPosition = GetBrushPosition(initialPosition, brushWidth, brushHeight);

        int worldPositionX = brushPosition.x + (int)((float)brushWidth / 2f);
        int worldPositionY = brushPosition.y + (int)((float)brushWidth / 2f);
        int initPositionX = initBrushPosition.x + (int)((float)brushWidth / 2f);
        int initPositionY = initBrushPosition.y + (int)((float)brushWidth / 2f);

        var brushSize = GetSafeBrushSize(brushPosition.x, brushPosition.y, brushWidth, brushHeight);

        var terrainData = GetTerrainData();

        int start, end;

        if (worldPositionX != initPositionX)
        {
            start = (int)Mathf.Min(worldPositionX, initPositionX);
            end = (int)Mathf.Max(worldPositionX, initPositionX);

            int y;

            for (var x = start; x < end; x++)
            {
                y = initPositionY + (int)((float)(x - initPositionX) / (float)(worldPositionX - initPositionX) * (worldPositionY - initPositionY));

                for (var yy = 0; yy < 20; yy++)
                {
                    for (var xx = 0; xx < 20; xx++)
                    {
                        if (virtualHeights[yy - 10 + y, xx - 10 + x] < height / terrainData.size.y)
                            virtualHeights[yy - 10 + y, xx - 10 + x] = height / terrainData.size.y;
                        if (alphas[yy - 10 + y, xx - 10 + x] < 1f)
                            alphas[yy - 10 + y, xx - 10 + x] = 0.95f;
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

                for (var yy = 0; yy < 20; yy++)
                {
                    for (var xx = 0; xx < 20; xx++)
                    {
                        if (virtualHeights[yy - 10 + y, xx - 10 + x] < height / terrainData.size.y)
                            virtualHeights[yy - 10 + y, xx - 10 + x] = height / terrainData.size.y;
                        if (alphas[yy - 10 + y, xx - 10 + x] < 1f)
                            alphas[yy - 10 + y, xx - 10 + x] = 1f;
                    }
                }
            }
        }
    }


    public void ApplyTerrain()
    {
        TerrainModifier modifier = Object.FindObjectOfType<TerrainModifier>();
        modifier.heights = virtualHeights;
        modifier.alphas = alphas;

        modifier.ModifyTerrain();
    }
    

    public void ClearTerrain()
    {
        var terrainData = GetTerrainData();

        int r = terrainData.heightmapResolution;

        for (var y = 0; y < r; y++)
        {
            for (var x = 0; x < r; x++)
            {
                virtualHeights[y, x] = terrainOffset / terrainData.size.y;
                alphas[y, x] = 0f;
            }
        }

        terrainData.SetHeights(0, 0, virtualHeights);
    }

    public void FlattenTerrain(Vector3 worldPosition, float height, int brushWidth, int brushHeight)
    {
        var brushPosition = GetBrushPosition(worldPosition, brushWidth, brushHeight);

        var brushSize = GetSafeBrushSize(brushPosition.x, brushPosition.y, brushWidth, brushHeight);

        var terrainData = GetTerrainData();

        var heights = terrainData.GetHeights(brushPosition.x, brushPosition.y, brushSize.x, brushSize.y);

        for (var y = 0; y < brushSize.y; y++)
        {
            for (var x = 0; x < brushSize.x; x++)
            {
                float distance = Mathf.Sqrt(Mathf.Pow((float)x - (float)brushSize.x / 2f, 2f) +
                                            Mathf.Pow((float)y - (float)brushSize.y / 2f, 2f));
                if (distance > 0f) heights[y, x] = height;
            }
        }

        terrainData.SetHeights(brushPosition.x, brushPosition.y, heights);
    }

    public float SampleHeight(Vector3 worldPosition)
    {
        var terrainPosition = WorldToTerrainPosition(worldPosition);

        return GetTerrainData().GetInterpolatedHeight((int)terrainPosition.x, (int)terrainPosition.z);
    }

    public float SampleAverageHeight(Vector3 worldPosition, int brushWidth, int brushHeight)
    {
        var brushPosition = GetBrushPosition(worldPosition, brushWidth, brushHeight);

        var brushSize = GetSafeBrushSize(brushPosition.x, brushPosition.y, brushWidth, brushHeight);

        var heights2D = GetTerrainData().GetHeights(brushPosition.x, brushPosition.y, brushSize.x, brushSize.y);

        var heights = new float[heights2D.Length];

        var i = 0;

        for (int y = 0; y <= heights2D.GetUpperBound(0); y++)
        {
            for (int x = 0; x <= heights2D.GetUpperBound(1); x++)
            {
                heights[i++] = heights2D[y, x];
            }
        }

        return heights.Average();
    }
}