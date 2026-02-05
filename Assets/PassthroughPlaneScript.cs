using UnityEngine;

public class PassthroughPlaneScript : MonoBehaviour
{
    void Start()
     {    
        #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_ANDROID
      OVRManager.eyeFovPremultipliedAlphaModeEnabled = false;
        #endif
     }

}
