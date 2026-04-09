using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// CutsceneManager sinifi, ilgili sistemin akisini ve durum yonetimini ustlenir.
/// </summary>
public class CutsceneManager : MonoBehaviour
{
    [Header("Tarama")]
    [Tooltip("Sadece bunun alt???±ndaki CutsceneClip'leri toplar; bo≈üsa sahnede hepsini bulur")]
    public Transform clipsParent;

    [Header("Konfig (opsiyonel) ‚Äî Listeyle y√∂netmek i√ßin doldur")]
    [SerializeField] private List<CutsceneEntry> entries = new();

    [Header("Persistans")]
    [SerializeField] private string saveFileName = "cutscenes_save.json";

    /// <summary>
    /// CutsceneEntry sinifi, cutscene akislarinda kullanilan ilgili davranisi yonetir.
    /// </summary>
    [Serializable]
    private class CutsceneEntry
    {
        public CutsceneClip clip;
        public string triggerKey = "";
        public CutscenePlayType playType = CutscenePlayType.Once;
        public string groupKey = "";
        public int priority = 0;
    }

    [Serializable] private class SaveModel
    {
        public List<string> playedIds = new();
        public List<GroupProgressRec> groups = new();
    }
    [Serializable] private class GroupProgressRec
    {
        public string groupKey;
        public int nextIndex;
    }

    private readonly Dictionary<string, CutsceneClip> allById = new();
    private readonly List<CutsceneClip> allClips = new();

    private HashSet<string> playedIds = new();
    private readonly Dictionary<string, int> groupNextIndex = new();

    private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

    private void Awake()
    {
        RefreshClipList();
        BuildGroupOrders();
        LoadState();
        ApplyPlayedStateOnBoot();
    }

    private void RefreshClipList()
    {
        allClips.Clear();
        allById.Clear();

        if (entries != null && entries.Count > 0)
        {
            foreach (var e in entries)
            {
                if (e == null || !e.clip) continue;
                var c = e.clip;

                if (string.IsNullOrWhiteSpace(c.id))
                    c.id = Guid.NewGuid().ToString("N");

                c.triggerKey = (e.triggerKey ?? "").Trim();
                c.groupKey   = string.IsNullOrWhiteSpace(e.groupKey) ? "" : e.groupKey.Trim();
                c.playType   = e.playType;
                c.priority   = e.priority;

                if (!allById.ContainsKey(c.id))
                {
                    allClips.Add(c);
                    allById[c.id] = c;
                }
            }
            return;
        }

        var found = clipsParent
            ? clipsParent.GetComponentsInChildren<CutsceneClip>(includeInactive: true)
            : FindObjectsOfType<CutsceneClip>(includeInactive: true);

        foreach (var c in found)
        {
            if (string.IsNullOrWhiteSpace(c.id))
                c.id = Guid.NewGuid().ToString("N");

            if (!allById.ContainsKey(c.id))
            {
                allClips.Add(c);
                allById[c.id] = c;
            }
        }
    }

    private void BuildGroupOrders()
    {
        var buckets = new Dictionary<string, List<CutsceneClip>>();
        foreach (var c in allClips)
        {
            if (c.playType != CutscenePlayType.SequenceStep) continue;
            string key = string.IsNullOrWhiteSpace(c.groupKey) ? "__DEFAULT__" : c.groupKey.Trim();
            if (!buckets.TryGetValue(key, out var list))
                list = buckets[key] = new List<CutsceneClip>();
            list.Add(c);
        }

        foreach (var kv in buckets)
        {
            kv.Value.Sort((a, b) => a.priority.CompareTo(b.priority));
            for (int i = 0; i < kv.Value.Count; i++)
                kv.Value[i].sequenceIndex = i;
        }
    }

    private void ApplyPlayedStateOnBoot()
    {
        foreach (var c in allClips)
        {
            if (c.playType == CutscenePlayType.Repeatable) continue;

            if (playedIds.Contains(c.id))
            {
                c.Skip();
            }
            else
            {
                if (c.deactivateSelfOnFinish) c.gameObject.SetActive(false);
            }
        }
    }

