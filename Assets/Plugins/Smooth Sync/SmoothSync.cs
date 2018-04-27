using UnityEngine;
using UnityEngine.Networking;
using System;
using UnityEngine.Serialization;

// Whalecome to SmoothSync. If you have any problems, suggestions, or comments, don't hesitate to let us hear them.
// With Love,
// Noble Whale Studios

namespace Smooth
{
    /// <summary>
    /// Sync a Transform or Rigidbody over the network. Uses interpolation and extrapolation.
    /// </summary>
    /// <remarks>
    /// Overview:
    /// Uses sendRate first and foremost to determine how often to send States.
    /// It will then defer to the thresholds to see if any of them have been passed and if so, it will send a State
    /// out to non-owners so that they have the updated Transform and Rigidbody information.
    /// If it doesn't pass any threshold, it will send out the next frame that the thresholds have been passed. 
    /// </remarks>
    public class SmoothSync : NetworkBehaviour
    {
        #region Configuration

        /// <summary>How much time in the past non-owned objects should be.</summary>
        /// <remarks>
        /// interpolationBackTime is the amount of time in the past the object will be on non-owners.
        /// This is so if you hit a latency spike, you still have a buffer of the interpolation back time of known States 
        /// before you start extrapolating into the unknown.
        /// 
        /// Increasing this will make interpolation more likely to be used, 
        /// which means the synced position will be more likely to be an actual position that the owner was at.
        /// 
        /// Decreasing this will make extrapolation more likely to be used, 
        /// this will increase reponsiveness, but with any latency spikes that last longer than the interpolationBackTime, 
        /// the position will be less correct to where the player was actually at.
        /// 
        /// Measured in seconds.
        /// </remarks>
        [Tooltip("How much time in the past non-owned objects should be. Increasing will make interpolation more likely to be used, decreasing will make extrapolation more likely to be used. In seconds.")]
        public float interpolationBackTime = .1f;

        /// <summary>How much time into the future a non-owned object is allowed to extrapolate.</summary>
        /// <remarks>
        /// Extrapolating too far tends to cause erratic and non-realistic movement, but a little bit of extrapolation is 
        /// better than none because it keeps things working semi-right during latency spikes.
        /// 
        /// Must be syncing velocity in order to utilize extrapolation.
        /// 
        /// Measured in seconds.
        /// </remarks>
        [Tooltip("How much time into the future a non-owned object is allowed to extrapolate. In seconds.")]
        public float extrapolationTimeLimit = .3f;

        /// <summary>How much distance into the future a non-owned object is allowed to extrapolate.</summary>
        /// <remarks>
        /// Extrapolating too far tends to cause erratic and non-realistic movement, but a little bit of extrapolation is 
        /// better than none because it keeps things working semi-right during latency spikes.
        /// 
        /// Must be syncing velocity in order to utilize extrapolation.
        /// 
        /// Measured in distance units.
        /// </remarks>
        [Tooltip("How much distance into the future a non-owned object is allowed to extrapolate. In distance units.")]
        public float extrapolationDistanceLimit = .3f;

        /// <summary>The position won't send unless it changed this much.</summary>
        /// <remarks>
        /// Set to 0 to always send the position of owned objects if it has changed since the last sent position, and for a hardware 
        /// performance increase, but at the cost of network usage.
        /// If greater than 0, a synced object's position is only sent if its position is off from the last sent position by more 
        /// than the threshold. 
        /// Measured in distance units.
        /// </remarks>
        [Tooltip("A synced object's position is only sent if it is off from the last sent position by more than the threshold. In distance units.")]
        public float sendPositionThreshold = .001f;

        /// <summary>The rotation won't send unless it changed this much.</summary>
        /// <remarks>
        /// Set to 0 to always send the rotation of owned objects if it has changed since the last sent rotation, and for a hardware 
        /// performance increase, but at the cost of network usage.
        /// If greater than 0, a synced object's rotation is only sent if its rotation is off from the last sent rotation by more 
        /// than the threshold.
        /// Measured in degrees.
        /// </remarks>
        [Tooltip("A synced object's rotation is only sent if it is off from the last sent rotation by more than the threshold. In degrees.")]
        public float sendRotationThreshold = .001f;

        /// <summary>The scale won't send unless it changed this much.</summary>
        /// <remarks>
        /// Set to 0 to always send the scale of owned objects if it has changed since the last sent scale, and for a hardware 
        /// performance increase, but at the cost of network usage.
        /// If greater than 0, a synced object's scale is only sent if its scale is off from the last sent scale by more 
        /// than the threshold. 
        /// Measured in distance units.
        /// </remarks>
        [Tooltip("A synced object's scale is only sent if it is off from the last sent scale by more than the threshold. In distance units.")]
        public float sendScaleThreshold = .001f;

        /// <summary>The velocity won't send unless it changed this much.</summary>
        /// <remarks>
        /// Set to 0 to always send the velocity of owned objects if it has changed since the last sent velocity, and for a hardware 
        /// performance increase, but at the cost of network usage.
        /// If greater than 0, a synced object's velocity is only sent if its velocity is off from the last sent velocity
        /// by more than the threshold. 
        /// Measured in velocity units.
        /// </remarks>
        [Tooltip("A synced object's velocity is only sent if it is off from the last sent velocity by more than the threshold. In velocity units.")]
        public float sendVelocityThreshold = .001f;

        /// <summary>The angular velocity won't send unless it changed this much.</summary>
        /// <remarks>
        /// Set to 0 to always send the angular velocity of owned objects if it has changed since the last sent angular velocity, and for a hardware 
        /// performance increase, but at the cost of network usage.
        /// If greater than 0, a synced object's angular velocity is only sent if its angular velocity is off from the last sent angular velocity
        /// by more than the threshold. 
        /// Measured in radians per second.
        /// </remarks>
        [Tooltip("A synced object's angular velocity is only sent if it is off from the last sent angular velocity by more than the threshold. In radians per second.")]
        public float sendAngularVelocityThreshold = .001f;

        /// <summary>The position won't be set on non-owned objects unless it changed this much.</summary>
        /// <remarks>
        /// Set to 0 to always update the position of non-owned objects if it has changed, and to use one less Vector3.Distance() check per frame if you also have positionSnapThreshold at 0.
        /// If greater than 0, a synced object's position is only updated if it is off from the target position by more than the threshold.
        /// Usually keep this at 0 or really low, at higher numbers it's useful if you are extrapolating into the future and want to stop instantly 
        /// and not have it backtrack to where it currently is on the owner.
        /// Measured in distance units.
        /// </remarks>
        [Tooltip("A synced object's position is only updated if it is off from the target position by more than the threshold. Set to 0 to always update. In distance units.")]
        public float receivedPositionThreshold = 0.0f;

        /// <summary>The rotation won't be set on non-owned objects unless it changed this much.</summary>
        /// <remarks>
        /// Set to 0 to always update the rotation of non-owned objects if it has changed, and to use one less Quaternion.Angle() check per frame if you also have rotationSnapThreshold at 0.
        /// If greater than 0, a synced object's rotation is only updated if it is off from the target rotation by more than the threshold.
        /// Usually keep this at 0 or really low, at higher numbers it's useful if you are extrapolating into the future and want to stop instantly and 
        /// not have it backtrack to where it currently is on the owner.
        /// Measured in degrees.
        /// </remarks>
        [Tooltip("A synced object's rotation is only updated if it is off from the target rotation by more than the threshold. Set to 0 to always update. In degrees.")]
        public float receivedRotationThreshold = 0.0f;

