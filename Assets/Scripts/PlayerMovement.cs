using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{

    public float speed = 1.0f;
    public float jumpSpeed = 100.0f;

    public Camera FirstPersonCamera;

    private float speedYaw = 2.0f;
    private float speedPitch = 2.0f;
    private float yaw = 0.0f;
    private float pitch = 0.0f;

    private Rigidbody _rb;
	// Use this for initialization
	void Start ()
	{
	    _rb = gameObject.GetComponent<Rigidbody>();

	}
	
	// Update is called once per frame
	void FixedUpdate ()
	{
	    var h = Input.GetAxisRaw("Horizontal");
	    var v = Input.GetAxisRaw("Vertical");

        Move(v, h);

	    if (Input.GetKeyDown(KeyCode.Space))
        {
            _rb.MovePosition(transform.position + Vector3.up * jumpSpeed);
        }
	}

    void Update()
    {
        yaw += speedYaw*Input.GetAxis("Mouse X");
        pitch -= speedPitch*Input.GetAxis("Mouse Y");

        FirstPersonCamera.transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);
    }

    private void Move(float v, float h)
    {
        const float rotScale = 300f;
        var eulerAngleVelocity = new Vector3(0, h * rotScale, 0);
        var movement = (transform.right*v).normalized *speed*Time.deltaTime;

        _rb.MovePosition(transform.position + movement);
        _rb.MoveRotation(transform.rotation * Quaternion.Euler(eulerAngleVelocity * Time.deltaTime));

    }
}