    private void LoadState()
    {
        if (!File.Exists(SavePath)) return;
        try
        {
            var json = File.ReadAllText(SavePath);
            var sm = JsonUtility.FromJson<SaveModel>(json);
            playedIds = new HashSet<string>(sm.playedIds ?? new List<string>());

            groupNextIndex.Clear();
            if (sm.groups != null)
            {
                foreach (var g in sm.groups)
                    groupNextIndex[g.groupKey] = g.nextIndex;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[CutsceneManager] LoadState hata: {e.Message}");
        }
    }

    private void SaveState()
    {
        try
        {
            var sm = new SaveModel
            {
                playedIds = new List<string>(playedIds),
                groups = new List<GroupProgressRec>()
            };
            foreach (var kv in groupNextIndex)
                sm.groups.Add(new GroupProgressRec { groupKey = kv.Key, nextIndex = kv.Value });

            File.WriteAllText(SavePath, JsonUtility.ToJson(sm));
        }
        catch (Exception e)
        {
            Debug.LogError($"[CutsceneManager] SaveState hata: {e.Message}");
        }
    }

    /// <summary>Uygun klibi bulur ve oynat???±r. Bulamazsa false d√∂ner.</summary>
    public bool TryStart(string triggerKey)
    {
        CutsceneClip candidate = FindNextSequence(triggerKey);
        if (candidate == null) candidate = FindBestOnce(triggerKey);
        if (candidate == null) candidate = FindBestRepeatable(triggerKey);
        if (candidate == null) return false;

        candidate.Play();
        return true;
    }

    /// <summary>Oynanm???±≈ü (Once/Sequence) kliplerin Skip ak???±≈ü???±n???± √ßal???±≈üt???±r???±r.</summary>
    public bool RunSkipForTrigger(string triggerKey)
    {
        bool any = false;
        string t = (triggerKey ?? "").Trim();

        foreach (var c in allClips)
        {
            if (!KeyMatch(c.triggerKey, t)) continue;

            if ((c.playType == CutscenePlayType.Once || c.playType == CutscenePlayType.SequenceStep)
                && playedIds.Contains(c.id))
            {
                c.Skip();
                any = true;
            }
        }
        return any;
    }

    /// <summary>√ñnce oynatmay???± dener; olmazsa Skip ak???±≈ü???±n???± √ßal???±≈üt???±r???±r.</summary>
    public void TryStartOrSkip(string triggerKey)
    {
        if (!TryStart(triggerKey))
            RunSkipForTrigger(triggerKey);
    }

    public void OnClipFinished(CutsceneClip clip)
    {
        if (!clip) return;

        switch (clip.playType)
        {
            case CutscenePlayType.Once:
                playedIds.Add(clip.id);
                break;

            case CutscenePlayType.SequenceStep:
            {
                playedIds.Add(clip.id);
                string gk = string.IsNullOrWhiteSpace(clip.groupKey) ? "__DEFAULT__" : clip.groupKey.Trim();
                int next = GetGroupNextIndex(gk);
                if (clip.sequenceIndex == next) next++;
                groupNextIndex[gk] = next;
                break;
            }

            case CutscenePlayType.Repeatable:
                break;
        }

        SaveState();
    }

    private CutsceneClip FindNextSequence(string triggerKey)
    {
        CutsceneClip best = null;
        int bestPriority = int.MaxValue;
        string t = (triggerKey ?? "").Trim();

        foreach (var c in allClips)
        {
            if (c.playType != CutscenePlayType.SequenceStep) continue;
            if (!KeyMatch(c.triggerKey, t)) continue;
            if (playedIds.Contains(c.id)) continue;

            string gk = string.IsNullOrWhiteSpace(c.groupKey) ? "__DEFAULT__" : c.groupKey.Trim();
            int next = GetGroupNextIndex(gk);
            if (c.sequenceIndex != next) continue;

            if (c.priority < bestPriority) { bestPriority = c.priority; best = c; }
        }
        return best;
    }

    private CutsceneClip FindBestOnce(string triggerKey)
    {
        CutsceneClip best = null;
        int bestPriority = int.MaxValue;
        string t = (triggerKey ?? "").Trim();

        foreach (var c in allClips)
        {
            if (c.playType != CutscenePlayType.Once) continue;
            if (!KeyMatch(c.triggerKey, t)) continue;
            if (playedIds.Contains(c.id)) continue;

            if (c.priority < bestPriority) { bestPriority = c.priority; best = c; }
        }
        return best;
    }

    private CutsceneClip FindBestRepeatable(string triggerKey)
    {
        CutsceneClip best = null;
        int bestPriority = int.MaxValue;
        string t = (triggerKey ?? "").Trim();

        foreach (var c in allClips)
        {
            if (c.playType != CutscenePlayType.Repeatable) continue;
            if (!KeyMatch(c.triggerKey, t)) continue;

            if (c.priority < bestPriority) { bestPriority = c.priority; best = c; }
        }
        return best;
    }

    private static bool KeyMatch(string a, string b)
        => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

    private int GetGroupNextIndex(string groupKeyNorm)
    {
        if (!groupNextIndex.TryGetValue(groupKeyNorm, out var idx))
            idx = groupNextIndex[groupKeyNorm] = 0;
        return idx;
    }

#if UNITY_EDITOR
    [ContextMenu("TOOLS / Fill Entries From Children")]
    private void FillEntriesFromChildren()
    {
        entries ??= new List<CutsceneEntry>();
        entries.Clear();

        var found = clipsParent
            ? clipsParent.GetComponentsInChildren<CutsceneClip>(includeInactive: true)
            : FindObjectsOfType<CutsceneClip>(includeInactive: true);

        foreach (var c in found)
        {
            if (string.IsNullOrWhiteSpace(c.id))
                c.id = Guid.NewGuid().ToString("N");

            entries.Add(new CutsceneEntry
            {
                clip = c,
                triggerKey = c.triggerKey,
                groupKey   = c.groupKey,
                playType   = c.playType,
                priority   = c.priority
            });
        }

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[CutsceneManager] Entries dolduruldu: {entries.Count}");
    }

    [ContextMenu("DEBUG / Reset All State")]
    private void DebugResetAll()
    {
        playedIds.Clear();
        groupNextIndex.Clear();
        SaveState();
        Debug.Log("[CutsceneManager] Reset All");
    }
#endif
}