        /// <summary>If a synced object's position is more than positionSnapThreshold units from the target position, it will jump to the target position immediately instead of lerping.</summary>
        /// <remarks>
        /// Set to zero to not use at all and use one less Vector3.Distance() check per frame if you also have receivedPositionThreshold at 0.
        /// Measured in distance units.
        /// </summary>
        [Tooltip("If the position is more than snapThreshold units from the target position, it will jump to the target position immediately instead of lerping. Set to 0 to not use at all. In distance units.")]
        public float positionSnapThreshold = 8;

        /// <summary>If a synced object's rotation is more than rotationSnapThreshold from the target rotation, it will jump to the target rotation immediately instead of lerping.</summary>
        /// <remarks>
        /// Set to zero to not use at all and use one less Quaternion.Angle() check per frame if you also have receivedRotationThreshold at 0.
        /// Measured in degrees.
        /// </remarks>
        [Tooltip("If the rotation is more than snapThreshold units from the target rotation, it will jump to the target rotation immediately instead of lerping. Set to 0 to not use at all. In degrees.")]
        public float rotationSnapThreshold = 60;

        /// <summary>If a synced object's scale is more than scaleSnapThreshold units from the target scale, it will jump to the target scale immediately instead of lerping.</summary>
        /// <remarks>
        /// Set to zero to not use at all and use one less Vector3.Distance() check per frame.
        /// Measured in distance units.
        /// </remarks>
        [Tooltip("If the scale is more than snapThreshold units from the target scale, it will jump to the target scale immediately instead of lerping. Set to 0 to not use at all. In distance units.")]
        public float scaleSnapThreshold = 3;

        /// <summary>How fast to lerp the position to the target state. 0 is never, 1 is instant.</summary>
        /// <remarks>
        /// Lower values mean smoother but maybe sluggish movement.
        /// Higher values mean more responsive but maybe jerky or stuttery movement.
        /// </remarks>
        [Range(0, 1)]
        [Tooltip("How fast to lerp to the new target state. 0 is never, 1 is instant.")]
        public float positionLerpSpeed = .2f;

        /// <summary>How fast to lerp the rotation to the target state. 0 is never, 1 is instant..</summary>
        /// <remarks>
        /// Lower values mean smoother but maybe sluggish movement.
        /// Higher values mean more responsive but maybe jerky or stuttery movement.
        /// </remarks>
        [Range(0, 1)]
        [Tooltip("How fast to lerp to the new target state. 0 is never, 1 is instant.")]
        public float rotationLerpSpeed = .2f;

        /// <summary>How fast to lerp the scale to the target state. 0 is never, 1 is instant.</summary>
        /// <remarks>
        /// Lower values mean smoother but maybe sluggish movement.
        /// Higher values mean more responsive but maybe jerky or stuttery movement.
        /// </remarks>
        [Range(0, 1)]
        [Tooltip("How fast to lerp to the new target state. 0 is never, 1 is instant.")]
        public float scaleLerpSpeed = .2f;

        /// <summary>Position sync mode</summary>
        /// <remarks>
        /// Fine tune how position is synced. 
        /// For objects that don't move, use SyncMode.NONE
        /// </remarks>
        [Tooltip("Fine tune how position is synced.")]
        public SyncMode syncPosition = SyncMode.XYZ;

        /// <summary>Rotation sync mode</summary>
        /// <remarks>
        /// Fine tune how rotation is synced. 
        /// For objects that don't rotate, use SyncMode.NONE
        /// </remarks>
        [Tooltip("Fine tune how rotation is synced.")]
        public SyncMode syncRotation = SyncMode.XYZ;

        /// <summary>Scale sync mode</summary>
        /// <remarks>
        /// Fine tune how scale is synced. 
        /// For objects that don't scale, use SyncMode.NONE
        /// </remarks>
        [Tooltip("Fine tune how scale is synced.")]
        public SyncMode syncScale = SyncMode.XYZ;

        /// <summary>Velocity sync mode</summary>
        /// <remarks>
        /// Fine tune how velocity is synced.
        /// </remarks>
        [Tooltip("Fine tune how velocity is synced.")]
        public SyncMode syncVelocity = SyncMode.XYZ;

        /// <summary>Angular velocity sync mode</summary>
        /// <remarks>
        /// Fine tune how angular velocity is synced. 
        /// </remarks>
        [Tooltip("Fine tune how angular velocity is synced.")]
        public SyncMode syncAngularVelocity = SyncMode.XYZ;

        /// <summary>Compress position floats when sending over the network.</summary>
        /// <remarks>
        /// Convert position floats sent over the network to Halfs, which use half as much bandwidth but are also half as precise.
        /// </remarks>
        [Tooltip("Compress floats to save bandwidth.")]
        public bool isPositionCompressed = true;
        /// <summary>Compress rotation floats when sending over the network.</summary>
        /// <remarks>
        /// Convert rotation floats sent over the network to Halfs, which use half as much bandwidth but are also half as precise.
        /// </remarks>
        [Tooltip("Compress floats to save bandwidth.")]
        public bool isRotationCompressed = true;
        /// <summary>Compress scale floats when sending over the network.</summary>
        /// <remarks>
        /// Convert scale floats sent over the network to Halfs, which use half as much bandwidth but are also half as precise.
        /// </remarks>
        [Tooltip("Compress floats to save bandwidth.")]
        public bool isScaleCompressed = true;
        /// <summary>Compress velocity floats when sending over the network.</summary>
        /// <remarks>
        /// Convert velocity floats sent over the network to Halfs, which use half as much bandwidth but are also half as precise.
        /// </remarks>
        [Tooltip("Compress floats to save bandwidth.")]
        public bool isVelocityCompressed = true;
        /// <summary>Compress angular velocity floats when sending over the network.</summary>
        /// <remarks>
        /// Convert angular velocity floats sent over the network to Halfs, which use half as much bandwidth but are also half as precise.
        /// </remarks>
        [Tooltip("Compress floats to save bandwidth.")]
        public bool isAngularVelocityCompressed = true;

        /// <summary>
        /// Info to know where to update the Transform.
        /// </summary>
        public enum WhereToUpdateTransform
        {
            Update, FixedUpdate
        }
        /// <summary>Where the object's Transform is updated on non-owners.</summary>
        /// <remarks>
        /// Update will have smoother results but FixedUpdate is better for physics.
        /// </remarks>
        [Tooltip("Update will have smoother results but FixedUpdate is better for physics.")]
        public WhereToUpdateTransform whereToUpdateTransform = WhereToUpdateTransform.FixedUpdate;

        /// <summary>How many times per second to send network updates</summary>
        /// <remarks>
        /// Keep in mind actual send rate may be limited by the NetworkManager configuration.
        /// </remarks>
        [Tooltip("How many times per second to send network updates.")]
        public float sendRate = 30;

        /// <summary>The channel to send network updates on.</summary>
        [Tooltip("The channel to send network updates on.")]
        public int networkChannel = Channels.DefaultUnreliable;

        /// <summary>Child object to sync</summary>
        /// <remarks>
        /// Leave blank if you want to sync this object. 
        /// In order to sync a child object, you must add two instances of SmoothSync to the parent. 
        /// Set childObjectToSync on one of them to point to the child you want to sync and leave it blank on the other to sync the parent.
        /// You cannot sync children without syncing the parent.
        /// </remarks>
        [Tooltip("Set this to sync a child object, leave blank to sync this object. Must have one blank to sync the parent in order to sync children.")]
        public GameObject childObjectToSync;
        /// <summary>Does this game object have a child object to sync?</summary>
        /// <remarks>
        /// Is much less resource intensive to check a boolean than if a Gameobject exists.
        /// </remarks>
        [NonSerialized]
        public bool hasChildObject = false;


