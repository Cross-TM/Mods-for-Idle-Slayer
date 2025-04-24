using UnityEngine;
using UnityEngine.SceneManagement;

namespace ExperimentalMods
{
    public class GameStart : MonoBehaviour
    {
        void Start ()
        {
            Scene currentScene = SceneManager.GetActiveScene();

            if (currentScene.name == "Title Screen")
            {
                SceneManager.LoadScene("Game");
            }
        }
    }
}