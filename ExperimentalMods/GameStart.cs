using UnityEngine;
using UnityEngine.SceneManagement;

namespace ExperimentalMods
{
    public class GameStart : MonoBehaviour
    {
        void Start ()
        {
            SceneManager.LoadScene("Game");
        }
    }
}
