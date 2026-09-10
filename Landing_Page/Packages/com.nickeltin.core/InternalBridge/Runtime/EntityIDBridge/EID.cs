using UnityEngine;

namespace nickeltin.InternalBridge.Runtime
{
    /// <summary>
    /// Wrapper for EntityId (Unity 6.2+) with fallback to int instanceId on older versions.
    /// </summary>
    public readonly struct EID
    {
#if UNITY_6000_2_OR_NEWER
        private readonly EntityId _entityID;

        private EID(EntityId entityID) => _entityID = entityID;

        public static EID None => new(EntityId.None);

        public static implicit operator EntityId(EID eid) => eid._entityID;
        public static implicit operator EID(EntityId entityID) => new(entityID);

#if UNITY_6000_4_OR_NEWER
        // 6.4+: From → FromULong
        public static implicit operator EID(ulong raw) => new(EntityId.FromULong(raw));

#if !UNITY_6000_5_OR_NEWER
        // 6.4 only: EntityId is still effectively 32-bit; int construction is OK.
        // 6.5+: EntityId is 64-bit — do not construct from int.
        public static implicit operator EID(int instanceID) => new(EntityId.FromULong((ulong)instanceID));
#endif

#else
        // 6.2 / 6.3: EntityId <-> int via Unity implicit operators (From/FromULong not available yet)
        public static implicit operator EID(int instanceID) => new(instanceID);
        public static explicit operator int(EID eid) => eid._entityID;
#endif

#else
        private readonly int _instanceID;

        private EID(int instanceID) => _instanceID = instanceID;

        public static EID None => new(0);

        public static implicit operator int(EID eid) => eid._instanceID;
        public static implicit operator EID(int instanceID) => new(instanceID);
#endif
    }
}
