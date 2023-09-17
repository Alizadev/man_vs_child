using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowItem : MonoBehaviour
{
    float dmgCooldown = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void Update()
    {
        dmgCooldown -= Time.deltaTime;

        //teleport back
        if (transform.position.y < -3f)
        {
            transform.position = Vector3.up * 5;
            GetComponent<Rigidbody>().velocity = Vector3.down;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (dmgCooldown <= 0 && collision.relativeVelocity.magnitude > 2f)
        {
            if (collision.transform.root.tag == "NPC")
            {
                dmgCooldown = 1f;
                StartCoroutine(ImpactDamage(collision.transform.root.GetComponent<ChildControl>(), collision.contacts[0].point, collision.relativeVelocity.magnitude));
                //Debug.Log(collision.relativeVelocity.magnitude.ToString("0.0 Force"));
            }
        }
    }

    IEnumerator ImpactDamage(ChildControl _kid, Vector3 _hitPoint, float _force)
    {
        //clamp
        _force = Mathf.Clamp(_force, 1f, 15f);
        //ragdoll
        _kid._ragdollHelper.ragdolled = true;
        //impact force
        yield return new WaitForSeconds(0.05f);
        foreach (Rigidbody rg in _kid._ragdollHelper.rigids)
        {
            if (rg.name.Contains("Head") || rg.name.Contains("Spine"))
            {
                rg.AddForce(_hitPoint * _force, ForceMode.Impulse);
            }
        }
    }
}
