using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System;
using Smooth;

/// <summary>
/// An example player controller that works with or without a rigidbody.
/// </summary>
/// <remarks>
/// Use arrow keys to move the parent. Use WASD to move the child.
/// Use + and - with left shift to scale the parent, use right shift to scale the child.
/// Press 't' to teleport. 
/// Press '0' to stop instantly.
/// </remarks>
public class SmoothSyncExamplePlayerController : NetworkBehaviour
{
    Rigidbody rb;
    Rigidbody2D rb2D;
    SmoothSync smoothSync;

    /// <summary>
    /// The speed to move per second when there is no rigidbody component on the player
    /// </summary>
    public float transformMovementSpeed = 30.0f;

    /// <summary>
    /// The force to add on key up when there is a rigidbody component on the player
    /// </summary>
    public float rigidbodyMovementForce = 500;

    public GameObject childObjectToControl;

    /// <summary>
    /// Cache a reference to the rigidbody component and Smooth Sync scripts.
    /// </summary>
	void Start ()
    {
        rb = GetComponent<Rigidbody>();
        rb2D = GetComponent<Rigidbody2D>();
        smoothSync = GetComponent<SmoothSync>();
        // Set up a validation method to check incoming States to see if cheating may be happening. 
        smoothSync.validateStateMethod = validateStateOfPlayer;
    }

