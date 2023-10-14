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
    private List<Stroke> strokes;
    private GameObject surface;

    private Vector3 lastStrokePosition;
    private bool filled;
    private float erosionStrength;

    public float leftBrushSize = 1f;
    public float rightBrushSize = 1f;

    private int leftEditingIndex;
    private int rightEditingIndex;

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

    private bool editing;

    void OnStartDrawing(Vector3 position)
    {
        stroke = new GameObject("Stroke").AddComponent<Stroke>();
        stroke.CreateStroke(position, leftBrushSize, rightBrushSize, filled);
        strokes.Add(stroke);
    }

    void OnDrawing(Vector3 position)
    {
        if (stroke != null)
            stroke.DrawStroke(position);
    }

    void OnFinishingDrawing(Stroke s)
    {
        if (s == null) return;

        Vector3 position, derivative;
        float offset, brushSize;

        if (filled)
        {
            for (int i = 0; i < s.GetComponent<LineRenderer>().positionCount; i++)
            {
                position = s.GetComponent<Stroke>().GetPosition(i);

                if (i == 0) derivative = s.GetPosition(i + 1) - s.GetPosition(i);
                else if (i == s.GetComponent<LineRenderer>().positionCount - 1) derivative = s.GetPosition(i) - s.GetPosition(i - 1);
                else derivative = s.GetPosition(i + 1) - s.GetPosition(i - 1);

                offset = position.y - terrainTool._targetTerrain.transform.position.y;
                brushSize = Mathf.Abs(offset - terrainTool.terrainOffset) * 2f;

                if (i != 0)
                {
                    terrainTool.FillTerrain(new Vector3(position.x,
                                                        terrainTool._targetTerrain.transform.position.y,
                                                        position.z), s.GetPosition(0), Mathf.Abs(offset), 20, 20);
                }
            }
        }

        for (int i = 0; i < s.GetComponent<LineRenderer>().positionCount; i++)
        {
            position = s.GetPosition(i);

            if (i == 0) derivative = s.GetPosition(i + 1) - s.GetPosition(i);
            else if (i == s.GetComponent<LineRenderer>().positionCount - 1) derivative = s.GetPosition(i) - s.GetPosition(i - 1);
            else derivative = s.GetPosition(i + 1) - s.GetPosition(i - 1);

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

        for (int i = 0; i < s.GetComponent<LineRenderer>().positionCount; i++)
        {
            position = s.GetPosition(i);

            if (i == 0) derivative = s.GetPosition(i + 1) - s.GetPosition(i);
            else if (i == s.GetComponent<LineRenderer>().positionCount - 1) derivative = s.GetPosition(i) - s.GetPosition(i - 1);
            else derivative = s.GetPosition(i + 1) - s.GetPosition(i - 1);

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

    void OnSingleEditingStroke(Vector3 position, int controllerIndex)
    {
        if (stroke != null)
        {
            if (controllerIndex == 0)
                stroke.EditStroke(position, new Vector3(), leftEditingIndex, -1);
            else
                stroke.EditStroke(new Vector3(), position, -1, rightEditingIndex);
        }
    }

    void OnBothEditingStroke(Vector3 leftPosition, Vector3 rightPosition)
    {
        if (stroke != null)
            stroke.EditStroke(leftPosition, rightPosition, leftEditingIndex, rightEditingIndex);
    }

    public void OnFirstInput(Vector3 position, int controllerIndex)
    {
        if (editing)
        {
            if (controllerIndex == 0)
            {
                leftEditingIndex = stroke.LocateEditingIndex(position);
            }
            else
            {
                rightEditingIndex = stroke.LocateEditingIndex(position);
            }

            return;
        }

        foreach (Stroke s in strokes)
        {
            if (controllerIndex == 0)
            {
                leftEditingIndex = s.LocateEditingIndex(position);
                if (leftEditingIndex >= 0)
                {
                    stroke = s;
                    editing = true;
                    break;
                }
            }
            else
            {
                rightEditingIndex = s.LocateEditingIndex(position);
                if (rightEditingIndex >= 0)
                {
                    stroke = s;
                    editing = true;
                    break;
                }
            }
        }

        if (!editing) OnStartDrawing(position);
    }

    public void OnInput(Vector3 position, int controllerIndex)
    {
        if (editing) OnSingleEditingStroke(position, controllerIndex);
        else OnDrawing(position);
    }

    public void OnFinishingInput()
    {
        if (editing)
        {
            terrainTool.ClearTerrain();
            foreach (Stroke s in strokes)
                OnFinishingDrawing(s);
        }
        else
        {
            OnFinishingDrawing(stroke);
        }

        editing = false;
    }

    public void ClearInput()
    {
        if (terrainTool == null) return;

        terrainTool.ClearTerrain();

        strokes = new List<Stroke>();
        Stroke[] ss = FindObjectsOfType<Stroke>();
        foreach (Stroke stroke in ss) stroke.DestroyStroke();
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

        terrainTool = FindObjectOfType<TerrainTool>();

        userInput = new Texture2D(500, 500, TextureFormat.ARGB32, false);

        erosionStrength = 100f;

        strokes = new List<Stroke>();

        leftEditingIndex = -1;
        rightEditingIndex = -1;
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
            {
                leftTriggerButtonPressed_f = true;

                OnFirstInput(leftController.transform.position, 0);
            }

            if (rightTriggerButtonPressed_f)
            {
                OnBothEditingStroke(leftController.transform.position, rightController.transform.position);
            }
            else
            {
                OnInput(leftController.transform.position, 0);
            }
        }
        else
        {
            if (leftTriggerButtonPressed_f)
            {
                leftTriggerButtonPressed_f = false;

                if (!rightTriggerButtonPressed_f)
                    OnFinishingInput();
            }
        }

        if (rightTriggerButtonPressed)
        {
            if (!rightTriggerButtonPressed_f)
            {
                rightTriggerButtonPressed_f = true;

                OnFirstInput(rightController.transform.position, 1);
            }

            if (!leftTriggerButtonPressed_f)
                OnInput(rightController.transform.position, 1);
        }
        else
        {
            if (rightTriggerButtonPressed_f)
            {
                rightTriggerButtonPressed_f = false;

                if (!leftTriggerButtonPressed_f)
                    OnFinishingInput();
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

                ClearInput();
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
