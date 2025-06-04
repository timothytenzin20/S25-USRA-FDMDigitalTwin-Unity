using UnityEngine;

public class DrawLines : MonoBehaviour
{
    public GameObject trailPrefab; 
    public float distanceThreshold = 0.0015f; 
    public GameObject parentObject;
    private Vector3 lastPosition;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        if (Vector3.Distance(transform.position, lastPosition) > distanceThreshold)
        {
            GameObject clonedObject = Instantiate(trailPrefab, transform.position, Quaternion.identity);
            clonedObject.transform.parent = parentObject.transform;
            lastPosition = transform.position;
        }
    }
}

//public class DrawLines : MonoBehaviour
//{
//    public Transform bed;
//    public GameObject trailBlockPrefab;

//    private Vector3 lastSpawnPosition;
//    public float spawnDistance = 0.1f;

//    void Start()
//    {
//        lastSpawnPosition = GetTrailPosition();
//        SpawnTrailBlock(lastSpawnPosition);
//    }

//    void Update()
//    {
//        Vector3 currentPos = GetTrailPosition();

//        if (Vector3.Distance(lastSpawnPosition, currentPos) >= spawnDistance)
//        {
//            SpawnTrailBlock(currentPos);
//            lastSpawnPosition = currentPos;
//        }
//    }

//    Vector3 GetTrailPosition()
//    {
//        // Combine X from objectA, Z from objectB (Y is optional, often 0 or ground height)
//        return new Vector3(transform.position.x, transform.position.y, bed.position.z  - 9.1006107f);
//    }

//    void SpawnTrailBlock(Vector3 position)
//    {
//        Instantiate(trailBlockPrefab, position, Quaternion.identity);
//    }
//}
