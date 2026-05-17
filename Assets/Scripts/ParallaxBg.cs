using UnityEngine;

public class ParallaxBg : MonoBehaviour
{
    [SerializeField] private float parallaxFactor = 0.1f;

    private Camera _cam;
    private Vector3 _lastCamPos;

    void Start()
    {
        _cam = Camera.main;
        _lastCamPos = _cam.transform.position;
    }

    void Update()
    {
        Vector3 camDelta = _cam.transform.position - _lastCamPos;
        transform.position += new Vector3(camDelta.x * parallaxFactor, 0, 0);
        _lastCamPos = _cam.transform.position;
    }
}