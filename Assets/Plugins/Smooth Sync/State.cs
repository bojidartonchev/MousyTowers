using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System;

namespace Smooth
{
    /// <summary>
    /// The state of an object: timestamp, position, rotation, scale, velocity, angular velocity.
    /// </summary>
    public class State
    {
        /// <summary>
        /// The network timestamp of the owner when the state was sent.
        /// </summary>
        public int ownerTimestamp;
        /// <summary>
        /// The position of the owned object when the state was sent.
        /// </summary>
        public Vector3 position;
        /// <summary>
        /// The rotation of the owned object when the state was sent.
        /// </summary>
        public Quaternion rotation;
        /// <summary>
        /// The scale of the owned object when the state was sent.
        /// </summary>
        public Vector3 scale;
        /// <summary>
        /// The velocity of the owned object when the state was sent.
        /// </summary>
        public Vector3 velocity;
        /// <summary>
        /// The angularVelocity of the owned object when the state was sent.
        /// </summary>
        public Vector3 angularVelocity;

        /// <summary>
        /// The server will set this to true if it is received so we know to relay the information back out to other clients.
        /// </summary>
        public bool serverShouldRelayPosition = false;
        /// <summary>
        /// The server will set this to true if it is received so we know to relay the information back out to other clients.
        /// </summary>
        public bool serverShouldRelayRotation = false;
        /// <summary>
        /// The server will set this to true if it is received so we know to relay the information back out to other clients.
        /// </summary>
        public bool serverShouldRelayScale = false;
        /// <summary>
        /// The server will set this to true if it is received so we know to relay the information back out to other clients.
        /// </summary>
        public bool serverShouldRelayVelocity = false;
        /// <summary>
        /// The server will set this to true if it is received so we know to relay the information back out to other clients.
        /// </summary>
        public bool serverShouldRelayAngularVelocity = false;

        /// <summary>
        /// Default constructor. Does nothing.
        /// </summary>
        public State() { }

        /// <summary>
        /// Copy an existing State.
        /// </summary>
        public State(State state)
        { 
            ownerTimestamp = state.ownerTimestamp;
            position = state.position;
            rotation = state.rotation;
            scale = state.scale;
            velocity = state.velocity;
            angularVelocity = state.angularVelocity;
        }

        /// <summary>
        /// Create a State from a SmoothSync script.
        /// </summary>
        /// <remarks>
        /// This is called on owners when creating the States to be passed over the network.
        /// </remarks>
        /// <param name="smoothSyncScript"></param>
        public State(SmoothSync smoothSyncScript)
        {
#if UNITY_WEBGL
            ownerTimestamp = (int)(Time.time * 1000.0f);
#else
            ownerTimestamp = NetworkTransport.GetNetworkTimestamp();
#endif
            position = smoothSyncScript.getPosition();
            rotation = smoothSyncScript.getRotation();
            scale = smoothSyncScript.getScale();

            if (smoothSyncScript.hasRigdibody)
            {
                velocity = smoothSyncScript.rb.velocity;
                angularVelocity = smoothSyncScript.rb.angularVelocity;
            }
            else if (smoothSyncScript.hasRigidbody2D)
            {
                velocity = smoothSyncScript.rb2D.velocity;
                angularVelocity = new Vector3(0, 0, smoothSyncScript.rb2D.angularVelocity);
            }
            else
            {
                velocity = Vector3.zero;
                angularVelocity = Vector3.zero;
            }
        }

        /// <summary>
        /// Returns a Lerped state that is between two States in time.
        /// </summary>
        /// <param name="start">Start State</param>
        /// <param name="end">End State</param>
        /// <param name="t">Time</param>
        /// <returns></returns>
        public static State Lerp(State start, State end, float t)
        {
            State state = new State();

            state.position = Vector3.Lerp(start.position, end.position, t);            
            state.rotation = Quaternion.Lerp(start.rotation, end.rotation, t);
            state.scale = Vector3.Lerp(start.scale, end.scale, t);
            state.velocity = Vector3.Lerp(start.velocity, end.velocity, t);
            state.angularVelocity = Vector3.Lerp(start.angularVelocity, end.angularVelocity, t);
            
            state.ownerTimestamp = (int)Mathf.Lerp(start.ownerTimestamp, end.ownerTimestamp, t);

            return state;
        }
    }

    /// <summary>
    /// Wraps the State in the NetworkMessage so we can send it over the network.
    /// </summary>
    /// <remarks>
    /// This only sends and receives the parts of the State that are enabled on the SmoothSync component.
    /// </remarks>
    public class NetworkState : MessageBase
    {
        /// <summary>
        /// The SmoothSync object associated with this State.
        /// </summary>
        public SmoothSync smoothSync;
        /// <summary>
        /// The State that will be sent over the network
        /// </summary>
        public State state = new State();

