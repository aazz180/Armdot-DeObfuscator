using BepInEx;
using HarmonyLib;

namespace SampleDLL
{
    [BepInPlugin(Name, GUID, Version)]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            if (!patchedHarmony)
            {
                new Harmony(GUID).PatchAll();
                patchedHarmony = true;
            }
        }

        public const string Name = "Sample";
        public const string GUID = "com.Sample.dll";
        public const string Version = "1.0";
        private bool patchedHarmony;
    }
}
