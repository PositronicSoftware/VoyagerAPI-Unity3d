// This is free and unencumbered software released into the public domain.
// For more information, please refer to <http://unlicense.org/>

using UnityEngine;
using UnityEngine.Rendering;

public class ScreenFadeControl : MonoBehaviour
{
	public Material fadeMaterial = null;

	// Based on OVRScreenFade
#if UNITY_ANDROID && !UNITY_EDITOR
	void OnCustomPostRender()
#else
	void OnPostRender()
#endif
	{
		fadeMaterial.SetPass(0);
		GL.PushMatrix();
		GL.LoadOrtho();
		GL.Color(fadeMaterial.color);
		GL.Begin(GL.QUADS);
		GL.Vertex3(0f, 0f, -12f);
		GL.Vertex3(0f, 1f, -12f);
		GL.Vertex3(1f, 1f, -12f);
		GL.Vertex3(1f, 0f, -12f);
		GL.End();
		GL.PopMatrix();
	}

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
		#if UNITY_ANDROID && !UNITY_EDITOR
			OnCustomPostRender();
		#else
			OnPostRender();
		#endif
	}
	#endif
}