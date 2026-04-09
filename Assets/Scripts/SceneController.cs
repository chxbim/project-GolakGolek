using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadMainGame()
    {
        SceneManager.LoadScene("MainGameScene");
    }

    public void LoadSelectMode()
    {
        SceneManager.LoadScene("SelectMode");
    }

    public void LoadProfileScene()
    {
        SceneManager.LoadScene("ProfileScene");
    }

    public void LoadSettingScene()
    {
        SceneManager.LoadScene("SettingScene");
    }

    public void LoadHomeScene()
    {
        SceneManager.LoadScene("HomeScene");
    }
}