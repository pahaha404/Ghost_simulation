/*
 * 파일 역할: 프롤로그와 GameScene 사이의 검은 화면 전환을 만든다.
 * - FadeFromBlack(): 최상위 Overlay Canvas와 검은 Image를 임시 생성한다.
 * - FadeFromBlackAndLoadScene(): 전환 오브젝트를 유지한 채 GameScene을 로드한다.
 * - Reveal(): 잠시 검은 화면을 유지한 뒤 alpha를 줄여 게임 화면을 서서히 보여준다.
 * 전환이 끝나면 생성했던 임시 오브젝트를 자동으로 삭제한다.
 */
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GhostCounselor
{
    /// <summary>Temporary full-screen layer used to reveal the game after the prologue.</summary>
    public sealed class PrologueScreenFade : MonoBehaviour
    {
        private static PrologueScreenFade activeFade;

        private Image overlay;

        public static void FadeFromBlack(float holdDuration, float fadeDuration)
        {
            if (activeFade != null)
                Destroy(activeFade.gameObject);

            GameObject root = new GameObject("Prologue Transition - Fade Out", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = short.MaxValue;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            PrologueScreenFade fade = root.AddComponent<PrologueScreenFade>();
            fade.overlay = new GameObject("Warm Black Overlay", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            fade.overlay.transform.SetParent(root.transform, false);
            fade.overlay.raycastTarget = false;
            fade.overlay.color = new Color(0.018f, 0.014f, 0.025f, 1f);
            Stretch(fade.overlay.rectTransform);

            activeFade = fade;
            fade.StartCoroutine(fade.Reveal(holdDuration, fadeDuration));
        }

        /// <summary>Keeps the black layer alive while the separate game scene is loaded.</summary>
        public static void FadeFromBlackAndLoadScene(string sceneName, float holdDuration, float fadeDuration)
        {
            FadeFromBlack(holdDuration, fadeDuration);
            DontDestroyOnLoad(activeFade.gameObject);
            SceneManager.LoadScene(sceneName);
        }

        private IEnumerator Reveal(float holdDuration, float fadeDuration)
        {
            yield return new WaitForSecondsRealtime(holdDuration);

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / fadeDuration);
                float alpha = 1f - Mathf.SmoothStep(0f, 1f, progress);
                overlay.color = new Color(0.018f, 0.014f, 0.025f, alpha);
                yield return null;
            }

            activeFade = null;
            Destroy(gameObject);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