        /// To tie in your own validation method, check the SmoothSyncExample scene and 
        /// SmoothSyncExamplePlayerController.cs on how to use the validation delegate.
        /// <summary>Validation delegate</summary>
        /// <remarks>
        /// Smooth Sync will call this on the server on every incoming State message. By default it allows every received 
        /// State but you can set the validateStateMethod to a custom one in order to validate that the 
        /// clients aren't modifying their owned objects beyond the game's intended limits.
        /// </remarks>
        public delegate bool validateStateDelegate(State receivedState, State latestVerifiedState);
        /// <summary>Validation method</summary>
        /// <remarks>
        /// The default validation method that allows all States. Your custom validation method
        /// must match the parameter types of this method. 
        /// Return false to deny the State. The State will not be added locally on the server
        /// and it will not be sent out to other clients.
        /// Return true to accept the State. The State will be added locally on the server and will be 
        /// sent out to other clients.
        /// </remarks>
        public static bool validateState(State latestReceivedState, State latestValidatedState)
        {
            return true;
        }
        /// <summary>Validation method variable</summary>
        /// <remarks>
        /// Holds a reference to the method that will be called to validate incoming States. 
        /// You will set it to your custom validation method. It will be something like 
        /// smoothSync.validateStateMethod = myCoolCustomValidatePlayerMethod; 
        /// in the Start or Awake method of your object's script.
        /// </remarks>
        [NonSerialized]
        public validateStateDelegate validateStateMethod = validateState;
        /// <summary>Latest validated State</summary>
        /// <remarks>
        /// The last received State that was validated by the validateStateDelegate.
        /// This means the State was passed to the delegate and the method returned true.
        /// </remarks>
        State latestValidatedState;


        #endregion Configuration

        #region Runtime data

        /// <summary>Non-owners keep a list of recent States received over the network for interpolating.</summary>
        [NonSerialized]
        public State[] stateBuffer; // TODO: A circular buffer would be more efficient but it probably doesn't matter.

        /// <summary>The number of States in the stateBuffer</summary>
        [NonSerialized]
        public int stateCount;

        /// <summary>Store a reference to the rigidbody so that we only have to call GetComponent() once.</summary>
        /// <remarks>Will automatically use Rigidbody or Rigidbody2D depending on what is on the game object.</remarks>
        [NonSerialized]
        public Rigidbody rb;
        /// <summary>Does this game object have a Rigidbody component?</summary>
        /// <remarks>
        /// Is much less resource intensive to check a boolean than if a component exists.
        /// </remarks>
        [NonSerialized]
        public bool hasRigdibody = false;
        /// <summary>Store a reference to the 2D rigidbody so that we only have to call GetComponent() once.</summary>
        [NonSerialized]
        public Rigidbody2D rb2D;
        /// <summary>Does this game object have a Rigidbody2D component?</summary>
        /// <remarks>
        /// Is much less resource intensive to check a boolean than if a component exists.
        /// </remarks>
        [NonSerialized]
        public bool hasRigidbody2D = false;

        /// <summary>
        /// Used via stopLerping() and restartLerping() to 'teleport' a synced object without unwanted lerping.
        /// Useful for player spawning and whatnot.
        /// </summary>
        bool skipLerp = false;
        /// <summary>
        /// Used via stopLerping() and restartLerping() to 'teleport' a synced object without unwanted lerping.
        /// Useful for player spawning and whatnot.
        /// </summary>
        bool dontLerp = false;
        /// <summary>Last time the object was teleported.</summary>
        [NonSerialized]
        public float lastTeleportOwnerTime;

        /// <summary>Last time owner sent a State.</summary>
        [NonSerialized]
        public float lastTimeStateWasSent;

        /// <summary>Last time a State was received on non-owner.</summary>
        [NonSerialized]
        public float lastTimeStateWasReceived;

        /// <summary>Position owner was at when the last position State was sent.</summary>
        [NonSerialized]
        public Vector3 lastPositionWhenStateWasSent;

        /// <summary>Rotation owner was at when the last rotation State was sent.</summary>
        [NonSerialized]
        public Quaternion lastRotationWhenStateWasSent = Quaternion.identity;

        /// <summary>Scale owner was at when the last scale State was sent.</summary>
        [NonSerialized]
        public Vector3 lastScaleWhenStateWasSent;

        /// <summary>Velocity owner was at when the last velocity State was sent.</summary>
        [NonSerialized]
        public Vector3 lastVelocityWhenStateWasSent;

        /// <summary>Angular velocity owner was at when the last angular velociy State was sent.</summary>
        [NonSerialized]
        public Vector3 lastAngularVelocityWhenStateWasSent;

        /// <summary>Cached network ID.</summary>
        [NonSerialized]
        public NetworkIdentity netID;

        /// <summary>Gets assigned to the real object to sync. Either this object or a child object.</summary>
        [NonSerialized]
        public GameObject realObjectToSync;
        /// <summary>Index to know which object to sync.</summary>
        [NonSerialized]
        public int syncIndex = 0;
        /// <summary>Reference to child objects so you can compare to syncIndex.</summary>
        [NonSerialized]
        public SmoothSync[] childObjectSmoothSyncs = new SmoothSync[0];

        /// <summary>State when extrapolation ended.</summary>
        State extrapolationEndState;
        /// <summary>Time when extrapolation ended.</summary>
        float extrapolationStopTime;

        /// <summary>Gets set to true in order to force the state to be sent next frame on owners.</summary>
        [NonSerialized]
        public bool forceStateSend = false;

        /// <summary>Variable we set at the beginning of Update so we only need to do the checks once a frame.</summary>
        [NonSerialized]
        public bool sendPosition;
        /// <summary>Variable we set at the beginning of Update so we only need to do the checks once a frame.</summary>
        [NonSerialized]
        public bool sendRotation;
        /// <summary>Variable we set at the beginning of Update so we only need to do the checks once a frame.</summary>
        [NonSerialized]
        public bool sendScale;
        /// <summary>Variable we set at the beginning of Update so we only need to do the checks once a frame.</summary>
        [NonSerialized]
        public bool sendVelocity;
        /// <summary>Variable we set at the beginning of Update so we only need to do the checks once a frame.</summary>
        [NonSerialized]
        public bool sendAngularVelocity;

        #endregion Runtime data

        #region Unity methods

        /// <summary>Cache references to components.</summary>
        void Awake()
        {
            // Uses a state buffer of at least 30 for ease of use, or a buffer size in relation 
            // to the send rate and how far back in time we want to be. Doubled buffer as estimation for forced State sends.
            int calculatedStateBufferSize = ((int)(sendRate * interpolationBackTime) + 1) * 2;
            stateBuffer = new State[Mathf.Max(calculatedStateBufferSize, 30)];

            netID = GetComponent<NetworkIdentity>();
            rb = GetComponent<Rigidbody>();
            rb2D = GetComponent<Rigidbody2D>();
            if (rb && childObjectToSync == null)
            {
                hasRigdibody = true;
            }
            if (rb2D && childObjectToSync == null)
            {
                hasRigidbody2D = true;
                // If 2D rigidbody, it only has a velocity of X, Y and an angular veloctiy of Z. So force it if you want any syncing.
                if (syncVelocity != SyncMode.NONE) syncVelocity = SyncMode.XY;
                if (syncAngularVelocity != SyncMode.NONE) syncAngularVelocity = SyncMode.Z;
                
            }
            // If no rigidbodies or is child object, there is no rigidbody supplied velocity, so don't sync it.
            if ((!rb && !rb2D) || childObjectToSync)
            {
                syncVelocity = SyncMode.NONE;
                syncAngularVelocity = SyncMode.NONE;
            }

            // If you want to sync a child object, assign it.
            if (childObjectToSync)
            {
                realObjectToSync = childObjectToSync;
                hasChildObject = true;

                // Throw error if no SmoothSync script is handling the parent object.
                bool foundAParent = false;
                childObjectSmoothSyncs = GetComponents<SmoothSync>();
                for (int i = 0; i < childObjectSmoothSyncs.Length; i++)
                {
                    if (!childObjectSmoothSyncs[i].childObjectToSync)
                    {
                        foundAParent = true;
                    }
                }
                if (!foundAParent)
                {
                    Debug.LogError("You must have one SmoothSync script with unassigned childObjectToSync to sync the parent object");
                }
            }
            // If you want to sync this object, assign it
            // and then assign indexes to know which objects to sync to what.
            // Unity guarantees same order in GetComponents<>() so indexes are already synced across the network.
            else
            {
                realObjectToSync = this.gameObject;

                int indexToGive = 0;
                childObjectSmoothSyncs = GetComponents<SmoothSync>();
                for (int i = 0; i < childObjectSmoothSyncs.Length; i++)
                {
                    childObjectSmoothSyncs[i].syncIndex = indexToGive;
                    indexToGive++;
                }
            }
        }

