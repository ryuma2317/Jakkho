using UnityEngine;

namespace nickeltin.InternalBridge.Runtime
{
    public static class EIDExtensions
    {
        public static EID GetEID<T>(this LazyLoadReference<T> lazyLoadReference) where T : Object
        {
#if UNITY_6000_3_OR_NEWER
            return lazyLoadReference.entityId;
#else
            return lazyLoadReference.instanceID;
#endif
        }
    }
}