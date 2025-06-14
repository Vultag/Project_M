using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class FliterToShader : MonoBehaviour
{

    Material filterMat;
    private Shader lowpassShader;
    private Shader highpassShader;
    private Shader bandpassShader;
    private Shader activeShader;
    //private RenderTexture renderTexture;
    private Vector2 imageRes;



    void Awake()
    {
        // Create RenderTexture with desired dimensions
        //renderTexture = new RenderTexture(145 * 2, 45 * 2, 24);
        //renderTexture.Create();

        filterMat = this.GetComponent<Image>().material;
        imageRes = new Vector2(this.GetComponent<RectTransform>().rect.width, this.GetComponent<RectTransform>().rect.height);
        lowpassShader = Shader.Find("Unlit/LowpassFilterScreenShader");
        highpassShader = Shader.Find("Unlit/HighpassFilterScreenShader");
        bandpassShader = Shader.Find("Unlit/BandpassFilterScreenShader");

        activeShader = lowpassShader;
        ModifyFilter(1,0, 0);
        //BakeShaderToTexture();
    }

    public void SwitchFilterShader(short type)
    {
        switch (type)
        {
            case 0:
                activeShader = lowpassShader;
                break;
            case 1:
                activeShader = highpassShader;
                break;
            case 2:
                activeShader = bandpassShader;
                break;
        }
        filterMat.shader = activeShader;
    }

    public void ModifyFilter(float cutoff, float resonance,float envelope)
    {
        // Ensure the shader properties are updated before baking
        //filterMat.SetPass(0);
        if (filterMat == null)
            return;

        filterMat.shader = activeShader;

        filterMat.SetFloat("_FrequencyNorm", cutoff);
        filterMat.SetFloat("_Q", resonance);
        filterMat.SetFloat("_Envelope", envelope);

        //Debug.Log(resonance);

        //BakeShaderToTexture();
    }

    /// <summary>
    /// to rework / fix
    /// </summary>
    /*
    public void BakeShaderToTexture()
    {

        // Remember currently active render texture
        //RenderTexture currentActiveRT = RenderTexture.active;
        //// Create a new Texture2D to store the result
        //Texture2D newTexture = new Texture2D((int)imageRes.x*10, (int)imageRes.y*10, TextureFormat.RGBA32, false);
        ////Texture2D newTexture = test;

        RenderTexture renderTexture = new RenderTexture(145 * 2, 45 * 2, 24);
        renderTexture.Create();

        //GL.Clear(true, true, Color.clear); // Clear with transparent color



        //filterMat.SetFloat("_Cutoff", -1);
        //filterMat.SetFloat("_Resonance", 0);


        // Clear the RenderTexture
        RenderTexture.active = renderTexture;
        //GL.Clear(true, true, Color.clear);

        // Render the shader output to the RenderTexture
        Graphics.Blit(null, renderTexture, filterMat);


        // Create a Texture2D to read the pixels from the RenderTexture
        Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();
        //texture2D.


        filterMat.shader = activeShader;
        filterMat.mainTexture = texture2D;
        //filterMat.SetTexture("_MainTex",texture2D);

        // Reset the active RenderTexture
        RenderTexture.active = null;

        /// Require for the material to properly update
        this.GetComponent<Image>().enabled = false;
        this.GetComponent<Image>().enabled = true;

        // Restore previously active render texture
        //RenderTexture.active = currentActiveRT;
    }
    */


    //private IEnumerator BakeShaderNextFrame()
    //{
    //    yield return new WaitForEndOfFrame(); // Wait for the end of the frame to ensure the shader properties are applied

    //    Debug.Log("Baking shader to texture with cutoff: " + filterMat.GetFloat("_Cutoff") + ", resonance: " + filterMat.GetFloat("_Resonance"));
    //    BakeShaderToTexture();
    //}

    private void OnDisable()
    {
        activeShader = lowpassShader;
        filterMat.shader = activeShader;
    }

}
