using UnityEngine;

public class BuilderActions
{
	public static BuilderAction CreateAttachToPlayer(int cmdId, int pieceId, Vector3 localPosition, Quaternion localRotation, int actorNumber, bool leftHand)
	{
		return new BuilderAction
		{
			type = BuilderActionType.AttachToPlayer,
			localCommandId = cmdId,
			pieceId = pieceId,
			playerActorNumber = actorNumber,
			localPosition = localPosition,
			localRotation = localRotation,
			isLeftHand = leftHand
		};
	}

	public static BuilderAction CreateAttachToPlayerRollback(int cmdId, BuilderPiece piece)
	{
		return CreateAttachToPlayer(cmdId, piece.pieceId, piece.transform.localPosition, piece.transform.localRotation, piece.heldByPlayerActorNumber, piece.heldInLeftHand);
	}

	public static BuilderAction CreateDetachFromPlayer(int cmdId, int pieceId, int actorNumber)
	{
		return new BuilderAction
		{
			type = BuilderActionType.DetachFromPlayer,
			localCommandId = cmdId,
			pieceId = pieceId,
			playerActorNumber = actorNumber
		};
	}

	public static BuilderAction CreateAttachToPiece(int cmdId, int pieceId, int parentPieceId, int attachIndex, int parentAttachIndex, sbyte bumpOffsetX, sbyte bumpOffsetZ, byte twist, int actorNumber, int timeStamp)
	{
		return new BuilderAction
		{
			type = BuilderActionType.AttachToPiece,
			localCommandId = cmdId,
			pieceId = pieceId,
			parentPieceId = parentPieceId,
			attachIndex = attachIndex,
			parentAttachIndex = parentAttachIndex,
			bumpOffsetx = bumpOffsetX,
			bumpOffsetz = bumpOffsetZ,
			twist = twist,
			playerActorNumber = actorNumber,
			timeStamp = timeStamp
		};
	}

	public static BuilderAction CreateAttachToPieceRollback(int cmdId, BuilderPiece piece, int actorNumber)
	{
		byte pieceTwist = piece.GetPieceTwist();
		piece.GetPieceBumpOffset(pieceTwist, out var xOffset, out var zOffset);
		return CreateAttachToPiece(cmdId, piece.pieceId, piece.parentPiece.pieceId, piece.attachIndex, piece.parentAttachIndex, xOffset, zOffset, pieceTwist, actorNumber, piece.activatedTimeStamp);
	}

	public static BuilderAction CreateDetachFromPiece(int cmdId, int pieceId, int actorNumber)
	{
		return new BuilderAction
		{
			type = BuilderActionType.DetachFromPiece,
			localCommandId = cmdId,
			pieceId = pieceId,
			playerActorNumber = actorNumber
		};
	}

	public static BuilderAction CreateMakeRoot(int cmdId, int pieceId)
	{
		return new BuilderAction
		{
			type = BuilderActionType.MakePieceRoot,
			localCommandId = cmdId,
			pieceId = pieceId
		};
	}

	public static BuilderAction CreateDropPiece(int cmdId, int pieceId, Vector3 localPosition, Quaternion localRotation, Vector3 velocity, Vector3 angVelocity, int actorNumber)
	{
		return new BuilderAction
		{
			type = BuilderActionType.DropPiece,
			localCommandId = cmdId,
			pieceId = pieceId,
			localPosition = localPosition,
			localRotation = localRotation,
			velocity = velocity,
			angVelocity = angVelocity,
			playerActorNumber = actorNumber
		};
	}

	public static BuilderAction CreateDropPieceRollback(int cmdId, BuilderPiece rootPiece, int actorNumber)
	{
		Vector3 position = rootPiece.transform.position;
		Quaternion rotation = rootPiece.transform.rotation;
		Vector3 velocity = Vector3.zero;
		Vector3 angVelocity = Vector3.zero;
		Rigidbody component = rootPiece.GetComponent<Rigidbody>();
		if (component != null)
		{
			position = component.position;
			rotation = component.rotation;
			velocity = component.linearVelocity;
			angVelocity = component.angularVelocity;
		}
		return CreateDropPiece(cmdId, rootPiece.pieceId, position, rotation, velocity, angVelocity, actorNumber);
	}

	public static BuilderAction CreateAttachToShelfRollback(int cmdId, BuilderPiece piece, int shelfID, bool isConveyor, int timestamp = 0, float splineTime = 0f)
	{
		return new BuilderAction
		{
			type = BuilderActionType.AttachToShelf,
			localCommandId = cmdId,
			pieceId = piece.pieceId,
			attachIndex = shelfID,
			parentAttachIndex = timestamp,
			isLeftHand = isConveyor,
			velocity = new Vector3(splineTime, 0f, 0f),
			localPosition = piece.transform.localPosition,
			localRotation = piece.transform.localRotation
		};
	}
}
