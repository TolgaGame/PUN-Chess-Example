using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CopyToClipboard : MonoBehaviour
{
    public TextMeshProUGUI textMeshProText;
    public Button copyButton;

    private void Start()
    {
        copyButton.onClick.AddListener(CopyTextToClipboard);
    }

    private void CopyTextToClipboard()
    {
        string textToCopy = textMeshProText.text;
        GUIUtility.systemCopyBuffer = textToCopy;

        Debug.Log("Metin panoya kopyalandı: " + textToCopy);
    }
}
