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
    [SerializeField] float jumpCooldown = 0.1f;
    [SerializeField] float switchCooldown = 1.0f;
    [SerializeField] float horizontalMovementSmoothingFactor = .05f;

    public GameObject currentWaypoint;
    [SerializeField] GameObject tovWorld;
    [SerializeField] GameObject raWorld;

    // Tags
    private string waypointTag = "Waypoint";
    private string finalWaypointTag = "Final Waypoint";
    private string horizontalInputName = "Horizontal";
    private string verticalInputName = "Vertical";
    private string jumpInputName = "Jump";
    private string fire1AxisName = "Fire1";
    private string fire2AxisName = "Fire2";
    private string groundLayerTag = "Ground";

    // Status variables
    [SerializeField] bool isGrounded = false;
    public bool isTovSide { get; private set; } = true;
    private bool usedDash = false;
    private float curSwitchCooldown;
    private float curDashCooldown;
    private float curJumpCooldown;
    private float dashProgress;
    private Vector3 dashStart;
    private Vector3 dashHeading;
    public float horizontalInput;
    public float horizontalVelocity;
    public float prevHorizontalVelocity;
    private Vector3 curVelocity;


    // Cache variables
    BoxCollider2D myBoxCollider2D;
    Rigidbody2D myRigidbody2D;
    EvolutionTracker myEvolutionTracker;
    [SerializeField] BoxCollider2D myDownCollider2D;
    [SerializeField] BoxCollider2D myUpCollider2D;
    [SerializeField] BoxCollider2D myLeftCollider2D;
    [SerializeField] BoxCollider2D myRightCollider2D;

    // Debug variables
    public float horizontalVelocityDiff;

    private void Start()
    {
        myBoxCollider2D = GetComponent<BoxCollider2D>();
        myRigidbody2D = GetComponent<Rigidbody2D>();
        myEvolutionTracker = GetComponent<EvolutionTracker>();
        if(myEvolutionTracker == null) { Debug.LogWarning("Missing DistanceSinceSwitchTracker"); }
        if (currentWaypoint == null) { Debug.LogWarning("Missing waypoint"); }
        tovWorld.SetActive(isTovSide);
        raWorld.SetActive(!isTovSide);
        
    }

    private void Update()
    {
        ProcessMovements();
    }

    private void ProcessMovements()
    {
        ProcessJump();

        ProcessHorizontalMovement();

        ProcessDimensionSwitch();

        ProcessDash();
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
                dashHeading = ComputeHeading();
                Debug.DrawLine(dashStart, dashStart + dashHeading * dashDistance, Color.red, 10f);
            }
        }
        else
        {
            dashProgress += Time.deltaTime * dashSpeed;
            if (dashProgress >= 1)
            {
                if (curDashCooldown == dashCooldown)
                {
                    myRigidbody2D.velocity = dashHeading * dashSpeed;
                    Debug.DrawRay(transform.position, dashHeading, Color.green, 10f);
                }
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
                myRigidbody2D.MovePosition(Vector3.Lerp(dashStart, dashStart + dashHeading * dashDistance, dashProgress));
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
        if (curJumpCooldown <=0)
        {
            if (Input.GetAxis(jumpInputName) > 0 && isGrounded)
            {
                if (myDownCollider2D.IsTouchingLayers(LayerMask.GetMask(groundLayerTag)))
                {
                    myRigidbody2D.AddForce(new Vector3(0, jumpForce));
                    Debug.Log("Vertical jump " + myRigidbody2D.velocity);
                }
                else if (myLeftCollider2D.IsTouchingLayers(LayerMask.GetMask(groundLayerTag)))
                {
                    myRigidbody2D.velocity = new Vector3(0, myRigidbody2D.velocity.y);
                    myRigidbody2D.AddForce(new Vector3(jumpForce * Mathf.Sin(Mathf.PI * 0.25f), jumpForce * Mathf.Cos(Mathf.PI * 0.25f)));
                    Debug.Log("Right jump " + myRigidbody2D.velocity);
                }
                else if (myRightCollider2D.IsTouchingLayers(LayerMask.GetMask(groundLayerTag)))
                {
                    myRigidbody2D.velocity = new Vector3(0, myRigidbody2D.velocity.y);
                    myRigidbody2D.AddForce(new Vector3(jumpForce * Mathf.Sin(Mathf.PI * -0.25f), jumpForce * Mathf.Cos(Mathf.PI * -0.25f)));
                    Debug.Log("Left jump" + myRigidbody2D.velocity);
                }
                else if (myUpCollider2D.IsTouchingLayers(LayerMask.GetMask(groundLayerTag)))
                {
                    myRigidbody2D.gravityScale = 1;
                }
                curJumpCooldown = jumpCooldown;
            }
        }
        else
        {
            curJumpCooldown -= Time.deltaTime;
        }
    }

    private void ProcessHorizontalMovement()
    {
        horizontalInput = Input.GetAxis(horizontalInputName);
        if (isGrounded && !myDownCollider2D.IsTouchingLayers(LayerMask.GetMask(groundLayerTag))
                && (((myRightCollider2D.IsTouchingLayers(LayerMask.GetMask(groundLayerTag)) && horizontalInput == 1))
                || (myLeftCollider2D.IsTouchingLayers(LayerMask.GetMask(groundLayerTag)) && horizontalInput == -1)
                || (myUpCollider2D.IsTouchingLayers(LayerMask.GetMask(groundLayerTag)) && Input.GetAxisRaw(verticalInputName) == 1)))
        {
            myRigidbody2D.velocity = Vector3.zero;
            myRigidbody2D.gravityScale = 0;
        }
        else if (horizontalInput != 0 && curJumpCooldown <= 0)
        {
            myRigidbody2D.gravityScale = 1;
            myRigidbody2D.velocity = Vector3.SmoothDamp(myRigidbody2D.velocity, new Vector3(horizontalInput * runSpeed, myRigidbody2D.velocity.y), ref curVelocity, horizontalMovementSmoothingFactor);
        }
        else
        {
            myRigidbody2D.gravityScale = 1;
        }
    }

    // Check if Grounded
    // TODO check if the trigger is ground and not wall or ceiling

    private void OnCollisionEnter2D(Collision2D collision)
    {
        isGrounded = true;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        myRigidbody2D.gravityScale = 1;
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
