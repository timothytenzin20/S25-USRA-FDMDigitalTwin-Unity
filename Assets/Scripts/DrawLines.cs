using UnityEngine;
using System.Collections.Generic;

public class DrawLines : MonoBehaviour
{
    public GameObject trailPrefab;
    public float distanceThreshold = 0f;
    public GameObject parentObject;
    private Vector3 lastPositionBed;
    private Vector3 lastPositionHead;

    void Start()
    {
        lastPositionBed = parentObject.transform.position;
        lastPositionHead = transform.position;
    }

    void FixedUpdate()
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