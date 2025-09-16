using GorillaLocomotion;
using UnityEngine;

public class BuilderSizeLayerChanger : MonoBehaviour
{
	public float maxScale;

	public float minScale;

	public bool isAssurance;

	public bool affectLayerA = true;

	public bool affectLayerB = true;

	public bool affectLayerC = true;

	public bool affectLayerD = true;

	[SerializeField]
	private bool applyOnTriggerEnter = true;

	[SerializeField]
	private bool applyOnTriggerExit;

	[SerializeField]
	private GameObject fxForLayerChange;

	public int SizeLayerMask
	{
		get
		{
			int num = 0;
			if (affectLayerA)
			{
				num |= 1;
			}
			if (affectLayerB)
			{
				num |= 2;
			}
			if (affectLayerC)
			{
				num |= 4;
			}
			if (affectLayerD)
			{
				num |= 8;
			}
			return num;
		}
	}

	private void Awake()
	{
		minScale = Mathf.Max(minScale, 0.01f);
	}

	public void OnTriggerEnter(Collider other)
	{
		if (other != GTPlayer.Instance.bodyCollider)
		{
			return;
		}
		VRRig offlineVRRig = GorillaTagger.Instance.offlineVRRig;
		if (!(offlineVRRig == null) && applyOnTriggerEnter)
		{
			if (offlineVRRig.sizeManager.currentSizeLayerMaskValue != SizeLayerMask && fxForLayerChange != null)
			{
				ObjectPools.instance.Instantiate(fxForLayerChange, offlineVRRig.transform.position);
			}
			offlineVRRig.sizeManager.currentSizeLayerMaskValue = SizeLayerMask;
		}
	}

	public void OnTriggerExit(Collider other)
	{
		if (other != GTPlayer.Instance.bodyCollider)
		{
			return;
		}
		VRRig offlineVRRig = GorillaTagger.Instance.offlineVRRig;
		if (!(offlineVRRig == null) && applyOnTriggerExit)
		{
			if (offlineVRRig.sizeManager.currentSizeLayerMaskValue != SizeLayerMask && fxForLayerChange != null)
			{
				ObjectPools.instance.Instantiate(fxForLayerChange, offlineVRRig.transform.position);
			}
			offlineVRRig.sizeManager.currentSizeLayerMaskValue = SizeLayerMask;
		}
	}
}
