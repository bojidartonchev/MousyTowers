namespace Smooth
{
    public class MsgType : UnityEngine.Networking.MsgType
    {
        public static short SmoothSyncFromServerToNonOwners = short.MaxValue - 2;
        public static short SmoothSyncFromOwnerToServer = short.MaxValue - 1;
    }
}