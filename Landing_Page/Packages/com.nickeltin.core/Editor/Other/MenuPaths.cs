namespace nickeltin.Core.Editor
{
    public static class MenuPaths
    {
        public const string TOOLBAR = "Tools/[nklt] Core/";
        public const string GAME_OBJECT_CREATE_UI = "GameObject/" + 
#if UNITY_6000_3_OR_NEWER
                                                    "UI (Canvas)/";
#else
                                                    "UI/";
#endif
    }
}