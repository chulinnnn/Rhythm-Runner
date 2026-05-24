using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChangeScene : MonoBehaviour
{
    private Button clickBtn;
    public int sceneIndex;
    public string sceneName;

    void Start()
    {
        clickBtn = GetComponent<Button>();
        if (clickBtn != null)
        {
            clickBtn.onClick.AddListener(LoadConfiguredScene);
        }
    }

    private void LoadConfiguredScene()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneTransitionManager.LoadScene(sceneName);
            return;
        }

        SceneTransitionManager.LoadScene(sceneIndex);
    }

    public void OnChangeScene(int sceneIndex)
    {
        SceneTransitionManager.LoadScene(sceneIndex);
    }

    public void OnChangeScene(string sceneName)
    {
        SceneTransitionManager.LoadScene(sceneName);
    }

    public void EG()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
