using System;
using UnityEngine;

/// <summary>
/// Global alarm: when a guard spots the player, a siren plays, alarm lights flash red
/// and every guard on the map starts chasing the reported position.
/// The siren clip is generated in code, so no audio asset is required.
/// </summary>
public class AlarmSystem : MonoBehaviour
{
    public static event Action<GuardAI, Vector3> AlarmRaised;
    public static bool IsActive { get; private set; }
    public static float TimeLeft { get; private set; }

    public float alarmDuration = 12f;
    public Light[] alarmLights;
    public float sirenVolume = 0.35f;

    static AlarmSystem instance;
    AudioSource audioSource;

    void Awake()
    {
        instance = this;
        IsActive = false;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = CreateSirenClip();
        audioSource.loop = true;
        audioSource.volume = sirenVolume;
        audioSource.spatialBlend = 0f;
        SetLights(false);
    }

    public static void Raise(GuardAI source, Vector3 position)
    {
        if (instance == null) return;
        instance.TimeLeft_Reset();
        if (!IsActive)
        {
            IsActive = true;
            instance.audioSource.Play();
            instance.SetLights(true);
            Debug.Log($"[ALARM] {source.name} oyuncuyu gördü! Konum: {position}");
        }
        AlarmRaised?.Invoke(source, position);
    }

    /// <summary>A chasing guard still sees the player: share the position, keep the alarm alive.</summary>
    public static void ReportSighting(Vector3 position)
    {
        if (instance == null || !IsActive) return;
        instance.TimeLeft_Reset();
        foreach (var g in FindObjectsByType<GuardAI>()) g.HearReport(position);
    }

    void TimeLeft_Reset() { TimeLeft = alarmDuration; }

    void Update()
    {
        if (!IsActive) return;

        TimeLeft -= Time.deltaTime;
        float pulse = Mathf.PingPong(Time.time * 4f, 1f);
        foreach (var l in alarmLights) if (l != null) l.intensity = Mathf.Lerp(0.5f, 6f, pulse);

        if (TimeLeft <= 0f)
        {
            IsActive = false;
            audioSource.Stop();
            SetLights(false);
            Debug.Log("[ALARM] Alarm sustu.");
        }
    }

    void SetLights(bool on)
    {
        if (alarmLights == null) return;
        foreach (var l in alarmLights) if (l != null) l.enabled = on;
    }

    static AudioClip CreateSirenClip()
    {
        const int sampleRate = 44100;
        const float length = 1.2f;
        int samples = (int)(sampleRate * length);
        var data = new float[samples];
        float phase = 0f;
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            // frequency sweeps 600Hz -> 1200Hz -> 600Hz (wee-woo)
            float freq = Mathf.Lerp(600f, 1200f, Mathf.Sin(t * Mathf.PI));
            phase += 2f * Mathf.PI * freq / sampleRate;
            data[i] = Mathf.Sign(Mathf.Sin(phase)) * 0.25f + Mathf.Sin(phase) * 0.25f;
        }
        var clip = AudioClip.Create("Siren", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
