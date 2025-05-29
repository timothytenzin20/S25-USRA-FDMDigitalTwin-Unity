using UnityEngine;

public class DrawLines : MonoBehaviour
{
    public GameObject trailPrefab; // Assign your small cube prefab here
    public float distanceThreshold = 0.1f; // Minimum distance before placing a new segment

    private Vector3 lastPosition;

    void Start()
    {
        lastPosition = transform.position;
    }

    void FixedUpdate()
    {
        if (Vector3.Distance(transform.position, lastPosition) > distanceThreshold)
        {
            Instantiate(trailPrefab, transform.position, Quaternion.identity);
            lastPosition = transform.position;
        }
    }
}
