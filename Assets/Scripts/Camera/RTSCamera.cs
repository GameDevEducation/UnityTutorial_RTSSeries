using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RTSCamera : MonoBehaviour
{
    [System.Serializable]
    public class CameraConfig
    {
        public float HeightOffset = 20f;
        public float HorizontalOffset = 20f;
    }

    [Header("Linked Game Objects")]
    [SerializeField] GameObject CameraFocus;
    [SerializeField] Terrain LinkedTerrain;

    [Header("Zoom")]
    [SerializeField][Range(0f, 1f)] float InitialZoomLevel = 0.5f;
    [SerializeField] CameraConfig MinimumZoomConfig = new CameraConfig()
                                                        { 
                                                            HeightOffset = 50, 
                                                            HorizontalOffset = 50 
                                                        };
    [SerializeField] CameraConfig MaximumZoomConfig = new CameraConfig()
                                                        {
                                                            HeightOffset = 10,
                                                            HorizontalOffset = 5
                                                        };
    [SerializeField] AnimationCurve ZoomMappingCurve;
    [SerializeField] float ZoomSensitivity = 0.1f;
    [SerializeField] bool InvertZoomDirection = false;

    [Header("Panning")]
    [SerializeField] float CameraPanSafetyMargin = 0.05f;
    [SerializeField] float CameraPanSpeed = 50f;

    Camera LinkedCamera;
    float CurrentZoomLevel;
    Vector2 MoveInput;
    Vector3 DesiredCameraFocusLocation;

    void Awake()
    {
        LinkedCamera = GetComponent<Camera>();
        CurrentZoomLevel = InitialZoomLevel;
        DesiredCameraFocusLocation = CameraFocus.transform.position;
    }

    // Start is called before the first frame update
    void Start()
    {
        UpdateCameraPosition();
    }

    // Update is called once per frame
    void Update()
    {
        // if we have movement input then update the desired focus location
        if (MoveInput.sqrMagnitude > float.Epsilon)
        {
            UpdateCameraFocusLocation(MoveInput * CameraPanSpeed * Time.deltaTime);
        }

        // if the camera needs to move then smoothly move it to the new location
        if ((DesiredCameraFocusLocation - CameraFocus.transform.position).sqrMagnitude > float.Epsilon)
        {
            // calculate the new location
            Vector3 newPosition = Vector3.MoveTowards(CameraFocus.transform.position,
                                                    DesiredCameraFocusLocation,
                                                    CameraPanSpeed * Time.deltaTime);

            // clamp the camera focus to the terrain
            CameraFocus.transform.position = GetClampedFocusLocation(newPosition);

            UpdateCameraPosition();
        }
    }

    void UpdateCameraFocusLocation(Vector2 positionDelta)
    {
        Vector3 newPosition = CameraFocus.transform.position;

        newPosition += CameraFocus.transform.forward * positionDelta.y;
        newPosition += CameraFocus.transform.right * positionDelta.x;

        DesiredCameraFocusLocation = GetClampedFocusLocation(newPosition);
    }

    Vector3 GetClampedFocusLocation(Vector3 newPosition)
    {
        // get the normalised location clamped to the terrain bounds
        Vector3 normalisedTerrainPosition = newPosition - LinkedTerrain.transform.position;
        normalisedTerrainPosition.x = Mathf.Clamp01(normalisedTerrainPosition.x /
                                                    LinkedTerrain.terrainData.size.x);
        normalisedTerrainPosition.z = Mathf.Clamp01(normalisedTerrainPosition.z /
                                                    LinkedTerrain.terrainData.size.z);
        normalisedTerrainPosition.y = LinkedTerrain.terrainData.GetInterpolatedHeight(normalisedTerrainPosition.x,
                                                        normalisedTerrainPosition.z);

        // apply safety boundary
        normalisedTerrainPosition.x = Mathf.Clamp(normalisedTerrainPosition.x,
                                                    CameraPanSafetyMargin,
                                                    1f - CameraPanSafetyMargin);
        normalisedTerrainPosition.z = Mathf.Clamp(normalisedTerrainPosition.z,
                                                    CameraPanSafetyMargin,
                                                    1f - CameraPanSafetyMargin);

        // convert back to world location
        newPosition.x = LinkedTerrain.transform.position.x + normalisedTerrainPosition.x *
                        LinkedTerrain.terrainData.size.x;
        newPosition.y = normalisedTerrainPosition.y;
        newPosition.z = LinkedTerrain.transform.position.z + normalisedTerrainPosition.z *
                        LinkedTerrain.terrainData.size.z;

        return newPosition;
    }

    void UpdateCameraPosition()
    {
        Vector3 cameraLocation = CameraFocus.transform.position;

        // pass the zoom level through the animation curve
        float workingZoomLevel = ZoomMappingCurve.Evaluate(CurrentZoomLevel);

        // determine the working offsets
        float workingHeightOffset = Mathf.Lerp(MinimumZoomConfig.HeightOffset, 
                                                MaximumZoomConfig.HeightOffset,
                                                workingZoomLevel);
        float workingHorizontalOffset = Mathf.Lerp(MinimumZoomConfig.HorizontalOffset,
                                                MaximumZoomConfig.HorizontalOffset,
                                                workingZoomLevel);

        // calculate the new camera location
        cameraLocation += workingHeightOffset * Vector3.up;
        cameraLocation -= workingHorizontalOffset * CameraFocus.transform.forward;

        // reposition the camera
        LinkedCamera.transform.position = cameraLocation;

        // look at the camera target
        LinkedCamera.transform.LookAt(CameraFocus.transform, Vector3.up);
    }

    void OnCameraZoom(InputValue value)
    {
        float zoomInput = value.Get<float>();

        if (zoomInput > float.Epsilon)
            UpdateZoom(1f);
        else if (zoomInput < -float.Epsilon)
            UpdateZoom(-1f);
    }

    void OnMove(InputValue value)
    {
        MoveInput = value.Get<Vector2>();
    }

    void UpdateZoom(float delta)
    {
        float workingDelta = InvertZoomDirection ? -delta : delta;
        CurrentZoomLevel = Mathf.Clamp01(CurrentZoomLevel + workingDelta * ZoomSensitivity * Time.deltaTime);

        UpdateCameraPosition();
    }

    public void FocusCameraOn(Vector3 location)
    {
        DesiredCameraFocusLocation = GetClampedFocusLocation(location);
    }
}
