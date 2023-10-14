using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class VRPlayer : MonoBehaviour
{
    public GameObject leftController;
    public GameObject rightController;

    private Stroke stroke;
    private GameObject surface;

    private Vector3 lastStrokePosition;
    private bool filled;
    private float erosionStrength;

    public float leftBrushSize = 1f;
    public float rightBrushSize = 1f;

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

    public void OnStartDrawing(Vector3 position)
    {
        stroke = new GameObject("Stroke").AddComponent<Stroke>();
        stroke.CreateStroke(position, leftBrushSize, rightBrushSize, filled);
    }

    public void OnDrawing(Vector3 position)
    {
        if (stroke != null)
            stroke.DrawStroke(position);
    }

    public void OnFinishDrawing()
    {
        if (stroke == null) return;

        Vector3 position, derivative;
        float offset, brushSize;

        if (filled)
        {
            for (int i = 0; i < stroke.GetComponent<LineRenderer>().positionCount; i++)
            {
                position = stroke.GetComponent<Stroke>().GetPosition(i);

                if (i == 0) derivative = stroke.GetPosition(i + 1) - stroke.GetPosition(i);
                else if (i == stroke.GetComponent<LineRenderer>().positionCount - 1) derivative = stroke.GetPosition(i) - stroke.GetPosition(i - 1);
                else derivative = stroke.GetPosition(i + 1) - stroke.GetPosition(i - 1);

                offset = position.y - terrainTool._targetTerrain.transform.position.y;
                brushSize = Mathf.Abs(offset - terrainTool.terrainOffset) * 2f;

                if (i != 0)
                {
                    terrainTool.FillTerrain(new Vector3(position.x,
                                                        terrainTool._targetTerrain.transform.position.y,
                                                        position.z), stroke.GetPosition(0), Mathf.Abs(offset), 20, 20);
                }
            }
        }

        for (int i = 0; i < stroke.GetComponent<LineRenderer>().positionCount; i++)
        {
            position = stroke.GetPosition(i);

            if (i == 0) derivative = stroke.GetPosition(i + 1) - stroke.GetPosition(i);
            else if (i == stroke.GetComponent<LineRenderer>().positionCount - 1) derivative = stroke.GetPosition(i) - stroke.GetPosition(i - 1);
            else derivative = stroke.GetPosition(i + 1) - stroke.GetPosition(i - 1);

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
            position = stroke.GetPosition(i);

            if (i == 0) derivative = stroke.GetPosition(i + 1) - stroke.GetPosition(i);
            else if (i == stroke.GetComponent<LineRenderer>().positionCount - 1) derivative = stroke.GetPosition(i) - stroke.GetPosition(i - 1);
            else derivative = stroke.GetPosition(i + 1) - stroke.GetPosition(i - 1);

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

        terrainTool.ApplyTerrain();
    }

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

                OnStartDrawing(rightController.transform.position);
            }

            OnDrawing(rightController.transform.position);
        }
        else
        {
            if (rightTriggerButtonPressed_f)
            {
                rightTriggerButtonPressed_f = false;

                OnFinishDrawing();
            }
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
