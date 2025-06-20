using UnityEngine;
using System.Collections.Generic;

public class DrawLines : MonoBehaviour
{
    public GameObject trailPrefab;
    public float distanceThreshold = 0.0000000000000000000000000000000000015f;
    public GameObject parentObject;
    private Vector3 lastPositionBed;
    private Vector3 lastPositionHead;

    void Start()
    {
        lastPositionBed = parentObject.transform.position;
        lastPositionHead = transform.position;
    }

    void Update()
    {
        if (ParseGCode.IsCurrentlyPrintingHead())
        {
                GameObject clonedObject = Instantiate(trailPrefab, transform.position, Quaternion.identity);
                clonedObject.transform.parent = parentObject.transform;
                lastPositionBed = parentObject.transform.position;
                lastPositionHead = transform.position;
        }
    }
}
