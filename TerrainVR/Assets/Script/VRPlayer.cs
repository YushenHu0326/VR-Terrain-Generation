using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class VRPlayer : MonoBehaviour
{
    public GameObject leftController;
    public GameObject rightController;

    private GameObject stroke;
    private GameObject surface;
    private int strokeIndex;

    private List<Vector3> positions;
    private Vector3 lastStrokePosition;
    private bool filled;
    private float erosionStrength;

    public float leftBrushSize = 1f;
    public float rightBrushSize = 1f;

    private float xMin, xMax, yMin, yMax, radius;

    Texture2D userInput;

    private bool leftPrimaryButtonPressed_f,
                 rightPrimaryButtonPressed_f,
                 leftSecondaryButtonPressed_f,
                 rightSecondaryButtonPressed_f,
                 leftTriggerButtonPressed_f,
                 rightTriggerButtonPressed_f;

    private InputDevice leftDevice;
    private InputDevice rightDevice;

    private TerrainTool terrainTool;

    // Start is called before the first frame update
    void Start()
    {
        List<InputDevice> leftDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, leftDevices);

        if (leftDevices.Count > 0) leftDevice = leftDevices[0];

        List<InputDevice> rightDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rightDevices);

        if (rightDevices.Count > 0) rightDevice = rightDevices[0];

        terrainTool = GetComponent<TerrainTool>();

        userInput = new Texture2D(500, 500, TextureFormat.ARGB32, false);

        positions = new List<Vector3>();

        xMin = 10000f;
        xMax = -10000f;
        yMin = 10000f;
        yMax = -10000f;
        radius = 0f;

        erosionStrength = 100f;
    }

    // Update is called once per frame
    void Update()
    {
        leftDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool leftPrimaryButtonPressed);
        rightDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool rightPrimaryButtonPressed);

        leftDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out bool leftSecondaryButtonPressed);
        rightDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out bool rightSecondaryButtonPressed);

        leftDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool leftTriggerButtonPressed);
        rightDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool rightTriggerButtonPressed);

        if (leftTriggerButtonPressed)
        {
            if (!leftTriggerButtonPressed_f)
                leftTriggerButtonPressed_f = true;

            erosionStrength -= 1f;
            if (erosionStrength < 0f) erosionStrength = 100f;
            erosionStrength = Mathf.Clamp(erosionStrength, 0f, 100f);

            Debug.Log(erosionStrength);
        }
        else
        {
            if (leftTriggerButtonPressed_f)
            {
                leftTriggerButtonPressed_f = false;
            }
        }

        if (rightTriggerButtonPressed)
        {
            if (!rightTriggerButtonPressed_f)
            {
                rightTriggerButtonPressed_f = true;

                lastStrokePosition = rightController.transform.position;
                stroke = new GameObject("Stroke");
                stroke.AddComponent<LineRenderer>();
                stroke.GetComponent<LineRenderer>().material = new Material(Shader.Find("Sprites/Default"));

                stroke.GetComponent<LineRenderer>().startColor = Color.red;
                stroke.GetComponent<LineRenderer>().endColor = Color.red;

                if (filled)
                {
                    surface = new GameObject("Surface");
                    surface.AddComponent<MeshFilter>();
                    surface.AddComponent<MeshRenderer>();
                    surface.GetComponent<MeshFilter>().mesh = new Mesh();
                    surface.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Sprites/Default"));
                }
            }

            positions.Add(rightController.transform.position);

            float offset = rightController.transform.position.y - terrainTool._targetTerrain.transform.position.y;

            float brushSize = Mathf.Abs(offset - terrainTool.terrainOffset) * 2f;
            brushSize *= Mathf.Max(leftBrushSize, rightBrushSize);

            if (brushSize > radius) radius = brushSize;

            if (rightController.transform.position.x < xMin) xMin = rightController.transform.position.x;
            if (rightController.transform.position.x > xMax) xMax = rightController.transform.position.x;
            if (rightController.transform.position.z < yMin) yMin = rightController.transform.position.z;
            if (rightController.transform.position.z > yMax) yMax = rightController.transform.position.z;

            if (Vector3.Distance(lastStrokePosition, rightController.transform.position) > 1f)
            {
                stroke.GetComponent<LineRenderer>().positionCount = strokeIndex + 1;
                stroke.GetComponent<LineRenderer>().SetPosition(strokeIndex, rightController.transform.position);
                strokeIndex++;

                if (surface != null)
                {
                    Vector3[] vertices = new Vector3[surface.GetComponent<MeshFilter>().mesh.vertices.Length + 3];
                    int[] triangles = new int[surface.GetComponent<MeshFilter>().mesh.triangles.Length + 3];

                    Array.Copy(surface.GetComponent<MeshFilter>().mesh.vertices, vertices, surface.GetComponent<MeshFilter>().mesh.vertices.Length);
                    Array.Copy(surface.GetComponent<MeshFilter>().mesh.triangles, triangles, surface.GetComponent<MeshFilter>().mesh.triangles.Length);

                    vertices[surface.GetComponent<MeshFilter>().mesh.vertices.Length] = positions[0];
                    vertices[surface.GetComponent<MeshFilter>().mesh.vertices.Length + 1] = lastStrokePosition;
                    vertices[surface.GetComponent<MeshFilter>().mesh.vertices.Length + 2] = rightController.transform.position;
                    surface.GetComponent<MeshFilter>().mesh.vertices = vertices;

                    triangles[surface.GetComponent<MeshFilter>().mesh.triangles.Length] = surface.GetComponent<MeshFilter>().mesh.triangles.Length;
                    triangles[surface.GetComponent<MeshFilter>().mesh.triangles.Length + 1] = surface.GetComponent<MeshFilter>().mesh.triangles.Length + 1;
                    triangles[surface.GetComponent<MeshFilter>().mesh.triangles.Length + 2] = surface.GetComponent<MeshFilter>().mesh.triangles.Length + 2;
                    surface.GetComponent<MeshFilter>().mesh.triangles = triangles;
                }

                lastStrokePosition = rightController.transform.position;
            }
        }
        else
        {
            if (rightTriggerButtonPressed_f)
            {
                rightTriggerButtonPressed_f = false;

                Vector3 position, derivative;
                float offset, brushSize;

                if (filled)
                {
                    for (int i = 0; i < stroke.GetComponent<LineRenderer>().positionCount; i++)
                    {
                        position = positions[i];

                        if (i == 0) derivative = positions[i + 1] - positions[i];
                        else if (i == stroke.GetComponent<LineRenderer>().positionCount - 1) derivative = positions[i] - positions[i - 1];
                        else derivative = positions[i + 1] - positions[i - 1];

                        offset = position.y - terrainTool._targetTerrain.transform.position.y;
                        brushSize = Mathf.Abs(offset - terrainTool.terrainOffset) * 2f;

                        if (i != 0)
                        {
                            terrainTool.FillTerrain(new Vector3(position.x,
                                                                terrainTool._targetTerrain.transform.position.y,
                                                                position.z), positions[0], Mathf.Abs(offset), 20, 20);
                        }
                    }
                }

                for (int i = 0; i < stroke.GetComponent<LineRenderer>().positionCount; i++)
                {
                    position = positions[i];

                    if (i == 0) derivative = positions[i + 1] - positions[i];
                    else if (i == stroke.GetComponent<LineRenderer>().positionCount - 1) derivative = positions[i] - positions[i - 1];
                    else derivative = positions[i + 1] - positions[i - 1];

                    offset = position.y - terrainTool._targetTerrain.transform.position.y;
                    brushSize = Mathf.Abs(offset - terrainTool.terrainOffset) * 2f;

                    if (filled)
                    {
                        terrainTool.RaiseTerrain(new Vector3(position.x,
                                                             terrainTool._targetTerrain.transform.position.y,
                                                             position.z),
                                                             Mathf.Abs(offset) / 2f, brushSize / 2f, 
                                                             Mathf.Max(leftBrushSize, rightBrushSize), 
                                                             Mathf.Max(leftBrushSize, rightBrushSize),
                                                             derivative);
                    }
                    else
                    {
                        terrainTool.RaiseTerrain(new Vector3(position.x,
                                                             terrainTool._targetTerrain.transform.position.y,
                                                             position.z),
                                                             Mathf.Abs(offset), brushSize, leftBrushSize, rightBrushSize,
                                                             derivative);
                    }
                }

                for (int i = 0; i < stroke.GetComponent<LineRenderer>().positionCount; i++)
                {
                    position = positions[i];

                    if (i == 0) derivative = positions[i + 1] - positions[i];
                    else if (i == stroke.GetComponent<LineRenderer>().positionCount - 1) derivative = positions[i] - positions[i - 1];
                    else derivative = positions[i + 1] - positions[i - 1];

                    offset = position.y - terrainTool._targetTerrain.transform.position.y;
                    brushSize = Mathf.Abs(offset - terrainTool.terrainOffset) * 2f;

                    if (filled)
                    {
                        terrainTool.PaintStroke(new Vector3(position.x,
                                                            terrainTool._targetTerrain.transform.position.y,
                                                            position.z),
                                                            Mathf.Abs(offset), brushSize, leftBrushSize, rightBrushSize,
                                                            derivative, 0.6f, 6);
                    }
                    else
                    {
                        terrainTool.PaintStroke(new Vector3(position.x,
                                                            terrainTool._targetTerrain.transform.position.y,
                                                            position.z),
                                                            Mathf.Abs(offset), brushSize, leftBrushSize, rightBrushSize,
                                                            derivative, 1f, 2);
                    }
                }

                terrainTool.ApplyTerrain(xMin, xMax, yMin, yMax, radius);

                //Destroy(stroke);
                positions.Clear();

                if (surface != null) Destroy(surface);

                xMin = 10000f;
                xMax = -10000f;
                yMin = 10000f;
                yMax = -10000f;
                radius = 0f;
            }

            strokeIndex = 0;
        }

        if (leftPrimaryButtonPressed)
        {
            if (!leftPrimaryButtonPressed_f)
            {
                leftPrimaryButtonPressed_f = true;
            }
        }
        else
        {
            if (leftPrimaryButtonPressed_f)
                leftPrimaryButtonPressed_f = false;
        }

        if (leftSecondaryButtonPressed)
        {
            if (!leftSecondaryButtonPressed_f)
            {
                leftSecondaryButtonPressed_f = true;
            }
        }
        else
        {
            if (leftSecondaryButtonPressed_f)
                leftSecondaryButtonPressed_f = false;
        }

        if (rightPrimaryButtonPressed)
        {
            if (!rightPrimaryButtonPressed_f)
            {
                rightPrimaryButtonPressed_f = true;
                terrainTool.ClearTerrain();
            }
        }
        else
        {
            if (rightPrimaryButtonPressed_f)
                rightPrimaryButtonPressed_f = false;
        }

        if (rightSecondaryButtonPressed)
        {
            if (!rightSecondaryButtonPressed_f)
            {
                rightSecondaryButtonPressed_f = true;

                filled = !filled;
            }
            /*
            byte[] bytes = userInput.EncodeToPNG();

            System.IO.File.WriteAllBytes(Application.dataPath + "/i.png", bytes);
            */
        }
        else
        {
            if (rightSecondaryButtonPressed_f)
                rightSecondaryButtonPressed_f = false;
        }
    }
}
