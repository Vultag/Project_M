using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// update in bulk at lateUpdate if I do playback
/// </summary>
public class DrumPadVFX : MonoBehaviour
{
    [HideInInspector]
    private Material KickVFXMaterial;
    [HideInInspector]
    private Material SnareVFXMaterial;
    [HideInInspector]
    private Material HitHatVFXMaterial;

    void Start()
    {
        KickVFXMaterial = this.transform.GetChild(0).GetComponent<Renderer>().material;
        SnareVFXMaterial = this.transform.GetChild(1).GetComponent<Renderer>().material;
        HitHatVFXMaterial = this.transform.GetChild(2).GetComponent<Renderer>().material;
    }

    public void UpdateKickShader(float TimeForShaderSync)
    {
        //Debug.Log(TimeForShaderSync);
        KickVFXMaterial.SetFloat("_KickPressTime", TimeForShaderSync);
    }
    public void UpdateSnareShader(float normalizedRadian, float TimeForShaderSync)
    {
        //Debug.Log(TimeForShaderSync);
        SnareVFXMaterial.SetVector("_SnareInfo", new Vector4(normalizedRadian-0.25f, TimeForShaderSync, 0, 0));
    }
    public void UpdateHitHatShader(float TimeForShaderSync)
    {
        //Debug.Log(TimeForShaderSync);
        HitHatVFXMaterial.SetFloat("_HitHatPressTime", TimeForShaderSync);
    }



}
