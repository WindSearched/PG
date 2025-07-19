using TMPro;
using UnityEngine;

public class EscPage : MonoBehaviour
{
    public GameObject exit_b;

    private void Start()
    {
        Page.exits["esc"] = () =>
        {
            gameObject.SetActive(false);
            exit_b.GetComponentInChildren<TextMeshProUGUI>().text = "exit";
        };
    }
    public void ExitB()
    {
        if (exitConfirm)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        UnityEngine.Application.Quit();
#endif
        }
        else
        {
            exit_b.GetComponentInChildren<TextMeshProUGUI>().text += "?";
            exitConfirm = true;
        }
    }


    public bool exitConfirm = false;
}
