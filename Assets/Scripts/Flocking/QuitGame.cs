using UnityEngine;

// quits the game when called from a UI button
public class QuitGame : MonoBehaviour
{
    // exits play mode in editor and quits the application in a build
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
