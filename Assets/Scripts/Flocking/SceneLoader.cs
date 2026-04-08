using UnityEngine;
using UnityEngine.SceneManagement;

// loads a configured scene when called from a button or event
public class SceneLoader : MonoBehaviour
{
    // -------------------------------------------------------------------------------------------------------------

    [SerializeField]
    [Tooltip("Scene name loaded by this component")]
    private string sceneName = "Level1";

    // loads the configured scene
    public void LoadScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
