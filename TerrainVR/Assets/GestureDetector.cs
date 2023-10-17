using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class GestureDetector : MonoBehaviour
{
    public OVRSkeleton skeleton;
    private List<OVRBone> fingerBones;
    public OVRHand rightHand;
    public OVRHand leftHand;
    // Start is called before the first frame update
    void Start() 
    {
        fingerBones = new List<OVRBone>(skeleton.Bones);

    }

    // Update is called once per frame
    void Update()
    {
        bool rightHandPinching = rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        if(rightHandPinching) {
            Debug.Log("right hand pinching");
            //var pos = OVRHand.PointerPose;
            //print(pos);
        }
        bool leftHandPinching = leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        if (leftHandPinching) {
            Debug.Log("left hand pinching");
            //var pos = OVRHand.PointerPose;
            //print(pos);
        }
    }

}
