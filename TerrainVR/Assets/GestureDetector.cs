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

    private OVRCameraRig rig;

    public bool rightHandPinching, leftHandPinching;
    public Vector3 leftHandPos, rightHandPos;
    // Start is called before the first frame update
    void Start() 
    {
        //fingerBones = new List<OVRBone>(skeleton.Bones);
        rig = FindObjectOfType<OVRCameraRig>();

        StartCoroutine(AddOutline());
    }

    IEnumerator AddOutline()
    {
        yield return new WaitForSeconds(3);

        rightHand.gameObject.AddComponent<Outline>();
        leftHand.gameObject.AddComponent<Outline>();
    }

    // Update is called once per frame
    void Update()
    {
        rightHandPinching = rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        float rightHandPinchingStrength = rightHand.GetFingerPinchStrength(OVRHand.HandFinger.Ring);
        //Debug.Log("rightHandPinchingStrength" + rightHandPinchingStrength);
        if (rightHandPinching) {
            rightHandPos = rightHand.PointerPose.gameObject.transform.position * rig.transform.localScale.x;
        }
        leftHandPinching = leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        float leftHandPinchingStrength = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Ring);
        if (leftHandPinching) {
            //Debug.Log("left hand pinching");
            leftHandPos = leftHand.PointerPose.gameObject.transform.position * rig.transform.localScale.x;
        }
    }

}
