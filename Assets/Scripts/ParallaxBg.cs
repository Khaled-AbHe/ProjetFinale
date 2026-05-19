using UnityEngine;

public class ParallaxBg : MonoBehaviour
{
    public float parallaxFactor = 0.1f;

    private Camera cam;
    private Vector3 prevCamPosition;

    void Start()
    {
        cam = Camera.main;
        prevCamPosition = cam.transform.position;
    }

    void Update()
    {
        float difference = cam.transform.position.x - prevCamPosition.x;
        transform.position += new Vector3(difference * parallaxFactor, 0, 0);
        prevCamPosition = cam.transform.position;
    }
}