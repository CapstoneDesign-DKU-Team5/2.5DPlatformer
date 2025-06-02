using UnityEngine;

public class ClosePanelButton : MonoBehaviour
{
    [Tooltip("닫힐 대상 패널")]
    public GameObject targetPanel;

    public void ClosePanel()
    {
        if (targetPanel != null)
        {
            targetPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("ClosePanelButton에 targetPanel이 지정되지 않았어요!", this);
        }
    }
}