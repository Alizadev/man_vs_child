using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using CnControls;

public class GameManager : MonoBehaviour
{
    [HideInInspector]
    public PlayerControl _player;
    [HideInInspector]
    public List<ChildControl> _children;

    [HideInInspector]
    public Camera localCam;

    public Text fpsCounter;
    float fpsTimer = 0;

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 60;
        //get camera
        localCam = Camera.main;
        //get the player
        _player = FindObjectOfType<PlayerControl>();
        _player._gameManager = this;
        //get the kids
        ChildControl[] allKids = FindObjectsOfType<ChildControl>();
        _children = new List<ChildControl>();
        foreach (ChildControl cc in allKids)
        {
            cc._gameManager = this;
            _children.Add(cc);
        }
    }

    // Update is called once per frame
    void Update()
    {
        //fps
        if (fpsTimer <= 0)
        {
            fpsTimer = 0.5f;
            fpsCounter.text = (1f / Time.unscaledDeltaTime).ToString("0");
        }
        fpsTimer -= Time.unscaledDeltaTime;
        //quit
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
        //slow mo
        if (CnInputManager.GetButtonDown("C"))
        {
            Time.timeScale = Time.timeScale == 1f ? 0.2f : 1f;
        }
        //camera
        CameraControl();
    }

    void CameraControl()
    {
        Vector3 dirToPlayer = _player.transform.position - localCam.transform.position;
        if (dirToPlayer != Vector3.zero)
        {
            Vector3 finalRot = Quaternion.LookRotation(dirToPlayer).eulerAngles;
            //lock on landscape
            if (Screen.width > Screen.height)
            {
                finalRot.y = Mathf.Clamp(finalRot.y, -80, -60);
            }
            finalRot.x = Mathf.Clamp(finalRot.x, 32, 45);
            localCam.transform.rotation = Quaternion.Lerp(localCam.transform.rotation, Quaternion.Euler(finalRot), 3f * Time.deltaTime);
        }
    }

    public ChildControl GetClosestChild()
    {
        ChildControl closestChild = null;
        float closestDistanceSqr = 20;
        Vector3 currentPosition = _player.transform.position + (_player.transform.forward * 3);
        foreach (ChildControl potentialTarget in _children)
        {
            Vector3 directionToTarget = potentialTarget.hips.transform.position - currentPosition;
            float dSqrToTarget = directionToTarget.sqrMagnitude;
            if (dSqrToTarget < closestDistanceSqr)
            {
                closestDistanceSqr = dSqrToTarget;
                closestChild = potentialTarget;
            }
        }
        return closestChild;
    }
}
