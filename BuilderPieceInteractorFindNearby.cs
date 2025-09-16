using UnityEngine;

public class BuilderPieceInteractorFindNearby : MonoBehaviour
{
	public BuilderPieceInteractor pieceInteractor;

	private void Awake()
	{
	}

	private void LateUpdate()
	{
		if (pieceInteractor != null)
		{
			pieceInteractor.StartFindNearbyPieces();
		}
	}
}
