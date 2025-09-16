using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GorillaNetworking;
using GorillaNetworking.Store;
using UnityEngine;
using UnityEngine.UI;

public class ATM_Manager : MonoBehaviour
{
	public enum CreatorCodeStatus
	{
		Empty,
		Unchecked,
		Validating,
		Valid
	}

	public enum ATMStages
	{
		Unavailable,
		Begin,
		Menu,
		Balance,
		Choose,
		Confirm,
		Purchasing,
		Success,
		Failure,
		SafeAccount
	}

	[OnEnterPlay_SetNull]
	public static volatile ATM_Manager instance;

	private const int MAX_CODE_LENGTH = 10;

	public List<ATM_UI> atmUIs = new List<ATM_UI>();

	[HideInInspector]
	public List<CreatorCodeSmallDisplay> smallDisplays;

	private string currentCreatorCode;

	private string codeFirstUsedTime;

	private string initialCode;

	private string temporaryOverrideCode;

	private CreatorCodeStatus creatorCodeStatus;

	private ATMStages currentATMStage;

	public int numShinyRocksToBuy;

	public float shinyRocksCost;

	private Member supportedMember;

	public bool alreadyBegan;

	public string ValidatedCreatorCode { get; set; }

	public ATMStages CurrentATMStage => currentATMStage;

	public void Awake()
	{
		if ((bool)instance)
		{
			UnityEngine.Object.Destroy(this);
		}
		else
		{
			instance = this;
		}
		foreach (ATM_UI atmUI in atmUIs)
		{
			atmUI.creatorCodeTitle.text = "CREATOR CODE: ";
		}
		SwitchToStage(ATMStages.Unavailable);
		smallDisplays = new List<CreatorCodeSmallDisplay>();
	}

	public void Start()
	{
		Debug.Log("ATM COUNT: " + atmUIs.Count);
		Debug.Log("SMALL DISPLAY COUNT: " + smallDisplays.Count);
		GameEvents.OnGorrillaATMKeyButtonPressedEvent.AddListener(PressButton);
		currentCreatorCode = "";
		if (PlayerPrefs.HasKey("CodeUsedTime"))
		{
			codeFirstUsedTime = PlayerPrefs.GetString("CodeUsedTime");
			DateTime dateTime = DateTime.Parse(codeFirstUsedTime);
			if ((DateTime.Now - dateTime).TotalDays > 14.0)
			{
				PlayerPrefs.SetString("CreatorCode", "");
			}
			else
			{
				currentCreatorCode = PlayerPrefs.GetString("CreatorCode", "");
				initialCode = currentCreatorCode;
				Debug.Log("Initial code: " + initialCode);
				if (string.IsNullOrEmpty(currentCreatorCode))
				{
					creatorCodeStatus = CreatorCodeStatus.Empty;
				}
				else
				{
					creatorCodeStatus = CreatorCodeStatus.Unchecked;
				}
				foreach (CreatorCodeSmallDisplay smallDisplay in smallDisplays)
				{
					smallDisplay.SetCode(currentCreatorCode);
				}
			}
		}
		foreach (ATM_UI atmUI in atmUIs)
		{
			atmUI.creatorCodeField.text = currentCreatorCode;
		}
	}

	public void PressButton(GorillaATMKeyBindings buttonPressed)
	{
		if (currentATMStage != ATMStages.Confirm || creatorCodeStatus == CreatorCodeStatus.Validating)
		{
			return;
		}
		foreach (ATM_UI atmUI in atmUIs)
		{
			atmUI.creatorCodeTitle.text = "CREATOR CODE:";
		}
		if (buttonPressed == GorillaATMKeyBindings.delete)
		{
			if (currentCreatorCode.Length > 0)
			{
				currentCreatorCode = currentCreatorCode.Substring(0, currentCreatorCode.Length - 1);
				if (currentCreatorCode.Length == 0)
				{
					creatorCodeStatus = CreatorCodeStatus.Empty;
					ValidatedCreatorCode = "";
					foreach (CreatorCodeSmallDisplay smallDisplay in smallDisplays)
					{
						smallDisplay.SetCode("");
					}
					PlayerPrefs.SetString("CreatorCode", "");
					PlayerPrefs.Save();
				}
				else
				{
					creatorCodeStatus = CreatorCodeStatus.Unchecked;
				}
			}
		}
		else if (currentCreatorCode.Length < 10)
		{
			string text = currentCreatorCode;
			string text2;
			if (buttonPressed >= GorillaATMKeyBindings.delete)
			{
				text2 = buttonPressed.ToString();
			}
			else
			{
				int num = (int)buttonPressed;
				text2 = num.ToString();
			}
			currentCreatorCode = text + text2;
			creatorCodeStatus = CreatorCodeStatus.Unchecked;
		}
		foreach (ATM_UI atmUI2 in atmUIs)
		{
			atmUI2.creatorCodeField.text = currentCreatorCode;
		}
	}

