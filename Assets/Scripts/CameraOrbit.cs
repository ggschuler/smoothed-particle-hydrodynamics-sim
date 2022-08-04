using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    // rotation relevant variables:
    [SerializeField] private float mouseSensitivity = 1.0f;
    [SerializeField] private Transform orbitTarget;
    [SerializeField] private float zoomOut = 3.0f;
    private float _rotationX;
    private float _rotationY;
    // zoom relevant variables:
    [SerializeField] private Camera cam;
    [SerializeField] private float zoomMult = 2f;
    [SerializeField] private float defaultFov = 90f;

    [SerializeField] private float zoomDuration = 2;

    // Update is called once per frame
    void Update()
    {
        // handles rotation:
        if (Input.GetMouseButton(2))
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * -1;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * -1;
            _rotationX += mouseX;
            _rotationY += mouseY;
            transform.localEulerAngles = new Vector3(_rotationY, _rotationX, 0);
            transform.position = orbitTarget.position - transform.forward * zoomOut;
        }
        
        // handles zoom:
        if (Input.GetMouseButton(1))
        {
            ZoomCamera(defaultFov / zoomMult);
        }
        else if (Input.GetMouseButton(0))
        {
            ZoomCamera(defaultFov);
        }
    }

    void ZoomCamera(float target)
    {
        float angle = Mathf.Abs((defaultFov / zoomMult) - defaultFov);
        cam.fieldOfView = Mathf.MoveTowards(cam.fieldOfView, target, angle / zoomDuration * Time.deltaTime);
    }
}
