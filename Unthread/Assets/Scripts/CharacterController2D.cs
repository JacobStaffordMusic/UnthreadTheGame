using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GlobalTypes;

public class CharacterController2D : MonoBehaviour
{

    //CONSTANT VARIABLES

    //raycast positions
    const int LEFT = 0;
    const int MIDDLE = 1;
    const int RIGHT = 2;



    public float raycastDistance = .2f;
    public LayerMask layerMask;
    public float downForceAdjustment = 1.2f;
    public float slopeAngleLimit = 45f;

    //flags
    public bool below;
    private bool disableGroundCheck;
    public bool left, right, above;



    public GroundType groundType;
    public bool hitGroundThisFrame;
    public bool hitWallThisFrame;

    //vectors
    private Vector2 moveAmount;
    private Vector2 currentPosition;
    private Vector2 lastPosition;
    
    private Vector2 slopeNormal;
    private float slopeAngle;
    private bool inAirLastFrame;
    private bool noSideCollisionsLastFrame;

    //components
    private Rigidbody2D myRigidBody2D;
    private CapsuleCollider2D myCapCollider2D;

    //arrays

    //3 elements for behind, underneath, and in front of the player
    private Vector2[] raycastPosition = new Vector2[3];
    private RaycastHit2D[] rayHits = new RaycastHit2D[3];


    // Start is called before the first frame update
    void Start()
    {
        myRigidBody2D = GetComponent<Rigidbody2D>();
        myCapCollider2D = GetComponent<CapsuleCollider2D>();
    }

    // FixedUpdate is standard for physics simulations
    void Update()
    {
        inAirLastFrame = !below;

        noSideCollisionsLastFrame = (!right && !left);
        
        lastPosition = myRigidBody2D.position;

        //this prevents the following code from running unless we are on a slope and in contact with the floor
        if(slopeAngle != 0f && below == true) 
        {
            //this makes sure player is on a downward slope in either left or right directions
            if((moveAmount.x > 0f && slopeAngle > 0f) || (moveAmount.x < 0f && slopeAngle < 0f))
            {
                //we calculate an adjustment on the y axis in the downwards
                //direction based on the Tangent(theta) * the adjacent side. giving us the opposite 
                //side of the triangle which is equal to the sloped platform
                moveAmount.y = -Mathf.Abs(Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * moveAmount.x);
                moveAmount.y *= downForceAdjustment;
            }
        } 

        currentPosition = lastPosition + moveAmount;
        myRigidBody2D.MovePosition(currentPosition);
        moveAmount = Vector2.zero;

        if(!disableGroundCheck)
        {
            CheckGounded();
        }

        CheckOtherCollisions();

        if(below && inAirLastFrame)
        {
            hitGroundThisFrame = true;
        }
        else
        {
            hitGroundThisFrame = false;
        }

        if((right || left) && noSideCollisionsLastFrame)
        {
            hitWallThisFrame = true;
        }
        else
        {
            hitWallThisFrame = false;
        }
    }

    public void Move(Vector2 movement)
    {
        moveAmount += movement;
    }

    private void CheckGounded()
    {
        RaycastHit2D belowHit = Physics2D.CapsuleCast(myCapCollider2D.bounds.center, myCapCollider2D.size, 
            CapsuleDirection2D.Vertical, 0f, Vector2.down, raycastDistance, layerMask);

        if(belowHit.collider)
        {
            groundType = DetermineGroundType(belowHit.collider);
            slopeNormal = belowHit.normal;
            slopeAngle = Vector2.SignedAngle(slopeNormal, Vector2.up);

            if(slopeAngle > slopeAngleLimit || slopeAngle < -slopeAngleLimit)
            {
                below = false;
            }
            else
            {
                below = true;
            }
        }
        else
        {
            groundType = GroundType.None;
            below = false;
        }
    }

