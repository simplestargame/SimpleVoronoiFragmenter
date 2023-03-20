using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SimplestarGame
{
    public class RestartUI : MonoBehaviour
    {
        [SerializeField] Button buttonRestart;

        void Start()
        {
            this.buttonRestart.onClick.AddListener(this.OnRestart);
        }

        void OnRestart()
        {
            SceneManager.LoadScene(0);
        }
    }
}