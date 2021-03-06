﻿using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PlayerAction : NetworkBehaviour
{
    public GameObject laserLineRendererPrefab;
    public int laserRange;                  // Range of laser
    public float destroyLaserDelay;         // Time delay before destroying laser
    public Vector3 laserOriginOffset;       // Offset to increase laser visibility
    public Text targetedPlayerText;
    public int targetingRange;              // Range to cover while attempting to find target

    private Transform cam;                  // local camera transform
    private Quaternion serverCamRotation;   // server camera rotation
    private PlayerInfo playerInfo;          // display area for name of targeted player

    private int scorePerHit = 100;          // score gained per player hit

    void Start()
    {
        cam = GetComponentInChildren<Camera>().transform;   // Get position of player camera
        serverCamRotation = cam.rotation;                   // Initialize rotation of camera on server
        playerInfo = GetComponent<PlayerInfo>();            // Get reference to player's info
        laserOriginOffset = new Vector3(0, -.25f, 0);       // Lower origin of raycast so laser is visible
        laserRange = 200;                                   
        destroyLaserDelay = .25f;
        targetingRange = 3 * laserRange;
    }

    void Update()
    {
        if (hasAuthority == false)
        {
            cam.rotation = serverCamRotation;    // update camera's rotation to enable proper raycasts
            return;
        }

        // If pointing at another player, identify them and display on UI
        //Ray ray = new Ray(cam.position, cam.forward);
        //RaycastHit hit;
        //if (Physics.Raycast(ray, out hit, targetingRange) && hit.transform.gameObject.name == "CapGuy")  // cast ray to find another player
        //{
        //    uint targetedPlayerId = hit.transform.parent.gameObject.GetComponent<NetworkIdentity>().netId.Value;
        //    targetedPlayerText.text = "Player " + targetedPlayerId;
        //}
        //else
        //{
        //    targetedPlayerText.text = "";   // if no player found, empty the display text
        //}

        // Fire laser if mouse clicked & we have authority over this player
        if (Input.GetMouseButtonDown(0) && hasAuthority == true)
        {
            CmdCreateLaser();  // create laser on server
        }

        CmdUpdateCameraTransform(cam.rotation);
    }

    // Create visible laser beam on server, then determine if player was hit
    [Command]
    void CmdCreateLaser()
    {
        // Get origin of ray based on player heading
        Ray ray = new Ray(cam.position + laserOriginOffset, cam.forward);
        RaycastHit hit;
        // if raycast hits something
        if (Physics.Raycast(ray, out hit, laserRange))
        {
            RpcCreateLaser(ray.origin, hit.point);              // create visible lasers on clients
            if (hit.transform.gameObject.name == "CapGuy")      // if ray hits a player
            {
                // Get netId from CapGuy model's parent gameObject
                NetworkIdentity hitPlayerIdentity = hit.transform.parent.gameObject.GetComponent<NetworkIdentity>();
                PlayerGameObject localHitPlayer = NetworkServer.FindLocalObject(hitPlayerIdentity.netId).GetComponent<PlayerGameObject>();

                //Checks if death If Death Doesnt Allow for adding more damage and getting more points
                if(localHitPlayer.GetComponent<PlayerInfo>().GetDeath() == false) 
                { 
                    localHitPlayer.GetComponent<PlayerInfo>().RpcRegisterHit(); // register hit on other player
                    playerInfo.IncreaseScore(scorePerHit);                      // increase points for this player
                }
            }
        }
        // If raycast hits nothing
        else
        {
            RpcCreateLaser(ray.origin, cam.forward * laserRange);
        }
    }

    // Create visible laser beam on client
    [ClientRpc]
    void RpcCreateLaser(Vector3 origin, Vector3 point)
    {
        StartCoroutine(CreateLaser(origin, point));
    }

    // Async method for creating and destroying visible laser
    private IEnumerator CreateLaser(Vector3 origin, Vector3 target)
    {
        // Instantiate prefab object containing LineRenderer component
        GameObject laserLineRendererObject = Object.Instantiate(laserLineRendererPrefab);
        // Get the new instance's LineRenderer component
        LineRenderer laserLineRenderer = laserLineRendererObject.GetComponent<LineRenderer>();
        laserLineRenderer.SetPosition(0, target);
        laserLineRenderer.SetPosition(1, origin);
        yield return new WaitForSeconds(destroyLaserDelay);  // Show rendered line for this many seconds...
        Destroy(laserLineRendererObject);       // ...then destroy it and its associated game object
    }

    [Command]
    void CmdUpdateCameraTransform(Quaternion rotation)
    {
        serverCamRotation = rotation;
    }
}
