using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using GlobalTypes;

public class PlayerMovement : MonoBehaviour
{
    #region public properties
    [Header("Player Properties")]
    public float walkSpeed = 20f;
    public float creepSpeed = 10f;
    public float gravity = 150f;
    public float jumpSpeed = 50f;
    public float extraJumpSpeed = 40f;
    public float xWallJumpSpeed = 50f;
    public float yWallJumpSpeed = 50f;
    public float wallRunAmount = 10f;
    public float wallSlideAmount = 0.1f;
    public float glideTime = 2f;
    public float glideDescentAmount = 2f;
    public float powerJumpSpeed = 100f;
    public float powerJumpWaitTime = 0.8f;
    public float dashSpeed = 20f;
    public float dashTime = 0.2f;
    public float dashCoolDownTime = 1f;
    public float groundSlamSpeed = 100f;
    public float deadzoneAdjustment = 0.15f;

    //player ability toggles
    [Header("Player Abilities")]
    public bool canDoubleJump;
    public bool canTripleJump;
    public bool canWallJump;
    public bool canJumpAfterWallJump;
    public bool canWallRun;
    public bool canMultipleWallRun;
    public bool canWallSlide;
    public bool canGlide;
    public bool canGlideAfterWallContact;
    public bool canPowerJump;
    public bool canGroundDash;
    public bool canAirDash;
    public bool canGroundSlam;


    //player state
    [Header("Player State")]
    public bool isJumping;
    public bool isDoubleJumping;
    public bool isTripleJumping;
    public bool isWallJumping;
    public bool isWallRunning;
    public bool isWallSliding;
    public bool isCrouching;
    public bool isCreeping;
    public bool isGliding;
    public bool isPowerJumping;
    public bool isDashing; //very dashing indeed
    public bool isGroundSlamming;
    #endregion


    #region private properties
    //input flags
    private bool startJump;
    private bool releaseJump;

    //private variables
    private bool ableToWallRun = true;
    private Vector2 input;
    private Vector2 moveDirection;
    private float currentGlideTime;
    private bool startGlide = true;
    private float powerJumpTimer;
    private bool facingRight;//true is right, false is left;
    private float dashTimer;
    private CharacterController2D myCharController2D;
    private CapsuleCollider2D myCapCollider2D;
    private Vector2 originalColliderSize;
    private RaycastHit2D hitCeilingWhileCrouching;
    //TODO: remove later when not needed
    private SpriteRenderer mySpriteRenderer;
    #endregion


    void Start()
    {
        myCharController2D = gameObject.GetComponent<CharacterController2D>();
        myCapCollider2D = gameObject.GetComponent<CapsuleCollider2D>();
        mySpriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        originalColliderSize = myCapCollider2D.size;
    }

    void Update()
    {

        if(dashTimer > 0f) dashTimer -= Time.deltaTime;

        //better input processing for gamepad use
        ApplyDeadzones();

        ProcessHorizontalMovement();

        if(myCharController2D.below) // On the ground
        {
            OnGround();
        }
        else // In the air
        {
            InAir();
        }


        myCharController2D.Move(moveDirection * Time.deltaTime);

    }

    #region methods
    private void OnGround()
    {
        //clear any downward motion when on the ground
        moveDirection.y = 0f;

        ClearAirAbilityFlags();
        
        Jump();
        
        CrouchingAndCreeping();        
    }

    private void ClearAirAbilityFlags()
    {
        //clear flags for in air abilities
        isJumping = false;
        isDoubleJumping = false;
        isTripleJumping = false;
        isWallJumping = false;
        currentGlideTime = glideTime;
        isGroundSlamming = false;
        startGlide = true;
    }
    
    private void Jump()
    {
        //jumping
        if(startJump)
        {
            startJump = false;

            if(canPowerJump && isCrouching && 
                myCharController2D.groundType != GroundType.OneWayPlatform && (powerJumpTimer > powerJumpWaitTime))
            {
                moveDirection.y = powerJumpSpeed;
                StartCoroutine("PowerJumpWaiter");
            }
            else
            {
                moveDirection.y = jumpSpeed;
            }
            isJumping = true;
            myCharController2D.DisableGroundCheck();
            ableToWallRun = true;
        }
    }