        /// <summary>
        /// Default contstructor, does nothing.
        /// </summary>
        public NetworkState() { }

        /// <summary>
        /// Create a NetworkState from a SmoothSync object.
        /// </summary>
        /// <param name="smoothSyncScript">The SmoothSync object</param>
        public NetworkState(SmoothSync smoothSyncScript)
        {
            this.smoothSync = smoothSyncScript;
            state = new State(smoothSyncScript);
        }
        /// <summary>
        /// Serialize the message over the network.
        /// </summary>
        /// <remarks>
        /// Only sends what it needs and compresses floats if you chose to.
        /// </remarks>
        /// <param name="writer">The NetworkWriter to write to.</param>
        override public void Serialize(NetworkWriter writer)
        {
            bool sendPosition, sendRotation, sendScale, sendVelocity, sendAngularVelocity;

            // If is a server trying to relay client information back out to other clients.
            if (NetworkServer.active && !smoothSync.hasAuthority)
            {
                sendPosition = state.serverShouldRelayPosition;
                sendRotation = state.serverShouldRelayRotation;
                sendScale = state.serverShouldRelayScale;
                sendVelocity = state.serverShouldRelayVelocity;
                sendAngularVelocity = state.serverShouldRelayAngularVelocity;
            }
            else // If is a server or client trying to send owned object information across the network.
            {
                sendPosition = smoothSync.sendPosition;
                sendRotation = smoothSync.sendRotation;
                sendScale = smoothSync.sendScale;
                sendVelocity = smoothSync.sendVelocity;
                sendAngularVelocity = smoothSync.sendAngularVelocity;
            }
            // Only set last sync States on clients here because the server needs to send multiple Serializes.
            if (!NetworkServer.active)
            {
                if (sendPosition) smoothSync.lastPositionWhenStateWasSent = state.position;
                if (sendRotation) smoothSync.lastRotationWhenStateWasSent = state.rotation;
                if (sendScale) smoothSync.lastScaleWhenStateWasSent = state.scale;
                if (sendVelocity) smoothSync.lastVelocityWhenStateWasSent = state.velocity;
                if (sendAngularVelocity) smoothSync.lastAngularVelocityWhenStateWasSent = state.angularVelocity;
            }

            writer.Write(encodeSyncInformation(sendPosition, sendRotation, sendScale,
                sendVelocity, sendAngularVelocity));
            writer.Write(smoothSync.netID);
            writer.WritePackedUInt32((uint)smoothSync.syncIndex);
            writer.WritePackedUInt32((uint)state.ownerTimestamp);

            // Write position.
            if (sendPosition)
            {
                if (smoothSync.isPositionCompressed)
                {
                    if (smoothSync.isSyncingXPosition)
                    {
                        writer.Write(HalfHelper.Compress(state.position.x));
                    }
                    if (smoothSync.isSyncingYPosition)
                    {
                        writer.Write(HalfHelper.Compress(state.position.y));
                    }
                    if (smoothSync.isSyncingZPosition)
                    {
                        writer.Write(HalfHelper.Compress(state.position.z));
                    }
                }
                else
                {
                    if (smoothSync.isSyncingXPosition)
                    {
                        writer.Write(state.position.x);
                    }
                    if (smoothSync.isSyncingYPosition)
                    {
                        writer.Write(state.position.y);
                    }
                    if (smoothSync.isSyncingZPosition)
                    {
                        writer.Write(state.position.z);
                    }
                }
            }
            // Write rotation.
            if (sendRotation)
            {
                Vector3 rot = state.rotation.eulerAngles;
                if (smoothSync.isRotationCompressed)
                {
                    if (smoothSync.isSyncingXRotation)
                    {
                        writer.Write(HalfHelper.Compress(rot.x));
                    }
                    if (smoothSync.isSyncingYRotation)
                    {
                        writer.Write(HalfHelper.Compress(rot.y));
                    }
                    if (smoothSync.isSyncingZRotation)
                    {
                        writer.Write(HalfHelper.Compress(rot.z));
                    }
                }
                else
                {
                    if (smoothSync.isSyncingXRotation)
                    {
                        writer.Write(rot.x);
                    }
                    if (smoothSync.isSyncingYRotation)
                    {
                        writer.Write(rot.y);
                    }
                    if (smoothSync.isSyncingZRotation)
                    {
                        writer.Write(rot.z);
                    }
                }
            }
            // Write scale.
            if (sendScale)
            {
                if (smoothSync.isScaleCompressed)
                {
                    if (smoothSync.isSyncingXScale)
                    {
                        writer.Write(HalfHelper.Compress(state.scale.x));
                    }
                    if (smoothSync.isSyncingYScale)
                    {
                        writer.Write(HalfHelper.Compress(state.scale.y));
                    }
                    if (smoothSync.isSyncingZScale)
                    {
                        writer.Write(HalfHelper.Compress(state.scale.z));
                    }
                }
                else
                {
                    if (smoothSync.isSyncingXScale)
                    {
                        writer.Write(state.scale.x);
                    }
                    if (smoothSync.isSyncingYScale)
                    {
                        writer.Write(state.scale.y);
                    }
                    if (smoothSync.isSyncingZScale)
                    {
                        writer.Write(state.scale.z);
                    }
                }
            }
            // Write velocity.
            if (sendVelocity)
            {
                if (smoothSync.isVelocityCompressed)
                {
                    if (smoothSync.isSyncingXVelocity)
                    {
                        writer.Write(HalfHelper.Compress(state.velocity.x));
                    }
                    if (smoothSync.isSyncingYVelocity)
                    {
                        writer.Write(HalfHelper.Compress(state.velocity.y));
                    }
                    if (smoothSync.isSyncingZVelocity)
                    {
                        writer.Write(HalfHelper.Compress(state.velocity.z));
                    }
                }
                else
                {
                    if (smoothSync.isSyncingXVelocity)
                    {
                        writer.Write(state.velocity.x);
                    }
                    if (smoothSync.isSyncingYVelocity)
                    {
                        writer.Write(state.velocity.y);
                    }
                    if (smoothSync.isSyncingZVelocity)
                    {
                        writer.Write(state.velocity.z);
                    }
                }
            }
            // Write angular velocity.
            if (sendAngularVelocity)
            {
                if (smoothSync.isAngularVelocityCompressed)
                {
                    if (smoothSync.isSyncingXAngularVelocity)
                    {
                        writer.Write(HalfHelper.Compress(state.angularVelocity.x));
                    }
                    if (smoothSync.isSyncingYAngularVelocity)
                    {
                        writer.Write(HalfHelper.Compress(state.angularVelocity.y));
                    }
                    if (smoothSync.isSyncingZAngularVelocity)
                    {
                        writer.Write(HalfHelper.Compress(state.angularVelocity.z));
                    }
                }
                else
                {
                    if (smoothSync.isSyncingXAngularVelocity)
                    {
                        writer.Write(state.angularVelocity.x);
                    }
                    if (smoothSync.isSyncingYAngularVelocity)
                    {
                        writer.Write(state.angularVelocity.y);
                    }
                    if (smoothSync.isSyncingZAngularVelocity)
                    {
                        writer.Write(state.angularVelocity.z);
                    }
                }
            }
        }

