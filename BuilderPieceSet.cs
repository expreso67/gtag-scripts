using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "BuilderPieceSet01", menuName = "Gorilla Tag/Builder/PieceSet", order = 0)]
public class BuilderPieceSet : ScriptableObject
{
	public enum BuilderPieceCategory
	{
		FLAT = 0,
		TALL = 1,
		HALF_HEIGHT = 2,
		BEAM = 3,
		SLOPE = 4,
		OVERSIZED = 5,
		SPECIAL_DISPLAY = 6,
		FUNCTIONAL = 18,
		DECORATIVE = 19,
		MISC = 20
	}

	[Serializable]
	public class BuilderPieceSubset
	{
		public string shelfButtonName;

		public BuilderPieceCategory pieceCategory;

		public List<PieceInfo> pieceInfos;
	}

	[Serializable]
	public struct PieceInfo
	{
		public BuilderPiece piecePrefab;

		public bool overrideSetMaterial;

		public string[] pieceMaterialTypes;
	}

	public class BuilderDisplayGroup
	{
		public string displayName;

		public List<BuilderPieceSubset> pieceSubsets;

		public string defaultMaterial;

		public int setID;

		public string uniqueGroupID;

		public BuilderDisplayGroup()
		{
			displayName = string.Empty;
			pieceSubsets = new List<BuilderPieceSubset>();
			defaultMaterial = string.Empty;
			setID = -1;
			uniqueGroupID = string.Empty;
		}

		public BuilderDisplayGroup(string groupName, string material, int inSetID, string groupID)
		{
			displayName = groupName;
			pieceSubsets = new List<BuilderPieceSubset>();
			defaultMaterial = material;
			setID = inSetID;
			uniqueGroupID = groupID;
		}

		public int GetDisplayGroupIdentifier()
		{
			return uniqueGroupID.GetStaticHash();
		}
	}

	[Tooltip("Display Name")]
	public string setName;

	[Tooltip("Set to null if not for sale")]
	public GameObject displayModel;

	[FormerlySerializedAs("uniqueId")]
	public string playfabID;

	[Tooltip("Default Material ID applied to all prefabs without OverrideSetMaterial")]
	public string materialId;

	[Tooltip("If this set is not available on launch day use scheduling")]
	public bool isScheduled;

	public string scheduledDate = "1/1/0001 00:00:00";

	public List<BuilderPieceSubset> subsets;

	public int GetIntIdentifier()
	{
		return playfabID.GetStaticHash();
	}

	public DateTime GetScheduleDateTime()
	{
		if (isScheduled)
		{
			try
			{
				return DateTime.Parse(scheduledDate, CultureInfo.InvariantCulture);
			}
			catch
			{
				return DateTime.MinValue;
			}
		}
		return DateTime.MinValue;
	}
}
