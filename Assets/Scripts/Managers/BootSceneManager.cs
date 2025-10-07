using UnityEngine;
using UnityEngine.SceneManagement;

public class BootSceneManager : MonoBehaviour
{
    void Start()
    {
        SceneManager.LoadScene(1);
    }
}
