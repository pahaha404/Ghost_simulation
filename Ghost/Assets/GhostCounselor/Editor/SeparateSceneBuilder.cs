using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GhostCounselor.Editor
{
    /// <summary>Creates two independently editable scenes from the original combined prototype scene.</summary>
    public static class SeparateSceneBuilder
    {
        private const string SourceScene = "Assets/Scenes/SampleScene.unity";
        private const string PrologueScene = "Assets/Scenes/PrologueScene.unity";
        private const string GameScene = "Assets/Scenes/GameScene.unity";
        private static bool waitingForEditMode;

        [MenuItem("Ghost Counselor/Create Separate Prologue And Game Scenes")]
        private static void CreateSeparateScenesMenu()
        {
            CreateSeparateScenes();
        }

        private static void CreateScenesWhenSafe()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (!waitingForEditMode)
                {
                    waitingForEditMode = true;
                    EditorApplication.playModeStateChanged += CreateAfterLeavingPlayMode;
                }
                return;
            }

            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(PrologueScene) ||
                !AssetDatabase.LoadAssetAtPath<SceneAsset>(GameScene))
                CreateSeparateScenes();
        }

        private static void CreateAfterLeavingPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
                return;

            waitingForEditMode = false;
            EditorApplication.playModeStateChanged -= CreateAfterLeavingPlayMode;
            EditorApplication.delayCall += CreateSeparateScenes;
        }

        private static void CreateSeparateScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                CreateScenesWhenSafe();
                return;
            }

            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScene))
            {
                Debug.LogError("[귀신 상담소] 씬 분리에 필요한 SampleScene을 찾지 못했습니다.");
                return;
            }

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            bool createdPrologue = CopyIfMissing(PrologueScene);
            bool createdGame = CopyIfMissing(GameScene);
            AssetDatabase.Refresh();

            if (createdPrologue)
                StripForPrologue();
            if (createdGame)
                StripForGame();

            ConfigureBuildScenes();
            EditorSceneManager.RestoreSceneManagerSetup(previousSetup);

            if (createdPrologue || createdGame)
                Debug.Log("[귀신 상담소] PrologueScene과 GameScene을 분리했습니다. GameScene에서 본편 UI를 편집하세요.");
        }

        private static bool CopyIfMissing(string destination)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(destination))
                return false;

            if (!AssetDatabase.CopyAsset(SourceScene, destination))
            {
                Debug.LogError($"[귀신 상담소] 씬 복사 실패: {destination}");
                return false;
            }

            return true;
        }

        private static void StripForPrologue()
        {
            Scene scene = EditorSceneManager.OpenScene(PrologueScene, OpenSceneMode.Single);
            DestroyObjectNamed("Counselor UI");
            DestroyObjectNamed("Shrine Background");
            DestroyObjectNamed("Game Camera");
            DestroyObjectNamed("Global Light 2D");
            EditorSceneManager.SaveScene(scene);
        }

        private static void StripForGame()
        {
            Scene scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
            DestroyObjectNamed("Prologue UI - Opening Story");
            DestroyObjectNamed("Prologue Camera");
            EditorSceneManager.SaveScene(scene);
        }

        private static void DestroyObjectNamed(string objectName)
        {
            GameObject target = GameObject.Find(objectName);
            if (target != null)
                UnityEngine.Object.DestroyImmediate(target);
        }

        private static void ConfigureBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(PrologueScene, true),
                new EditorBuildSettingsScene(GameScene, true)
            };
        }
    }
}