        /// <summary>Send the owned object's State over the network and set the interpolated / extrapolated 
        /// Transforms and Rigidbodies of non-owned objects.</summary>
        void Update()
        {
            // Set the interpolated / extrapolated Transforms and Rigidbodies of non-owned objects.
            if (!hasAuthority && whereToUpdateTransform == WhereToUpdateTransform.Update)
            {
                applyInterpolationOrExtrapolation();
            }

            // The rest of Update() is about sending the States of owned objects.

            // We only want to send from owners who are ready
            if (!hasAuthority || (!NetworkServer.active && !ClientScene.ready)) return;

            // If hasn't been long enough since the last send(and we aren't forcing a state send), return and don't send out.
            if (Time.realtimeSinceStartup - lastTimeStateWasSent < GetNetworkSendInterval() &&
                !forceStateSend) return;

            // Checks the core variables to see if we should be sending them out.
            sendPosition = shouldSendPosition();
            sendRotation = shouldSendRotation();
            sendScale = shouldSendScale();
            sendVelocity = shouldSendVelocity();
            sendAngularVelocity = shouldSendAngularVelocity();

            // Check that at least one variable has changed that we want to sync.
            if (!sendPosition && !sendRotation && !sendScale && !sendVelocity && !sendAngularVelocity) return;

            lastTimeStateWasSent = Time.realtimeSinceStartup;

            // Get the current state of the object and send it out
            NetworkState networkState = new NetworkState(this);

            if (NetworkServer.active)
            {
                // If owner is the host then send the state to everyone else
                SendStateToNonOwners(networkState);

                // If sending certain variables, set latest version of them accordingly.
                // Do it here instead of Serialize for the server since it's going to be sending it out to each client
                // and we only want to do it once.
                if (sendPosition) lastPositionWhenStateWasSent = networkState.state.position;
                if (sendRotation) lastRotationWhenStateWasSent = networkState.state.rotation;
                if (sendScale) lastScaleWhenStateWasSent = networkState.state.scale;
                if (sendVelocity) lastVelocityWhenStateWasSent = networkState.state.velocity;
                if (sendAngularVelocity) lastAngularVelocityWhenStateWasSent = networkState.state.angularVelocity;
            }
            else
            {
                // If owner is not the host then send the state to the host so they can send it to everyone else
                NetworkManager.singleton.client.connection.SendByChannel(MsgType.SmoothSyncFromOwnerToServer, networkState, networkChannel);
            }

            // Reset back to default state.
            forceStateSend = false;
        }

        /// <summary>Set interpolated / extrapolated Transforms and Rigidbodies on non-owned objects.</summary>
        void FixedUpdate()
        {
            if (!hasAuthority && whereToUpdateTransform == WhereToUpdateTransform.FixedUpdate)
            {
                applyInterpolationOrExtrapolation();
            }
        }

        #endregion

        #region Internal stuff

