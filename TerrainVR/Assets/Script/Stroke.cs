using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stroke : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private int strokeIndex;

    private List<Vector3> positions;
    private Vector3 lastStrokePosition;

    private float[,] height;

    private List<Vector3> derivatives;

    private float leftBrushSize = 1f;
    private float rightBrushSize = 1f;
    private float leftBrushCurve = 1f;
    private float rightBrushCurve = 1f;
    public bool filled;
    public bool activated;

    private float xMin, xMax, yMin, yMax, zMax, zMin;

    private GameObject surface;
    private GameObject slopeVisualCue;
    private int slopeIndex;

    private float terrainOffset;

    private Terrain terrain;

    public void CreateStroke(Material mat, Vector3 position, float leftBrushSize, float rightBrushSize, bool filled)
    {
        positions = new List<Vector3>();
        strokeIndex = 0;

        this.lastStrokePosition = position;
        this.leftBrushSize = leftBrushSize;
        this.rightBrushSize = rightBrushSize;
        this.filled = filled;

        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = mat;

        lineRenderer.material.SetColor("_OutlineColor", Color.red);

        xMin = position.x;
        xMax = position.x;
        yMin = position.y;
        yMax = position.y;
        zMin = position.z;
        zMax = position.z;

        if (filled)
        {
            surface = new GameObject("Surface");
            surface.AddComponent<MeshFilter>();
            surface.AddComponent<MeshRenderer>();
            surface.GetComponent<MeshFilter>().mesh = new Mesh();
            surface.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Sprites/Default"));
        }

        terrain = FindObjectOfType<Terrain>();
        TerrainTool terrainTool = FindObjectOfType<TerrainTool>();
        if (terrainTool != null)
        { 
            terrainOffset = terrainTool.terrainOffset;
            height = new float[terrainTool.GetHeightmapResolution(), terrainTool.GetHeightmapResolution()];
        }

        activated = true;
    }

    public void DrawStroke(Vector3 position)
    {
        if (strokeIndex == 0 || (strokeIndex != 0 && Vector3.Distance(lastStrokePosition, position) > 1f))
        {
            positions.Add(position);

            lineRenderer.positionCount = strokeIndex + 1;
            lineRenderer.SetPosition(strokeIndex, position);
            strokeIndex++;

            if (position.x < xMin) xMin = position.x;
            if (position.x > xMax) xMax = position.x;
            if (position.y < yMin) yMin = position.y;
            if (position.y > yMax) yMax = position.y;
            if (position.z < zMin) zMin = position.z;
            if (position.z > zMax) zMax = position.z;

            if (filled)
            {
                Vector3[] vertices = new Vector3[surface.GetComponent<MeshFilter>().mesh.vertices.Length + 3];
                int[] triangles = new int[surface.GetComponent<MeshFilter>().mesh.triangles.Length + 3];

                Array.Copy(surface.GetComponent<MeshFilter>().mesh.vertices, vertices, surface.GetComponent<MeshFilter>().mesh.vertices.Length);
                Array.Copy(surface.GetComponent<MeshFilter>().mesh.triangles, triangles, surface.GetComponent<MeshFilter>().mesh.triangles.Length);

                vertices[surface.GetComponent<MeshFilter>().mesh.vertices.Length] = positions[0];
                vertices[surface.GetComponent<MeshFilter>().mesh.vertices.Length + 1] = lastStrokePosition;
                vertices[surface.GetComponent<MeshFilter>().mesh.vertices.Length + 2] = position;
                surface.GetComponent<MeshFilter>().mesh.vertices = vertices;

                triangles[surface.GetComponent<MeshFilter>().mesh.triangles.Length] = surface.GetComponent<MeshFilter>().mesh.triangles.Length;
                triangles[surface.GetComponent<MeshFilter>().mesh.triangles.Length + 1] = surface.GetComponent<MeshFilter>().mesh.triangles.Length + 1;
                triangles[surface.GetComponent<MeshFilter>().mesh.triangles.Length + 2] = surface.GetComponent<MeshFilter>().mesh.triangles.Length + 2;
                surface.GetComponent<MeshFilter>().mesh.triangles = triangles;
            }

            lastStrokePosition = position;
        }
    }

    public void FinishStroke()
    {
        derivatives = new List<Vector3>();

        Vector3 derivative;

        for (int i = 0; i < GetComponent<LineRenderer>().positionCount; i++)
        {
            if (i == 0) derivative = GetPosition(i + 1) - GetPosition(i);
            else if (i == GetComponent<LineRenderer>().positionCount - 1) derivative = GetPosition(i) - GetPosition(i - 1);
            else derivative = GetPosition(i + 1) - GetPosition(i - 1);

            derivatives.Add(derivative.normalized);
        }
    }

    public int LocateEditingIndex(Vector3 position)
    {
        if (Vector3.Distance(position, positions[0]) < 20f) return 0;
        if (Vector3.Distance(position, positions[positions.Count - 1]) < 20f) return positions.Count - 1;

        for (int i = 0; i < positions.Count; i++)
            if (Vector3.Distance(position, positions[i]) < 10f)
                return i;

        return -1;
    }

    public void OnStartEditing(int index)
    {
        if (index < 0 || index >= positions.Count) return;

        slopeVisualCue = new GameObject("VisualCue");
        slopeVisualCue.AddComponent<LineRenderer>();
        slopeVisualCue.GetComponent<LineRenderer>().material = lineRenderer.material;
        slopeVisualCue.GetComponent<LineRenderer>().material.SetColor("_OutlineColor", Color.blue);

        slopeVisualCue.GetComponent<LineRenderer>().positionCount = 5;

        slopeIndex = index;
    }

    public void OnFinishEditing()
    {
        Destroy(slopeVisualCue);
    }

    public void EditStroke(Vector3 leftPosition, Vector3 rightPosition, int leftIndex, int rightIndex)
    {
        if (terrain == null) return;

        if (leftIndex < 0 && rightIndex < 0) return;

        slopeVisualCue.GetComponent<LineRenderer>().SetPosition(2, positions[slopeIndex]);

        Vector3 derivative;
        if (slopeIndex == 0) derivative = positions[slopeIndex + 1] - positions[slopeIndex];
        else if (slopeIndex == positions.Count - 1) derivative = positions[slopeIndex] - positions[slopeIndex - 1];
        else derivative = positions[slopeIndex + 1] - positions[slopeIndex - 1];
        derivative.y = 0f;

        Vector3 leftEnd = positions[slopeIndex] + (Quaternion.AngleAxis(-90, Vector3.up) * derivative.normalized) *
                          Mathf.Abs(positions[slopeIndex].y - (terrain.gameObject.transform.position.y + terrainOffset)) * leftBrushSize;
        Vector3 rightEnd = positions[slopeIndex] + (Quaternion.AngleAxis(90, Vector3.up) * derivative.normalized) *
                          Mathf.Abs(positions[slopeIndex].y - (terrain.gameObject.transform.position.y + terrainOffset)) * rightBrushSize;

        leftEnd.y = terrain.gameObject.transform.position.y + terrainOffset;
        rightEnd.y = terrain.gameObject.transform.position.y + terrainOffset;

        Vector3 leftMid = (positions[slopeIndex] + leftEnd) / 2f;
        Vector3 rightMid = (positions[slopeIndex] + rightEnd) / 2f;

        leftMid.y = leftEnd.y + Mathf.Pow(0.5f, leftBrushCurve) * (positions[slopeIndex].y - leftEnd.y);
        rightMid.y = rightEnd.y + Mathf.Pow(0.5f, rightBrushCurve) * (positions[slopeIndex].y - rightEnd.y);

        slopeVisualCue.GetComponent<LineRenderer>().SetPosition(0, leftEnd);
        slopeVisualCue.GetComponent<LineRenderer>().SetPosition(1, leftMid);
        slopeVisualCue.GetComponent<LineRenderer>().SetPosition(3, rightMid);
        slopeVisualCue.GetComponent<LineRenderer>().SetPosition(4, rightEnd);

        if (leftIndex <= -100)
        {
            if (leftIndex == -100)
            {
                if (Vector3.Distance(leftPosition, leftEnd) < 20f)
                {
                    Vector3 d = leftPosition - positions[slopeIndex];
                    d.y = 0f;
                    leftBrushSize = d.magnitude / Mathf.Abs(positions[slopeIndex].y - leftPosition.y);
                    leftBrushSize = Mathf.Clamp(leftBrushSize, 0.2f, 2f);
                }
                else
                {
                    if (leftEnd.y < positions[slopeIndex].y)
                    {
                        if (leftPosition.y > leftEnd.y && leftPosition.y < positions[slopeIndex].y)
                        {
                            leftBrushCurve = (positions[slopeIndex].y - leftPosition.y) / (positions[slopeIndex].y - leftEnd.y) * 2f;
                        }
                    }
                    else
                    {
                        if (leftPosition.y < leftEnd.y && leftPosition.y > positions[slopeIndex].y)
                        {
                            leftBrushCurve = (leftEnd.y - leftPosition.y) / (leftEnd.y - positions[slopeIndex].y) * 2f;
                        }
                    }
                }
            }
            else if (leftIndex == -101)
            {
                if (Vector3.Distance(leftPosition, rightEnd) < 20f)
                {
                    Vector3 d = leftPosition - positions[slopeIndex];
                    d.y = 0f;
                    rightBrushSize = d.magnitude / Mathf.Abs(positions[slopeIndex].y - leftPosition.y);
                    rightBrushSize = Mathf.Clamp(rightBrushSize, 0.2f, 2f);
                }
                else
                {
                    if (rightEnd.y < positions[slopeIndex].y)
                    {
                        if (leftPosition.y > rightEnd.y && leftPosition.y < positions[slopeIndex].y)
                        {
                            rightBrushCurve = (positions[slopeIndex].y - leftPosition.y) / (positions[slopeIndex].y - rightEnd.y) * 2f;
                        }
                    }
                    else
                    {
                        if (leftPosition.y < rightEnd.y && leftPosition.y > positions[slopeIndex].y)
                        {
                            rightBrushCurve = (rightEnd.y - leftPosition.y) / (rightEnd.y - positions[slopeIndex].y) * 2f;
                        }
                    }
                }
            }
        }

        if (rightIndex <= -100)
        {
            if (rightIndex == -100)
            {
                if (Vector3.Distance(rightPosition, leftEnd) < 40f)
                {
                    Vector3 d = rightPosition - positions[slopeIndex];
                    d.y = 0f;
                    leftBrushSize = d.magnitude / Mathf.Abs(positions[slopeIndex].y - leftPosition.y);
                    leftBrushSize = Mathf.Clamp(leftBrushSize, 0.2f, 2f);
                }
                else
                {
                    if (leftEnd.y < positions[slopeIndex].y)
                    {
                        if (rightPosition.y > leftEnd.y && rightPosition.y < positions[slopeIndex].y)
                        {
                            leftBrushCurve = (positions[slopeIndex].y - rightPosition.y) / (positions[slopeIndex].y - leftEnd.y) * 2f;
                        }
                    }
                    else
                    {
                        if (rightPosition.y < leftEnd.y && rightPosition.y > positions[slopeIndex].y)
                        {
                            leftBrushCurve = (leftEnd.y - rightPosition.y) / (leftEnd.y - positions[slopeIndex].y) * 2f;
                        }
                    }
                }
            }
            else if (rightIndex == -101)
            {
                if (Vector3.Distance(rightPosition, rightEnd) < 40f)
                {
                    Vector3 d = rightPosition - positions[slopeIndex];
                    d.y = 0f;
                    rightBrushSize = d.magnitude / Mathf.Abs(positions[slopeIndex].y - leftPosition.y);
                    rightBrushSize = Mathf.Clamp(rightBrushSize, 0.2f, 2f);
                }
                else
                {
                    if (rightEnd.y < positions[slopeIndex].y)
                    {
                        if (rightPosition.y > rightEnd.y && rightPosition.y < positions[slopeIndex].y)
                        {
                            rightBrushCurve = (positions[slopeIndex].y - rightPosition.y) / (positions[slopeIndex].y - rightEnd.y) * 2f;
                        }
                    }
                    else
                    {
                        if (rightPosition.y < rightEnd.y && rightPosition.y > positions[slopeIndex].y)
                        {
                            rightBrushCurve = (rightEnd.y - rightPosition.y) / (rightEnd.y - positions[slopeIndex].y) * 2f;
                        }
                    }
                }
            }

            return;
        }

        if (rightIndex < 0)
        {
            Vector3 d = leftPosition - positions[leftIndex];
            int start = leftIndex;

            if (start < positions.Count)
            {
                for (int i = start; i < positions.Count; i++)
                {
                    positions[i] += d * (1f - (float)(i - start) / (float)(positions.Count - start));
                    lineRenderer.SetPosition(i, positions[i]);
                }
            }

            start -= 1;

            if (start > 0)
            {
                for (int i = start; i >= 0; i--)
                {
                    positions[i] += d * (float)i / (float)start;
                    lineRenderer.SetPosition(i, positions[i]);
                }
            }
        }
        else if (leftIndex < 0)
        {
            Vector3 d = rightPosition - positions[rightIndex];
            int start = rightIndex;

            if (start < positions.Count)
            {
                for (int i = start; i < positions.Count; i++)
                {
                    positions[i] += d * (1f - (float)(i - start) / (float)(positions.Count - start));
                    lineRenderer.SetPosition(i, positions[i]);
                }
            }

            start -= 1;

            if (start > 0)
            {
                for (int i = start; i >= 0; i--)
                {
                    positions[i] += d * (float)i / (float)start;
                    lineRenderer.SetPosition(i, positions[i]);
                }
            }
        }
        else if (leftIndex >= 0 && rightIndex >= 0)
        {
            Vector3 d1, d2;

            int start = Mathf.Min(leftIndex, rightIndex);
            int end = Mathf.Max(leftIndex, rightIndex);

            Vector3 anchorStart = positions[start];
            Vector3 anchorEnd = positions[end];
            Vector3 newAnchor;

            if (leftIndex < rightIndex)
            {
                d1 = leftPosition - positions[leftIndex];
                d2 = rightPosition - positions[rightIndex];

                newAnchor = leftPosition;
            }
            else
            {
                d2 = leftPosition - positions[leftIndex];
                d1 = rightPosition - positions[rightIndex];

                newAnchor = rightPosition;
            }

            if (start - 1 > 0)
            {
                for (int i = start - 1; i >= 0; i--)
                {
                    positions[i] += d1 * (float)i / (float)start;
                    lineRenderer.SetPosition(i, positions[i]);
                }
            }

            if (end + 1 < positions.Count)
            {
                for (int i = end + 1; i < positions.Count; i++)
                {
                    positions[i] += d2 * (1f - (float)(i - end) / (float)(positions.Count - end));
                    lineRenderer.SetPosition(i, positions[i]);
                }
            }

            Quaternion rotator = Quaternion.FromToRotation(anchorStart - anchorEnd, leftPosition - rightPosition);
            if (leftIndex > rightIndex) rotator = Quaternion.FromToRotation(anchorStart - anchorEnd, rightPosition - leftPosition);

            float scalar = Vector3.Distance(leftPosition, rightPosition) / Vector3.Distance(anchorStart, anchorEnd);
            Vector3 originalVector;
            
            if (start < positions.Count)
            {
                for (int i = start; i <= end; i++)
                {
                    originalVector = positions[i] - anchorStart;
                    positions[i] = newAnchor + (rotator * (originalVector * scalar));
                    lineRenderer.SetPosition(i, positions[i]);
                }
            }
        }
    }

    public void SetHeight(int x, int y, float h)
    {
        if (height != null)
            height[x, y] = h;
    }

    public float GetHeight(int x, int y)
    {
        if (height != null)
            return height[x, y];

        return 0f;
    }

    public float Volume()
    {
        return (xMax - xMin) * (yMax - yMin) * (zMax - zMin);
    }

    public void HideStroke()
    {
        if (surface != null) Destroy(surface);
        lineRenderer.enabled = false;
        activated = false;
    }

    public void DestroyStroke()
    {
        if (surface != null) Destroy(surface);
        Destroy(gameObject);
    }

    public Vector3 GetPosition(int i)
    {
        return positions[i];
    }

    public Vector3 GetDerivative(int i)
    {
        return derivatives[i];
    }

    public bool GetIsStroke(int i)
    {
        if (Mathf.Abs(derivatives[i].y) < 0.2f) return true;
        if ((i > 0 && i < derivatives.Count - 1) && derivatives[i - 1].y * derivatives[i + 1].y < 0f) return true;

        return false;
    }

    public float GetLeftBrushSize()
    {
        return leftBrushSize;
    }

    public float GetRightBrushSize()
    {
        return rightBrushSize;
    }

    public float GetLeftBrushCurve()
    {
        return Mathf.Clamp(leftBrushCurve, 0.2f, 2f);
    }

    public float GetRightBrushCurve()
    {
        return Mathf.Clamp(rightBrushCurve, 0.2f, 2f);
    }

    public int GetPositionCount()
    {
        return positions.Count;
    }
}
