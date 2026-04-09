using UnityEngine;

// shows a button group once the level reaches a win or lose state
public class EndScreenButtons : MonoBehaviour
{
    // -------------------------------------------------------------------------------------------------------------

    [SerializeField]
    [Tooltip("Level objective used to decide when buttons should appear")]
    private LevelObjective levelObjective;

    [SerializeField]
    [Tooltip("Child object containing the restart and next level buttons")]
    private GameObject buttonGroup;

    // hides the button group until the level ends
    private void Awake()
    {
        if (levelObjective == null)
        {
            levelObjective = FindFirstObjectByType<LevelObjective>();
        }

        if (buttonGroup == null)
        {
            buttonGroup = gameObject;
        }

        buttonGroup.SetActive(false);
    }

    // reveals the buttons once the level is over
    private void Update()
    {
        if (levelObjective == null || buttonGroup == null)
        {
            return;
        }

        bool shouldShow = levelObjective.DidWin || levelObjective.DidLose;

        if (buttonGroup.activeSelf != shouldShow)
        {
            buttonGroup.SetActive(shouldShow);
        }
    }
}
