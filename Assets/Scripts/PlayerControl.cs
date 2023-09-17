using CnControls;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerControl : MonoBehaviour
{
    public bool grabbing = false;
    bool pickingUp = false;
    float searchCoolDown = 0.5f;

    public Transform grabGuide = null;
    [HideInInspector]
    public Rigidbody grabItem = null;

    public bool throwing = false;

    [Header("~~~PLAYER~~~")]
    public Animator _anim;
    public Rigidbody m_rigidbody;

    [Header("~~~TWEAK~~~")]
    public float playerMoveSmooth = 10f;
    public float playerRotSmooth = 10f;
    public float playerAnimSmooth = 10f;

    [HideInInspector]
    public float inputX, inputY, joystick;

    [HideInInspector]
    public Vector3 moveDirection = Vector3.zero;

    Vector3 rootMotionVel = Vector3.zero;

    [HideInInspector]
    public GameManager _gameManager;

    ChildControl targetChild = null;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        ////grab
        //if (CnInputManager.GetButtonDown("E"))
        //{
        //    grabbing = !grabbing;
        //}
        //throw
        if (CnInputManager.GetButtonDown("Jump"))
        {
            if (grabbing && grabItem)
            {
                if (!throwing)
                {
                    StartCoroutine(ThrowObject());
                }
            }
            else
            {
                grabbing = !grabbing;
            }
        }
        Movement();
        Animation();
    }

    void FixedUpdate()
    {
        //grab
        if (grabItem)
        {
            if (grabbing)
            {
                //hold
                grabItem.velocity = ((grabGuide.position + (transform.forward * joystick * 0.15f)) - grabItem.position) * (throwing ? 50f : 20f);
                //rotate
                grabItem.rotation = grabGuide.rotation;
                //grabItem.MoveRotation(grabGuide.rotation);
            }
            else
            {
                //DropItem();
            }
        }
        else
        {
            //search for item
            if (searchCoolDown <= 0 && grabbing && pickingUp == false)
            {
                searchCoolDown = 0.5f;
                Collider[] cols = Physics.OverlapSphere(transform.position + Vector3.up, 1f, ~LayerMask.GetMask("Player"));
                foreach (Collider col in cols)
                {
                    if (col.transform.root.tag == "Pickable" && pickingUp == false)
                    {
                        StartCoroutine(GrabObject(col.transform.root.GetComponent<Rigidbody>()));
                    }
                }
            }
            searchCoolDown -= Time.deltaTime;
        }
        ApplyMovement();
    }

    void ApplyMovement()
    {
        //move
        Vector3 gravity = Vector3.zero;
        gravity.y = m_rigidbody.velocity.y;
        gravity.y += Physics.gravity.y * Time.fixedDeltaTime;
        m_rigidbody.velocity = (throwing ? rootMotionVel : moveDirection) + gravity;

        //aim the child
        if (throwing && targetChild)
        {
            Vector3 dirToChild = targetChild.hips.transform.position - transform.position;
            dirToChild.y = 0;
            if (dirToChild != Vector3.zero)
            {
                m_rigidbody.MoveRotation(Quaternion.Lerp(m_rigidbody.rotation, Quaternion.LookRotation(dirToChild), playerRotSmooth * Time.deltaTime));
            }
        }
        //follow joystick
        else
        {
            if (moveDirection != Vector3.zero)
            {
                m_rigidbody.MoveRotation(Quaternion.Lerp(m_rigidbody.rotation, Quaternion.LookRotation(moveDirection), playerRotSmooth * Time.deltaTime));
            }
        }
    }

    void Movement()
    {
        //movment input
        inputX = CnInputManager.GetAxis("Horizontal");
        inputY = CnInputManager.GetAxis("Vertical");
        //total
        joystick = Vector3.ClampMagnitude(new Vector3(inputX, 0, inputY), 1f).magnitude;
        //Matches with camera
        Vector3 Helper = _gameManager.localCam.transform.TransformDirection(Vector3.forward);
        Helper.y = 0;
        Helper = Helper.normalized;
        Vector3 right = new Vector3(Helper.z, 0, -Helper.x);
        //speed
        float playerSpeed = 2f;
        if (grabbing)
        {
            playerSpeed = 1.4f;
        }
        if (pickingUp)
        {
            playerSpeed = 0.1f;
        }
        //grounded
        if (joystick > 0.5f)
        {
            //move dir
            moveDirection = Vector3.Lerp(moveDirection, (inputX * right + inputY * Helper) * playerSpeed, playerMoveSmooth * Time.deltaTime);
        }
        else
        {
            moveDirection = Vector3.Lerp(moveDirection, Vector3.zero, playerMoveSmooth * Time.deltaTime);
        }
        Debug.DrawRay(transform.position + Vector3.up * 1.8f, moveDirection, Color.yellow);
    }

    void Animation()
    {
        Vector3 rigDir = transform.InverseTransformDirection(m_rigidbody.velocity);
        _anim.SetFloat("InputX", Mathf.Lerp(_anim.GetFloat("InputX"), Mathf.Clamp(rigDir.x, -1f, 1f), playerAnimSmooth * Time.deltaTime));
        _anim.SetFloat("InputY", Mathf.Lerp(_anim.GetFloat("InputY"), Mathf.Clamp01(rigDir.z), playerAnimSmooth * Time.deltaTime));

        //grabbing
        if (grabbing)
        {
            _anim.SetLayerWeight(1, Mathf.Lerp(_anim.GetLayerWeight(1), 1f, 5f * Time.deltaTime));
        }
        else
        {
            _anim.SetLayerWeight(1, Mathf.Lerp(_anim.GetLayerWeight(1), 0f, 5f * Time.deltaTime));
        }
    }

    IEnumerator GrabObject(Rigidbody _item)
    {
        pickingUp = true;
        //if low do pick up animation
        if (_item.transform.position.y < 0.5f)
        {
            _anim.CrossFadeInFixedTime("PickUp", 0.2f);
            yield return new WaitForSeconds(0.4f);
        }
        else
        {
            yield return new WaitForSeconds(0.1f);
        }
        //cancel
        if (grabbing == false)
        {
            pickingUp = false;
            yield break;
        }
        //disable collision
        _item.detectCollisions = false;
        //hold
        grabItem = _item;
        grabbing = true;
        pickingUp = false;
    }

    IEnumerator ThrowObject()
    {
        if (grabItem == null)
        {
            yield break;
        }
        //get the closest child
        targetChild = _gameManager.GetClosestChild();
        float distToChild = Random.Range(0, 10);
        if (targetChild)
        {
            distToChild = (targetChild.hips.transform.position - transform.position).magnitude;
            Debug.DrawLine(transform.position + Vector3.up, targetChild.hips.transform.position, Color.red, 3f);
        }
        //throw
        throwing = true;
        if (distToChild < 5)
        {
            _anim.CrossFadeInFixedTime("Throw_In", 0.2f);
            yield return new WaitForSeconds(1.1f);
        }
        else
        {
            _anim.CrossFadeInFixedTime("Throw_Out", 0.2f);
            yield return new WaitForSeconds(0.85f);
        }
        //force
        if (grabItem)
        {
            if (targetChild)
            {
                grabItem.velocity = (targetChild.hips.transform.position - grabItem.transform.position).normalized * Random.Range(10f, 15f);
            }
            else
            {
                grabItem.velocity = transform.forward * Random.Range(10f, 15f);
            }
            grabItem.AddTorque(transform.right * Random.Range(50, 300), ForceMode.Impulse);
        }
        grabbing = false;
        throwing = false;
        DropItem();
    }

    void DropItem()
    {
        if (grabItem)
        {
            //enable collision
            grabItem.detectCollisions = true;
            grabItem = null;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.root.tag == "NPC")
        {
            if (collision.relativeVelocity.magnitude > 1)
            {
                ChildControl cc = collision.transform.root.GetComponent<ChildControl>();
                cc._ragdollHelper.ragdolled = true;
                cc.hips.velocity += transform.forward * 5;
            }
        }
    }

    private void OnAnimatorMove()
    {
        rootMotionVel = _anim.deltaPosition / Time.deltaTime;
    }

    void OnAnimatorIK()
    {
        if (grabItem)
        {
            float ikWeight = _anim.GetLayerWeight(1) * 0.5f;
            if (_anim.GetCurrentAnimatorStateInfo(1).IsName("Throw_Out"))
            {
                ikWeight = 0;
            }
            //pos
            _anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, ikWeight);
            //_anim.SetIKPositionWeight(AvatarIKGoal.RightHand, _anim.GetLayerWeight(1));
            _anim.SetIKPosition(AvatarIKGoal.LeftHand, grabItem.transform.position - (grabItem.transform.right * 0.5f));
            //_anim.SetIKPosition(AvatarIKGoal.RightHand, grabItem.transform.position + (grabItem.transform.right * 0.5f));

            //rot
            _anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, ikWeight);
            //_anim.SetIKRotationWeight(AvatarIKGoal.RightHand, _anim.GetLayerWeight(1));
            _anim.SetIKRotation(AvatarIKGoal.LeftHand, grabItem.transform.rotation * Quaternion.Euler(-45, 0, 90));
            //_anim.SetIKRotation(AvatarIKGoal.RightHand, grabItem.transform.rotation * Quaternion.Euler(-45, 0, -90));

            //hint
            //_anim.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, ikWeight);
            //_anim.SetIKHintPosition(AvatarIKHint.LeftElbow, grabItem.transform.position + (transform.right * 1f));
        }
    }
}
