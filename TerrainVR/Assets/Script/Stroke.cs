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

    private float leftBrushSize = 1f;
    private float rightBrushSize = 1f;
    private bool filled;

    private GameObject surface;

    public void CreateStroke(Vector3 position, float leftBrushSize, float rightBrushSize, bool filled)
    {
        positions = new List<Vector3>();
        strokeIndex = 0;

        this.lastStrokePosition = position;
        this.leftBrushSize = leftBrushSize;
        this.rightBrushSize = rightBrushSize;
        this.filled = filled;

        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;

        if (filled)
        {
            surface = new GameObject("Surface");
            surface.AddComponent<MeshFilter>();
            surface.AddComponent<MeshRenderer>();
            surface.GetComponent<MeshFilter>().mesh = new Mesh();
            surface.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Sprites/Default"));
        }
    }

    public void DrawStroke(Vector3 position)
    {
        if (strokeIndex == 0 || (strokeIndex != 0 && Vector3.Distance(lastStrokePosition, position) > 1f))
        {
            positions.Add(position);

            lineRenderer.positionCount = strokeIndex + 1;
            lineRenderer.SetPosition(strokeIndex, position);
            strokeIndex++;

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

    public int LocateEditingIndex(Vector3 position)
    {
        if (Vector3.Distance(position, positions[0]) < 20f) return 0;
        if (Vector3.Distance(position, positions[positions.Count - 1]) < 20f) return positions.Count - 1;

        for (int i = 0; i < positions.Count; i++)
            if (Vector3.Distance(position, positions[i]) < 5f)
                return i;

        return -1;
    }

    public void EditStroke(Vector3 leftPosition, Vector3 rightPosition, int leftIndex, int rightIndex)
    {
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

    public void DestroyStroke()
    {
        if (surface != null) Destroy(surface);
        Destroy(gameObject);
    }

    public Vector3 GetPosition(int i)
    {
        return positions[i];
    }
}
