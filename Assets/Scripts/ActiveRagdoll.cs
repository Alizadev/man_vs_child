using UnityEngine;
using System.Collections;

public class ActiveRagdoll : MonoBehaviour
{

    [Range(0, 5000)]
    public float spring = 5000;

    public ConfigurableJoint[] playerParts;
    public Transform[] targetParts;

    private Quaternion[] startRots;

    public Animator _anim;
    public LayerMask rayMask;

    public Rigidbody hips;


    void Start()
    {
        //disable culling
        _anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        startRots = new Quaternion[playerParts.Length];
        for (int i = 0; i < playerParts.Length; i++)
        {
            startRots[i] = targetParts[i].localRotation;
        }
        foreach (ConfigurableJoint cj in playerParts)
        {
            //self collision
            cj.enableCollision = false;
            //preprocessing
            cj.enablePreprocessing = false;
            //projection
            cj.projectionMode = JointProjectionMode.PositionAndRotation;
            //rotations
            cj.angularXMotion = ConfigurableJointMotion.Free;
            cj.angularYMotion = ConfigurableJointMotion.Free;
            cj.angularZMotion = ConfigurableJointMotion.Free;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (hips.isKinematic)
        {
            //spring = 500;
            if (_anim.gameObject.activeSelf)
            {
                _anim.gameObject.SetActive(false);
                foreach (ConfigurableJoint cj in playerParts)
                {
                    //X axis angle spring
                    var sp = cj.angularXDrive;
                    sp.positionSpring = 10;
                    cj.angularXDrive = sp;
                    //Z and Y axis spring
                    var spYZ = cj.angularYZDrive;
                    spYZ.positionSpring = 10;
                    cj.angularYZDrive = spYZ;
                }
            }
        }
        else
        {
            //spring = 500;
            foreach (ConfigurableJoint cj in playerParts)
            {
                //X axis angle spring
                var sp = cj.angularXDrive;
                sp.positionSpring = Mathf.Lerp(sp.positionSpring, spring, 3f * Time.deltaTime);
                cj.angularXDrive = sp;
                //Z and Y axis spring
                var spYZ = cj.angularYZDrive;
                spYZ.positionSpring = Mathf.Lerp(spYZ.positionSpring, spring, 3f * Time.deltaTime);
                cj.angularYZDrive = spYZ;
            }

            for (int i = 0; i < playerParts.Length; i++)
            {
                var tp = targetParts[i].localRotation;
                playerParts[i].SetTargetRotationLocal(tp, startRots[i]);
            }
            if (!_anim.gameObject.activeSelf)
            {
                _anim.gameObject.SetActive(true);
                _anim.CrossFadeInFixedTime("Crawl", 0f);
            }
            //animation
            _anim.SetFloat("Crawl", Mathf.MoveTowards(_anim.GetFloat("Crawl"), Mathf.Round(-hips.transform.forward.y), 5f * Time.deltaTime));
            _anim.SetLayerWeight(1, Mathf.Lerp(_anim.GetLayerWeight(1), Mathf.Clamp01(hips.velocity.magnitude * 0.5f), 2f * Time.deltaTime));
        }
    }
}
