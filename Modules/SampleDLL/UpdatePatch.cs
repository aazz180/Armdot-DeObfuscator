using GorillaLocomotion;
using HarmonyLib;
using UnityEngine;

namespace SampleDLL
{
    [HarmonyPatch(typeof(Player), "FixedUpdate")] // the patch for my game
    internal class UpdatePatch
    {
        private static void Postfix()
        {
            if (!alreadyInit)
            {
                alreadyInit = true;
                gameObject = new GameObject();
                gameObject.AddComponent<Class1>();
                UnityEngine.Object.DontDestroyOnLoad(gameObject);
            }
        }

        private static bool alreadyInit;
        public static GameObject gameObject;
    }
}
