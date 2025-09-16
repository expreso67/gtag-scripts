using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GorillaTagScripts;
using GorillaTagScripts.Builder;
using TMPro;
using UnityEngine;

public class BuilderScanKiosk : MonoBehaviour
{
	private enum ScannerState
	{
		IDLE,
		CONFIRMATION,
		SAVING
	}

	[SerializeField]
	private GorillaPressableButton saveButton;

	[SerializeField]
	private GorillaPressableButton noneButton;

	[SerializeField]
	private List<GorillaPressableButton> scanButtons;

	[SerializeField]
	private BuilderTable targetTable;

	[SerializeField]
	private float saveCooldownSeconds = 5f;

	[SerializeField]
	private TMP_Text screenText;

	[SerializeField]
	private SoundBankPlayer soundBank;

	[SerializeField]
	private Animation scanAnimation;

	private MeshRenderer scanTriangle;

	private bool isAnimating;

	private static string playerPrefKey = "BuilderSaveSlot";

	private static string SAVE_FOLDER = "MonkeBlocks";

	private static string SAVE_FILE = "MyBuild";

	public static int NUM_SAVE_SLOTS = 3;

	public static int DEV_SAVE_SLOT = -2;

	private Texture2D buildCaptureTexture;

	private bool isDirty;

	private bool saveError;

	private string errorMsg = string.Empty;

	private bool coolingDown;

	private double coolDownCompleteTime;

	private double scanCompleteTime;

	private ScannerState scannerState;

	public static bool IsSaveSlotValid(int slot)
	{
		if (slot >= 0)
		{
			return slot < NUM_SAVE_SLOTS;
		}
		return false;
	}

	private void Start()
	{
		if (saveButton != null)
		{
			saveButton.onPressButton.AddListener(OnSavePressed);
		}
		if (targetTable != null)
		{
			targetTable.OnSaveDirtyChanged.AddListener(OnSaveDirtyChanged);
			targetTable.OnSaveSuccess.AddListener(OnSaveSuccess);
			targetTable.OnSaveFailure.AddListener(OnSaveFail);
			SharedBlocksManager.OnSaveTimeUpdated += OnSaveTimeUpdated;
		}
		if (noneButton != null)
		{
			noneButton.onPressButton.AddListener(OnNoneButtonPressed);
		}
		foreach (GorillaPressableButton scanButton in scanButtons)
		{
			scanButton.onPressed += OnScanButtonPressed;
		}
		scanTriangle = scanAnimation.GetComponent<MeshRenderer>();
		scanTriangle.enabled = false;
		scannerState = ScannerState.IDLE;
		LoadPlayerPrefs();
	}

	private void OnDestroy()
	{
		if (saveButton != null)
		{
			saveButton.onPressButton.RemoveListener(OnSavePressed);
		}
		SharedBlocksManager.OnSaveTimeUpdated -= OnSaveTimeUpdated;
		if (targetTable != null)
		{
			targetTable.OnSaveDirtyChanged.RemoveListener(OnSaveDirtyChanged);
			targetTable.OnSaveFailure.RemoveListener(OnSaveFail);
		}
		if (noneButton != null)
		{
			noneButton.onPressButton.RemoveListener(OnNoneButtonPressed);
		}
		foreach (GorillaPressableButton scanButton in scanButtons)
		{
			if (!(scanButton == null))
			{
				scanButton.onPressed -= OnScanButtonPressed;
			}
		}
	}

	private void OnNoneButtonPressed()
	{
		if (!(targetTable == null))
		{
			if (scannerState == ScannerState.CONFIRMATION)
			{
				scannerState = ScannerState.IDLE;
			}
			if (targetTable.CurrentSaveSlot != -1)
			{
				targetTable.CurrentSaveSlot = -1;
				SavePlayerPrefs();
				UpdateUI();
			}
		}
	}

