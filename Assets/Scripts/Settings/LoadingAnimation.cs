using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[AddComponentMenu("UI/Loading Animation")]
public class LoadingAnimation : MonoBehaviour
{
    [Header("Loading Text (TMP)")]
    public TMP_Text loadingText;

    [Header("Loading Image (Optional)")]
    public Image loadingImage;

    [Header("Loading Sprites (Optional)")]
    public Sprite[] loadingSprites;

    [Header("Base Message")]
    [TextArea]
    public string baseMessage = "Loading";

    private Coroutine animationCoroutine;

    private void OnEnable()
    {
        StartAnimation();
    }

    private void OnDisable()
    {
        StopAnimation();
    }

    /// <summary>
    /// Begins the loading dots animation.
    /// </summary>
    public void StartAnimation()
    {
        if (animationCoroutine == null)
        {
            animationCoroutine = StartCoroutine(PlayLoadingAnimation());
        }
    }

    /// <summary>
    /// Stops the loading dots animation.
    /// </summary>
    public void StopAnimation()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
    }

    private IEnumerator PlayLoadingAnimation()
    {
        int dotCount = 0;
        int spriteIndex = 0;

        while (true)
        {
            // Update text with dots
            if (loadingText != null)
            {
                loadingText.text = baseMessage + new string('.', dotCount % 4);
            }

            // Update image sprite if provided
            if (loadingImage != null && loadingSprites != null && loadingSprites.Length > 0)
            {
                loadingImage.sprite = loadingSprites[spriteIndex % loadingSprites.Length];
                spriteIndex++;
            }

            dotCount++;
            yield return new WaitForSeconds(0.1f);
        }
    }
}
