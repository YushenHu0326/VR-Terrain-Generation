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

    private float xMin, xMax, yMin, yMax;

    private GameObject surface;

    public void CreateStroke(Vector3 position, float leftBrushSize, float rightBrushSize, bool filled)
    {
        positions = new List<Vector3>();
        strokeIndex = 0;

        this.lastStrokePosition = position;
        this.leftBrushSize = leftBrushSize;
        this.rightBrushSize = rightBrushSize;
        this.filled = filled;

        this.xMin = position.x;
        this.xMax = position.x;
        this.yMin = position.z;
        this.yMax = position.z;

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

            if (position.x < xMin) xMin = position.x;
            if (position.x > xMax) xMax = position.x;
            if (position.z < yMin) yMin = position.z;
            if (position.z > yMax) yMax = position.z;
        }
    }

    public bool ControllerInRange(Vector3 position)
    {
        if (position.x > xMin && position.x < xMax)
            if (position.z > yMin && position.z < yMax)
                return true;

        return false;
    }

    public int LocateEditingIndex(Vector3 position)
    {
        for (int i = 0; i < positions.Count; i++)
            if (Vector3.Distance(position, positions[i]) < 5f)
                return i;

        return -1;
    }

    public void EditStroke(Vector3 firstPosition, Vector3 secondPosition, float brushSize, int firstIndex, int secondIndex)
    {
        
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
