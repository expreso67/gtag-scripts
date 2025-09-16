using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ExitGames.Client.Photon;
using GorillaLocomotion;
using GorillaNetworking;
using Photon.Pun;
using Photon.Realtime;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.Events;

public class BuilderSetManager : MonoBehaviour
{
	[Serializable]
	public struct BuilderSetStoreItem
	{
		public string displayName;

		public string playfabID;

		public int setID;

		public uint cost;

		public bool hasPrice;

		public BuilderPieceSet setRef;

		public GameObject displayModel;

		[NonSerialized]
		public bool isNullItem;
	}

	[Serializable]
	public struct BuilderPieceSetInfo
	{
		public int pieceType;

		public int materialType;

		public List<int> setIds;
	}

	[SerializeField]
	private List<BuilderPieceSet> _allPieceSets;

	[SerializeField]
	private List<BuilderPieceSet> _starterPieceSets;

	[SerializeField]
	private List<BuilderPieceSet> _setsAlwaysForSale;

	[SerializeField]
	private List<BuilderPieceSet> _seasonalSetsForSale;

	private List<BuilderPieceSet> livePieceSets;

	private List<BuilderPieceSet> scheduledPieceSets;

	private List<BuilderPieceSet.BuilderDisplayGroup> liveDisplayGroups;

	private Coroutine monitor;

	private List<BuilderSetStoreItem> _allStoreItems;

	private List<BuilderPieceSet> _unlockedPieceSets;

	private static Dictionary<int, BuilderSetStoreItem> _setIdToStoreItem;

	private static List<BuilderPieceSetInfo> pieceSetInfos;

	private static Dictionary<int, int> pieceSetInfoMap;

	private List<BuilderPieceSet.BuilderDisplayGroup> displayGroups;

	private Dictionary<int, int> displayGroupMap;

	[OnEnterPlay_SetNull]
	public static volatile BuilderSetManager instance;

	[HideInInspector]
	public string catalog;

	[HideInInspector]
	public string currencyName;

	private string[] tempStringArray;

	[HideInInspector]
	public UnityEvent OnLiveSetsUpdated;

	[HideInInspector]
	public UnityEvent OnOwnedSetsUpdated;

	[HideInInspector]
	public bool pulledStoreItems;

	private static string concatStarterSets = string.Empty;

	private static string concatAllSets = string.Empty;

	private bool foundCosmetic;

	private int attempts;

	private List<string> playerIDList = new List<string>();

	private static List<int> pieceTypes;

	[HideInInspector]
	public static List<BuilderPiece> pieceList;

	private static Dictionary<int, int> pieceTypeToIndex;

	private bool hasPieceDictionary;

	public static bool hasInstance { get; private set; }

	public string GetStarterSetsConcat()
	{
		if (concatStarterSets.Length > 0)
		{
			return concatStarterSets;
		}
		concatStarterSets = string.Empty;
		foreach (BuilderPieceSet starterPieceSet in _starterPieceSets)
		{
			concatStarterSets += starterPieceSet.playfabID;
		}
		return concatStarterSets;
	}

	public string GetAllSetsConcat()
	{
		if (concatAllSets.Length > 0)
		{
			return concatAllSets;
		}
		concatAllSets = string.Empty;
		foreach (BuilderPieceSet allPieceSet in _allPieceSets)
		{
			concatAllSets += allPieceSet.playfabID;
		}
		return concatAllSets;
	}