	public void ProcessATMState(string currencyButton)
	{
		switch (currentATMStage)
		{
		case ATMStages.Begin:
			SwitchToStage(ATMStages.Menu);
			break;
		case ATMStages.Menu:
			if (PlayFabAuthenticator.instance.GetSafety())
			{
				if (!(currencyButton == "one"))
				{
					if (currencyButton == "four")
					{
						SwitchToStage(ATMStages.Begin);
					}
				}
				else
				{
					SwitchToStage(ATMStages.Balance);
				}
				break;
			}
			switch (currencyButton)
			{
			case "one":
				SwitchToStage(ATMStages.Balance);
				break;
			case "two":
				SwitchToStage(ATMStages.Choose);
				break;
			case "back":
				SwitchToStage(ATMStages.Begin);
				break;
			}
			break;
		case ATMStages.Balance:
			if (currencyButton == "back")
			{
				SwitchToStage(ATMStages.Menu);
			}
			break;
		case ATMStages.Choose:
			switch (currencyButton)
			{
			case "one":
				numShinyRocksToBuy = 1000;
				shinyRocksCost = 4.99f;
				CosmeticsController.instance.itemToPurchase = "1000SHINYROCKS";
				CosmeticsController.instance.buyingBundle = false;
				SwitchToStage(ATMStages.Confirm);
				break;
			case "two":
				numShinyRocksToBuy = 2200;
				shinyRocksCost = 9.99f;
				CosmeticsController.instance.itemToPurchase = "2200SHINYROCKS";
				CosmeticsController.instance.buyingBundle = false;
				SwitchToStage(ATMStages.Confirm);
				break;
			case "three":
				numShinyRocksToBuy = 5000;
				shinyRocksCost = 19.99f;
				CosmeticsController.instance.itemToPurchase = "5000SHINYROCKS";
				CosmeticsController.instance.buyingBundle = false;
				SwitchToStage(ATMStages.Confirm);
				break;
			case "four":
				numShinyRocksToBuy = 11000;
				shinyRocksCost = 39.99f;
				CosmeticsController.instance.itemToPurchase = "11000SHINYROCKS";
				CosmeticsController.instance.buyingBundle = false;
				SwitchToStage(ATMStages.Confirm);
				break;
			case "back":
				SwitchToStage(ATMStages.Menu);
				break;
			}
			break;
		case ATMStages.Confirm:
			if (!(currencyButton == "one"))
			{
				if (currencyButton == "back")
				{
					SwitchToStage(ATMStages.Choose);
				}
			}
			else if (creatorCodeStatus == CreatorCodeStatus.Empty)
			{
				CosmeticsController.instance.SteamPurchase();
				SwitchToStage(ATMStages.Purchasing);
			}
			else
			{
				StartCoroutine(CheckValidationCoroutine());
			}
			break;
		default:
			SwitchToStage(ATMStages.Menu);
			break;
		case ATMStages.Unavailable:
		case ATMStages.Purchasing:
			break;
		}
	}

	public void AddATM(ATM_UI newATM)
	{
		atmUIs.Add(newATM);
		newATM.creatorCodeField.text = currentCreatorCode;
		SwitchToStage(currentATMStage);
	}

	public void RemoveATM(ATM_UI atmToRemove)
	{
		atmUIs.Remove(atmToRemove);
	}

	public void SetTemporaryCreatorCode(string creatorCode, bool onlyIfEmpty = true, Action<bool> OnComplete = null)
	{
		if (onlyIfEmpty && (creatorCodeStatus != CreatorCodeStatus.Empty || !currentCreatorCode.IsNullOrEmpty()))
		{
			OnComplete?.Invoke(obj: false);
			return;
		}
		string pattern = "^[a-zA-Z0-9]+$";
		if (creatorCode.Length > 10 || !Regex.IsMatch(creatorCode, pattern))
		{
			OnComplete?.Invoke(obj: false);
			return;
		}
		NexusManager.instance.VerifyCreatorCode(creatorCode, delegate
		{
			if (currentATMStage > ATMStages.Confirm)
			{
				OnComplete?.Invoke(obj: false);
			}
			else if (onlyIfEmpty && (creatorCodeStatus != CreatorCodeStatus.Empty || !currentCreatorCode.IsNullOrEmpty()))
			{
				OnComplete?.Invoke(obj: false);
			}
			else
			{
				temporaryOverrideCode = creatorCode;
				currentCreatorCode = creatorCode;
				creatorCodeStatus = CreatorCodeStatus.Unchecked;
				foreach (CreatorCodeSmallDisplay smallDisplay in smallDisplays)
				{
					smallDisplay.SetCode(currentCreatorCode);
				}
				foreach (ATM_UI atmUI in atmUIs)
				{
					atmUI.creatorCodeField.text = currentCreatorCode;
				}
				OnComplete?.Invoke(obj: true);
			}
		}, delegate
		{
			OnComplete?.Invoke(obj: false);
		});
	}

