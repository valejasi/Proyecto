using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyBackgroundLoader : MonoBehaviour
{
    [SerializeField] private string mapSceneName = "MapScene"; // nombre exacto de tu escena

    void Start()
    {
        if (!SceneManager.GetSceneByName(mapSceneName).isLoaded)
        {
            SceneManager.LoadSceneAsync(mapSceneName, LoadSceneMode.Additive);
        }
    }
}