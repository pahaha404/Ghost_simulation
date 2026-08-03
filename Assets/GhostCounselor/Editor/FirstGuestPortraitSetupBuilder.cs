using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GhostCounselor.Editor
{
    /// <summary>Connects every ghost portrait to the shared editable image slot.</summary>
    public static class FirstGuestPortraitSetupBuilder
    {
        private const string GameScenePath = "Assets/Scenes/GameScene.unity";
        private const string ArtRoot = "Assets/Art/ghost/";
        private static readonly (string ghostId, string folder)[] GhostPortraitFolders =
        {
            ("sticker", "딱지_할아버지"),
            ("mirror", "거울각시_연화"),
            ("bus", "막차_소년_민우"),
            ("merchant", "저승상인_만복"),
            ("bell", "방울무녀_해주")
        };
        private static readonly string[] MirrorExpressions =
        {
            "default.png", "resentful.png", "thinking.png", "happy.png", "scary.png"
        };

        [MenuItem("Ghost Counselor/Set Up Ghost Portraits")]
        private static void SetUpGhostPortraits()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                AssetDatabase.LoadAssetAtPath<SceneAsset>(GameScenePath) == null)
                return;

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            Scene gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            GhostCounselorUIReferences ui = UnityEngine.Object.FindAnyObjectByType<GhostCounselorUIReferences>();
            if (ui == null || ui.portraitPanel == null)
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                return;
            }

            if (ui.portraitImage == null)
            {
                Image image = new GameObject("Portrait Image - Edit Size Here", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                image.transform.SetParent(ui.portraitPanel.transform, false);
                image.transform.SetSiblingIndex(0);
                image.raycastTarget = false;
                image.preserveAspect = true;
                Stretch(image.rectTransform);
                ui.portraitImage = image;
            }

            ui.ghostPortraits = Array.ConvertAll(GhostPortraitFolders, portrait => new GhostPortraitSet
            {
                ghostId = portrait.ghostId,
                defaultPortrait = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + portrait.folder + "/default.png"),
                questionExpressionSequence = portrait.ghostId == "mirror"
                    ? Array.ConvertAll(MirrorExpressions, expression =>
                        AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + portrait.folder + "/" + expression))
                    : Array.Empty<Sprite>()
            });
            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene);
            EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            Debug.Log("[귀신 상담소] 모든 귀신 초상과 거울각시 표정 시퀀스를 GameScene에 연결했습니다.");
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