	public void Awake()
	{
		if (instance == null)
		{
			instance = this;
			hasInstance = true;
		}
		else if (instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		Init();
		if (monitor == null)
		{
			monitor = StartCoroutine(MonitorTime());
		}
	}

	private void Init()
	{
		InitPieceDictionary();
		catalog = "DLC";
		currencyName = "SR";
		pulledStoreItems = false;
		_setIdToStoreItem = new Dictionary<int, BuilderSetStoreItem>(_allPieceSets.Count);
		_setIdToStoreItem.Clear();
		pieceSetInfos = new List<BuilderPieceSetInfo>(_allPieceSets.Count * 45);
		pieceSetInfoMap = new Dictionary<int, int>(_allPieceSets.Count * 45);
		livePieceSets = new List<BuilderPieceSet>(_allPieceSets.Count);
		scheduledPieceSets = new List<BuilderPieceSet>(_allPieceSets.Count);
		displayGroups = new List<BuilderPieceSet.BuilderDisplayGroup>(_allPieceSets.Count * 2);
		displayGroupMap = new Dictionary<int, int>(_allPieceSets.Count * 2);
		liveDisplayGroups = new List<BuilderPieceSet.BuilderDisplayGroup>();
		Dictionary<string, int> dictionary = new Dictionary<string, int>(5);
		foreach (BuilderPieceSet allPieceSet in _allPieceSets)
		{
			dictionary.Clear();
			int num = 0;
			BuilderSetStoreItem value = new BuilderSetStoreItem
			{
				displayName = allPieceSet.setName,
				playfabID = allPieceSet.playfabID,
				setID = allPieceSet.GetIntIdentifier(),
				cost = 0u,
				setRef = allPieceSet,
				displayModel = allPieceSet.displayModel,
				isNullItem = false
			};
			_setIdToStoreItem.TryAdd(allPieceSet.GetIntIdentifier(), value);
			int num2 = -1;
			if (!string.IsNullOrEmpty(allPieceSet.materialId))
			{
				num2 = allPieceSet.materialId.GetHashCode();
			}
			for (int i = 0; i < allPieceSet.subsets.Count; i++)
			{
				BuilderPieceSet.BuilderPieceSubset builderPieceSubset = allPieceSet.subsets[i];
				if (!allPieceSet.setName.Equals("HIDDEN"))
				{
					string text = allPieceSet.subsets[i].shelfButtonName;
					if (text.IsNullOrEmpty())
					{
						text = allPieceSet.setName;
					}
					text = text.ToUpper();
					if (dictionary.TryGetValue(text, out var value2))
					{
						displayGroupMap.TryGetValue(value2, out var value3);
						BuilderPieceSet.BuilderDisplayGroup builderDisplayGroup = displayGroups[value3];
						builderDisplayGroup.pieceSubsets.Add(allPieceSet.subsets[i]);
						displayGroups[value3] = builderDisplayGroup;
					}
					else
					{
						string groupUniqueID = GetGroupUniqueID(allPieceSet.playfabID, num);
						num++;
						BuilderPieceSet.BuilderDisplayGroup builderDisplayGroup2 = new BuilderPieceSet.BuilderDisplayGroup(text, allPieceSet.materialId, allPieceSet.GetIntIdentifier(), groupUniqueID);
						builderDisplayGroup2.pieceSubsets.Add(allPieceSet.subsets[i]);
						dictionary.Add(text, builderDisplayGroup2.GetDisplayGroupIdentifier());
						displayGroupMap.Add(builderDisplayGroup2.GetDisplayGroupIdentifier(), displayGroups.Count);
						displayGroups.Add(builderDisplayGroup2);
						if (!allPieceSet.isScheduled)
						{
							liveDisplayGroups.Add(builderDisplayGroup2);
						}
					}
				}
				for (int j = 0; j < builderPieceSubset.pieceInfos.Count; j++)
				{
					BuilderPiece piecePrefab = builderPieceSubset.pieceInfos[j].piecePrefab;
					int staticHash = piecePrefab.name.GetStaticHash();
					int pieceMaterial = num2;
					if (piecePrefab.materialOptions == null)
					{
						pieceMaterial = -1;
						AddPieceToInfoMap(staticHash, pieceMaterial, allPieceSet.GetIntIdentifier());
					}
					else if (builderPieceSubset.pieceInfos[j].overrideSetMaterial)
					{
						if (builderPieceSubset.pieceInfos[j].pieceMaterialTypes.Length == 0)
						{
							Debug.LogErrorFormat("Material List for piece {0} in set {1} is empty", piecePrefab.name, allPieceSet.setName);
						}
						string[] pieceMaterialTypes = builderPieceSubset.pieceInfos[j].pieceMaterialTypes;
						foreach (string text2 in pieceMaterialTypes)
						{
							if (string.IsNullOrEmpty(text2))
							{
								Debug.LogErrorFormat("Material List Entry for piece {0} in set {1} is empty", piecePrefab.name, allPieceSet.setName);
							}
							else
							{
								pieceMaterial = text2.GetHashCode();
								AddPieceToInfoMap(staticHash, pieceMaterial, allPieceSet.GetIntIdentifier());
							}
						}
					}
					else
					{
						piecePrefab.materialOptions.GetMaterialFromType(num2, out var material, out var _);
						if (material == null)
						{
							pieceMaterial = -1;
						}
						AddPieceToInfoMap(staticHash, pieceMaterial, allPieceSet.GetIntIdentifier());
					}
				}
			}
			if (!allPieceSet.isScheduled)
			{
				livePieceSets.Add(allPieceSet);
			}
			else
			{
				scheduledPieceSets.Add(allPieceSet);
			}
		}
		_unlockedPieceSets = new List<BuilderPieceSet>(_allPieceSets.Count);
		_unlockedPieceSets.AddRange(_starterPieceSets);
	}

	private string GetGroupUniqueID(string setPlayfabID, int groupNumber)
	{
		return setPlayfabID.Trim('.') + (char)(65 + groupNumber);
	}

	public void InitPieceDictionary()
	{
		if (hasPieceDictionary)
		{
			return;
		}
		pieceTypes = new List<int>(256);
		pieceList = new List<BuilderPiece>(256);
		pieceTypeToIndex = new Dictionary<int, int>(256);
		int num = 0;
		foreach (BuilderPieceSet allPieceSet in _allPieceSets)
		{
			foreach (BuilderPieceSet.BuilderPieceSubset subset in allPieceSet.subsets)
			{
				foreach (BuilderPieceSet.PieceInfo pieceInfo in subset.pieceInfos)
				{
					int staticHash = pieceInfo.piecePrefab.name.GetStaticHash();
					if (!pieceTypeToIndex.ContainsKey(staticHash))
					{
						pieceList.Add(pieceInfo.piecePrefab);
						pieceTypes.Add(staticHash);
						pieceTypeToIndex.Add(staticHash, num);
						num++;
					}
				}
			}
		}
		hasPieceDictionary = true;
	}

	public BuilderPiece GetPiecePrefab(int pieceType)
	{
		if (pieceTypeToIndex.TryGetValue(pieceType, out var value))
		{
			return pieceList[value];
		}
		Debug.LogErrorFormat("No Prefab found for type {0}", pieceType);
		return null;
	}

	private void OnEnable()
	{
		if (monitor == null && scheduledPieceSets.Count > 0)
		{
			monitor = StartCoroutine(MonitorTime());
		}
	}

	private void OnDisable()
	{
		if (monitor != null)
		{
			StopCoroutine(monitor);
		}
		monitor = null;
	}

	private IEnumerator MonitorTime()
	{
		while (GorillaComputer.instance == null || GorillaComputer.instance.startupMillis == 0L)
		{
			yield return null;
		}
		while (scheduledPieceSets.Count > 0)
		{
			bool flag = false;
			for (int num = scheduledPieceSets.Count - 1; num >= 0; num--)
			{
				BuilderPieceSet builderPieceSet = scheduledPieceSets[num];
				if (GorillaComputer.instance.GetServerTime() > builderPieceSet.GetScheduleDateTime())
				{
					flag = true;
					livePieceSets.Add(builderPieceSet);
					scheduledPieceSets.RemoveAt(num);
					int intIdentifier = builderPieceSet.GetIntIdentifier();
					foreach (BuilderPieceSet.BuilderDisplayGroup displayGroup in displayGroups)
					{
						if (displayGroup != null && displayGroup.setID == intIdentifier && !liveDisplayGroups.Contains(displayGroup))
						{
							liveDisplayGroups.Add(displayGroup);
						}
					}
				}
			}
			if (flag)
			{
				OnLiveSetsUpdated.Invoke();
			}
			yield return new WaitForSeconds(60f);
		}
		monitor = null;
	}

	private void AddPieceToInfoMap(int pieceType, int pieceMaterial, int setID)
	{
		if (pieceSetInfoMap.TryGetValue(HashCode.Combine(pieceType, pieceMaterial), out var value))
		{
			BuilderPieceSetInfo value2 = pieceSetInfos[value];
			if (!value2.setIds.Contains(setID))
			{
				value2.setIds.Add(setID);
			}
			pieceSetInfos[value] = value2;
		}
		else
		{
			BuilderPieceSetInfo item = new BuilderPieceSetInfo
			{
				pieceType = pieceType,
				materialType = pieceMaterial,
				setIds = new List<int> { setID }
			};
			pieceSetInfoMap.Add(HashCode.Combine(pieceType, pieceMaterial), pieceSetInfos.Count);
			pieceSetInfos.Add(item);
		}
	}

	public static bool IsItemIDBuilderItem(string playfabID)
	{
		return instance.GetAllSetsConcat().Contains(playfabID);
	}

	public void OnGotInventoryItems(GetUserInventoryResult inventoryResult, GetCatalogItemsResult catalogResult)
	{
		CosmeticsController.instance.concatStringCosmeticsAllowed += GetStarterSetsConcat();
		_unlockedPieceSets.Clear();
		_unlockedPieceSets.AddRange(_starterPieceSets);
		foreach (CatalogItem item in catalogResult.Catalog)
		{
			if (IsItemIDBuilderItem(item.ItemId) && _setIdToStoreItem.TryGetValue(item.ItemId.GetStaticHash(), out var value))
			{
				bool hasPrice = false;
				uint value2 = 0u;
				if (item.VirtualCurrencyPrices.TryGetValue(currencyName, out value2))
				{
					hasPrice = true;
				}
				value.playfabID = item.ItemId;
				value.cost = value2;
				value.hasPrice = hasPrice;
				_setIdToStoreItem[value.setRef.GetIntIdentifier()] = value;
			}
		}
		foreach (ItemInstance item2 in inventoryResult.Inventory)
		{
			if (IsItemIDBuilderItem(item2.ItemId))
			{
				if (_setIdToStoreItem.TryGetValue(item2.ItemId.GetStaticHash(), out var value3))
				{
					Debug.LogFormat("BuilderSetManager: Unlocking Inventory Item {0}", item2.ItemId);
					_unlockedPieceSets.Add(value3.setRef);
					CosmeticsController.instance.concatStringCosmeticsAllowed += item2.ItemId;
				}
				else
				{
					Debug.Log("BuilderSetManager: No store item found with id" + item2.ItemId);
				}
			}
		}
		pulledStoreItems = true;
		OnOwnedSetsUpdated?.Invoke();
	}

	public BuilderSetStoreItem GetStoreItemFromSetID(int setID)
	{
		return _setIdToStoreItem.GetValueOrDefault(setID, BuilderKiosk.nullItem);
	}

	public BuilderPieceSet GetPieceSetFromID(int setID)
	{
		if (_setIdToStoreItem.TryGetValue(setID, out var value))
		{
			return value.setRef;
		}
		return null;
	}

	public BuilderPieceSet.BuilderDisplayGroup GetDisplayGroupFromIndex(int groupID)
	{
		if (displayGroupMap.TryGetValue(groupID, out var value))
		{
			return displayGroups[value];
		}
		return null;
	}

	public List<BuilderPieceSet> GetAllPieceSets()
	{
		return _allPieceSets;
	}

	public List<BuilderPieceSet> GetLivePieceSets()
	{
		return livePieceSets;
	}

	public List<BuilderPieceSet.BuilderDisplayGroup> GetLiveDisplayGroups()
	{
		return liveDisplayGroups;
	}

	public List<BuilderPieceSet> GetUnlockedPieceSets()
	{
		return _unlockedPieceSets;
	}

	public List<BuilderPieceSet> GetPermanentSetsForSale()
	{
		return _setsAlwaysForSale;
	}

	public List<BuilderPieceSet> GetSeasonalSetsForSale()
	{
		return _seasonalSetsForSale;
	}

	public bool IsSetSeasonal(string playfabID)
	{
		if (_seasonalSetsForSale.IsNullOrEmpty())
		{
			return false;
		}
		return _seasonalSetsForSale.FindIndex((BuilderPieceSet x) => x.playfabID.Equals(playfabID)) >= 0;
	}

	public bool DoesPlayerOwnDisplayGroup(Player player, int groupID)
	{
		if (player == null)
		{
			return false;
		}
		if (displayGroupMap.TryGetValue(groupID, out var value))
		{
			if (value < 0 || value >= displayGroups.Count)
			{
				return false;
			}
			BuilderPieceSet.BuilderDisplayGroup builderDisplayGroup = displayGroups[value];
			if (builderDisplayGroup != null)
			{
				return DoesPlayerOwnPieceSet(player, builderDisplayGroup.setID);
			}
			return false;
		}
		return false;
	}

	public bool DoesPlayerOwnPieceSet(Player player, int setID)
	{
		BuilderPieceSet pieceSetFromID = GetPieceSetFromID(setID);
		if (pieceSetFromID == null)
		{
			return false;
		}
		if (VRRigCache.Instance.TryGetVrrig(player, out var playerRig))
		{
			bool flag = playerRig.Rig.IsItemAllowed(pieceSetFromID.playfabID);
			Debug.LogFormat("BuilderSetManager: does player {0} own set {1} {2}", player.ActorNumber, pieceSetFromID.setName, flag);
			return flag;
		}
		Debug.LogFormat("BuilderSetManager: could not get rig for player {0}", player.ActorNumber);
		return false;
	}

	public bool DoesAnyPlayerInRoomOwnPieceSet(int setID)
	{
		BuilderPieceSet pieceSetFromID = GetPieceSetFromID(setID);
		if (pieceSetFromID == null)
		{
			return false;
		}
		if (GetStarterSetsConcat().Contains(pieceSetFromID.setName))
		{
			return true;
		}
		foreach (NetPlayer item in RoomSystem.PlayersInRoom)
		{
			if (VRRigCache.Instance.TryGetVrrig(item, out var playerRig) && playerRig.Rig.IsItemAllowed(pieceSetFromID.playfabID))
			{
				return true;
			}
		}
		return false;
	}

	public bool IsPieceOwnedByRoom(int pieceType, int materialType)
	{
		if (pieceSetInfoMap.TryGetValue(HashCode.Combine(pieceType, materialType), out var value))
		{
			foreach (int setId in pieceSetInfos[value].setIds)
			{
				if (DoesAnyPlayerInRoomOwnPieceSet(setId))
				{
					return true;
				}
			}
			return false;
		}
		return false;
	}

	public bool IsPieceOwnedLocally(int pieceType, int materialType)
	{
		if (pieceSetInfoMap.TryGetValue(HashCode.Combine(pieceType, materialType), out var value))
		{
			foreach (int setId in pieceSetInfos[value].setIds)
			{
				if (IsPieceSetOwnedLocally(setId))
				{
					return true;
				}
			}
			return false;
		}
		return false;
	}

	public bool IsPieceSetOwnedLocally(int setID)
	{
		return _unlockedPieceSets.FindIndex((BuilderPieceSet x) => setID == x.GetIntIdentifier()) >= 0;
	}

	public void UnlockSet(int setID)
	{
		int num = _allPieceSets.FindIndex((BuilderPieceSet x) => setID == x.GetIntIdentifier());
		if (num >= 0 && !_unlockedPieceSets.Contains(_allPieceSets[num]))
		{
			Debug.Log("BuilderSetManager: unlocking set " + _allPieceSets[num].setName);
			_unlockedPieceSets.Add(_allPieceSets[num]);
		}
		OnOwnedSetsUpdated?.Invoke();
	}

	public void TryPurchaseItem(int setID, Action<bool> resultCallback)
	{
		if (!_setIdToStoreItem.TryGetValue(setID, out var storeItem))
		{
			Debug.Log("BuilderSetManager: no store Item for set " + setID);
			resultCallback?.Invoke(obj: false);
			return;
		}
		if (IsPieceSetOwnedLocally(setID))
		{
			Debug.Log("BuilderSetManager: set already owned " + setID);
			resultCallback?.Invoke(obj: false);
			return;
		}
		PlayFabClientAPI.PurchaseItem(new PurchaseItemRequest
		{
			ItemId = storeItem.playfabID,
			Price = (int)storeItem.cost,
			VirtualCurrency = currencyName,
			CatalogVersion = catalog
		}, delegate(PurchaseItemResult result)
		{
			if (result.Items.Count > 0)
			{
				foreach (ItemInstance item in result.Items)
				{
					Debug.Log("BuilderSetManager: unlocking set " + item.ItemId);
					UnlockSet(item.ItemId.GetStaticHash());
				}
				CosmeticsController.instance.UpdateMyCosmetics();
				if (PhotonNetwork.InRoom)
				{
					StartCoroutine(CheckIfMyCosmeticsUpdated(storeItem.playfabID));
				}
				resultCallback?.Invoke(obj: true);
			}
			else
			{
				Debug.Log("BuilderSetManager: no items purchased ");
				resultCallback?.Invoke(obj: false);
			}
		}, delegate(PlayFabError error)
		{
			Debug.LogErrorFormat("BuilderSetManager: purchase {0} Error {1}", setID, error.ErrorMessage);
			resultCallback?.Invoke(obj: false);
		});
	}

	private IEnumerator CheckIfMyCosmeticsUpdated(string itemToBuyID)
	{
		yield return new WaitForSeconds(1f);
		foundCosmetic = false;
		attempts = 0;
		while (!foundCosmetic && attempts < 10 && PhotonNetwork.InRoom)
		{
			playerIDList.Clear();
			if (GorillaServer.Instance != null && GorillaServer.Instance.NewCosmeticsPath())
			{
				playerIDList.Add("Inventory");
				PlayFabClientAPI.GetSharedGroupData(new PlayFab.ClientModels.GetSharedGroupDataRequest
				{
					Keys = playerIDList,
					SharedGroupId = PhotonNetwork.LocalPlayer.UserId + "Inventory"
				}, delegate(GetSharedGroupDataResult result)
				{
					attempts++;
					foreach (KeyValuePair<string, PlayFab.ClientModels.SharedGroupDataRecord> datum in result.Data)
					{
						if (datum.Value.Value.Contains(itemToBuyID))
						{
							PhotonNetwork.RaiseEvent(199, null, new RaiseEventOptions
							{
								Receivers = ReceiverGroup.Others
							}, SendOptions.SendReliable);
							foundCosmetic = true;
						}
					}
					_ = foundCosmetic;
				}, delegate(PlayFabError error)
				{
					attempts++;
					CosmeticsController.instance.ReauthOrBan(error);
				});
				yield return new WaitForSeconds(1f);
				continue;
			}
			playerIDList.Add(PhotonNetwork.LocalPlayer.ActorNumber.ToString());
			PlayFabClientAPI.GetSharedGroupData(new PlayFab.ClientModels.GetSharedGroupDataRequest
			{
				Keys = playerIDList,
				SharedGroupId = PhotonNetwork.CurrentRoom.Name + Regex.Replace(PhotonNetwork.CloudRegion, "[^a-zA-Z0-9]", "").ToUpper()
			}, delegate(GetSharedGroupDataResult result)
			{
				attempts++;
				foreach (KeyValuePair<string, PlayFab.ClientModels.SharedGroupDataRecord> datum2 in result.Data)
				{
					if (datum2.Value.Value.Contains(itemToBuyID))
					{
						Debug.Log("BuilderSetManager: found it! updating others cosmetic!");
						PhotonNetwork.RaiseEvent(199, null, new RaiseEventOptions
						{
							Receivers = ReceiverGroup.Others
						}, SendOptions.SendReliable);
						foundCosmetic = true;
					}
					else
					{
						Debug.Log("BuilderSetManager: didnt find it, updating attempts and trying again in a bit. current attempt is " + attempts);
					}
				}
			}, delegate(PlayFabError error)
			{
				attempts++;
				if (error.Error == PlayFabErrorCode.NotAuthenticated)
				{
					PlayFabAuthenticator.instance.AuthenticateWithPlayFab();
				}
				else if (error.Error == PlayFabErrorCode.AccountBanned)
				{
					Application.Quit();
					PhotonNetwork.Disconnect();
					UnityEngine.Object.DestroyImmediate(PhotonNetworkController.Instance);
					UnityEngine.Object.DestroyImmediate(GTPlayer.Instance);
					GameObject[] array = UnityEngine.Object.FindObjectsOfType<GameObject>();
					for (int i = 0; i < array.Length; i++)
					{
						UnityEngine.Object.Destroy(array[i]);
					}
				}
				Debug.Log("BuilderSetManager: Got error retrieving user data, on attempt " + attempts);
				Debug.Log(error.GenerateErrorReport());
			});
			yield return new WaitForSeconds(1f);
		}
		Debug.Log("BuilderSetManager: done!");
	}
}
