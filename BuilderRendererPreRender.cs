using UnityEngine;

public class BuilderRendererPreRender : MonoBehaviour
{
	public BuilderRenderer builderRenderer;

	private void Awake()
	{
	}

	private void LateUpdate()
	{
		if (builderRenderer != null)
		{
			builderRenderer.PreRenderIndirect();
		}
	}
}