	public void ResetTemporaryCreatorCode()
	{
		if (creatorCodeStatus == CreatorCodeStatus.Unchecked && currentCreatorCode.Equals(temporaryOverrideCode))
		{
			currentCreatorCode = "";
			creatorCodeStatus = CreatorCodeStatus.Empty;
			foreach (CreatorCodeSmallDisplay smallDisplay in smallDisplays)
			{
				smallDisplay.SetCode("");
			}
			foreach (ATM_UI atmUI in atmUIs)
			{
				atmUI.creatorCodeField.text = currentCreatorCode;
			}
		}
		temporaryOverrideCode = "";
	}

	private void ResetCreatorCode()
	{
		Debug.Log("Resetting creator code");
		foreach (ATM_UI atmUI in atmUIs)
		{
			atmUI.creatorCodeTitle.text = "CREATOR CODE:";
		}
		foreach (CreatorCodeSmallDisplay smallDisplay in smallDisplays)
		{
			smallDisplay.SetCode("");
		}
		currentCreatorCode = "";
		creatorCodeStatus = CreatorCodeStatus.Empty;
		supportedMember = default(Member);
		ValidatedCreatorCode = "";
		PlayerPrefs.SetString("CreatorCode", "");
		PlayerPrefs.Save();
		foreach (ATM_UI atmUI2 in atmUIs)
		{
			atmUI2.creatorCodeField.text = currentCreatorCode;
		}
	}

	private IEnumerator CheckValidationCoroutine()
	{
		foreach (ATM_UI atmUI in atmUIs)
		{
			atmUI.creatorCodeTitle.text = "CREATOR CODE: VALIDATING";
		}
		VerifyCreatorCode();
		while (creatorCodeStatus == CreatorCodeStatus.Validating)
		{
			yield return new WaitForSeconds(0.5f);
		}
		if (creatorCodeStatus != CreatorCodeStatus.Valid)
		{
			yield break;
		}
		foreach (ATM_UI atmUI2 in atmUIs)
		{
			atmUI2.creatorCodeTitle.text = "CREATOR CODE: VALID";
		}
		SwitchToStage(ATMStages.Purchasing);
		CosmeticsController.instance.SteamPurchase();
	}

