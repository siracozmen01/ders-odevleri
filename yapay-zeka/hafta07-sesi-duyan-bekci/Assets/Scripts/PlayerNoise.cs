// File: Assets/Scripts/PlayerNoise.cs  (homework "Sesi Duyan Bekçi": the noise script on the player)
// N key (Input Action "MakeNoise" in InputSystem_Actions.inputactions, new Input System only):
// the player makes a noise where they stand and the guard decides in Guard.HearNoise whether it heard it.
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerNoise : MonoBehaviour
{
    public Guard guard;
    public Material ringMaterial;   // ring drawn on the floor so the noise and the 8 m range are visible

    InputAction makeNoise;

    void Awake()
    {
        makeNoise = InputSystem.actions.FindAction("MakeNoise");
        if (makeNoise == null) Debug.LogError("PlayerNoise: 'MakeNoise' eylemi Input Actions dosyasında yok.");
    }

    void Update()
    {
        if (makeNoise != null && makeNoise.WasPressedThisFrame())
            MakeNoise();
    }

    public void MakeNoise()
    {
        Vector3 p = transform.position;
        p.y = 0f;   // the noise is on the floor where the player stands
        Debug.Log($"[{Time.time:0.0} sn] Oyuncu ses çıkardı: ({p.x:0.0}; {p.z:0.0})");
        if (guard != null) NoiseRing.Show(p, guard.hearingDistance, ringMaterial);
        if (guard != null) guard.HearNoise(p);
    }
}
