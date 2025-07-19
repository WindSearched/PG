using TMPro;
using UnityEngine;

public class Noter : MonoBehaviour
{
    public RectTransform rect;
    public TextMeshProUGUI text;
    public string note;
    public static string color = "#FFFF00";
    private void Start()
    {
        Enable();
    }
    public void Enable()
    {
        text.text = note;

        NoteManager.noters.Add(this);
        NoteManager.UpdateNoter();
    }
    private void OnDisable()
    {
        NoteManager.noters.Remove(this);
        NoteManager.UpdateNoter();
    }
}
