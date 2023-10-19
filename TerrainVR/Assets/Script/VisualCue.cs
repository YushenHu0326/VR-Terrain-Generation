using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualCue : MonoBehaviour
{
    private LineRenderer leftLine, rightLine;
    public float leftBrushSize, rightBrushSize;
    private Vector3 forwardVector;
    private Quaternion leftTurn, rightTurn;
    private Terrain terrain;

    // Start is called before the first frame update
    void Start()
    {
        leftLine = new GameObject("LeftLine").AddComponent<LineRenderer>();
        rightLine = new GameObject("RightLine").AddComponent<LineRenderer>();

        leftLine.material = new Material(Shader.Find("Sprites/Default"));
        rightLine.material = new Material(Shader.Find("Sprites/Default"));

        leftLine.startColor = Color.red;
        leftLine.endColor = Color.red;
        rightLine.startColor = Color.red;
        rightLine.endColor = Color.red;

        leftLine.positionCount = 2;
        rightLine.positionCount = 2;

        leftBrushSize = 1f;
        rightBrushSize = 1f;

        forwardVector = Vector3.forward;
        leftTurn = new Quaternion();
        rightTurn = new Quaternion();
        leftTurn.y = -90f;
        rightTurn.y = 90f;

        terrain = FindObjectOfType<Terrain>();
    }

    public void UpdateVisualCue(Vector3 position, Vector3 forwardVector, bool visible)
    {
        transform.position = position;

        leftLine.SetPosition(0, transform.position);
        rightLine.SetPosition(0, transform.position);

        if (visible)
        {
            Vector3 leftEnd = transform.position + (leftTurn * forwardVector.normalized) * ((transform.position.y - terrain.gameObject.transform.position.y) * leftBrushSize);
            Vector3 rightEnd = transform.position + (rightTurn * forwardVector.normalized) * ((transform.position.y - terrain.gameObject.transform.position.y) * rightBrushSize);
            leftEnd.y = terrain.gameObject.transform.position.y;
            rightEnd.y = terrain.gameObject.transform.position.y;

            leftLine.SetPosition(1, leftEnd);
            rightLine.SetPosition(1, rightEnd);
        }
        else
        {
            leftLine.SetPosition(1, transform.position);
            rightLine.SetPosition(1, transform.position);
        }
    }
}