	public void SwitchToStage(ATMStages newStage)
	{
		foreach (ATM_UI atmUI in atmUIs)
		{
			if (!atmUI.atmText)
			{
				break;
			}
			currentATMStage = newStage;
			switch (newStage)
			{
			case ATMStages.Unavailable:
				atmUI.atmText.text = "ATM NOT AVAILABLE! PLEASE TRY AGAIN LATER!";
				atmUI.ATM_RightColumnButtonText[0].text = "";
				atmUI.ATM_RightColumnArrowText[0].enabled = false;
				atmUI.ATM_RightColumnButtonText[1].text = "";
				atmUI.ATM_RightColumnArrowText[1].enabled = false;
				atmUI.ATM_RightColumnButtonText[2].text = "";
				atmUI.ATM_RightColumnArrowText[2].enabled = false;
				atmUI.ATM_RightColumnButtonText[3].text = "";
				atmUI.ATM_RightColumnArrowText[3].enabled = false;
				atmUI.creatorCodeObject.SetActive(value: false);
				break;
			case ATMStages.Begin:
				atmUI.atmText.text = "WELCOME! PRESS ANY BUTTON TO BEGIN.";
				atmUI.ATM_RightColumnButtonText[0].text = "";
				atmUI.ATM_RightColumnArrowText[0].enabled = false;
				atmUI.ATM_RightColumnButtonText[1].text = "";
				atmUI.ATM_RightColumnArrowText[1].enabled = false;
				atmUI.ATM_RightColumnButtonText[2].text = "";
				atmUI.ATM_RightColumnArrowText[2].enabled = false;
				atmUI.ATM_RightColumnButtonText[3].text = "BEGIN";
				atmUI.ATM_RightColumnArrowText[3].enabled = true;
				atmUI.creatorCodeObject.SetActive(value: false);
				break;
			case ATMStages.Menu:
				if (PlayFabAuthenticator.instance.GetSafety())
				{
					atmUI.atmText.text = "CHECK YOUR BALANCE.";
					atmUI.ATM_RightColumnButtonText[0].text = "BALANCE";
					atmUI.ATM_RightColumnArrowText[0].enabled = true;
					atmUI.ATM_RightColumnButtonText[1].text = "";
					atmUI.ATM_RightColumnArrowText[1].enabled = false;
					atmUI.ATM_RightColumnButtonText[2].text = "";
					atmUI.ATM_RightColumnArrowText[2].enabled = false;
					atmUI.ATM_RightColumnButtonText[3].text = "";
					atmUI.ATM_RightColumnArrowText[3].enabled = false;
					atmUI.creatorCodeObject.SetActive(value: false);
				}
				else
				{
					atmUI.atmText.text = "CHECK YOUR BALANCE OR PURCHASE MORE SHINY ROCKS.";
					atmUI.ATM_RightColumnButtonText[0].text = "BALANCE";
					atmUI.ATM_RightColumnArrowText[0].enabled = true;
					atmUI.ATM_RightColumnButtonText[1].text = "PURCHASE";
					atmUI.ATM_RightColumnArrowText[1].enabled = true;
					atmUI.ATM_RightColumnButtonText[2].text = "";
					atmUI.ATM_RightColumnArrowText[2].enabled = false;
					atmUI.ATM_RightColumnButtonText[3].text = "";
					atmUI.ATM_RightColumnArrowText[3].enabled = false;
					atmUI.creatorCodeObject.SetActive(value: false);
				}
				break;
			case ATMStages.Balance:
				atmUI.atmText.text = "CURRENT BALANCE:\n\n" + CosmeticsController.instance.CurrencyBalance;
				atmUI.ATM_RightColumnButtonText[0].text = "";
				atmUI.ATM_RightColumnArrowText[0].enabled = false;
				atmUI.ATM_RightColumnButtonText[1].text = "";
				atmUI.ATM_RightColumnArrowText[1].enabled = false;
				atmUI.ATM_RightColumnButtonText[2].text = "";
				atmUI.ATM_RightColumnArrowText[2].enabled = false;
				atmUI.ATM_RightColumnButtonText[3].text = "";
				atmUI.ATM_RightColumnArrowText[3].enabled = false;
				atmUI.creatorCodeObject.SetActive(value: false);
				break;
			case ATMStages.Choose:
				atmUI.atmText.text = "CHOOSE AN AMOUNT OF SHINY ROCKS TO PURCHASE.";
				atmUI.ATM_RightColumnButtonText[0].text = "1000 for $4.99";
				atmUI.ATM_RightColumnArrowText[0].enabled = true;
				atmUI.ATM_RightColumnButtonText[1].text = "2200 for $9.99\n(10% BONUS!)";
				atmUI.ATM_RightColumnArrowText[1].enabled = true;
				atmUI.ATM_RightColumnButtonText[2].text = "5000 for $19.99\n(25% BONUS!)";
				atmUI.ATM_RightColumnArrowText[2].enabled = true;
				atmUI.ATM_RightColumnButtonText[3].text = "11000 for $39.99\n(37% BONUS!)";
				atmUI.ATM_RightColumnArrowText[3].enabled = true;
				atmUI.creatorCodeObject.SetActive(value: false);
				break;
			case ATMStages.Confirm:
				atmUI.atmText.text = "YOU HAVE CHOSEN TO PURCHASE " + numShinyRocksToBuy + " SHINY ROCKS FOR $" + shinyRocksCost + ". CONFIRM TO LAUNCH A STEAM WINDOW TO COMPLETE YOUR PURCHASE.";
				atmUI.ATM_RightColumnButtonText[0].text = "CONFIRM";
				atmUI.ATM_RightColumnArrowText[0].enabled = true;
				atmUI.ATM_RightColumnButtonText[1].text = "";
				atmUI.ATM_RightColumnArrowText[1].enabled = false;
				atmUI.ATM_RightColumnButtonText[2].text = "";
				atmUI.ATM_RightColumnArrowText[2].enabled = false;
				atmUI.ATM_RightColumnButtonText[3].text = "";
				atmUI.ATM_RightColumnArrowText[3].enabled = false;
				atmUI.creatorCodeObject.SetActive(value: true);
				break;
			case ATMStages.Purchasing:
				atmUI.atmText.text = "PURCHASING IN STEAM...";
				atmUI.creatorCodeObject.SetActive(value: false);
				break;
			case ATMStages.Success:
				atmUI.atmText.text = "SUCCESS! NEW SHINY ROCKS BALANCE: " + (CosmeticsController.instance.CurrencyBalance + numShinyRocksToBuy);
				if (creatorCodeStatus == CreatorCodeStatus.Valid)
				{
					string text = supportedMember.name;
					if (!string.IsNullOrEmpty(text))
					{
						Text atmText = atmUI.atmText;
						atmText.text = atmText.text + "\n\nTHIS PURCHASE SUPPORTED\n" + text + "!";
						foreach (CreatorCodeSmallDisplay smallDisplay in smallDisplays)
						{
							smallDisplay.SuccessfulPurchase(text);
						}
					}
				}
				atmUI.ATM_RightColumnButtonText[0].text = "";
				atmUI.ATM_RightColumnArrowText[0].enabled = false;
				atmUI.ATM_RightColumnButtonText[1].text = "";
				atmUI.ATM_RightColumnArrowText[1].enabled = false;
				atmUI.ATM_RightColumnButtonText[2].text = "";
				atmUI.ATM_RightColumnArrowText[2].enabled = false;
				atmUI.ATM_RightColumnButtonText[3].text = "";
				atmUI.ATM_RightColumnArrowText[3].enabled = false;
				atmUI.creatorCodeObject.SetActive(value: false);
				break;
			case ATMStages.Failure:
				atmUI.atmText.text = "PURCHASE CANCELLED. NO FUNDS WERE SPENT.";
				atmUI.ATM_RightColumnButtonText[0].text = "";
				atmUI.ATM_RightColumnArrowText[0].enabled = false;
				atmUI.ATM_RightColumnButtonText[1].text = "";
				atmUI.ATM_RightColumnArrowText[1].enabled = false;
				atmUI.ATM_RightColumnButtonText[2].text = "";
				atmUI.ATM_RightColumnArrowText[2].enabled = false;
				atmUI.ATM_RightColumnButtonText[3].text = "";
				atmUI.ATM_RightColumnArrowText[3].enabled = false;
				atmUI.creatorCodeObject.SetActive(value: false);
				break;
			case ATMStages.SafeAccount:
				atmUI.atmText.text = "Out Of Order.";
				atmUI.ATM_RightColumnButtonText[0].text = "";
				atmUI.ATM_RightColumnArrowText[0].enabled = false;
				atmUI.ATM_RightColumnButtonText[1].text = "";
				atmUI.ATM_RightColumnArrowText[1].enabled = false;
				atmUI.ATM_RightColumnButtonText[2].text = "";
				atmUI.ATM_RightColumnArrowText[2].enabled = false;
				atmUI.ATM_RightColumnButtonText[3].text = "";
				atmUI.ATM_RightColumnArrowText[3].enabled = false;
				atmUI.creatorCodeObject.SetActive(value: false);
				break;
			}
		}
	}

