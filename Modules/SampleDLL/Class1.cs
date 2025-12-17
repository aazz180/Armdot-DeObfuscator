using UnityEngine;

namespace SampleDLL
{
    internal class Class1 : MonoBehaviour
    {
        public static bool testToggle,
            OffUpdate;

        public void Update()
        {
            if (OffUpdate)
            {
                Debug.Log("Sample.dll is working");
            }
            if (testToggle)
            {
                Debug.Log("Test Toggle is working");
            }
        }

        public void OnGUI()
        {
            OffUpdate = GUILayout.Toggle(OffUpdate, "Turn off it saying \"Sample.dll is working\"");
            if (GUILayout.Button("Test Button"))
            {
                Debug.Log("Test Button is working");
            }
            testToggle = GUILayout.Toggle(testToggle, "Test Toggle");
            GUILayout.Label("Test Label");
        }
    }
}