	private void OnScanButtonPressed(GorillaPressableButton button, bool isLeft)
	{
		if (targetTable == null)
		{
			return;
		}
		if (scannerState == ScannerState.CONFIRMATION)
		{
			scannerState = ScannerState.IDLE;
		}
		for (int i = 0; i < scanButtons.Count; i++)
		{
			if (button.Equals(scanButtons[i]))
			{
				if (i != targetTable.CurrentSaveSlot)
				{
					targetTable.CurrentSaveSlot = i;
					SavePlayerPrefs();
					UpdateUI();
				}
				break;
			}
		}
	}

	public void OnDevScanPressed()
	{
	}

	private void LoadPlayerPrefs()
	{
		int currentSaveSlot = PlayerPrefs.GetInt(playerPrefKey, -1);
		targetTable.CurrentSaveSlot = currentSaveSlot;
		UpdateUI();
	}

	private void SavePlayerPrefs()
	{
		PlayerPrefs.SetInt(playerPrefKey, targetTable.CurrentSaveSlot);
		PlayerPrefs.Save();
	}

	private void ToggleSaveButton(bool enabled)
	{
		if (enabled)
		{
			saveButton.enabled = true;
			saveButton.buttonRenderer.material = saveButton.unpressedMaterial;
		}
		else
		{
			saveButton.enabled = false;
			saveButton.buttonRenderer.material = saveButton.pressedMaterial;
		}
	}

	private void Update()
	{
		if (isAnimating)
		{
			if (scanAnimation == null)
			{
				isAnimating = false;
			}
			else if ((double)Time.time > scanCompleteTime)
			{
				scanTriangle.enabled = false;
				isAnimating = false;
			}
		}
		if (coolingDown && (double)Time.time > coolDownCompleteTime)
		{
			coolingDown = false;
			UpdateUI();
		}
	}

	private void OnSavePressed()
	{
		if (targetTable == null || !isDirty || coolingDown)
		{
			return;
		}
		switch (scannerState)
		{
		case ScannerState.IDLE:
			scannerState = ScannerState.CONFIRMATION;
			UpdateUI();
			break;
		case ScannerState.CONFIRMATION:
			scannerState = ScannerState.SAVING;
			if (scanAnimation != null)
			{
				scanCompleteTime = Time.time + scanAnimation.clip.length;
				scanTriangle.enabled = true;
				scanAnimation.Rewind();
				scanAnimation.Play();
			}
			if (soundBank != null)
			{
				soundBank.Play();
			}
			isAnimating = true;
			saveError = false;
			errorMsg = string.Empty;
			coolDownCompleteTime = Time.time + saveCooldownSeconds;
			coolingDown = true;
			UpdateUI();
			targetTable.SaveTableForPlayer();
			break;
		}
	}

	private string GetSavePath()
	{
		return GetSaveFolder() + Path.DirectorySeparatorChar + SAVE_FILE + "_" + targetTable.CurrentSaveSlot + ".png";
	}

	private string GetSaveFolder()
	{
		return Application.persistentDataPath + Path.DirectorySeparatorChar + SAVE_FOLDER;
	}

	private void OnSaveDirtyChanged(bool dirty)
	{
		isDirty = dirty;
		UpdateUI();
	}

	private void OnSaveTimeUpdated()
	{
		scannerState = ScannerState.IDLE;
		saveError = false;
		UpdateUI();
	}

	private void OnSaveSuccess()
	{
		scannerState = ScannerState.IDLE;
		saveError = false;
		UpdateUI();
	}

	private void OnSaveFail(string errorMsg)
	{
		scannerState = ScannerState.IDLE;
		saveError = true;
		this.errorMsg = errorMsg;
		UpdateUI();
	}

	private void UpdateUI()
	{
		screenText.text = GetTextForScreen();
		ToggleSaveButton(IsSaveSlotValid(targetTable.CurrentSaveSlot) && !coolingDown);
		noneButton.buttonRenderer.material = ((!IsSaveSlotValid(targetTable.CurrentSaveSlot)) ? noneButton.pressedMaterial : noneButton.unpressedMaterial);
		for (int i = 0; i < scanButtons.Count; i++)
		{
			scanButtons[i].buttonRenderer.material = ((targetTable.CurrentSaveSlot == i) ? scanButtons[i].pressedMaterial : scanButtons[i].unpressedMaterial);
		}
		if (scannerState == ScannerState.CONFIRMATION)
		{
			saveButton.myTmpText.text = "YES UPDATE SCAN";
		}
		else
		{
			saveButton.myTmpText.text = "UPDATE SCAN";
		}
	}

