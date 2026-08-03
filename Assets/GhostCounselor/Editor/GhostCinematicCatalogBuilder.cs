#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

namespace GhostCounselor.Editor
{
    public static class GhostCinematicCatalogBuilder
    {
        private const string Root = "Assets/Art/ghost";
        private const string Output = "Assets/Resources/GhostCinematicCatalog.asset";

        private static readonly Dictionary<string, string> FolderToCinematicId =
            new(StringComparer.Ordinal)
            {
                ["딱지_할아버지"] = "cin_sticker_last_match",
                ["거울각시_연화"] = "cin_mirror_first_smile",
                ["막차_소년_민우"] = "cin_bus_homecoming",
                ["저승상인_만복"] = "cin_merchant_last_bowl",
                ["방울무녀_해주"] = "cin_bell_teacher_grace"
            };

        [MenuItem("Ghost Counselor/Cinematics/Scan Ghost Videos")]
        public static void ScanGhostVideos()
        {
            EnsureFolder("Assets/Resources");
            GhostCinematicCatalog catalog = AssetDatabase.LoadAssetAtPath<GhostCinematicCatalog>(Output);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<GhostCinematicCatalog>();
                AssetDatabase.CreateAsset(catalog, Output);
            }

            List<GhostCinematicClipEntry> entries = new();
            foreach (string folder in FolderToCinematicId.Keys)
            {
                string folderPath = $"{Root}/{folder}";
                string[] guids = AssetDatabase.FindAssets("t:VideoClip", new[] { folderPath });
                string path = guids.Select(AssetDatabase.GUIDToAssetPath)
                    .FirstOrDefault(p => p.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                                         p.EndsWith(".mov", StringComparison.OrdinalIgnoreCase) ||
                                         p.EndsWith(".webm", StringComparison.OrdinalIgnoreCase));
                VideoClip clip = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<VideoClip>(path);
                if (clip == null)
                {
                    Debug.LogWarning($"[GhostCinematic] {folderPath}에서 영상을 찾지 못했습니다.");
                    continue;
                }

                entries.Add(new GhostCinematicClipEntry
                {
                    cinematicId = FolderToCinematicId[folder],
                    clip = clip
                });
            }

            catalog.clips = entries.ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[GhostCinematic] {entries.Count}개 영상을 연결했습니다: {Output}");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            AssetDatabase.CreateFolder("Assets", "Resources");
        }
    }
}
#endif
