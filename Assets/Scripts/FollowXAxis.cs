using UnityEngine;

public class FollowXAxis : MonoBehaviour
{
    public Rigidbody X_axis;
    public Rigidbody printerHead;
    float yOffset = 0.1009f; // defined by printer model

    void FixedUpdate()
    {
        Vector3 headPos = printerHead.position;
        headPos.y = X_axis.position.y + yOffset;  
        printerHead.MovePosition(headPos);
    }
}
