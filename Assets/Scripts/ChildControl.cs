using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChildControl : MonoBehaviour
{
    public Animator _anim;
    public RagdollHelper _ragdollHelper;
    public ActiveRagdoll _activeRagdoll;
    public Rigidbody hips;
    public Rigidbody m_rigidbody;


    Rigidbody grabItem = null;
    bool pickingUp = false;

    Vector3 targetToMove = Vector3.zero;
    Vector3 targetVel = Vector3.zero;

    float ragdollTimer = 0;

    [HideInInspector]
    public GameManager _gameManager;
    // Start is called before the first frame update
    void Start()
    {
        InvokeRepeating("RandomLocation", 1, 3f);
    }

    // Update is called once per frame
    void Update()
    {
        //auto get up
        if (_ragdollHelper.ragdolled && hips.velocity.magnitude < 2f)
        {
            ragdollTimer += Time.deltaTime;
            if (ragdollTimer > 3f)
            {
                ragdollTimer = -99f;
                StartCoroutine(GetUp());
            }
        }
        else
        {
            ragdollTimer = 0;
        }
        //anim
        Vector3 relativeVel = transform.InverseTransformDirection(m_rigidbody.velocity);
        _anim.SetFloat("InputY", Mathf.Lerp(_anim.GetFloat("InputY"), Mathf.Clamp(relativeVel.z, 0, 1f), 3f * Time.deltaTime));
        //teleport back
        if (transform.position.y < -3f)
        {
            _ragdollHelper.ragdolled = true;
            transform.position = Vector3.up * 5;
            hips.velocity = Vector3.down;
        }
    }

    IEnumerator GetUp()
    {
        Debug.Log("Getting Up..");
        yield return new WaitUntil(() => _activeRagdoll.gameObject.activeSelf);
        if (hips.transform.forward.y > 0)
        {
            _activeRagdoll._anim.CrossFadeInFixedTime("Sleep", 1f, 0, 12f);
        }
        else
        {
            _activeRagdoll._anim.CrossFadeInFixedTime("Sleep", 1f);
        }
        yield return new WaitForSeconds(Random.Range(4f, 8f));
        //get up
        _ragdollHelper.ragdolled = false;
        //animation
        if (hips.transform.forward.y > 0)
        {
            _anim.CrossFadeInFixedTime("GetUpFront", 0.1f);
        }
        else
        {
            _anim.CrossFadeInFixedTime("GetUpBack", 0.1f);
        }
    }

    private void FixedUpdate()
    {
        if (targetToMove != Vector3.zero)
        {
            Vector3 dir = (targetToMove - transform.position).normalized;
            dir.y = m_rigidbody.velocity.y;
            dir.y += Physics.gravity.y * Time.fixedDeltaTime;
            //lerp vel
            targetVel = Vector3.Lerp(targetVel, dir, 3f * Time.fixedDeltaTime);
            Debug.DrawRay(transform.position + Vector3.up, targetVel, Color.green);
            //stop when ragdolled
            if (hips.isKinematic == false)
            {
                return; 
            }
            //stop when getting up
            if (_anim.isInitialized)
            {
                if (_anim.GetCurrentAnimatorStateInfo(0).IsName("GetUpFront") ||
                   _anim.GetCurrentAnimatorStateInfo(0).IsName("GetUpBack") ||
                   _anim.IsInTransition(0))
                {
                    return;
                }
            }
            //move
            m_rigidbody.velocity = targetVel * 1.5f;
            dir.y = 0;
            m_rigidbody.MoveRotation(Quaternion.Lerp(m_rigidbody.rotation, Quaternion.LookRotation(dir), 3f * Time.fixedDeltaTime));
        }
        //
        if (grabItem)
        {
            grabItem.velocity = ((transform.position + (Vector3.up * 1.5f)) - grabItem.position) * 20f;
            //rotate
            grabItem.rotation = transform.rotation;
            //drop
            if (_ragdollHelper.ragdolled)
            {
                grabItem = null;
            }
        }
    }

    void RandomLocation()
    {
        Vector3 origin = transform.position + (Vector3.up * 2);
        Vector3 randomDir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
        if (Physics.Raycast(origin, randomDir, out RaycastHit hit, 100f, ~LayerMask.GetMask("Player", "NPC", "Ragdoll")))
        {
            targetToMove = hit.point;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.root.tag == "Pickable")
        {
            //pick up
            if (pickingUp == false)
            {
                //StartCoroutine(PickUp(collision.transform.root.GetComponent<Rigidbody>()));
            }
        }
    }

    IEnumerator PickUp(Rigidbody _item)
    {
        pickingUp = true;
        _item.detectCollisions = false;
        grabItem = _item;
        yield return new WaitForSeconds(0.5f);
        _item.detectCollisions = true;
        pickingUp = false;
    }
}
