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

    public GameObject water;
    private bool draggingWater;

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

    public bool freezeInput;

    private UserStudy study;

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

    IEnumerator OnFinishingDrawing()
    {
        foreach (Stroke s in strokes)
        {
            if (s == null) continue;
            if (!s.activated) continue;

            Vector3 position;
            float offset, brushSize;

            if (s.filled)
            {
                for (int i = 0; i < s.GetPositionCount(); i++)
                {
                    position = s.GetPosition(i);

                    if (i != 0)
                    {
                        terrainTool.FillTerrain(position, s.GetPosition(0), 20, 20);
                    }

                    if (i % 10 == 0) yield return null;
                }
            }

            for (int i = 0; i < s.GetPositionCount(); i++)
            {
                position = s.GetPosition(i);

                int startIndex = Mathf.Max(0, i - 20);
                int endIndex = Mathf.Min(s.GetPositionCount() - 1, i + 20);

                RaycastHit hit;

                if (Physics.Raycast(new Vector3(position.x, 500f, position.z), Vector3.down * 500f, out hit))
                {
                    if (hit.point.y < position.y)
                    {
                        offset = position.y - terrainTool.targetTerrain.transform.position.y;
                        brushSize = Mathf.Abs(offset - terrainTool.terrainOffset) * 2f;

                        terrainTool.RaiseTerrain(position,
                                                 Mathf.Abs(offset), brushSize, s.GetLeftBrushSize(), s.GetRightBrushSize(), s.GetLeftBrushCurve(), s.GetRightBrushCurve(),
                                                 s.GetDerivative(i), s.GetPosition(startIndex), s.GetPosition(endIndex), s.GetPosition(0), s.GetPosition(s.GetPositionCount() - 1));
                    }
                    else
                    {
                        offset = position.y - hit.point.y;
                        brushSize = Mathf.Abs(offset) * 2f;

                        terrainTool.LowerTerrain(position,
                                                 hit.point.y, brushSize, s.GetLeftBrushSize(), s.GetRightBrushSize(), s.GetLeftBrushCurve(), s.GetRightBrushCurve(),
                                                 s.GetDerivative(i));
                    }
                }

                if (i % 10 == 0) yield return null;
            }
        }
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
        if (water != null && Mathf.Abs(position.y - (water.transform.position.y + 50f)) < 5f)
        {
            draggingWater = true;
            water.transform.position = new Vector3(water.transform.position.x, position.y - 50f, water.transform.position.z);
            return;
        }

        if (editing)
        {
            if (controllerIndex == 0)
            {
                Vector3 d = stroke.GetDerivative(rightEditingIndex);
                d.y = 0f;

                float angle  = Vector3.Angle(d, (stroke.GetPosition(rightEditingIndex) - position).normalized) *
                                             Mathf.Sign(d.x * (stroke.GetPosition(rightEditingIndex) - position).normalized.z 
                                             - d.z * (stroke.GetPosition(rightEditingIndex) - position).normalized.x);

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
                Vector3 d = stroke.GetDerivative(leftEditingIndex);
                d.y = 0f;

                float angle = Vector3.Angle(d, (stroke.GetPosition(leftEditingIndex) - position).normalized) *
                                             Mathf.Sign(d.x * (stroke.GetPosition(leftEditingIndex) - position).normalized.z
                                             - d.z * (stroke.GetPosition(leftEditingIndex) - position).normalized.x);

                angle /= 180f;

                if (angle > -0.55f && angle < -0.45f)
                    rightEditingIndex = -100;
                else if (angle < 0.55f && angle > 0.45f)
                    rightEditingIndex = -101;
                else
                    rightEditingIndex = stroke.LocateEditingIndex(position);
            }

            /****************************************************************/
            /*                       User Study use                         */
            /****************************************************************/

            if (study != null)
            { 
                study.Write(controllerIndex, 1, position);
            }

            /****************************************************************/
            /*                       User Study use                         */
            /****************************************************************/

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

        /****************************************************************/
        /*                       User Study use                         */
        /****************************************************************/

        if (study != null)
        {
            if (editing)
            {
                study.Write(controllerIndex, 1, position);
            }
            else
            {
                study.Write(controllerIndex, 0, position);
            }
        }

        /****************************************************************/
        /*                       User Study use                         */
        /****************************************************************/
    }

    public void OnSingleInput(Vector3 position, int controllerIndex)
    {
        if (draggingWater)
        {
            water.transform.position = new Vector3(water.transform.position.x, position.y - 50f, water.transform.position.z);
            return;
        }

        if (editing) OnSingleEditingStroke(position, controllerIndex);
        else OnDrawing(position);

        /****************************************************************/
        /*                       User Study use                         */
        /****************************************************************/

        if (study != null)
        {
            if (editing)
            {
                study.Write(controllerIndex, 1, position);
            }
            else
            {
                study.Write(controllerIndex, 0, position);
            }
        }

        /****************************************************************/
        /*                       User Study use                         */
        /****************************************************************/
    }

    public void OnBothInput(Vector3 leftPosition, Vector3 rightPosition)
    {
        if (draggingWater)
        {
            water.transform.position = new Vector3(water.transform.position.x, rightPosition.y - 50f, water.transform.position.z);
            return;
        }

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

        /****************************************************************/
        /*                       User Study use                         */
        /****************************************************************/

        if (study != null)
        {
            if (editing)
            {
                study.Write(0, 1, leftPosition);
                study.Write(1, 1, rightPosition);
            }
            else
            {
                study.Write(0, 0, leftPosition);
                study.Write(1, 0, rightPosition);
            }
        }

        /****************************************************************/
        /*                       User Study use                         */
        /****************************************************************/
    }

    public IEnumerator OnFinishingInput(int controllerIndex)
    {
        if (draggingWater)
        {
            draggingWater = false;
            yield break;
        }

        if (editing)
        {
            freezeInput = true;

            if (stroke.Volume() < 20f)
            {
                strokes.Remove(stroke);
                stroke.DestroyStroke();
                terrainTool.ClearTerrain(false);
                yield return StartCoroutine(OnFinishingDrawing());
            }
            else
            {
                stroke.OnFinishEditing();
                terrainTool.ClearTerrain(false);
                yield return StartCoroutine(OnFinishingDrawing());
            }

            yield return StartCoroutine(terrainTool.ApplyTerrain());
        }
        else
        {
            if (stroke.Volume() < 20f)
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
                freezeInput = true;

                stroke.FinishStroke();
                strokes.Add(stroke);

                terrainTool.ClearTerrain(false);
                yield return StartCoroutine(OnFinishingDrawing());
                yield return StartCoroutine(terrainTool.ApplyTerrain());
                visualized = true;
            }
        }

        editing = false;
    }

    public void ClearStrokes()
    {
        terrainTool.SaveTerrain();
        Stroke[] ss = FindObjectsOfType<Stroke>();
        foreach (Stroke stroke in ss) stroke.HideStroke();
    }

    public void ClearInput()
    {
        if (terrainTool == null) return;

        terrainTool.ClearTerrain(true);

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

        study = FindObjectOfType<UserStudy>();
    }

    // Update is called once per frame
    void Update()
    {
        if (gestureDetector == null) return;
        if (freezeInput) return;

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
                    StartCoroutine(OnFinishingInput(0));
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
                    StartCoroutine(OnFinishingInput(1));
            }
        }
    }
}
