using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrosshairController : MonoBehaviour
{
    [SerializeField] private CanvasGroup hitmarker;
    [SerializeField] private CanvasGroup crosshair;
    [SerializeField] private CanvasGroup crosshairHit;

    [Header("Hitmarker details")]
    [SerializeField] private float fadeDuration = 1.2f;
    private bool isFading = false;
    private Coroutine fadingCoroutine = null;
    void Start()
    {
        hitmarker.alpha = 0f;
        crosshair.alpha = 1f;
        crosshairHit.alpha = 0f;
    }

    public void ShowHitmarker()
    {
        if (isFading)
        {
            // Stop coroutine
            if (fadingCoroutine != null)
            {
                StopCoroutine(fadingCoroutine);
            }
            isFading = false;
        }

        hitmarker.alpha = 1f;
        fadingCoroutine = StartCoroutine(FadeOut());
    }
    private IEnumerator FadeOut()
    {
        isFading = true;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            hitmarker.alpha = Mathf.Lerp(1f, 0f, elapsedTime/fadeDuration);
            yield return null;
        }

        hitmarker.alpha = 0f;
        isFading=false;
    }
    public void ShowCrosshairHit()
    {
        crosshair.alpha = 0f;
        crosshairHit.alpha = 1f;
    }
    public void HideCrosshairHit()
    {
        crosshair.alpha = 1f;
        crosshairHit.alpha = 0f;
    }
    public void HideAllCrosshair()
    {
        crosshair.alpha = 0f;
        crosshairHit.alpha = 0f;
    }
}