        /// <summary>Use the State buffer to set interpolated or extrapolated Transforms and Rigidbodies on non-owned objects.</summary>
        void applyInterpolationOrExtrapolation()
        {
            if (stateCount == 0) return;

            State targetState;
            bool triedToExtrapolateTooFar = false;
            
            if (dontLerp)
            {
                targetState = new State(this);
            }
            else
            {
                // The target playback time
                float interpolationTime = approximateNetworkTimeOnOwner - interpolationBackTime * 1000;

                // Use interpolation if the target playback time is present in the buffer.
                if (stateCount > 1 && stateBuffer[0].ownerTimestamp > interpolationTime)
                {
                    interpolate(interpolationTime, out targetState);
                }
                // The newest state is too old, we'll have to use extrapolation.
                else
                {
                    bool success = extrapolate(interpolationTime, out targetState);
                    triedToExtrapolateTooFar = !success;
                }
            }

            float actualPositionLerpSpeed = positionLerpSpeed;
            float actualRotationLerpSpeed = rotationLerpSpeed;
            float actualScaleLerpSpeed = scaleLerpSpeed;

            // This has to do with teleporting
            if (skipLerp)
            {
                actualPositionLerpSpeed = 1;
                actualRotationLerpSpeed = 1;
                actualScaleLerpSpeed = 1;
                skipLerp = false;
                dontLerp = false;
            }
            else if (dontLerp)
            {
                actualPositionLerpSpeed = 1;
                actualRotationLerpSpeed = 1;
                actualScaleLerpSpeed = 1;
                stateCount = 0;
            }

            // Set position, rotation, scale, velocity, and angular velocity (as long as we didn't try and extrapolate too far).
            if (!triedToExtrapolateTooFar || (!hasRigdibody && !hasRigidbody2D))
            {
                bool changedPositionEnough = false;
                float distance = 0;
                // If the current position is different from target position
                if (getPosition() != targetState.position)
                {
                    // If we want to use either of these variables, we need to calculate the distance.
                    if (positionSnapThreshold != 0 || receivedPositionThreshold != 0)
                    {
                        distance = Vector3.Distance(getPosition(), targetState.position);
                    }
                }
                // If we want to use receivedPositionThreshold, check if the distance has passed the threshold.
                if (receivedPositionThreshold != 0)
                {
                    if (distance > receivedPositionThreshold)
                    {
                        changedPositionEnough = true;
                    }
                }
                else // If we don't want to use receivedPositionThreshold, we will always set the new position.
                {
                    changedPositionEnough = true;
                }

                bool changedRotationEnough = false;
                float angleDifference = 0;
                // If the current rotation is different from target rotation
                if (getRotation() != targetState.rotation)
                {
                    // If we want to use either of these variables, we need to calculate the angle difference.
                    if (rotationSnapThreshold != 0 || receivedRotationThreshold != 0)
                    {
                        angleDifference = Quaternion.Angle(getRotation(), targetState.rotation);
                    }
                }
                // If we want to use receivedRotationThreshold, check if the angle difference has passed the threshold.
                if (receivedRotationThreshold != 0)
                {
                    if (angleDifference > receivedRotationThreshold)
                    {
                        changedRotationEnough = true;
                    }
                }
                else // If we don't want to use receivedRotationThreshold, we will always set the new position.
                {
                    changedRotationEnough = true;
                }

                bool changedScaleEnough = false;
                float scaleDistance = 0;
                // If current scale is different from target scale
                if (getScale() != targetState.scale)
                {
                    changedScaleEnough = true;
                    // If we want to use scaleSnapThreshhold, calculate the distance.
                    if (scaleSnapThreshold != 0)
                    {
                        scaleDistance = Vector3.Distance(getScale(), targetState.scale);
                    }
                }

                if (hasRigdibody && !rb.isKinematic)
                {
                    if (changedPositionEnough)
                    {
                        Vector3 newVelocity = rb.velocity;
                        if (isSyncingXVelocity)
                        {
                            newVelocity.x = targetState.velocity.x;
                        }
                        if (isSyncingYVelocity)
                        {
                            newVelocity.y = targetState.velocity.y;
                        }
                        if (isSyncingZVelocity)
                        {
                            newVelocity.z = targetState.velocity.z;
                        }
                        rb.velocity = Vector3.Lerp(rb.velocity, newVelocity, actualPositionLerpSpeed);
                    }
                    else
                    {
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                    if (changedRotationEnough)
                    {
                        Vector3 newAngularVelocity = rb.angularVelocity;
                        if (isSyncingXAngularVelocity)
                        {
                            newAngularVelocity.x = targetState.angularVelocity.x;
                        }
                        if (isSyncingYAngularVelocity)
                        {
                            newAngularVelocity.y = targetState.angularVelocity.y;
                        }
                        if (isSyncingZAngularVelocity)
                        {
                            newAngularVelocity.z = targetState.angularVelocity.z;
                        }
                        rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, newAngularVelocity, actualRotationLerpSpeed);
                    }
                    else
                    {
                        rb.angularVelocity = Vector3.zero;
                    }
                }
                else if (hasRigidbody2D && !rb2D.isKinematic)
                {
                    if (syncVelocity == SyncMode.XY)
                    {
                        if (changedPositionEnough)
                        {
                            rb2D.velocity = Vector2.Lerp(rb2D.velocity, targetState.velocity, actualPositionLerpSpeed);
                        }
                        else
                        {
                            rb2D.velocity = Vector2.zero;
                        }                        
                    }
                    if (syncAngularVelocity == SyncMode.Z)
                    {
                        if (changedRotationEnough)
                        {
                            rb2D.angularVelocity = Mathf.Lerp(rb2D.angularVelocity, targetState.angularVelocity.z, actualRotationLerpSpeed);
                        }
                        else
                        {
                            rb2D.angularVelocity = 0;
                        }
                    }
                }
                if (syncPosition != SyncMode.NONE)
                {
                    if (changedPositionEnough)
                    {
                        bool shouldTeleport = false;
                        if (distance > positionSnapThreshold)
                        {
                            actualPositionLerpSpeed = 1;
                            shouldTeleport = true;
                        }
                        Vector3 newPosition = getPosition();
                        if (isSyncingXPosition)
                        {
                            newPosition.x = targetState.position.x;
                        }
                        if (isSyncingYPosition)
                        {
                            newPosition.y = targetState.position.y;
                        }
                        if (isSyncingZPosition)
                        {
                            newPosition.z = targetState.position.z;
                        }
                        setPosition(Vector3.Lerp(getPosition(), newPosition, actualPositionLerpSpeed), shouldTeleport);
                    }
                }
                if (syncRotation != SyncMode.NONE)
                {
                    if (changedRotationEnough) 
                    {
                        bool shouldTeleport = false;
                        if (angleDifference > rotationSnapThreshold)
                        {
                            actualRotationLerpSpeed = 1;
                            shouldTeleport = true;
                        }
                        Vector3 newRotation = getRotation().eulerAngles;
                        if (isSyncingXRotation)
                        {
                            newRotation.x = targetState.rotation.eulerAngles.x;
                        }
                        if (isSyncingYRotation)
                        {
                            newRotation.y = targetState.rotation.eulerAngles.y;
                        }
                        if (isSyncingZRotation)
                        {
                            newRotation.z = targetState.rotation.eulerAngles.z;
                        }
                        Quaternion newQuaternion = Quaternion.Euler(newRotation);
                        setRotation(Quaternion.Lerp(getRotation(), newQuaternion, actualRotationLerpSpeed), shouldTeleport);
                    }
                }
                if (syncScale != SyncMode.NONE)
                {
                    if (changedScaleEnough)
                    {
                        bool shouldTeleport = false;
                        if (scaleDistance > scaleSnapThreshold)
                        {
                            actualScaleLerpSpeed = 1;
                            shouldTeleport = true;
                        }
                        Vector3 newScale = getScale();
                        if (isSyncingXScale)
                        {
                            newScale.x = targetState.scale.x;
                        }
                        if (isSyncingYScale)
                        {
                            newScale.y = targetState.scale.y;
                        }
                        if (isSyncingZScale)
                        {
                            newScale.z = targetState.scale.z;
                        }
                        setScale(Vector3.Lerp(getScale(), newScale, actualScaleLerpSpeed), shouldTeleport);
                    }
                }
            }
            else if (triedToExtrapolateTooFar)
            {
                if (hasRigdibody)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                if (hasRigidbody2D)
                {
                    rb2D.velocity = Vector2.zero;
                    rb2D.angularVelocity = 0;
                }
            }
        }

        /// <summary>
        /// Interpolate between two States from the stateBuffer in order calculate the targetState.
        /// </summary>
        /// <param name="interpolationTime">The target time</param>
        void interpolate(float interpolationTime, out State targetState)
        {
            // Go through buffer and find correct State to start at.
            int stateIndex = 0;
            for (; stateIndex < stateCount; stateIndex++)
            {
                if (stateBuffer[stateIndex].ownerTimestamp <= interpolationTime) break;
            }
            
            if (stateIndex == stateCount)
            {
                //Debug.LogError("Ran out of States in SmoothSync State buffer for object: " + gameObject.name);
                stateIndex--;
            }

            // The State one slot newer than the starting State.
            State end = stateBuffer[Mathf.Max(stateIndex - 1, 0)];
            // The starting playback State.
            State start = stateBuffer[stateIndex];

            // Calculate how far between the two States we should be.
            float t = (interpolationTime - start.ownerTimestamp) / (end.ownerTimestamp - start.ownerTimestamp);

            // Interpolate between the States to get the target State.
            targetState = State.Lerp(start, end, t);
        }

        /// <summary>
        /// Attempt to extrapolate from the newest State in the buffer
        /// </summary>
        /// <param name="interpolationTime">The target time</param>
        /// <returns>true on success, false if interpolationTime is more than extrapolationLength in the future</returns>
        bool extrapolate(float interpolationTime, out State targetState) // TODO: Wouldn't it make sense to at least extrapolate up to extrapolation limit even when it's trying to extrapolate too far?
        {
            // Start from the latest State
            targetState = new State(stateBuffer[0]);

            // See how far we will need to extrapolate.
            float extrapolationLength = (interpolationTime - targetState.ownerTimestamp) / 1000.0f;

            // If latest received velocity is close to zero, don't extrapolate. This is so we don't
            // try to extrapolate through the ground while at rest.
            if (syncVelocity == SyncMode.NONE || targetState.velocity.magnitude < sendVelocityThreshold)
            {
                return true;
            }

            if (((hasRigdibody && !rb.isKinematic) || (hasRigidbody2D && !rb2D.isKinematic)))
            {
                float simulatedTime = 0;
                while (simulatedTime < extrapolationLength)
                {
                    // Don't extrapolate for more than extrapolationTimeLimit.
                    if (simulatedTime > extrapolationTimeLimit)
                    {
                        if (extrapolationStopTime < lastTimeStateWasReceived)
                        {
                            extrapolationEndState = targetState;
                        }
                        extrapolationStopTime = Time.realtimeSinceStartup;
                        targetState = extrapolationEndState;
                        return false;
                    }

                    float timeDif = Mathf.Min(Time.fixedDeltaTime, extrapolationLength - simulatedTime);

                    // Velocity
                    targetState.position += targetState.velocity * timeDif;

                    // Gravity
                    if (hasRigdibody && rb.useGravity)
                    {
                        targetState.velocity += Physics.gravity * timeDif;
                    }
                    else if (hasRigidbody2D)
                    {
                        targetState.velocity += Physics.gravity * rb2D.gravityScale * timeDif;
                    }

                    // Drag
                    if (hasRigdibody)
                    {
                        targetState.velocity -= targetState.velocity * timeDif * rb.drag;
                    }
                    else if (hasRigidbody2D)
                    {
                        targetState.velocity -= targetState.velocity * timeDif * rb2D.drag;
                    }
                    
                    // Angular velocity
                    float axisLength = timeDif * targetState.angularVelocity.magnitude * Mathf.Rad2Deg;
                    Quaternion angularRotation = Quaternion.AngleAxis(axisLength, targetState.angularVelocity);
                    targetState.rotation = angularRotation * targetState.rotation;

                    // TODO: Angular drag?!

                    // Don't extrapolate for more than extrapolationDistanceLimit.
                    if (Vector3.Distance(stateBuffer[0].position, targetState.position) >= extrapolationDistanceLimit)
                    {
                        extrapolationEndState = targetState;
                        extrapolationStopTime = Time.realtimeSinceStartup;
                        targetState = extrapolationEndState;
                        return false;
                    }

                    simulatedTime += Time.fixedDeltaTime;
                }
            }

            return true;
        }