    private void CheckOtherCollisions()
    {
        //check left
        RaycastHit2D leftHit = Physics2D.BoxCast(myCapCollider2D.bounds.center, myCapCollider2D.size * 0.6f,
            0f, Vector2.left, raycastDistance * 2, layerMask);
        
        if(leftHit.collider)
        {
            left = true;
        }
        else
        {
            left = false;
        }

        //check right
        RaycastHit2D rightHit = Physics2D.BoxCast(myCapCollider2D.bounds.center, myCapCollider2D.size * 0.6f,
            0f, Vector2.right, raycastDistance * 2, layerMask);
        
        if(rightHit.collider)
        {
            right = true;
        }
        else
        {
            right = false;
        }


        //check above
        RaycastHit2D aboveHit = Physics2D.CapsuleCast(myCapCollider2D.bounds.center, myCapCollider2D.size, 
            CapsuleDirection2D.Vertical, 0f, Vector2.up, raycastDistance, layerMask);
        
        if(aboveHit.collider)
        {
            above = true;
        }
        else
        {
            above = false;
        }
    }
/*
    private void CheckGounded()
    {
        Vector2 raycastOrigin = myRigidBody2D.position - new Vector2(0, myCapCollider2D.size.y * 0.5f);
        raycastPosition[LEFT] = raycastOrigin + (Vector2.left * myCapCollider2D.size.x * 0.25f + Vector2.up * 0.086f);
        raycastPosition[MIDDLE] = raycastOrigin;
        raycastPosition[RIGHT] = raycastOrigin + (Vector2.right * myCapCollider2D.size.x * 0.25f + Vector2.up * 0.086f);

        DrawDebugRays(Vector2.down, Color.green);

        int numberOfGroundHits = 0;

        for(int i = 0; i < raycastPosition.Length; i++)
        {
            RaycastHit2D hit = Physics2D.Raycast(raycastPosition[i], Vector2.down, raycastDistance, layerMask);

            if(hit.collider)
            {
                rayHits[i] = hit;
                numberOfGroundHits++;
            }
        }
        
        if(numberOfGroundHits > 0)
        {
            if(rayHits[MIDDLE].collider)
            {
                groundType = DetermineGroundType(rayHits[1].collider);
                slopeNormal = rayHits[MIDDLE].normal;
                slopeAngle = Vector2.SignedAngle(slopeNormal, Vector2.up);
            }
            else
            {
                for(int i = 0; i < rayHits.Length; i++)
                {
                    if(rayHits[i].collider)
                    {
                        groundType = DetermineGroundType(rayHits[i].collider);
                        slopeNormal = rayHits[i].normal;
                        slopeAngle = Vector2.SignedAngle(slopeNormal, Vector2.up);
                    }
                }
            }

            if(slopeAngle > slopeAngleLimit || slopeAngle < -slopeAngleLimit)
            {
                below = false;
            }
            else
            {
                below = true;
            }
        }
        else
        {
            groundType = GroundType.None;
            below = false;
        }

        System.Array.Clear(rayHits, 0, rayHits.Length);
    }*/

    private void DrawDebugRays(Vector2 direction, Color color)
    {
        for (int i = 0; i < raycastPosition.Length; i++)
        {
            Debug.DrawRay(raycastPosition[i], direction * raycastDistance, color);
        }
    }

    public void DisableGroundCheck()
    {
        below = false;
        disableGroundCheck = true;
        StartCoroutine("EnableGroundCheck");
    }

    IEnumerator EnableGroundCheck()
    {
        yield return new WaitForSeconds(0.1f);
        disableGroundCheck = false;
    }

    private GroundType DetermineGroundType(Collider2D collider)
    {
        if(collider.GetComponent<GroundEffector>())
        {
            GroundEffector groundEffector = collider.GetComponent<GroundEffector>();
            return groundEffector.groundType;
        }
        else
        {
            return GroundType.LevelGeometry;
        }
    }
}
