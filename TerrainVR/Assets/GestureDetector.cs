using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class GestureDetector : MonoBehaviour
{
    //public OVRSkeleton skeleton;
    //private List<OVRBone> fingerBones;
    public OVRHand rightHand;
    public OVRHand leftHand;

    public GameObject centerEyeAnchor;

    private OVRCameraRig rig;

    public bool rightHandPinching, leftHandPinching;
    public Vector3 leftHandPos, rightHandPos;

    private LineRenderer rightLine, leftLine;
    public Material mat;
    // Start is called before the first frame update
    void Start() 
    {
        //fingerBones = new List<OVRBone>(skeleton.Bones);
        rig = FindObjectOfType<OVRCameraRig>();

        StartCoroutine(AddOutline());

        rightLine = new GameObject("LeftLine").AddComponent<LineRenderer>();
        leftLine = new GameObject("LeftLine").AddComponent<LineRenderer>();
        rightLine.material = mat;
        leftLine.material = mat;

        rightLine.positionCount = 2;
        leftLine.positionCount = 2;
    }

    IEnumerator AddOutline()
    {
        yield return new WaitForSeconds(1.5f);

        rightHand.gameObject.AddComponent<Outline>();
        leftHand.gameObject.AddComponent<Outline>();

    }

    // Update is called once per frame
    void Update()
    {
        if (leftHand == null || rightHand == null) return;

        //float rightHandPinchingStrength = rightHand.GetFingerPinchStrength(OVRHand.HandFinger.Ring);
        //Debug.Log("rightHandPinchingStrength" + rightHandPinchingStrength);
        /*
        if (rightHandPinching) {
            rightHandPos = rightHand.PointerPose.gameObject.transform.position * rig.transform.localScale.x;
        }
        leftHandPinching = leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        float leftHandPinchingStrength = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Ring);
        if (leftHandPinching) {
            //Debug.Log("left hand pinching");
            leftHandPos = leftHand.PointerPose.gameObject.transform.position * rig.transform.localScale.x;
        }*/
        rightHandPos = rightHand.PointerPose.gameObject.transform.position * rig.transform.localScale.x;
        leftHandPos = leftHand.PointerPose.gameObject.transform.position * rig.transform.localScale.x;

        bool rightHandInSight = Vector3.Angle(centerEyeAnchor.transform.forward, rightHandPos - centerEyeAnchor.transform.position) < 60f;
        bool leftHandInSight = Vector3.Angle(centerEyeAnchor.transform.forward, leftHandPos - centerEyeAnchor.transform.position) < 60f;

        rightHandPinching = rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index) && rightHandInSight;
        leftHandPinching = leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index) && leftHandInSight;

        if (rightLine != null && leftLine != null)
        {
            rightLine.SetPosition(0, rightHandPos);
            leftLine.SetPosition(0, leftHandPos);

            RaycastHit leftHit, rightHit;

            if (Physics.Raycast(rightHandPos, Vector3.down * 500f, out rightHit))
            {
                rightLine.SetPosition(1, rightHit.point);
                rightLine.material.SetColor("_OutlineColor", Color.yellow);
            }
            else if (Physics.Raycast(new Vector3(rightHandPos.x, 500f, rightHandPos.z), Vector3.down * 500f, out rightHit))
            {
                rightLine.SetPosition(1, rightHit.point);
                rightLine.material.SetColor("_OutlineColor", Color.blue);
            }
            else
            {
                rightLine.SetPosition(1, rightHandPos);
            }

            if (Physics.Raycast(leftHandPos, Vector3.down * 500f, out leftHit))
            {
                leftLine.SetPosition(1, leftHit.point);
                leftLine.material.SetColor("_OutlineColor", Color.yellow);
            }
            else if (Physics.Raycast(new Vector3(leftHandPos.x, 500f, leftHandPos.z), Vector3.down * 500f, out leftHit))
            {
                leftLine.SetPosition(1, leftHit.point);
                leftLine.material.SetColor("_OutlineColor", Color.blue);
            }
            else
            {
                leftLine.SetPosition(1, leftHandPos);
            }
        }
    }

}
