using nickeltin.InternalBridge.Runtime;
using UnityEditor;

namespace nickeltin.InternalBridge.Editor
{
    public static class _Selection
    {
        public static EID activeEID
        {
            get
            {
#if UNITY_6000_3_OR_NEWER
                return Selection.activeEntityId;
#else
                return Selection.activeInstanceID;
#endif
            }
            set
            {
#if UNITY_6000_3_OR_NEWER
                Selection.activeEntityId = value;
#else
                Selection.activeInstanceID = (int)value;
#endif
            }
        }
    }
}