        /// <summary>Get position of object based on if child or not.</summary>
        public Vector3 getPosition()
        {
            if (hasChildObject)
            {
                return realObjectToSync.transform.localPosition;
            }
            else
            {
                return realObjectToSync.transform.position;
            }
        }
        /// <summary>Get rotation of object based on if child or not.</summary>
        public Quaternion getRotation()
        {
            if (hasChildObject)
            {
                return realObjectToSync.transform.localRotation;
            }
            else
            {
                return realObjectToSync.transform.rotation;
            }
        }
        /// <summary>Get scale of object.</summary>
        public Vector3 getScale()
        {
            return realObjectToSync.transform.localScale;
        }
        /// <summary>Set position of object based on if child or not.</summary>
        public void setPosition(Vector3 position, bool isTeleporting)
        {
            if (hasChildObject)
            {
                realObjectToSync.transform.localPosition = position;
            }
            else
            {
                if (hasRigdibody && !isTeleporting && whereToUpdateTransform != WhereToUpdateTransform.Update)
                {
                    rb.MovePosition(position);
                }
                if (hasRigidbody2D && !isTeleporting && whereToUpdateTransform != WhereToUpdateTransform.Update)
                {
                    rb2D.MovePosition(position);
                }
                else
                {
                    realObjectToSync.transform.position = position;
                }
            }
        }
        /// <summary>Set rotation of object based on if child or not.</summary>
        public void setRotation(Quaternion rotation, bool isTeleporting)
        {
            if (hasChildObject)
            {
                realObjectToSync.transform.localRotation = rotation;
            }
            else
            {
                if (hasRigdibody && !isTeleporting && whereToUpdateTransform != WhereToUpdateTransform.Update)
                {
                    rb.MoveRotation(rotation);
                }
                if (hasRigidbody2D && !isTeleporting && whereToUpdateTransform != WhereToUpdateTransform.Update)
                {
                    rb2D.MoveRotation(rotation.eulerAngles.z);
                }
                else
                {
                    realObjectToSync.transform.rotation = rotation;
                }
            }
        }
        /// <summary>Set scale of object.</summary>
        public void setScale(Vector3 scale, bool isTeleporting)
        {
            realObjectToSync.transform.localScale = scale;
        }

        #endregion Internal stuff

        #region Public interface

        /// <summary>Add an incoming state to the stateBuffer on non-owned objects.</summary>
        public void addState(State state)
        {
            if (stateCount > 1 && state.ownerTimestamp < stateBuffer[0].ownerTimestamp)
            {
                // This state arrived out of order and we already have a newer state.
                // TODO: It should be possible to add this state at the proper place in the buffer
                // but I think that would cause erratic behaviour.
                Debug.LogWarning("Received state out of order for: " + realObjectToSync.name);
                return;
            }

            lastTimeStateWasReceived = Time.realtimeSinceStartup;

            // Shift the buffer, deleting the oldest State.
            for (int i = stateBuffer.Length - 1; i >= 1; i--)
            {
                stateBuffer[i] = stateBuffer[i - 1];
            }

            // Add the new State at the front of the buffer.
            stateBuffer[0] = state;

            // Keep track of how many States are in the buffer.
            stateCount = Mathf.Min(stateCount + 1, stateBuffer.Length);
        }

        /// <summary>Stop updating the States of non-owned objects so that the object can be teleported.</summary>
        public void stopLerping()
        {
            dontLerp = true;
        }

        /// <summary>Resuming updating the States of non-owned objects after teleport.</summary>
        public void restartLerping()
        {
            if (!dontLerp) return;

            skipLerp = true;
        }
        /// <summary>Effectively clear the state buffer. Used for teleporting and ownership changes.</summary>
        public void clearBuffer()
        {
            stateCount = 0;
        }
        /// <summary>
        /// Teleport the player so that position will not be interpolated on non-owners.
        /// </summary>
        /// <remarks>
        /// How to use: Call teleport() on the owner and then send a command to all other clients with the
        /// networkTimestamp, position, and rotation.
        /// Receive teleport command on non-owners and call Smoothsync.teleport().
        /// Full example of use in the example scene in SmoothSyncExamplePlayerController.cs.
        /// </remarks>
        /// <param name="networkTimestamp">The NetworkTransport.GetNetworkTimestamp() on the owner when the teleport message was sent.</param>
        /// <param name="position">The position to teleport to.</param>
        /// <param name="rotation">The rotation to teleport to.</param>
        public void teleport(int networkTimestamp, Vector3 position, Quaternion rotation)
        {
            lastTeleportOwnerTime = networkTimestamp;
            setPosition(position, true);
            setRotation(rotation, true);
            clearBuffer();
            stopLerping();
        }
        /// <summary>
        /// Forces the State to be sent on owned objects the next time it goes through Update().
        /// </summary>
        /// <remarks>
        /// The state will get sent next frame regardless of all limitations.
        /// </remarks>
        public void forceStateSendNextFrame()
        {
            forceStateSend = true;
        }

        #endregion Public interface

        #region Networking

        /// <summary>Register network message handlers on server.</summary>
        public override void OnStartServer()
        {
            if (GetComponent<NetworkIdentity>().localPlayerAuthority)
            {
                if (!NetworkServer.handlers.ContainsKey(MsgType.SmoothSyncFromOwnerToServer))
                {
                    NetworkServer.RegisterHandler(MsgType.SmoothSyncFromOwnerToServer, HandleSyncFromOwnerToServer);
                }

                if (NetworkManager.singleton.client != null)
                {
                    if (!NetworkManager.singleton.client.handlers.ContainsKey(MsgType.SmoothSyncFromServerToNonOwners))
                    {
                        NetworkManager.singleton.client.RegisterHandler(MsgType.SmoothSyncFromServerToNonOwners, HandleSyncFromServerToNonOwners);
                    }
                }
            }
        }

        /// <summary>Register network message handlers on clients.</summary>
        public override void OnStartClient()
        {
            if (!NetworkServer.active)
            {
                if (!NetworkManager.singleton.client.handlers.ContainsKey(MsgType.SmoothSyncFromServerToNonOwners))
                {
                    NetworkManager.singleton.client.RegisterHandler(MsgType.SmoothSyncFromServerToNonOwners, HandleSyncFromServerToNonOwners);
                }
            }
        }

