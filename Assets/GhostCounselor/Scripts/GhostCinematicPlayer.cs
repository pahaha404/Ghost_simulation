using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

namespace GhostCounselor
{
    [Serializable]
    public sealed class GhostCinematicClipEntry
    {
        public string cinematicId;
        public VideoClip clip;
    }

    /// <summary>귀신별 성불 시네마틱 영상 목록입니다. Resources/GhostCinematicCatalog.asset로 로드됩니다.</summary>
    public sealed class GhostCinematicCatalog : ScriptableObject
    {
        public GhostCinematicClipEntry[] clips = Array.Empty<GhostCinematicClipEntry>();

        public VideoClip Find(string cinematicId)
        {
            if (string.IsNullOrWhiteSpace(cinematicId) || clips == null)
                return null;

            foreach (GhostCinematicClipEntry entry in clips)
            {
                if (entry != null && entry.cinematicId == cinematicId)
                    return entry.clip;
            }

            return null;
        }
    }

    /// <summary>
    /// 성불 순간에만 표시되는 전체화면 영상 재생기입니다.
    /// 기존 Canvas/RectTransform을 이동하지 않고 별도 Overlay Canvas를 런타임에 생성합니다.
    /// 영상이 없거나 재생에 실패하면 호출자가 기존 텍스트 결말로 진행할 수 있도록 false를 반환합니다.
    /// </summary>
    public sealed class GhostCinematicPlayer : MonoBehaviour
    {
        private const string CatalogResourceName = "GhostCinematicCatalog";
        private const int SortingOrder = 5000;

        private Canvas overlayCanvas;
        private CanvasGroup canvasGroup;
        private RawImage videoImage;
        private VideoPlayer videoPlayer;
        private RenderTexture renderTexture;
        private Action completed;
        private bool isPlaying;

        public bool IsPlaying => isPlaying;

        public static bool TryPlay(string cinematicId, Action onComplete)
        {
            GhostCinematicCatalog catalog = Resources.Load<GhostCinematicCatalog>(CatalogResourceName);
            VideoClip clip = catalog?.Find(cinematicId);
            if (clip == null)
            {
                Debug.LogWarning($"[GhostCinematic] 영상 매핑을 찾지 못했습니다: {cinematicId}");
                return false;
            }

            GhostCinematicPlayer player = FindAnyObjectByType<GhostCinematicPlayer>();
            if (player == null)
            {
                GameObject host = new GameObject("Ghost Cinematic Player");
                player = host.AddComponent<GhostCinematicPlayer>();
                DontDestroyOnLoad(host);
            }

            return player.Play(clip, onComplete);
        }

        private void Awake()
        {
            BuildOverlay();
            HideOverlay();
        }

        private void Update()
        {
            if (isPlaying && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                StopAndComplete();
        }

        public bool Play(VideoClip clip, Action onComplete)
        {
            if (clip == null)
                return false;

            BuildOverlay();
            completed = onComplete;
            isPlaying = true;
            canvasGroup.alpha = 1f;
            overlayCanvas.gameObject.SetActive(true);
            videoPlayer.clip = clip;
            videoPlayer.Play();
            return true;
        }

        private void OnVideoFinished(VideoPlayer source)
        {
            StopAndComplete();
        }

        private void StopAndComplete()
        {
            if (!isPlaying)
                return;

            isPlaying = false;
            videoPlayer.Stop();
            HideOverlay();
            Action callback = completed;
            completed = null;
            callback?.Invoke();
        }

        private void BuildOverlay()
        {
            if (overlayCanvas != null)
                return;

            GameObject canvasObject = new GameObject("Ghost Cinematic Fullscreen Overlay");
            canvasObject.transform.SetParent(transform, false);
            overlayCanvas = canvasObject.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = SortingOrder;
            canvasObject.AddComponent<GraphicRaycaster>();
            canvasGroup = canvasObject.AddComponent<CanvasGroup>();

            GameObject imageObject = new GameObject("Cinematic Video");
            imageObject.transform.SetParent(canvasObject.transform, false);
            videoImage = imageObject.AddComponent<RawImage>();
            RectTransform rect = videoImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            videoImage.color = Color.white;

            renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32)
            {
                name = "Ghost Cinematic Render Texture",
                filterMode = FilterMode.Point
            };
            renderTexture.Create();
            videoImage.texture = renderTexture;

            videoPlayer = gameObject.GetComponent<VideoPlayer>();
            if (videoPlayer == null)
                videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;
            videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.loopPointReached += OnVideoFinished;
        }

        private void HideOverlay()
        {
            if (overlayCanvas != null)
            {
                canvasGroup.alpha = 0f;
                overlayCanvas.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (renderTexture != null)
                renderTexture.Release();
        }
    }
}
