using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Serialized variables
    [SerializeField] float runSpeed = 1.0f;
    [SerializeField] float jumpForce = 1.0f;
    [SerializeField] float dashSpeed = 1.0f;
    [SerializeField] float dashDistance = 1.0f;
    [SerializeField] float dashCooldown = 1.0f;
    [SerializeField] float switchCooldown = 1.0f;

    public GameObject currentWaypoint;
    [SerializeField] GameObject tovWorld;
    [SerializeField] GameObject raWorld;

    // Tags
    [SerializeField] private string waypointTag = "Waypoint";
    [SerializeField] private string finalWaypointTag = "Final Waypoint";
    [SerializeField] private string horizontalInputName = "Horizontal";
    [SerializeField] private string verticalInputName = "Vertical";
    [SerializeField] private string jumpInputName = "Jump";
    [SerializeField] private string fire1AxisName = "Fire1";
    [SerializeField] private string fire2AxisName = "Fire2";

    float horizontalVelocity;

    // Status variables
    [SerializeField] bool isGrounded = false;
    public bool isTovSide { get; private set; } = true;
    private bool usedDash = false;
    private float curSwitchCooldown;
    private float curDashCooldown;
    private float dashProgress;
    private Vector3 dashStart;
    private Vector3 dashEnd;

    // Cache variables
    Rigidbody2D myRigidbody2D;
    EvolutionTracker myEvolutionTracker;

    // Debug variables
    public float verticalInput;
    public float horizontalInput;

    private void Start()
    {
        myRigidbody2D = GetComponent<Rigidbody2D>();
        myEvolutionTracker = GetComponent<EvolutionTracker>();
        if(myEvolutionTracker == null) { Debug.LogWarning("Missing DistanceSinceSwitchTracker"); }
        if (currentWaypoint == null) { Debug.LogWarning("Missing waypoint"); }
        tovWorld.SetActive(isTovSide);
        raWorld.SetActive(!isTovSide);
        
    }

    private void Update()
    {
        ProcessJump();

        // TODO Change speed calculations to allow for gradient of movement (not to stop/start immediately)
        horizontalVelocity = 0;

        ProcessHorizontalMovement();

        ProcessDimensionSwitch();

        ProcessDash();

        // TODO Remove debug variables
        verticalInput = Input.GetAxisRaw(verticalInputName);
        horizontalInput = Input.GetAxisRaw(horizontalInputName);
    }

    private void ProcessDash()
    {
        if (!usedDash)
        {
            if (Input.GetAxisRaw(fire2AxisName) != 0)
            {
                usedDash = !usedDash;
                dashProgress = 0;
                curDashCooldown = dashCooldown;
                dashStart = transform.position;
                dashEnd = transform.position + ComputeHeading() * dashDistance;
                Debug.DrawLine(dashStart, dashEnd, Color.red, 10f);
            }
        }
        else
        {
            dashProgress += Time.deltaTime * dashSpeed;
            if (dashProgress >= 1)
            {
                if (curDashCooldown <= 0 && isGrounded)
                {
                    usedDash = !usedDash;
                }
                else
                {
                    curDashCooldown -= Time.deltaTime;
                }
            }
            else
            {
                myRigidbody2D.MovePosition(Vector3.Lerp(dashStart, dashEnd, dashProgress));
            }
        }
    }

    private Vector3 ComputeHeading()
    {
        float heading = Mathf.Atan(Input.GetAxisRaw(horizontalInputName) / Input.GetAxisRaw(verticalInputName));

        if (Input.GetAxisRaw(verticalInputName) == 0)
        {
            if (Input.GetAxisRaw(horizontalInputName) > 0)
            {
                heading = Mathf.PI * 0.5f;
            }
            else if (Input.GetAxisRaw(horizontalInputName) < 0)
            {
                heading = Mathf.PI * -0.5F;
            }
            else
            {
                heading = Mathf.PI * 0.5f;
            }
        }
        else if (Input.GetAxisRaw(horizontalInputName) == 0 && Input.GetAxisRaw(verticalInputName) == -1)
        {
            heading = Mathf.PI;
        }
        else if (Input.GetAxisRaw(verticalInputName) < 1)
        {
            heading -= Mathf.PI * Mathf.Sign(heading);
        }

        Vector3 direction = new Vector3(Mathf.Sin(heading), Mathf.Cos(heading));

        return direction;
    }

    private void ProcessDimensionSwitch()
    {
        if (curSwitchCooldown <= 0)
        {
            if (Input.GetAxisRaw(fire1AxisName) != 0)
            {
                curSwitchCooldown = switchCooldown;
                isTovSide = !isTovSide;
                tovWorld.SetActive(isTovSide);
                raWorld.SetActive(!isTovSide);
            }
        }
        else
        {
            curSwitchCooldown -= Time.deltaTime;
        }
    }

    private void ProcessJump()
    {
        // Jumping
        if (Input.GetAxis(jumpInputName) > 0 && isGrounded)
        {
            GetComponent<Rigidbody2D>().velocity = new Vector3(myRigidbody2D.velocity.x, jumpForce);
        }
    }

    private void ProcessHorizontalMovement()
    {
        float horizontalVelocity = Input.GetAxis(horizontalInputName) * runSpeed;
        GetComponent<Rigidbody2D>().velocity = new Vector3(horizontalVelocity, myRigidbody2D.velocity.y);
    }

    // Check if Grounded
    // TODO check if the trigger is ground and not wall or ceiling

    private void OnCollisionEnter2D(Collision2D collision)
    {
        isGrounded = true;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        isGrounded = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == waypointTag && collision.gameObject.transform.position.x > currentWaypoint.transform.position.x)
        {
            currentWaypoint = collision.gameObject;
        }

        if (collision.gameObject.tag == finalWaypointTag)
        {
            Debug.Log("Game Over");
        }
    }
}
