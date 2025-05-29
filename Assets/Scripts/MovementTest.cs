using UnityEngine;

public class MovementTest : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 5f; // Add this line
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        Debug.Log($"Input: W:{Input.GetKey(KeyCode.W)} A:{Input.GetKey(KeyCode.A)} S:{Input.GetKey(KeyCode.S)} D:{Input.GetKey(KeyCode.D)} Space:{Input.GetKey(KeyCode.Space)}");
        float moveX = Input.GetKey(KeyCode.A) ? -1 : Input.GetKey(KeyCode.D) ? 1 : 0;
        float moveZ = Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0;

        Vector3 move = new Vector3(moveX, 0, moveZ).normalized * moveSpeed;
        rb.MovePosition(rb.position + move * Time.fixedDeltaTime);

        // Simple grounded check: only allow jump if vertical velocity is near zero
        bool isGrounded = Mathf.Abs(rb.linearVelocity.y) < 0.01f;

        if (Input.GetKey(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
}
