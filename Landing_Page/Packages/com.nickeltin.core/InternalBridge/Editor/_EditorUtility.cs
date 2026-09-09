using nickeltin.InternalBridge.Runtime;
using UnityEditor;
using UnityEngine;

namespace nickeltin.InternalBridge.Editor
{
    public static class _EditorUtility
    {
        public static Object EIDToObject(EID eid)
        {
#if UNITY_6000_3_OR_NEWER
            return EditorUtility.EntityIdToObject(eid);
#else
            return EditorUtility.InstanceIDToObject((int)eid);
#endif
        }
    }
}