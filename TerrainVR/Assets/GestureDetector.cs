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
    // Start is called before the first frame update
    void Start() 
    {
        //fingerBones = new List<OVRBone>(skeleton.Bones);
    }

    // Update is called once per frame
    void Update()
    {
        bool rightHandPinching = rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        float rightHandPinchingStrength = rightHand.GetFingerPinchStrength(OVRHand.HandFinger.Ring);
        //Debug.Log("rightHandPinchingStrength" + rightHandPinchingStrength);
        if (rightHandPinching) {
            //Debug.Log("right hand pinching");
            var pos = rightHand.PointerPose.gameObject.transform.position;
            //Debug.Log(pos.x + " " + pos.y + " " + pos.z);
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = pos;
            sphere.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
        }
        bool leftHandPinching = leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        float leftHandPinchingStrength = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Ring);
        if (leftHandPinching) {
            //Debug.Log("left hand pinching");
            var pos = leftHand.PointerPose.gameObject.transform.position;
            //Debug.Log(pos.x + " " + pos.y + " " + pos.z);
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = pos;
            sphere.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
        }
    }

}