        /// <summary>
        /// Check if position has changed enough.
        /// </summary>
        /// <remarks>
        /// If sendPositionThreshold is 0, returns true if the current position is different than the latest sent position.
        /// If sendPositionThreshold is greater than 0, returns true if distance between position and latest sent position is greater 
        /// than the sendPositionThreshold.
        /// </remarks>
        public bool shouldSendPosition()
        {
            if (forceStateSend ||
                (getPosition() != lastPositionWhenStateWasSent &&
                (sendPositionThreshold == 0 || Vector3.Distance(lastPositionWhenStateWasSent, getPosition()) > sendPositionThreshold)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Check if rotation has changed enough.
        /// </summary>
        /// <remarks>
        /// If sendRotationThreshold is 0, returns true if the current rotation is different from the latest sent rotation.
        /// If sendRotationThreshold is greater than 0, returns true if difference (angle) between rotation and latest sent rotation is greater 
        /// than the sendRotationThreshold.
        /// </remarks>
        public bool shouldSendRotation()
        {
            if (forceStateSend ||
                (getRotation() != lastRotationWhenStateWasSent &&
                (sendRotationThreshold == 0 || Quaternion.Angle(lastRotationWhenStateWasSent, getRotation()) > sendRotationThreshold)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Check if scale has changed enough.
        /// </summary>
        /// <remarks>
        /// If sendScaleThreshold is 0, returns true if the current scale is different than the latest sent scale.
        /// If sendScaleThreshold is greater than 0, returns true if the difference between scale and latest sent scale is greater 
        /// than the sendScaleThreshold.
        /// </remarks>
        public bool shouldSendScale()
        {
            if (forceStateSend ||
                (getScale() != lastScaleWhenStateWasSent &&
                (sendScaleThreshold == 0 || Vector3.Distance(lastScaleWhenStateWasSent, getScale()) > sendScaleThreshold)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Check if velocity has changed enough.
        /// </summary>
        /// <remarks>
        /// If sendVelocityThreshold is 0, returns true if the current velocity is different from the latest sent velocity.
        /// If sendVelocityThreshold is greater than 0, returns true if difference between velocity and latest sent velocity is greater 
        /// than the velocity threshold.
        /// </remarks>
        public bool shouldSendVelocity()
        {
            if (hasRigdibody)
            {
                if (forceStateSend ||
                    (rb.velocity != lastVelocityWhenStateWasSent &&
                    (sendVelocityThreshold == 0 || Vector3.Distance(lastVelocityWhenStateWasSent, rb.velocity) > sendVelocityThreshold)))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (hasRigidbody2D)
            {
                if (forceStateSend ||
                    ((rb2D.velocity.x != lastVelocityWhenStateWasSent.x && rb2D.velocity.y != lastVelocityWhenStateWasSent.y) &&
                    (sendVelocityThreshold == 0 || Vector2.Distance(lastVelocityWhenStateWasSent, rb2D.velocity) > sendVelocityThreshold)))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Check if angular velocity has changed enough.
        /// </summary>
        /// <remarks>
        /// If sendAngularVelocityThreshold is 0, returns true if the current angular velocity is different from the latest sent angular velocity.
        /// If sendAngularVelocityThreshold is greater than 0, returns true if difference between angular velocity and latest sent angular velocity is 
        /// greater than the angular velocity threshold.
        /// </remarks>
        public bool shouldSendAngularVelocity()
        {
            if (hasRigdibody)
            {
                if (forceStateSend ||
                    (rb.angularVelocity != lastAngularVelocityWhenStateWasSent && 
                    (sendAngularVelocityThreshold == 0 || 
                    Vector3.Distance(lastAngularVelocityWhenStateWasSent, rb.angularVelocity) > sendAngularVelocityThreshold)))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (hasRigidbody2D)
            {
                if (forceStateSend ||
                    (rb2D.angularVelocity != lastAngularVelocityWhenStateWasSent.z &&
                    (sendAngularVelocityThreshold == 0 ||
                    Mathf.Abs(lastAngularVelocityWhenStateWasSent.z - rb2D.angularVelocity) > sendAngularVelocityThreshold)))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        #region Sync Properties
        /// <summary>
        /// Determine if should be syncing.
        /// </summary>
        public bool isSyncingXPosition
        {
            get
            {
                return syncPosition == SyncMode.XYZ ||
                     syncPosition == SyncMode.XY ||
                     syncPosition == SyncMode.XZ ||
                     syncPosition == SyncMode.X;
            }
        }
        /// <summary>
        /// Determine if should be syncing.
        /// </summary>
        public bool isSyncingYPosition
        {
            get
            {
                return syncPosition == SyncMode.XYZ ||
                     syncPosition == SyncMode.XY ||
                     syncPosition == SyncMode.YZ ||
                     syncPosition == SyncMode.Y;
            }
        }
        /// <summary>
        /// Determine if should be syncing.
        /// </summary>
        public bool isSyncingZPosition
        {
            get
            {
                return syncPosition == SyncMode.XYZ ||
                     syncPosition == SyncMode.XZ ||
                     syncPosition == SyncMode.YZ ||
                     syncPosition == SyncMode.Z;
            }
        }
        /// <summary>
        /// Determine if should be syncing.
        /// </summary>
        public bool isSyncingXRotation
        {
            get
            {
                return syncRotation == SyncMode.XYZ ||
                     syncRotation == SyncMode.XY ||
                     syncRotation == SyncMode.XZ ||
                     syncRotation == SyncMode.X;
            }
        }
        /// <summary>
        /// Determine if should be syncing.
        /// </summary>
        public bool isSyncingYRotation
        {
            get
            {
                return syncRotation == SyncMode.XYZ ||
                     syncRotation == SyncMode.XY ||
                     syncRotation == SyncMode.YZ ||
                     syncRotation == SyncMode.Y;
            }
        }
        /// <summary>
        /// Determine if should be syncing.
        /// </summary>
        public bool isSyncingZRotation
        {
            get
            {
                return syncRotation == SyncMode.XYZ ||
                     syncRotation == SyncMode.XZ ||
                     syncRotation == SyncMode.YZ ||
                     syncRotation == SyncMode.Z;
            }
        }
        /// <summary>
        /// Determine if should be syncing.
        /// </summary>
        public bool isSyncingXScale
        {
            get
            {
                return syncScale == SyncMode.XYZ ||
                     syncScale == SyncMode.XY ||
                     syncScale == SyncMode.XZ ||
                     syncScale == SyncMode.X;
            }
        }
        /// <summary>
        /// Determine if should be syncing.
        /// </summary>
        public bool isSyncingYScale
        {
            get
            {
                return syncScale == SyncMode.XYZ ||
                     syncScale == SyncMode.XY ||
                     syncScale == SyncMode.YZ ||
                     syncScale == SyncMode.Y;
            }
        }
        /// <summary>
        /// Determine if should be syncing.
        /// </summary>
        public bool isSyncingZScale
        {
            get
            {
                return syncScale == SyncMode.XYZ ||
                     syncScale == SyncMode.XZ ||
                     syncScale == SyncMode.YZ ||
                     syncScale == SyncMode.Z;
            }
        }
        /// <summary>
        /// Determine if should be syncing.
        /// </summary>
        public bool isSyncingXVelocity
        {
            get
            {
                return syncVelocity == SyncMode.XYZ ||
                     syncVelocity == SyncMode.XY ||
                     syncVelocity == SyncMode.XZ ||
                     syncVelocity == SyncMode.X;
            }
        }
        /// <summary>
        /// Determine if should be syncing.
        /// </summary>
        public bool isSyncingYVelocity
        {
            get
            {
                return syncVelocity == SyncMode.XYZ ||
                     syncVelocity == SyncMode.XY ||
                     syncVelocity == SyncMode.YZ ||
                     syncVelocity == SyncMode.Y;
            }
        }
        /// <summary>
        /// Determine if should be syncing.
        /// </summary>
        public bool isSyncingZVelocity
        {
            get
            {
                return syncVelocity == SyncMode.XYZ ||
                     syncVelocity == SyncMode.XZ ||
                     syncVelocity == SyncMode.YZ ||
                     syncVelocity == SyncMode.Z;
            }
        }
        /// <summary>
        /// Determine if should be syncing.
        /// </summary>
        public bool isSyncingXAngularVelocity
        {
            get
            {
                return syncAngularVelocity == SyncMode.XYZ ||
                     syncAngularVelocity == SyncMode.XY ||
                     syncAngularVelocity == SyncMode.XZ ||
                     syncAngularVelocity == SyncMode.X;
            }
        }
        /// <summary>
        /// Determine if should be syncing.
        /// </summary>
        public bool isSyncingYAngularVelocity
        {
            get
            {
                return syncAngularVelocity == SyncMode.XYZ ||
                     syncAngularVelocity == SyncMode.XY ||
                     syncAngularVelocity == SyncMode.YZ ||
                     syncAngularVelocity == SyncMode.Y;
            }
        }
        /// <summary>
        /// Determine if should be syncing.
        /// </summary>
        public bool isSyncingZAngularVelocity
        {
            get
            {
                return syncAngularVelocity == SyncMode.XYZ ||
                     syncAngularVelocity == SyncMode.XZ ||
                     syncAngularVelocity == SyncMode.YZ ||
                     syncAngularVelocity == SyncMode.Z;
            }
        }
        #endregion

        /// <summary>Called on the host to send the owner's State to non-owners.</summary>
        /// <remarks>
        /// The host does not send to itself nor does it send an owner's own State back to the owner.
        /// </remarks>
        /// <param name="state">The owner's State at the time the message was sent</param>
        [Server]
        void SendStateToNonOwners(MessageBase state)
        {
            // Skip sending the Command to ourselves and immediately send to all non-owners.
            for (int i = 0; i < NetworkServer.connections.Count; i++)
            {
                NetworkConnection conn = NetworkServer.connections[i];
                
                // Skip sending to clientAuthorityOwner since owners don't need their own State back.
                // Also skip sending to localClient (hostId == -1) since the State was already recorded.
                if (conn != null && conn != netID.clientAuthorityOwner && conn.hostId != -1 && conn.isReady)
                {
                    if (isObservedByConnection(conn) == false) continue;
                    // Send the message, this calls HandleRigidbodySyncFromServerToNonOwners on the receiving clients.
                    conn.SendByChannel(MsgType.SmoothSyncFromServerToNonOwners, state, networkChannel);
                }
            }
        }

        /// <summary>The server checks if it should send based on Network Proximity Checker.</summary>
        /// <remarks>
        /// Checks who it should send update information to. Will send to everyone unless something like the
        /// Network Proximity Checker is limiting it.
        /// </remarks>
        bool isObservedByConnection(NetworkConnection conn)
        {
            for (int i = 0; i < netID.observers.Count; i++)
            {
                if (netID.observers[i] == conn)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Receive incoming State on non-owners.</summary>
        /// <remarks>
        /// This static method receives incoming State messages for all SmoothSync objects and uses
        /// the netID included in the message to find the target game object.
        /// Calls NonOwnerReceiveState() on the target SmoothSync object.
        /// </remarks>
        static void HandleSyncFromServerToNonOwners(NetworkMessage msg)
        {
            NetworkState networkState = msg.ReadMessage<NetworkState>();

            if (networkState != null && networkState.smoothSync != null && !networkState.smoothSync.hasAuthority)
            {
                networkState.smoothSync.adjustOwnerTime(networkState.state.ownerTimestamp);
                if (networkState.state.ownerTimestamp > networkState.smoothSync.lastTeleportOwnerTime)
                {
                    networkState.smoothSync.restartLerping();
                    networkState.smoothSync.addState(networkState.state);
                }
            }
        }

        /// <summary>Receive owner's State on the host and send it back out to all non-owners.</summary>
        /// <remarks>
        /// This static method receives incoming State messages for all SmoothSync objects and uses
        /// the netID included in the message to find the target game object.
        /// Calls addState() and SendStateToNonOwners() on the target SmoothSync object.
        /// </remarks>
        static void HandleSyncFromOwnerToServer(NetworkMessage msg)
        {
            NetworkState networkState = msg.ReadMessage<NetworkState>();

            // Ignore all messages that do not match the server determined authority.
            if (networkState.smoothSync.netID.clientAuthorityOwner != msg.conn) return;

            // Always accept the first State so we have something to compare to. (if latestValidatedState == null)
            // Check each other State to make sure it passes the validation method. By default all States are accepted.
            // To tie in your own validation method, see the SmoothSyncExample scene and SmoothSyncExamplePlayerController.cs. 
            if (networkState.smoothSync.latestValidatedState == null ||
                networkState.smoothSync.validateStateMethod(networkState.state, networkState.smoothSync.latestValidatedState))
            {
                networkState.smoothSync.latestValidatedState = networkState.state;
                networkState.smoothSync.adjustOwnerTime(networkState.state.ownerTimestamp);
                networkState.smoothSync.SendStateToNonOwners(networkState);
                if (networkState.state.ownerTimestamp > networkState.smoothSync.lastTeleportOwnerTime)
                {
                    networkState.smoothSync.restartLerping();
                    networkState.smoothSync.addState(networkState.state);
                }
            }
        }

        public override float GetNetworkSendInterval()
        {
            return 1 / sendRate;
        }

        public override int GetNetworkChannel()
        {
            return networkChannel;
        }

        #region Time stuff

        /// <summary>
        /// The last owner time received over the network
        /// </summary>
        int _ownerTime;

        /// <summary>
        /// The realTimeSinceStartup when we received the last owner time.
        /// </summary>
        float lastTimeOwnerTimeWasSet;

        /// <summary>
        /// The current estimated time on the owner.
        /// </summary>
        /// <remarks>
        /// Time comes from the owner in every sync message.
        /// When it is received we set _ownerTime and lastTimeOwnerTimeWasSet.
        /// Then when we want to know what time it is we add time elapsed to the last _ownerTime we received.
        /// </remarks>
        public int approximateNetworkTimeOnOwner
        {
            get
            {
                return _ownerTime + (int)((Time.realtimeSinceStartup - lastTimeOwnerTimeWasSet) * 1000);
            }
            set
            {
                _ownerTime = value;
                lastTimeOwnerTimeWasSet = Time.realtimeSinceStartup;
            }
        }

        /// <summary>
        /// Adjust owner time based on latest timestamp.
        /// </summary>
        void adjustOwnerTime(int ownerTimestamp) // TODO: I'd love to see a graph of owner time
        {
            int newTime = ownerTimestamp;

            int maxTimeChange = 50;
            int timeChangeMagnitude = Mathf.Abs(approximateNetworkTimeOnOwner - newTime);
            if (approximateNetworkTimeOnOwner == 0 || timeChangeMagnitude < maxTimeChange || timeChangeMagnitude > maxTimeChange * 10)
            {
                approximateNetworkTimeOnOwner = newTime;
            }
            else
            {
                if (approximateNetworkTimeOnOwner < newTime)
                {
                    approximateNetworkTimeOnOwner += maxTimeChange;
                }
                else
                {
                    approximateNetworkTimeOnOwner -= maxTimeChange;
                }
            }
        }

        #endregion

        #endregion Networking
    }
}