    /// <summary>
    /// Simple movement. 
    /// <remarks>
    /// Adds rigidbodyMovementForce on keypress if there is a Rigidbody. 
    /// Shifts position based on transformMovementSpeed if there is no Rigidbody.
    /// Arrow keys control the parent rigidbody forces while WASD controls the child transform.
    /// Use + and - with left shift to scale the parent, use + and - with right shift to scale the child.
    /// Press '0' to stop instantly.
    /// </remarks>
    /// </summary>
	void Update ()
    {
        if (!hasAuthority) return;

        #region Movement Controls

        float speed = transformMovementSpeed * Time.deltaTime;

        // Scale changes here
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.Equals))
        {
            transform.localScale = transform.localScale + new Vector3(1, 1, 1) * speed * .2f;
        }
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.Minus))
        {
            transform.localScale = transform.localScale - new Vector3(1, 1, 1) * speed * .2f;
        }
        if (childObjectToControl)
        {
            if (Input.GetKey(KeyCode.RightShift) && Input.GetKey(KeyCode.Equals))
            {
                childObjectToControl.transform.localScale = childObjectToControl.transform.localScale + new Vector3(1, 1, 1) * speed * .2f;
            }
            if (Input.GetKey(KeyCode.RightShift) && Input.GetKey(KeyCode.Minus))
            {
                childObjectToControl.transform.localScale = childObjectToControl.transform.localScale - new Vector3(1, 1, 1) * speed * .2f;
            }
        }

        // Child position changes here
        if (childObjectToControl)
        {
            if (Input.GetKey(KeyCode.S))
            {
                childObjectToControl.transform.position = childObjectToControl.transform.position + new Vector3(0, -1.5f, -1) * speed;
            }
            if (Input.GetKey(KeyCode.W))
            {
                childObjectToControl.transform.position = childObjectToControl.transform.position + new Vector3(0, 1.5f, 1) * speed;
            }
            if (Input.GetKey(KeyCode.A))
            {
                childObjectToControl.transform.position = childObjectToControl.transform.position + new Vector3(-1, 0, 0) * speed;
            }
            if (Input.GetKey(KeyCode.D))
            {
                childObjectToControl.transform.position = childObjectToControl.transform.position + new Vector3(1, 0, 0) * speed;
            }
        }

        if (rb)
        {
            // Lose all velocity by pressing '0'
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            // Add forces to the parent rigidbody
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                rb.AddForce(new Vector3(0, -1.5f, -1) * rigidbodyMovementForce);
            }
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                rb.AddForce(new Vector3(0, 1.5f, 1) * rigidbodyMovementForce);
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                rb.AddForce(new Vector3(-1, 0, 0) * rigidbodyMovementForce);
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                rb.AddForce(new Vector3(1, 0, 0) * rigidbodyMovementForce);
            }
        }
        else if (rb2D)
        {
            // Lose all velocity by pressing '0'
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                rb2D.velocity = Vector3.zero;
                rb2D.angularVelocity = 0;
            }
            // Add forces to the parent rigidbody
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                rb2D.AddForce(new Vector3(0, -1.5f, -1) * rigidbodyMovementForce);
            }
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                rb2D.AddForce(new Vector3(0, 1.5f, 1) * rigidbodyMovementForce);
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                rb2D.AddForce(new Vector3(-1, 0, 0) * rigidbodyMovementForce);
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                rb2D.AddForce(new Vector3(1, 0, 0) * rigidbodyMovementForce);
            }
        }
        else
        {
            // Move the parent transform
            if (Input.GetKey(KeyCode.DownArrow))
            {
                transform.position = transform.position + new Vector3(0, -1.5f, -1) * speed;
            }
            if (Input.GetKey(KeyCode.UpArrow))
            {
                transform.position = transform.position + new Vector3(0, 1.5f, 1) * speed;
            }
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                transform.position = transform.position + new Vector3(-1, 0, 0) * speed;
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                transform.position = transform.position + new Vector3(1, 0, 0) * speed;
            }
        }

        #endregion

        // If you need to move a synced object and don't want the position to be interpolated on non-owners
        // you can use the teleport() method. This is useful for things like respawning.
        if (Input.GetKeyDown(KeyCode.T))
        {
            Vector3 position = transform.position + Vector3.right * 5;
            Quaternion rotation = transform.rotation;
            teleport(position, rotation);
        }

        // If you need to send a state update, call forceStateSendNextFrame() and the next frame's state will be sent.
        // Useful for collisions and fast changes in direction so you can be as accurate as possible in between your send rate.
        if (Input.GetKeyDown(KeyCode.F))
        {
            smoothSync.forceStateSendNextFrame();
        }
    }

    /// <summary>
    /// Custom validation method. 
    /// <remarks>
    /// Allows you to check variables to see if they are within allowed values. For example, position.
    /// This is for the server to check client owned objects to look for cheating like your players 
    /// modifying values beyond the game's intended limits.
    /// </remarks>
    /// </summary>
    public static bool validateStateOfPlayer(State latestReceivedState, State latestValidatedState)
    {
        // Here I do a simple distance check. If the object has gone more than 9000 units 
        // in less than a half of a second(500ms) then I ignore the message. You might want to kick 
        // players here, add them to a ban list, or collect your own data to see if it keeps 
        // happening. 
        if (Vector3.Distance(latestReceivedState.position, latestValidatedState.position) > 9000.0f &&
            (latestReceivedState.ownerTimestamp - latestValidatedState.ownerTimestamp < 500.0f))
        {
            // Return false and refuse to accept the State. The State will not be added locally
            // on the server or sent out to other clients.
            return false;
        }
        else
        {
            // Return true to accept the State. The State will be added locally on the server and sent out 
            // to other clients.
            return true;
        }
    }

    /// <summary>
    /// An example of how to teleport the player so that position will not be interpolated on non-owners.
    /// </summary>
    /// <remarks>
    /// Teleporting requires a command to be sent from the owner to the host.
    /// The SmoothSync teleport method is be called immediately on the owner before sending the command.
    /// </remarks>
    /// <param name="pos">The position to teleport to.</param>
    /// <param name="rot">The rotation to teleport to.</param>
    public void teleport(Vector3 pos, Quaternion rot)
    {
        if (!hasAuthority) return;
        int timestamp = NetworkTransport.GetNetworkTimestamp();
        // Teleport owned object
        smoothSync.teleport(timestamp, pos, rot);
        // Tell host to tell non-owners to teleport
        CmdTeleport(timestamp, pos, rot);
    }

    /// <summary>
    /// Echoes a teleport command from the host to all clients.
    /// </summary>
    /// <param name="networkTimestamp">The NetworkTransport.GetNetworkTimestamp() on the owner when the teleport message was sent.</param>
    /// <param name="pos">The position to teleport to.</param>
    /// <param name="rot">The rotation to teleport to.</param>
    [Command]
    public void CmdTeleport(int networkTimestamp, Vector3 pos, Quaternion rot)
    {
        RpcTeleport(networkTimestamp, pos, rot);
    }

    /// <summary>
    /// Receive teleport command on clients and call Smoothsync.teleport()
    /// </summary>
    /// <param name="networkTimestamp">The NetworkTransport.GetNetworkTimestamp() on the owner when the teleport message was sent.</param>
    /// <param name="pos">The position to teleport to.</param>
    /// <param name="rot">The rotation to teleport to.</param>
    [ClientRpc]
    public void RpcTeleport(int networkTimestamp, Vector3 pos, Quaternion rot)
    {
        // Don't teleport on owners because they already did.
        if (hasAuthority) return; 

        smoothSync.teleport(networkTimestamp, pos, rot);
    }
}