    private void CrouchingAndCreeping()
    {
        //crouching and creeping
        if(input.y < 0f)
        {
            if(!isCrouching && !isCreeping)
            {
                myCapCollider2D.size = new Vector2(myCapCollider2D.size.x, myCapCollider2D.size.y / 2);
                transform.position = new Vector2(transform.position.x, transform.position.y - (originalColliderSize.y / 4));
                isCrouching = true;
                mySpriteRenderer.sprite = Resources.Load<Sprite>("directionSpriteUp_crouching");
            }

            powerJumpTimer += Time.deltaTime;
        }
        else
        {
            if(isCrouching || isCreeping)
            {
                RayCastWhileCrouching();

                if(!hitCeilingWhileCrouching.collider)
                {
                    ReturnToOriginalSize();
                }
            }

            powerJumpTimer = 0f;
        }

        if(isCrouching && moveDirection.x != 0)
        {
            isCreeping = true;
        }
        else
        {
            isCreeping = false;
        }
    }

    private void InAir()
    {
        ClearGroundAbilityFlags();

        AirJump();
        
        WallRunning();
        
        GravityCalculations();
    }

    private void WallRunning()
    {
        //wall running
        if(canWallRun && (myCharController2D.left || myCharController2D.right))
        {
            if(input.y > 0f && ableToWallRun)
            {
                moveDirection.y = wallRunAmount;

                if(myCharController2D.left)
                {
                    transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                }
                else if( myCharController2D.right)
                {
                    transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                }

                StartCoroutine("WallRunWaiter");
            }
        }
        else
        {
            if(canMultipleWallRun)
            {
                StopCoroutine("WallRunWaiter");
                ableToWallRun = true;
                isWallRunning = false;
            }
        }

        //can glide after wall contact
        if((myCharController2D.left || myCharController2D.right) && canWallRun)
        {
            if(canGlideAfterWallContact)
            {
                currentGlideTime = glideTime;
            }
            else
            {
                currentGlideTime = 0f;
            }
        }
    }
    
    private void AirJump()
    {
        if(releaseJump)
        {
            releaseJump = false;
            if(moveDirection.y > 0)
            {
                moveDirection.y *= 0.5f;
            }
        }

        //pressed jump button in air
        if(startJump)
        {
            //triple jump if NOT in contact with the wall
            if(canTripleJump && (!myCharController2D.left && !myCharController2D.right))
            {
                if(isDoubleJumping && !isTripleJumping)
                {
                    moveDirection.y = extraJumpSpeed;
                    isTripleJumping = true;
                }
            }
            //double jump if NOT in contact with the wall
            if(canDoubleJump && (!myCharController2D.left && !myCharController2D.right))
            {
                if(!isDoubleJumping)
                {
                    moveDirection.y = extraJumpSpeed;
                    isDoubleJumping = true;
                }
            }
            //wall jumping
            if(canWallJump && (myCharController2D.left || myCharController2D.right))
            {
                if(moveDirection.x <= 0 && myCharController2D.left)
                {
                    moveDirection.x = xWallJumpSpeed;
                    moveDirection.y = yWallJumpSpeed;
                    transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                }
                else if(moveDirection.x >= 0 && myCharController2D.right)
                {
                    moveDirection.x = -xWallJumpSpeed;
                    moveDirection.y = yWallJumpSpeed;
                    transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                }

                //isWallJumping = true;

                StartCoroutine("WallJumpWaiter");
                if(canJumpAfterWallJump)
                {
                    isDoubleJumping = false;
                    isTripleJumping = false;
                }
            }

            startJump = false;
        }
    }

    private void ClearGroundAbilityFlags()
    {
        if((isCrouching || isCreeping) && moveDirection.y > 0f)
        {
            StartCoroutine("ClearCrouchingState");
        }

        //clear our jump timer
        powerJumpTimer = 0f;
    }

    private void ApplyDeadzones()
    {
        if(input.x > -deadzoneAdjustment && input.x < deadzoneAdjustment)
        {
            input.x = 0f;
        }

        if(input.y > -deadzoneAdjustment && input.y < deadzoneAdjustment)
        {
            input.y = 0f;
        }
    }
    
