using System;
using UnityEngine;
using UnityEngine.Events;

public class CutsceneClip : MonoBehaviour
{
    [Header("Kimlik")]
    public string id; // Manager state için benzersiz

    [Header("Oynatma Event'leri")]
    public bool deactivateSelfOnFinish = true;  // bitince GameObject kapansın
    public UnityEvent onPlay;                   // Timeline/dialog başlat
    public UnityEvent onSkip;                   // zaten oynandıysa yapılacaklar

    [NonSerialized] public string triggerKey;   // Manager doldurur (Entries)
    [NonSerialized] public string groupKey;     // Manager doldurur (Entries)
    [NonSerialized] public CutscenePlayType playType = CutscenePlayType.Once; // Manager doldurur
    [NonSerialized] public int priority = 0;    // Manager doldurur
    [NonSerialized] public int sequenceIndex = -1; // Manager doldurur

    private CutsceneManager manager;

    private void Awake()
    {
        manager = GetComponentInParent<CutsceneManager>();
        if (!manager) manager = FindObjectOfType<CutsceneManager>();

        if (string.IsNullOrWhiteSpace(id))
            id = Guid.NewGuid().ToString("N"); // bir kez üret; Inspector’da bırak, artık sabit
    }

    // Manager çağırır
    public void Play()
    {
        gameObject.SetActive(true);
        onPlay?.Invoke();
    }

    // Manager/boot çağırabilir
    public void Skip()
    {
        onSkip?.Invoke();
        if (deactivateSelfOnFinish) gameObject.SetActive(false);
    }

    // Timeline/Signal/Anim Event → bunu çağır
    public void Finish()
    {
        manager?.OnClipFinished(this);
        if (deactivateSelfOnFinish) gameObject.SetActive(false);
    }
}
