using UnityEngine;
using System.Collections.Generic;

public class DrawLines : MonoBehaviour
{
    public GameObject trailPrefab;
    public float distanceThreshold = 0.000015f;
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
            float distBed = Vector3.Distance(parentObject.transform.position, lastPositionBed);
            float distHead = Vector3.Distance(transform.position, lastPositionHead);

            if (distBed > distanceThreshold || distHead > distanceThreshold)
            {
                GameObject clonedObject = Instantiate(trailPrefab, transform.position, Quaternion.identity);
                clonedObject.transform.parent = parentObject.transform;
                lastPositionBed = parentObject.transform.position;
                lastPositionHead = transform.position;
            }
        }
    }
}
