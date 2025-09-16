using Fusion;
using GorillaGameModes;
using Photon.Pun;

public sealed class CasualGameMode : GorillaGameManager
{
	public delegate int MyMatDelegate(NetPlayer player);

	public MyMatDelegate GetMyMaterial;

	public override int MyMatIndex(NetPlayer player)
	{
		if (GetMyMaterial == null)
		{
			return 0;
		}
		return GetMyMaterial(player);
	}

	public override void OnSerializeRead(object newData)
	{
	}

	public override object OnSerializeWrite()
	{
		return null;
	}

	public override void OnSerializeRead(PhotonStream stream, PhotonMessageInfo info)
	{
	}

	public override void OnSerializeWrite(PhotonStream stream, PhotonMessageInfo info)
	{
	}

	public override GameModeType GameType()
	{
		return GameModeType.Casual;
	}

	public override void AddFusionDataBehaviour(NetworkObject behaviour)
	{
		behaviour.AddBehaviour<CasualGameModeData>();
	}

	public override string GameModeName()
	{
		return "CASUAL";
	}
}
