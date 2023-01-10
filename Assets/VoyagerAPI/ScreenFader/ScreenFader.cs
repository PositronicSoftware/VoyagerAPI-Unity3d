// This is free and unencumbered software released into the public domain.
// For more information, please refer to <http://unlicense.org/>

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
using UnityEngine.Rendering;
#else
using UnityEngine.VR;
#endif

public class ScreenFader : MonoBehaviour
{
    public bool fadeIn = true;
    public float fadeTime = 2.0f;
    public Color fadeColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
    public Material fadeMaterial = null;
	public bool fading = false;
    
    private bool faded = false;
    private bool lastFadeIn = false;
    private List<ScreenFadeControl> fadeControls = new List<ScreenFadeControl>();

#if UNITY_2019_1_OR_NEWER
	void OnEnable() {
		if (GraphicsSettings.renderPipelineAsset != null) {
			RenderPipelineManager.endCameraRendering += RenderPipelineManager_endCameraRendering;
		}
	}

	void OnDisable() {
		if (GraphicsSettings.renderPipelineAsset != null) {
			RenderPipelineManager.endCameraRendering -= RenderPipelineManager_endCameraRendering;
		}
	}

	private void RenderPipelineManager_endCameraRendering(ScriptableRenderContext context, Camera camera) {
		OnPostRender();
	}
#endif

	void Start() {
		faded = fadeIn;
	}

	//option 2: this is called after WaitForEndOfFrame()
	void OnPostRender() {
		// Debug.Log("Calling drawquad");
		fadeMaterial.SetPass(0);
		GL.PushMatrix();
		GL.LoadOrtho();
		GL.Begin(GL.QUADS);
		GL.Color(fadeMaterial.color);
		GL.Vertex3(0, 0, -1);
		GL.Vertex3(0, 1, -1);
		GL.Vertex3(1, 1, -1);
		GL.Vertex3(1, 0, -1);
		GL.End();
		GL.PopMatrix();
	}

    void SetFadersEnabled(bool value)
    {
        foreach (ScreenFadeControl fadeControl in fadeControls)
            fadeControl.enabled = value;
    }

    public IEnumerator FadeOut()
    {
        if (!faded) {
			// Derived from OVRScreenFade
			float elapsedTime = 0.0f;
			Color color = fadeColor;
			color.a = 0.0f;
			fadeMaterial.color = color;
			while (elapsedTime < fadeTime) {
				yield return new WaitForEndOfFrame();
				elapsedTime += Time.deltaTime;
				color.a = Mathf.Clamp01(elapsedTime / fadeTime);
				fadeMaterial.color = color;
			}
        }
        faded = true;
    }

    public IEnumerator FadeIn() {
        if (faded) {
			float elapsedTime = 0.0f;
			Color color = fadeMaterial.color = fadeColor;
			while (elapsedTime < fadeTime) {
				yield return new WaitForEndOfFrame();
				elapsedTime += Time.deltaTime;
				color.a = 1.0f - Mathf.Clamp01(elapsedTime / fadeTime);
				fadeMaterial.color = color;
			}
        }
        faded = false;
        SetFadersEnabled(false);
    }

    public void Update() {
        if (lastFadeIn != fadeIn) {
            lastFadeIn = fadeIn;
			
            StartCoroutine(DoFade());
        }
    }
    
    public IEnumerator DoFade() {
		fading = true;
        // Clean up from last fade
        foreach (ScreenFadeControl fadeControl in fadeControls) {
            Destroy(fadeControl);
        }
        fadeControls.Clear();
        
        // Find all cameras and add fade material to them (initially disabled)
        foreach (Camera c in Camera.allCameras) {
            var fadeControl = c.gameObject.AddComponent<ScreenFadeControl>();
            fadeControl.fadeMaterial = fadeMaterial;
            fadeControls.Add(fadeControl);
        }

        // Do fade
        if (fadeIn)
            yield return StartCoroutine(FadeIn());
        else
            yield return StartCoroutine(FadeOut());

		// Make sure black is shown before we start - sometimes two frames are needed
		for (int i = 0; i < 2; i++)
			yield return new WaitForEndOfFrame();
		fading = false;
    }
}
