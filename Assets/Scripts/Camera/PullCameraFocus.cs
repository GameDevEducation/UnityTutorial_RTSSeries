using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PullCameraFocus : MonoBehaviour
{
    [SerializeField] bool PerformAction = false;
    [SerializeField] RTSCamera LinkedCamera;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (PerformAction)
        {
            PerformAction = false;
            LinkedCamera.FocusCameraOn(transform.position);
        }
    }
}
