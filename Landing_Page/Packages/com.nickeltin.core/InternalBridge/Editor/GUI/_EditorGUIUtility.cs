using nickeltin.InternalBridge.Runtime;
using UnityEditor;
using UnityEngine;

namespace nickeltin.InternalBridge.Editor
{
    public class _EditorGUIUtility
    {
        public static GUIContent TempContent(string t, Texture i)
        {
            return EditorGUIUtility.TempContent(t, i);
        }

        public static void PingObject(EID eid)
        {
#if UNITY_6000_4_OR_NEWER
            EditorGUIUtility.PingObject(eid);       
#else
            EditorGUIUtility.PingObject((int)eid);
#endif
        }
    }
}