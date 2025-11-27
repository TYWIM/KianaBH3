using KianaBH.Data.Models.Sdk;
using KianaBH.GameServer.Game.Player;
using KianaBH.GameServer.Server.Packet.Send.Chat;
using KianaBH.KcpSharp;
using KianaBH.Util;
using Microsoft.AspNetCore.Mvc;

namespace KianaBH.SdkServer.Handlers.Admin;

[ApiController]
public class AdminController : ControllerBase
{
    [HttpGet("/admin/users")]
    public IActionResult GetOnlineUsers()
    {
        var users = PlayerInstance._playerInstances.Select(p => new
        {
            uid = p.Uid,
            name = p.Data.Name,
            level = p.Data.Level,
            online = p.Connection?.State == SessionStateEnum.ACTIVE,
            last_active_time = p.Data.LastActiveTime
        }).ToList();

        return Ok(new ResponseBase
        {
            Data = new { users, count = users.Count }
        });
    }

    public class BroadcastRequest
    {
        public string? Message { get; set; }
    }

    [HttpPost("/admin/announce")]
    public async Task<IActionResult> Broadcast([FromBody] BroadcastRequest req, Logger logger)
    {
        var msg = req.Message?.Trim();
        if (string.IsNullOrEmpty(msg))
            return BadRequest(new ResponseBase { Success = false, Message = "消息不能为空" });

        var players = PlayerInstance._playerInstances;
        var tasks = players.Select(p => p.SendPacket(new PacketRecvChatMsgNotify(msg!)).AsTask());
        await Task.WhenAll(tasks);

        logger.Info($"[ADMIN] Broadcast announce to {players.Count} players");
        return Ok(new ResponseBase { Message = "OK" });
    }

    [HttpGet("/admin/server")]
    public IActionResult GetServerInfo()
    {
        var info = new
        {
            version = GameConstants.GAME_VERSION,
            http = ConfigManager.Config.HttpServer.GetDisplayAddress(),
            game = ConfigManager.Config.GameServer.GetDisplayAddress(),
            player_count = PlayerInstance._playerInstances.Count,
            server_name = $"KianaBH-cxaim-{ConfigManager.Config.GameServer.GameServerName}"
        };

        return Ok(new ResponseBase { Data = info });
    }
}
