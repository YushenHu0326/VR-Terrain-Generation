using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class VRPlayer : MonoBehaviour
{
    private Stroke stroke;
    private List<Stroke> strokes;
    private GameObject surface;

    private Vector3 lastStrokePosition;
    private bool filled;
    private float erosionStrength;

    public float leftBrushSize = 1f;
    public float rightBrushSize = 1f;

    public Material strokeMat;

    private int leftEditingIndex;
    private int rightEditingIndex;

    private int activeHand;

    Texture2D userInput;

    private bool leftHandPinchBegin, rightHandPinchBegin;

    private GestureDetector gestureDetector;

    private TerrainTool terrainTool;

    private bool editing;
    private bool visualized;

    void OnStartDrawing(Vector3 position)
    {
        stroke = new GameObject("Stroke").AddComponent<Stroke>();
        stroke.CreateStroke(strokeMat, position, leftBrushSize, rightBrushSize, filled);
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

        if (s.filled)
        {
            for (int i = 0; i < s.GetPositionCount(); i++)
            {
                position = s.GetPosition(i);

                offset = position.y - terrainTool._targetTerrain.transform.position.y;
                brushSize = Mathf.Abs(offset - terrainTool.terrainOffset) * 2f;

                if (i != 0)
                {
                    terrainTool.FillTerrain(position, s.GetPosition(0), Mathf.Abs(offset), 20, 20);
                }
            }
        }

        for (int i = 0; i < s.GetPositionCount(); i++)
        {
            position = s.GetPosition(i);

            offset = position.y - terrainTool._targetTerrain.transform.position.y;
            brushSize = Mathf.Abs(offset - terrainTool.terrainOffset) * 2f;

            int startIndex = Mathf.Max(0, i - 20);
            int endIndex = Mathf.Min(s.GetPositionCount() - 1, i + 20);

            if (s.filled)
            {
                terrainTool.RaiseTerrain(position,
                                         Mathf.Abs(offset), brushSize,
                                         s.GetLeftBrushSize(), s.GetRightBrushSize(),
                                         s.GetDerivative(i), s.GetPosition(startIndex), s.GetPosition(endIndex), s.GetPosition(0), s.GetPosition(s.GetPositionCount() - 1));
            }
            else
            {
                terrainTool.RaiseTerrain(position,
                                         Mathf.Abs(offset), brushSize, s.GetLeftBrushSize(), s.GetRightBrushSize(),
                                         s.GetDerivative(i), s.GetPosition(startIndex), s.GetPosition(endIndex), s.GetPosition(0), s.GetPosition(s.GetPositionCount() - 1));
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
                Vector3 derivative;
                if (rightEditingIndex == 0) derivative = stroke.GetPosition(rightEditingIndex + 1) - stroke.GetPosition(rightEditingIndex);
                else if (rightEditingIndex == stroke.GetPositionCount() - 1) derivative = stroke.GetPosition(rightEditingIndex) - stroke.GetPosition(rightEditingIndex - 1);
                else derivative = stroke.GetPosition(rightEditingIndex + 1) - stroke.GetPosition(rightEditingIndex - 1);
                derivative.y = 0f;

                float angle  = Vector3.Angle(derivative.normalized, (stroke.GetPosition(rightEditingIndex) - position).normalized) *
                                             Mathf.Sign(derivative.normalized.x * (stroke.GetPosition(rightEditingIndex) - position).normalized.z 
                                             - derivative.normalized.z * (stroke.GetPosition(rightEditingIndex) - position).normalized.x);

                angle /= 180f;

                if (angle > -0.55f && angle < -0.45f)
                    leftEditingIndex = -100;
                else if (angle < 0.55f && angle > 0.45f)
                    leftEditingIndex = -101;
                else
                    leftEditingIndex = stroke.LocateEditingIndex(position);
            }
            else
            {
                Vector3 derivative;
                if (leftEditingIndex == 0) derivative = stroke.GetPosition(leftEditingIndex + 1) - stroke.GetPosition(leftEditingIndex);
                else if (leftEditingIndex == stroke.GetPositionCount() - 1) derivative = stroke.GetPosition(leftEditingIndex) - stroke.GetPosition(leftEditingIndex - 1);
                else derivative = stroke.GetPosition(leftEditingIndex + 1) - stroke.GetPosition(leftEditingIndex - 1);

                float angle = Vector3.Angle(derivative.normalized, (stroke.GetPosition(leftEditingIndex) - position).normalized) *
                                             Mathf.Sign(derivative.normalized.x * (stroke.GetPosition(leftEditingIndex) - position).normalized.z
                                             - derivative.normalized.z * (stroke.GetPosition(leftEditingIndex) - position).normalized.x);

                angle /= 180f;

                if ((angle > -0.55f && angle < -0.45f) && position.y < stroke.GetPosition(leftEditingIndex).y)
                    rightEditingIndex = -100;
                else if ((angle < 0.55f && angle > 0.45f) && position.y < stroke.GetPosition(leftEditingIndex).y)
                    rightEditingIndex = -101;
                else
                    rightEditingIndex = stroke.LocateEditingIndex(position);
            }

            return;
        }

        foreach (Stroke s in strokes)
        {
            if (s == null) continue;
            if (!s.activated) continue;
            if (controllerIndex == 0)
            {
                leftEditingIndex = s.LocateEditingIndex(position);
                if (leftEditingIndex >= 0)
                {
                    stroke = s;
                    editing = true;
                    stroke.OnStartEditing(leftEditingIndex);
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
                    stroke.OnStartEditing(rightEditingIndex);
                    break;
                }
            }
        }

        if (!editing && !(leftHandPinchBegin && rightHandPinchBegin))
        {
            activeHand = controllerIndex;
            OnStartDrawing(position);
        }
    }

    public void OnSingleInput(Vector3 position, int controllerIndex)
    {
        if (editing) OnSingleEditingStroke(position, controllerIndex);
        else OnDrawing(position);
    }

    public void OnBothInput(Vector3 leftPosition, Vector3 rightPosition)
    {
        if (editing)
        {
            OnBothEditingStroke(leftPosition, rightPosition);
        }
        else
        {
            if (activeHand == 0)
                OnDrawing(leftPosition);
            else
                OnDrawing(rightPosition);
        }
    }

    public void OnFinishingInput(int controllerIndex)
    {
        if (editing)
        {
            stroke.OnFinishEditing();
            terrainTool.ClearTerrain();
            foreach (Stroke s in strokes)
                OnFinishingDrawing(s);
        }
        else
        {
            if (stroke.Volume() < 50f)
            {
                if (controllerIndex == 0)
                {
                    if (visualized)
                    {
                        ClearStrokes();
                        visualized = false;
                    }
                    else
                    {
                        ClearInput();
                    }
                }
                else
                {
                    filled = !filled;
                }

                stroke.DestroyStroke();
            }
            else
            {
                stroke.FinishStroke();
                strokes.Add(stroke);
                OnFinishingDrawing(stroke);
                visualized = true;
            }
        }

        editing = false;
    }

    public void ClearStrokes()
    {
        Stroke[] ss = FindObjectsOfType<Stroke>();
        foreach (Stroke stroke in ss) stroke.HideStroke();
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
        gestureDetector = FindObjectOfType<GestureDetector>();

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
        if (gestureDetector == null) return;

        if (gestureDetector.leftHandPinching)
        {
            if (!leftHandPinchBegin)
            {
                leftHandPinchBegin = true;

                OnFirstInput(gestureDetector.leftHandPos, 0);
            }

            if (rightHandPinchBegin)
            {
                OnBothInput(gestureDetector.leftHandPos, gestureDetector.rightHandPos);
            }
            else
            {
                OnSingleInput(gestureDetector.leftHandPos, 0);
            }
        }
        else
        {
            if (leftHandPinchBegin)
            {
                leftHandPinchBegin = false;

                if (!rightHandPinchBegin)
                    OnFinishingInput(0);
            }
        }

        if (gestureDetector.rightHandPinching)
        {
            if (!rightHandPinchBegin)
            {
                rightHandPinchBegin = true;

                OnFirstInput(gestureDetector.rightHandPos, 1);
            }

            if (!leftHandPinchBegin)
                OnSingleInput(gestureDetector.rightHandPos, 1);
        }
        else
        {
            if (rightHandPinchBegin)
            {
                rightHandPinchBegin = false;

                if (!leftHandPinchBegin)
                    OnFinishingInput(1);
            }
        }
    }
}
