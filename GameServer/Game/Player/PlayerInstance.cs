using KianaBH.Data;
using KianaBH.Database;
using KianaBH.Database.Account;
using KianaBH.Database.Client;
using KianaBH.Database.Player;
using KianaBH.GameServer.Game.Avatar;
using KianaBH.GameServer.Game.Battle;
using KianaBH.GameServer.Game.Elf;
using KianaBH.GameServer.Game.Inventory;
using KianaBH.GameServer.Server;
using KianaBH.GameServer.Server.Packet.Send.Avatar;
using KianaBH.GameServer.Server.Packet.Send.Elf;
using KianaBH.GameServer.Server.Packet.Send.Item;
using KianaBH.KcpSharp;
using KianaBH.Util.Extensions;

namespace KianaBH.GameServer.Game.Player;

public class PlayerInstance(PlayerData data)
{
    public AvatarManager? AvatarManager { get; private set; }
    public InventoryManager? InventoryManager { get; private set; }
    public ElfManager? ElfManager { get; private set; }

    public static readonly List<PlayerInstance> _playerInstances = [];
    public PlayerData Data { get; set; } = data;
    public ClientData? ClientData { get; private set; }
    public GuideData? GuideData { get; private set; }
    public WorldChatManager? WorldChatManager { get; private set; }
    public int Uid { get; set; }
    public Connection? Connection { get; set; }
    public bool Initialized { get; set; }
    public bool IsNewPlayer { get; set; }
    public int GetMissionDataRequestCount = 0;

    #region Initializers
    public PlayerInstance(int uid) : this(new PlayerData { Uid = uid })
    {
        // new player
        IsNewPlayer = true;
        Data.Name = AccountData.GetAccountByUid(uid)?.Username;

        DatabaseHelper.CreateInstance(Data);

        var t = Task.Run(async () =>
        {
            await InitialPlayerManager();
            await AvatarManager!.AddAvatar(101);
            GuideData?.GuideFinishList.AddRange(GameData.TutorialData.Values.Select(x => x.Id));
        });
        t.Wait();

        Initialized = true;

    }
    private async ValueTask InitialPlayerManager()
    {
        Uid = Data.Uid;
        AvatarManager = new AvatarManager(this);
        InventoryManager = new InventoryManager(this);
        ElfManager = new ElfManager(this);
        ClientData = InitializeDatabase<ClientData>();
        GuideData = InitializeDatabase<GuideData>();
        WorldChatManager = new WorldChatManager(this);
        Data.LastActiveTime = Extensions.GetUnixSec();

        await Task.CompletedTask;
    }
    public T InitializeDatabase<T>() where T : BaseDatabaseDataHelper, new()
    {
        var instance = DatabaseHelper.GetInstanceOrCreateNew<T>(Uid);
        return instance!;
    }

    #endregion

    #region Network
    public async ValueTask OnGetToken()
    {
        if (!Initialized) await InitialPlayerManager();
    }

    public async ValueTask OnLogin()
    {
        _playerInstances.Add(this);
        await Task.CompletedTask;
    }

    public static PlayerInstance? GetPlayerInstanceByUid(long uid)
        => _playerInstances.FirstOrDefault(player => player.Uid == uid);
    public void OnLogoutAsync()
    {
        _playerInstances.Remove(this);
    }
    public async ValueTask SendPacket(BasePacket packet)
    {
        if (Connection?.IsOnline == true) await Connection.SendPacket(packet);
    }

    #endregion

    #region Actions
    public async ValueTask OnHeartBeat()
    {
        DatabaseHelper.ToSaveUidList.SafeAdd(Uid);
        await Task.CompletedTask;
    }

    #endregion

    #region Serialization

    public Proto.GetMainDataRsp ToProto()
    {
        return Data.ToProto();
    }

    public async ValueTask SyncElf()
    {
        await SendPacket(new PacketGetElfDataRsp(this));
    }

    public async ValueTask SyncInventory()
    {
        await SendPacket(new PacketGetEquipmentDataRsp(this));
    }

    public async ValueTask SyncValk()
    {
        await SendPacket(new PacketGetAvatarDataRsp(AvatarManager!.AvatarData!.Avatars!.ToList(), true));
    }

    public async ValueTask SyncAll()
    {
        await SendPacket(new PacketGetEquipmentDataRsp(this));
        await SendPacket(new PacketGetAvatarDataRsp(AvatarManager!.AvatarData!.Avatars!.ToList(), true));
    }

    #endregion
}