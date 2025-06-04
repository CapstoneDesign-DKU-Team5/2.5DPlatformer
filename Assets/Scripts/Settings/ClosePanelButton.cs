using UnityEngine;

public class ClosePanelButton : MonoBehaviour
{
    [Tooltip("���� ��� �г�")]
    public GameObject targetPanel;

    public void ClosePanel()
    {
        if (targetPanel != null)
        {
            targetPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("ClosePanelButton�� targetPanel�� �������� �ʾҾ��!", this);
        }
    }
}