	public void SetATMText(string newText)
	{
		foreach (ATM_UI atmUI in atmUIs)
		{
			atmUI.atmText.text = newText;
		}
	}

	public void PressCurrencyPurchaseButton(string currencyPurchaseSize)
	{
		ProcessATMState(currencyPurchaseSize);
	}

	public void VerifyCreatorCode()
	{
		creatorCodeStatus = CreatorCodeStatus.Validating;
		NexusManager.instance.VerifyCreatorCode(currentCreatorCode, OnCreatorCodeSucess, OnCreatorCodeFailure);
	}

	private void OnCreatorCodeSucess(Member member)
	{
		creatorCodeStatus = CreatorCodeStatus.Valid;
		supportedMember = member;
		ValidatedCreatorCode = currentCreatorCode;
		foreach (CreatorCodeSmallDisplay smallDisplay in smallDisplays)
		{
			smallDisplay.SetCode(ValidatedCreatorCode);
		}
		PlayerPrefs.SetString("CreatorCode", ValidatedCreatorCode);
		if (initialCode != ValidatedCreatorCode)
		{
			PlayerPrefs.SetString("CodeUsedTime", DateTime.Now.ToString());
		}
		PlayerPrefs.Save();
		Debug.Log("ATM CODE SUCCESS: " + supportedMember.name);
	}

	private void OnCreatorCodeFailure()
	{
		supportedMember = default(Member);
		ResetCreatorCode();
		foreach (ATM_UI atmUI in atmUIs)
		{
			atmUI.creatorCodeTitle.text = "CREATOR CODE: INVALID";
		}
		Debug.Log("ATM CODE FAILURE");
	}

	public void LeaveSystemMenu()
	{
	}
}