    private void ProcessHorizontalMovement()
    {
        if(!isWallJumping)
        {
            moveDirection.x = input.x;

            if(moveDirection.x < 0f)
            {
                transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                facingRight = false;
            }
            else if(moveDirection.x > 0f)
            {
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                facingRight = true;

            }
            
            if(isDashing)
            {
                if(facingRight)
                {
                    moveDirection.x = dashSpeed;
                }
                else
                {
                    moveDirection.x = -dashSpeed;
                }
                moveDirection.y = 0f;
            }
            else if(isCreeping)
            {
                moveDirection.x *= creepSpeed;
            }
            else
            {
                moveDirection.x *= walkSpeed;
            }
        }
    }
    
    private void RayCastWhileCrouching()
    {
        hitCeilingWhileCrouching = Physics2D.CapsuleCast(myCapCollider2D.bounds.center, 
                    transform.localScale, CapsuleDirection2D.Vertical, 0f, Vector2.up, originalColliderSize.y / 2, 
                    myCharController2D.layerMask);
    }

    private void ReturnToOriginalSize()
    {
        myCapCollider2D.size = originalColliderSize;
        transform.position = new Vector2(transform.position.x, transform.position.y + (originalColliderSize.y / 4));
        mySpriteRenderer.sprite = Resources.Load<Sprite>("directionSpriteUp");
        isCrouching = false;
        isCreeping = false;
    }

    private void GravityCalculations()
    {
        //detects if something above player
        if(moveDirection.y > 0f && myCharController2D.above)
        {
            moveDirection.y = 0f;
        }

        //apply wall slide adjustment
        if(canWallSlide && (myCharController2D.left || myCharController2D.right))
        {
            if(myCharController2D.hitWallThisFrame)
            {
                moveDirection.y = 0f;
                Debug.Log("Hit wall this frame is true");
            }

            if(moveDirection.y <= 0f)
            {
                moveDirection.y -= (gravity * wallSlideAmount) * Time.deltaTime;
            }
            else
            {
                moveDirection.y -= gravity * Time.deltaTime;
            }
        }
        else if(canGlide && input.y > 0f && moveDirection.y < 0.2f) // glide adjustment
        {
            if(currentGlideTime > 0f)
            {
                isGliding = true;

                if(startGlide)
                {
                    moveDirection.y = 0f;
                    startGlide = false;
                }

                moveDirection.y -= glideDescentAmount * Time.deltaTime;
                currentGlideTime -= Time.deltaTime;
            }
            else
            {
                isGliding = false;
                moveDirection.y -= gravity * Time.deltaTime;
            }
        }
        //else if(canGroundSlam && !isPowerJumping && input.y < 0f && moveDirection.y < 0f) //ground slam
        else if(isGroundSlamming && !isPowerJumping && moveDirection.y < 0f)
        {
            moveDirection.y = -groundSlamSpeed;
        }
        else if (!isDashing)//regular gravity
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

    }
    #endregion

    #region input
    //Input methods
    public void OnMovement(InputAction.CallbackContext context)
    {
        input = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if(context.started)
        {
            startJump = true;
            releaseJump = false;
        }
        else if (context.canceled)
        {
            releaseJump = true;
            startJump = false;
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if(context.started && dashTimer <= 0f)
        {
            if((canAirDash && !myCharController2D.below) || (canGroundDash && myCharController2D.below))
            {
                StartCoroutine("Dash");
            }
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if(context.performed && input.y < 0f)
        {
            if(canGroundSlam)
            {
                isGroundSlamming = true;
            }
        }
    }
    #endregion

    #region coroutines
    //coroutines
    IEnumerator WallJumpWaiter()
    {
        isWallJumping = true;
        yield return new WaitForSeconds(0.4f);
        isWallJumping = false;
    }

    IEnumerator WallRunWaiter()
    {
        isWallRunning = true;
        yield return new WaitForSeconds(0.5f);
        isWallRunning = false;
        if(!isWallJumping)
        {
            ableToWallRun = false;
        }
    }

    IEnumerator ClearCrouchingState()
    {
        yield return new WaitForSeconds(0.05f);
        RayCastWhileCrouching();
        if(!hitCeilingWhileCrouching)
        {
            ReturnToOriginalSize();
        }
    }

    IEnumerator PowerJumpWaiter()
    {
        isPowerJumping = true;
        yield return new WaitForSeconds(0.8f);
        isPowerJumping = false;
    }

    IEnumerator Dash()
    {
        isDashing = true;
        yield return new WaitForSeconds(dashTime);
        isDashing = false;
        dashTimer = dashCoolDownTime;
    }
    #endregion
}