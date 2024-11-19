using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MetronomeEffectSpawner : MonoBehaviour
{

    public GameObject validKeyPrefab;

    public void SpawnValidKeySpite()
    {
        GameObject newImage = Instantiate(validKeyPrefab,this.transform);
        StartCoroutine(FadeOutAndMove(newImage, new Vector2(UnityEngine.Random.Range(-1.2f,1.2f),3f), 2f));
    }
    public void SpawnInvalidKeySpite()
    {
        GameObject newImage = Instantiate(validKeyPrefab, this.transform);
        newImage.GetComponent<Image>().color = Color.red;
        StartCoroutine(FadeOutAndMove(newImage, Vector2.zero, 0.5f));
    }


    private IEnumerator FadeOutAndMove(GameObject image,Vector2 displacementVector, float fadeDuration)
    {
        Image img = image.GetComponent<Image>();
        Color originalColor = img.color;
        Vector2 originalPosition = new Vector2(this.transform.position.x, this.transform.position.y);

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float T = elapsedTime / fadeDuration;
            float easeOutT = T * (2 - T);
            float alpha = Mathf.Lerp(1f, 0f, easeOutT);
            img.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

            // Move the image upwards
            image.transform.position = originalPosition + new Vector2(Mathf.Lerp(0f,displacementVector.x, 1 - Mathf.Pow(1 - T, 2)), Mathf.LerpUnclamped(0f, displacementVector.y, easeOutT));

            yield return null;
        }

        Destroy(image);
    }

}
