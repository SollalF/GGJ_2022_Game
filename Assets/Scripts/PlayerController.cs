using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    // Serialized variables
    [SerializeField] float runSpeed = 1.0f;
    [SerializeField] float jumpForce = 1.0f;
    [SerializeField] float dashSpeed = 1.0f;
    [SerializeField] float dashDistance = 1.0f;
    [SerializeField] float dashCooldown = 1.0f;
    [SerializeField] float groundMovementSmoothingFactor = .05f;
    [SerializeField] float airMovementSmoothingFactor = .05f;
    [SerializeField] float peekingOpacity = .5f;
    [SerializeField] Transform spawnPoint;
    [SerializeField] float deathDelay = 3f;

    [SerializeField] GameObject tovWorld;
    [SerializeField] GameObject raWorld;

    // Tags
    private string groundLayerName = "Ground";
    private string damageLayerTag = "Damage";
    private string finishTag = "Finish";
    private string runVar = "isRunning";
    private string dashVar = "isDashing";
    private string jumpVar = "isJumping";
    private string dieVar = "isDead";
    private string grabVar = "isGrabbing";

    // Status variables
    [SerializeField] bool isGrounded = false;
    public bool isTovSide { get; private set; } = false;
    
    private bool usedDash = false;
    private bool isPeeking = false;
    private bool isGamePaused = false;

    private float nextDashAvaliable = -1f;
    private float nextJumpAvaliable = -1f;

    private bool isFacingRight = true;
    InputAction horizontalInput;
    private Vector3 curVelocity;


    // Cache variables
    Rigidbody2D myRigidbody2D;
    LevelLoader myLevelLoader;
    Animator myAnimator;
    InputMaster myInputMaster;
    SFXPlayer mySFXPlayer;
    [SerializeField] Canvas pauseCanvas;
    [SerializeField] BoxCollider2D myDownCollider2D;
    [SerializeField] BoxCollider2D myUpCollider2D;
    [SerializeField] BoxCollider2D myBackCollider2D;
    [SerializeField] BoxCollider2D myFrontCollider2D;
    [SerializeField] CapsuleCollider2D mySmallCollider2D;

    private void Awake()
    {
        myInputMaster = new InputMaster();
    }

    private void OnEnable()
    {
        horizontalInput = myInputMaster.Player.HorizontalMove;
        horizontalInput.Enable();

        myInputMaster.Player.Jump.performed += ctx => ProcessJump();
        myInputMaster.Player.Jump.Enable();

        myInputMaster.Player.Dash.performed += ctx => ProcessDash();
        myInputMaster.Player.Dash.Enable();

        myInputMaster.Player.Switch.performed += ctx => ProcessSwitch();
        myInputMaster.Player.Switch.Enable();
        
        myInputMaster.Player.Peek.performed += ctx => ProcessPeek();
        myInputMaster.Player.Peek.Enable();

        myInputMaster.Player.Pause.performed += ctx => PauseGame();
        myInputMaster.Player.Pause.Enable();
    }

    private void OnDisable()
    {
        horizontalInput.Disable();
        myInputMaster.Player.Jump.Disable();
        myInputMaster.Player.Dash.Disable();
        myInputMaster.Player.Switch.Disable();
        myInputMaster.Player.Peek.Disable();
        myInputMaster.Player.Pause.Disable();
        myInputMaster.Disable();
    }

    private void Start()
    {
        mySFXPlayer = GetComponent<SFXPlayer>();
        myAnimator = GetComponentInChildren<Animator>();
        myLevelLoader = FindObjectOfType<LevelLoader>();
        myRigidbody2D = GetComponent<Rigidbody2D>();
        Switch();
    }

    private void FixedUpdate()
    {
        ProcessHorizontalInput();

        if (Time.time >= nextDashAvaliable)
        {
            usedDash = false;
        }
    }
    private void ProcessPeek()
    {
        // TODO change the render layer so that the image is always in front/back of the real world
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
    }

    private IEnumerator Dash()
    {
        myAnimator.SetBool(dashVar, true);

        mySFXPlayer.PlayDashSFX();

        nextDashAvaliable = Time.time + dashDistance / dashSpeed + dashCooldown;
        
        usedDash = true;
        
        float dashProgress = 0;

        Vector3 dashStart = transform.position;

        Vector3 dashHeading = Vector3.right * horizontalInput.ReadValue<float>();

        Debug.DrawLine(dashStart, dashStart + dashHeading * dashDistance, Color.red, 10f);

        while(dashProgress < 1)
        {
            dashProgress += Time.deltaTime * dashSpeed;
            myRigidbody2D.MovePosition(Vector3.Lerp(dashStart, dashStart + dashHeading * dashDistance, dashProgress));
            yield return new WaitForEndOfFrame();
        }

        dashProgress = 1;
        myRigidbody2D.MovePosition(Vector3.Lerp(dashStart, dashStart + dashHeading * dashDistance, dashProgress));

        myAnimator.SetBool(dashVar, false);
        
        myRigidbody2D.velocity = dashHeading * dashSpeed;
        
        Debug.DrawRay(transform.position, dashHeading, Color.green, 10f);
    }

    private void ProcessDash()
    {
        if (!usedDash && Time.time >= nextDashAvaliable)
        {
            StartCoroutine(Dash());
        }
    }

    public void PauseGame()
    {
        Debug.Log("Pausing : " + !isGamePaused);
        isGamePaused = !isGamePaused;
        Time.timeScale = isGamePaused ? 0 : 1;
        pauseCanvas.gameObject.SetActive(isGamePaused);
    }

    private void ProcessSwitch()
    {
        foreach (Transform child in isTovSide ? raWorld.transform : tovWorld.transform)
        {
            if (child.gameObject.layer == LayerMask.NameToLayer(groundLayerName))
            {
                TilemapCollider2D tilemapCollider2D = child.GetComponent<TilemapCollider2D>();
                tilemapCollider2D.enabled = true;

                ContactFilter2D contactFilter2D = new ContactFilter2D();
                contactFilter2D.SetLayerMask(LayerMask.GetMask(groundLayerName));

                Collider2D[] smallColliderHits = new Collider2D[4];

                mySmallCollider2D.OverlapCollider(contactFilter2D, smallColliderHits);

                bool collision = false;

                foreach (Collider2D Hit in smallColliderHits)
                {

                    if (Hit == null)
                    {
                        break;
                    }

                    if (Hit.gameObject.Equals(child.gameObject))
                    {

                        collision = true;

                    }
                }

                tilemapCollider2D.enabled = false;
                if (!collision)
                {
                    Switch();
                }
            }
        }
    }
    
    private void Switch()
    {
        GameState.numberOfSwitchesThisGame++;
        mySFXPlayer.PlaySwitchSFX();
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
            else
            {
                child.GetComponentInChildren<SpriteRenderer>().color = newColor;
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
            else
            {
                child.GetComponentInChildren<SpriteRenderer>().color = newColor;
            }
        }
    }
    private void ProcessJump()
    {
        if (isGrounded)
        {
            if (myDownCollider2D.IsTouchingLayers(LayerMask.GetMask(groundLayerName)))
            {
                myRigidbody2D.AddForce(new Vector3(0, jumpForce));
            }
            else if (myFrontCollider2D.IsTouchingLayers(LayerMask.GetMask(groundLayerName)))
            {
                myRigidbody2D.velocity = new Vector3(0, myRigidbody2D.velocity.y);
                myRigidbody2D.AddForce(new Vector3(jumpForce * Mathf.Sin(Mathf.PI * (isFacingRight ? -0.25f : 0.25f)), jumpForce * Mathf.Cos(Mathf.PI * (isFacingRight ? -0.25f : 0.25f))));
            }
        }
    }
    
    private void ProcessHorizontalInput()
    {
        float input = horizontalInput.ReadValue<float>();
        if (isGrounded && !myDownCollider2D.IsTouchingLayers(LayerMask.GetMask(groundLayerName)) && myFrontCollider2D.IsTouchingLayers(LayerMask.GetMask(groundLayerName)))
        {
            myAnimator.SetBool(grabVar, true);
            myRigidbody2D.velocity = Vector3.zero;
            myRigidbody2D.gravityScale = 0;
        }
        else if (input != 0)
        {
            myAnimator.SetBool(grabVar, false);
            myAnimator.SetBool(runVar, true);
            myRigidbody2D.gravityScale = 1;
            myRigidbody2D.velocity = Vector3.SmoothDamp(myRigidbody2D.velocity, new Vector3(input * runSpeed, myRigidbody2D.velocity.y), ref curVelocity, groundMovementSmoothingFactor);
        }
        else if (isGrounded)
        {
            myRigidbody2D.velocity = Vector3.SmoothDamp(myRigidbody2D.velocity, new Vector3(0, myRigidbody2D.velocity.y), ref curVelocity, groundMovementSmoothingFactor);
            myAnimator.SetBool(grabVar, false);
            myAnimator.SetBool(runVar, false);
            myRigidbody2D.gravityScale = 1;
        }
        else
        {
            myRigidbody2D.velocity = Vector3.SmoothDamp(myRigidbody2D.velocity, new Vector3(0, myRigidbody2D.velocity.y), ref curVelocity, airMovementSmoothingFactor);
            myAnimator.SetBool(grabVar, false);
            myAnimator.SetBool(runVar, false);
            myRigidbody2D.gravityScale = 1;
        }
        if (input > 0 && !isFacingRight)
        {
            Flip();
        }
        else if (input < 0 && isFacingRight)
        {
            Flip();
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.gameObject.layer == LayerMask.NameToLayer(groundLayerName))
        {
            isGrounded = true;
            myAnimator.SetBool(jumpVar, !isGrounded);
        }

    }

    private IEnumerator PlayerDeath()
    {
        GameState.numberOfDeathsThisGame++;
        myRigidbody2D.velocity = Vector2.zero;
        myInputMaster.Disable();
        myAnimator.SetBool(dieVar, true);
        yield return new WaitForSeconds(deathDelay);
        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        spriteRenderer.enabled = false;
        myAnimator.SetBool(dieVar, false);
        transform.position = spawnPoint.position;
        spriteRenderer.enabled = true;
        myInputMaster.Enable();
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        myRigidbody2D.gravityScale = 1;

        if (collision.collider.gameObject.layer == LayerMask.NameToLayer(groundLayerName))
        {
            isGrounded = false;
            myAnimator.SetBool(jumpVar, !isGrounded);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer(damageLayerTag))
        {
            StartCoroutine(PlayerDeath());
        }

        if (collision.gameObject.tag == finishTag)
        {
            myLevelLoader.LoadNextScene();
        }
    }
    
    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 bodyScale = transform.localScale;
        bodyScale.x *= -1;
        transform.localScale = bodyScale;
    }
}