	private string GetTextForScreen()
	{
		if (targetTable == null)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		int currentSaveSlot = targetTable.CurrentSaveSlot;
		if (!IsSaveSlotValid(currentSaveSlot))
		{
			stringBuilder.Append("<b><color=red>NONE</color></b>");
		}
		else if (currentSaveSlot == DEV_SAVE_SLOT)
		{
			stringBuilder.Append("<b><color=red>DEV SCAN</color></b>");
		}
		else
		{
			stringBuilder.Append("<b><color=red>");
			stringBuilder.Append("SCAN ");
			stringBuilder.Append(currentSaveSlot + 1);
			stringBuilder.Append("</color></b>");
			SharedBlocksManager.LocalPublishInfo publishInfoForSlot = SharedBlocksManager.GetPublishInfoForSlot(currentSaveSlot);
			DateTime dateTime = DateTime.FromBinary(publishInfoForSlot.publishTime);
			if (dateTime > DateTime.MinValue)
			{
				stringBuilder.Append(": ");
				stringBuilder.Append("UPDATED ");
				stringBuilder.Append(dateTime.ToString());
				stringBuilder.Append("\n");
			}
			if (SharedBlocksManager.IsMapIDValid(publishInfoForSlot.mapID))
			{
				stringBuilder.Append("MAP ID: ");
				stringBuilder.Append(publishInfoForSlot.mapID.Substring(0, 4));
				stringBuilder.Append("-");
				stringBuilder.Append(publishInfoForSlot.mapID.Substring(4));
				stringBuilder.Append("\nUSE THIS CODE IN THE SHARE MY BLOCKS ROOM");
			}
		}
		stringBuilder.Append("\n");
		switch (scannerState)
		{
		case ScannerState.IDLE:
			if (saveError)
			{
				stringBuilder.Append("ERROR WHILE SCANNING: ");
				stringBuilder.Append(errorMsg);
			}
			else if (coolingDown)
			{
				stringBuilder.Append("COOLING DOWN...");
			}
			else if (!isDirty)
			{
				stringBuilder.Append("NO UNSAVED CHANGES");
			}
			break;
		case ScannerState.CONFIRMATION:
			stringBuilder.Append("YOU ARE ABOUT TO REPLACE ");
			if (currentSaveSlot == DEV_SAVE_SLOT)
			{
				stringBuilder.Append("<b><color=red>DEV SCAN</color></b>");
			}
			else
			{
				stringBuilder.Append("<b><color=red>SCAN ");
				stringBuilder.Append(currentSaveSlot + 1);
				stringBuilder.Append("</color></b>");
			}
			stringBuilder.Append(" ARE YOU SURE YOU WANT TO SCAN?");
			break;
		case ScannerState.SAVING:
			stringBuilder.Append("SCANNING BUILD...");
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		stringBuilder.Append("\n\n\n");
		stringBuilder.Append("CREATE A <b><color=red>NEW</color></b> PRIVATE ROOM TO LOAD ");
		if (!IsSaveSlotValid(currentSaveSlot))
		{
			stringBuilder.Append("<b><color=red>AN EMPTY TABLE</color></b>");
		}
		else if (currentSaveSlot == DEV_SAVE_SLOT)
		{
			stringBuilder.Append("<b><color=red>DEV SCAN</color></b>");
		}
		else
		{
			stringBuilder.Append("<b><color=red>");
			stringBuilder.Append("SCAN ");
			stringBuilder.Append(currentSaveSlot + 1);
			stringBuilder.Append("</color></b>");
		}
		return stringBuilder.ToString();
	}
}
