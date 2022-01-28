using UnityEngine;
using UnityEngine.Tilemaps;

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
    [SerializeField] float peekingOpacity = .5f;
    [SerializeField] Transform spawnPoint;

    [SerializeField] GameObject tovWorld;
    [SerializeField] GameObject raWorld;

    // Tags
    private string horizontalInputName = "Horizontal";
    private string verticalInputName = "Vertical";
    private string jumpInputName = "Jump";
    private string fire1AxisName = "Fire1";
    private string fire2AxisName = "Fire2";
    private string fire3AxisName = "Fire3";
    private string groundLayerName = "Ground";
    private string damageLayerTag = "Damage";
    private string tovSideTag = "TovSide";
    private string raSideTag = "RaSide";

    // Status variables
    [SerializeField] bool isGrounded = false;
    public bool isTovSide { get; private set; } = true;
    private bool usedDash = false;
    private bool isPeeking = false;
    private bool fire3AxisInUse = false;
    private bool fire1AxisInUse = false;
    private float curDashCooldown;
    private float curJumpCooldown;
    private float dashProgress;
    private Vector3 dashStart;
    private Vector3 dashHeading;
    private float horizontalInput;
    private Vector3 curVelocity;


    // Cache variables
    Rigidbody2D myRigidbody2D;
    [SerializeField] BoxCollider2D myDownCollider2D;
    [SerializeField] BoxCollider2D myUpCollider2D;
    [SerializeField] BoxCollider2D myLeftCollider2D;
    [SerializeField] BoxCollider2D myRightCollider2D;
    [SerializeField] BoxCollider2D myNonFeetCollider2D;

    // Debug variables
    public float horizontalVelocityDiff;

    private void Start()
    {
        myRigidbody2D = GetComponent<Rigidbody2D>();
        Switch();
    }

    private void Update()
    {
        ProcessMovements();
    }

    private void ProcessMovements()
    {
        ProcessJump();

        ProcessHorizontalMovement();

        ProcessPeek();

        ProcessDimensionSwitch();

        ProcessDash();
    }

    private void ProcessPeek()
    {
        // TODO change the render layer so that the image is always in front/back of the real world
        if (Input.GetAxisRaw(fire3AxisName) != 0)
        {
            if (fire3AxisInUse == false)
            {
                isPeeking = !isPeeking;
                foreach (Transform child in isTovSide ? raWorld.transform : tovWorld.transform)
                {
                    if (child.gameObject.layer == LayerMask.NameToLayer(groundLayerName))
                    {
                        Color newColor = child.GetComponent<Tilemap>().color;
                        newColor.a = isPeeking ? peekingOpacity : 0f;
                        child.GetComponent<Tilemap>().color = newColor;
                    }
                }
                fire3AxisInUse = true;
            }
        }
        if (Input.GetAxisRaw(fire3AxisName) == 0)
        {
            fire3AxisInUse = false;
        }
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
        if (Input.GetAxisRaw(fire1AxisName) != 0)
        {
            if (fire1AxisInUse == false)
            {
                foreach (Transform child in isTovSide ? raWorld.transform : tovWorld.transform)
                {
                    if (child.gameObject.layer == LayerMask.NameToLayer(groundLayerName))
                    {
                        TilemapCollider2D tilemapCollider2D = child.GetComponent<TilemapCollider2D>();
                        tilemapCollider2D.enabled = true;

                        ContactFilter2D contactFilter2D = new ContactFilter2D();
                        contactFilter2D.SetLayerMask(LayerMask.GetMask(groundLayerName));

                        Collider2D[] mainColliderHits = new Collider2D[4];

                        myRigidbody2D.OverlapCollider(contactFilter2D, mainColliderHits);

                        bool collision = false;

                        foreach (Collider2D mainHit in mainColliderHits)
                        {

                            if (mainHit == null)
                            {
                                break;
                            }

                            if (mainHit.gameObject.Equals(child.gameObject))
                            {

                                Collider2D[] nonFeetColliderHits = new Collider2D[4];

                                myNonFeetCollider2D.OverlapCollider(contactFilter2D, nonFeetColliderHits);

                                foreach (Collider2D nonFeetHit in nonFeetColliderHits)
                                {
                                    if (nonFeetHit == null)
                                    {
                                        break;
                                    }
                                    else if (nonFeetHit.gameObject.Equals(child.gameObject))
                                    {
                                        Debug.Log("Collided with: " + nonFeetHit.gameObject);
                                        collision = true;
                                        break;
                                    }
                                }
                                
                            }
                        }

                        tilemapCollider2D.enabled = false;
                        if (!collision)
                        {
                            Switch();
                        }
                    }
                }
                fire1AxisInUse = true;
            }
        }
        if (Input.GetAxisRaw(fire1AxisName) == 0)
        {
            fire1AxisInUse = false;
        }
    }

    private void Switch()
    {
        isPeeking = false;
        isTovSide = !isTovSide;
        foreach (Transform child in tovWorld.transform)
        {
            Color newColor = child.GetComponent<Tilemap>().color;
            newColor.a = isTovSide ? 1 : 0;
            child.GetComponent<Tilemap>().color = newColor;
            if (child.gameObject.layer == LayerMask.NameToLayer(groundLayerName) || child.gameObject.layer == LayerMask.NameToLayer(damageLayerTag))
            {
                child.GetComponent<TilemapCollider2D>().enabled = isTovSide;
            }
        }
        foreach (Transform child in raWorld.transform)
        {
            Color newColor = child.GetComponent<Tilemap>().color;
            newColor.a = !isTovSide ? 1 : 0;
            child.GetComponent<Tilemap>().color = newColor;
            if (child.gameObject.layer == LayerMask.NameToLayer(groundLayerName) || child.gameObject.layer == LayerMask.NameToLayer(damageLayerTag))
            {
                child.GetComponent<TilemapCollider2D>().enabled = !isTovSide;
            }
        }
        if (myRigidbody2D.IsTouchingLayers(LayerMask.GetMask(groundLayerName)))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    private void ProcessJump()
    {
        if (curJumpCooldown <= 0)
        {
            if (Input.GetAxis(jumpInputName) > 0 && isGrounded)
            {
                if (myDownCollider2D.IsTouchingLayers(LayerMask.GetMask(groundLayerName)))
                {
                    myRigidbody2D.AddForce(new Vector3(0, jumpForce));
                    Debug.Log("Vertical jump " + myRigidbody2D.velocity);
                }
                else if (myLeftCollider2D.IsTouchingLayers(LayerMask.GetMask(groundLayerName)))
                {
                    myRigidbody2D.velocity = new Vector3(0, myRigidbody2D.velocity.y);
                    myRigidbody2D.AddForce(new Vector3(jumpForce * Mathf.Sin(Mathf.PI * 0.25f), jumpForce * Mathf.Cos(Mathf.PI * 0.25f)));
                    Debug.Log("Right jump " + myRigidbody2D.velocity);
                }
                else if (myRightCollider2D.IsTouchingLayers(LayerMask.GetMask(groundLayerName)))
                {
                    myRigidbody2D.velocity = new Vector3(0, myRigidbody2D.velocity.y);
                    myRigidbody2D.AddForce(new Vector3(jumpForce * Mathf.Sin(Mathf.PI * -0.25f), jumpForce * Mathf.Cos(Mathf.PI * -0.25f)));
                    Debug.Log("Left jump" + myRigidbody2D.velocity);
                }
                else if (myUpCollider2D.IsTouchingLayers(LayerMask.GetMask(groundLayerName)))
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
        if (isGrounded && !myDownCollider2D.IsTouchingLayers(LayerMask.GetMask(groundLayerName))
                && (((myRightCollider2D.IsTouchingLayers(LayerMask.GetMask(groundLayerName)) && horizontalInput == 1))
                    || (myLeftCollider2D.IsTouchingLayers(LayerMask.GetMask(groundLayerName)) && horizontalInput == -1)
                    || (myUpCollider2D.IsTouchingLayers(LayerMask.GetMask(groundLayerName)) && Input.GetAxisRaw(verticalInputName) == 1)))
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
        if (collision.collider.gameObject.layer == LayerMask.NameToLayer(damageLayerTag))
        {
            transform.position = spawnPoint.position;
        }
        isGrounded = true;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        myRigidbody2D.gravityScale = 1;
        isGrounded = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {

    }
}