        /// <summary>
        /// Deserialize a message from the network.
        /// </summary>
        /// <remarks>
        /// Only receives what it needs and decompresses floats if you chose to.
        /// </remarks>
        /// <param name="writer">The Networkreader to read from.</param>
        override public void Deserialize(NetworkReader reader)
        {
            // The first received byte tells us what we need to be syncing.
            byte syncInfoByte = reader.ReadByte();
            bool syncPosition = shouldSyncPosition(syncInfoByte);
            bool syncRotation = shouldSyncRotation(syncInfoByte);
            bool syncScale = shouldSyncScale(syncInfoByte);
            bool syncVelocity = shouldSyncVelocity(syncInfoByte);
            bool syncAngularVelocity = shouldSyncAngularVelocity(syncInfoByte);

            NetworkInstanceId netID = reader.ReadNetworkId();
            int syncIndex = (int)reader.ReadPackedUInt32();
            state.ownerTimestamp = (int)reader.ReadPackedUInt32();

            // Find the GameObject
            GameObject ob = null;
            if (NetworkServer.active)
            {
                ob = NetworkServer.FindLocalObject(netID);
            }
            else
            {
                ob = ClientScene.FindLocalObject(netID);
            }

            if (!ob)
            {
                Debug.LogWarning("Could not find target for network state message.");
                return;
            }

            // It doesn't matter which SmoothSync is returned since they all have the same list.
            smoothSync = ob.GetComponent<SmoothSync>();

            // If we want the server to relay non-owned object information out to other clients, set these variables so we know what we need to send.
            if (NetworkServer.active && !smoothSync.hasAuthority)
            {
                state.serverShouldRelayPosition = syncPosition;
                state.serverShouldRelayRotation = syncRotation;
                state.serverShouldRelayScale = syncScale;
                state.serverShouldRelayVelocity = syncVelocity;
                state.serverShouldRelayAngularVelocity = syncAngularVelocity;
            }

            // Find the correct object to sync according to the syncIndex.
            for (int i = 0; i < smoothSync.childObjectSmoothSyncs.Length; i++)
            {
                if (smoothSync.childObjectSmoothSyncs[i].syncIndex == syncIndex)
                {
                    smoothSync = smoothSync.childObjectSmoothSyncs[i];
                }
            }

            if (!smoothSync)
            {
                Debug.LogWarning("Could not find target for network state message.");
                return;
            }

            // Read position.
            if (syncPosition)
            {
                if (smoothSync.isPositionCompressed)
                {
                    if (smoothSync.isSyncingXPosition)
                    {
                        state.position.x = HalfHelper.Decompress(reader.ReadUInt16());
                    }
                    if (smoothSync.isSyncingYPosition)
                    {
                        state.position.y = HalfHelper.Decompress(reader.ReadUInt16());
                    }
                    if (smoothSync.isSyncingZPosition)
                    {
                        state.position.z = HalfHelper.Decompress(reader.ReadUInt16());
                    }
                }
                else
                {
                    if (smoothSync.isSyncingXPosition)
                    {
                        state.position.x = reader.ReadSingle();
                    }
                    if (smoothSync.isSyncingYPosition)
                    {
                        state.position.y = reader.ReadSingle();
                    }
                    if (smoothSync.isSyncingZPosition)
                    {
                        state.position.z = reader.ReadSingle();
                    }
                }
            }
            else
            {
                if (smoothSync.stateCount > 0)
                {
                    state.position = smoothSync.stateBuffer[0].position;
                }
                else
                {
                    state.position = smoothSync.getPosition();
                }
            }
            // Read rotation.
            if (syncRotation)
            {
                Vector3 rot = new Vector3();
                if (smoothSync.isRotationCompressed)
                {
                    if (smoothSync.isSyncingXRotation)
                    {
                        rot.x = HalfHelper.Decompress(reader.ReadUInt16());
                    }
                    if (smoothSync.isSyncingYRotation)
                    {
                        rot.y = HalfHelper.Decompress(reader.ReadUInt16());
                    }
                    if (smoothSync.isSyncingZRotation)
                    {
                        rot.z = HalfHelper.Decompress(reader.ReadUInt16());
                    }
                    state.rotation = Quaternion.Euler(rot);
                }
                else
                {
                    if (smoothSync.isSyncingXRotation)
                    {
                        rot.x = reader.ReadSingle();
                    }
                    if (smoothSync.isSyncingYRotation)
                    {
                        rot.y = reader.ReadSingle();
                    }
                    if (smoothSync.isSyncingZRotation)
                    {
                        rot.z = reader.ReadSingle();
                    }
                    state.rotation = Quaternion.Euler(rot);
                }
            }
            else
            {
                if (smoothSync.stateCount > 0)
                {
                    state.rotation = smoothSync.stateBuffer[0].rotation;
                }
                else
                {
                    state.rotation = smoothSync.getRotation();
                }
            }
            // Read scale.
            if (syncScale)
            {
                if (smoothSync.isScaleCompressed)
                {
                    if (smoothSync.isSyncingXScale)
                    {
                        state.scale.x = HalfHelper.Decompress(reader.ReadUInt16());
                    }
                    if (smoothSync.isSyncingYScale)
                    {
                        state.scale.y = HalfHelper.Decompress(reader.ReadUInt16());
                    }
                    if (smoothSync.isSyncingZScale)
                    {
                        state.scale.z = HalfHelper.Decompress(reader.ReadUInt16());
                    }
                }
                else
                {
                    if (smoothSync.isSyncingXScale)
                    {
                        state.scale.x = reader.ReadSingle();
                    }
                    if (smoothSync.isSyncingYScale)
                    {
                        state.scale.y = reader.ReadSingle();
                    }
                    if (smoothSync.isSyncingZScale)
                    {
                        state.scale.z = reader.ReadSingle();
                    }
                }
            }
            else
            {
                if (smoothSync.stateCount > 0)
                {
                    state.scale = smoothSync.stateBuffer[0].scale;
                }
                else
                {
                    state.scale = smoothSync.getScale();
                }
            }
            // Read velocity.
            if (syncVelocity)
            {
                if (smoothSync.isVelocityCompressed)
                {
                    if (smoothSync.isSyncingXVelocity)
                    {
                        state.velocity.x = HalfHelper.Decompress(reader.ReadUInt16());
                    }
                    if (smoothSync.isSyncingYVelocity)
                    {
                        state.velocity.y = HalfHelper.Decompress(reader.ReadUInt16());
                    }
                    if (smoothSync.isSyncingZVelocity)
                    {
                        state.velocity.z = HalfHelper.Decompress(reader.ReadUInt16());
                    }
                }
                else
                {
                    if (smoothSync.isSyncingXVelocity)
                    {
                        state.velocity.x = reader.ReadSingle();
                    }
                    if (smoothSync.isSyncingYVelocity)
                    {
                        state.velocity.y = reader.ReadSingle();
                    }
                    if (smoothSync.isSyncingZVelocity)
                    {
                        state.velocity.z = reader.ReadSingle();
                    }
                }
            }
            else
            {
                state.velocity = Vector3.zero;
            }
            // Read anguluar velocity.
            if (syncAngularVelocity)
            {
                if (smoothSync.isAngularVelocityCompressed)
                {
                    if (smoothSync.isSyncingXAngularVelocity)
                    {
                        state.angularVelocity.x = HalfHelper.Decompress(reader.ReadUInt16());
                    }
                    if (smoothSync.isSyncingYAngularVelocity)
                    {
                        state.angularVelocity.y = HalfHelper.Decompress(reader.ReadUInt16());
                    }
                    if (smoothSync.isSyncingZAngularVelocity)
                    {
                        state.angularVelocity.z = HalfHelper.Decompress(reader.ReadUInt16());
                    }
                }
                else
                {
                    if (smoothSync.isSyncingXAngularVelocity)
                    {
                        state.angularVelocity.x = reader.ReadSingle();
                    }
                    if (smoothSync.isSyncingYAngularVelocity)
                    {
                        state.angularVelocity.y = reader.ReadSingle();
                    }
                    if (smoothSync.isSyncingZAngularVelocity)
                    {
                        state.angularVelocity.z = reader.ReadSingle();
                    }
                }
            }
            else
            {
                state.angularVelocity = Vector3.zero;
            }
        }
        /// <summary>
        /// Hardcoded information to determine position syncing.
        /// </summary>
        byte positionMask = 1;        // 0000_0001
        /// <summary>
        /// Hardcoded information to determine rotation syncing.
        /// </summary>
        byte rotationMask = 2;        // 0000_0010
        /// <summary>
        /// Hardcoded information to determine scale syncing.
        /// </summary>
        byte scaleMask = 4;        // 0000_0100
        /// <summary>
        /// Hardcoded information to determine velocity syncing.
        /// </summary>
        byte velocityMask = 8;        // 0000_1000
        /// <summary>
        /// Hardcoded information to determine angular velocity syncing.
        /// </summary>
        byte angularVelocityMask = 16; // 0001_0000
        /// <summary>
        /// Encode sync info based on what we want to send.
        /// </summary>
        byte encodeSyncInformation(bool sendPosition, bool sendRotation, bool sendScale, bool sendVelocity, bool sendAngularVelocity)
        {
            byte encoded = 0;

            if (sendPosition)
            {
                encoded = (byte)(encoded | positionMask);
            }
            if (sendRotation)
            {
                encoded = (byte)(encoded | rotationMask);
            }
            if (sendScale)
            {
                encoded = (byte)(encoded | scaleMask);
            }
            if (sendVelocity)
            {
                encoded = (byte)(encoded | velocityMask);
            }
            if (sendAngularVelocity)
            {
                encoded = (byte)(encoded | angularVelocityMask);
            }
            return encoded;
        }
        /// <summary>
        /// Decode sync info to see if we want to sync position.
        /// </summary>
        bool shouldSyncPosition (byte syncInformation)
        {
            if ((syncInformation & positionMask) == positionMask)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Decode sync info to see if we want to sync rotation.
        /// </summary>
        bool shouldSyncRotation(byte syncInformation)
        {
            if ((syncInformation & rotationMask) == rotationMask)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Decode sync info to see if we want to sync scale.
        /// </summary>
        bool shouldSyncScale(byte syncInformation)
        {
            if ((syncInformation & scaleMask) == scaleMask)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Decode sync info to see if we want to sync velocity.
        /// </summary>
        bool shouldSyncVelocity(byte syncInformation)
        {
            if ((syncInformation & velocityMask) == velocityMask)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Decode sync info to see if we want to sync angular velocity.
        /// </summary>
        bool shouldSyncAngularVelocity(byte syncInformation)
        {
            if ((syncInformation & angularVelocityMask) == angularVelocityMask)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}