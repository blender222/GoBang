using GoBang.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoBang.Hubs
{
	public class Battle : Hub
	{
		public async Task CreateRoom(string roomName)
		{
			User firstUser = GetUser(Context.ConnectionId);
			Room room = new Room(roomName, firstUser);
			Room.RoomList.Add(room);

			await Clients.Caller.SendAsync("ReturnRoomId", room.RoomId);
			await Clients.Caller.SendAsync("ReturnPlayers", JsonSerializer.Serialize(room.PlayerList));
			await UpdateRoomListToAll();
		}
		public async Task JoinRoom(string roomId)
		{
			Room room = GetRoom(roomId);
			if (room.PlayerList.Count < 2)
			{
				room.AddUser(GetUser(Context.ConnectionId));
			}
			//更新房內玩家
			foreach (var player in room.PlayerList)
			{
				await Clients.Client(player.ConnectionId)
					.SendAsync("ReturnPlayers", JsonSerializer.Serialize(room.PlayerList));
			}
			await UpdateRoomListToAll();
		}
		public async Task LeaveRoom(string roomId)
		{
			Room room = GetRoom(roomId);
			User user = GetUser(Context.ConnectionId);

			room.PlayerList.Remove(user);
			//更新房內玩家
			foreach (var player in room.PlayerList)
			{
				await Clients.Client(player.ConnectionId)
					.SendAsync("ReturnPlayers", JsonSerializer.Serialize(room.PlayerList));
			}
			await UpdateRoomListToAll();
		}
		public async Task CreateUser(string name)
		{
			User user = new User
			{
				ConnectionId = Context.ConnectionId,
				UserName = name
			};
			User.UserList.Add(user);

			await Clients.All.SendAsync("UpdateUserCount", User.UserList.Count);
			await UpdateRoomListToCaller();
		}
		public async Task SetPiece(int x, int y, string color)
		{
			//todo 僅更新同房間棋盤
			await Clients.All.SendAsync("UpdateBoard", x, y, color);
		}
		public override Task OnConnectedAsync()
		{
			Clients.All.SendAsync("UpdateUserCount", User.UserList.Count);
			return base.OnConnectedAsync();
		}
		public override async Task OnDisconnectedAsync(Exception exception)
		{
			//斷線事件
			User user = GetUser(Context.ConnectionId);

			//1.判斷User是否在房內並移除
			var room = Room.RoomList.FirstOrDefault(x => x.PlayerList.Contains(user));

			if (room != null)
			{
				room.PlayerList.Remove(user);
				//2.判斷房間人數是否為0並移除
				if (room.PlayerList.Count == 0)
				{
					Room.RoomList.Remove(room);
				}
			}
			//3.從UserList移除User
			User.UserList.Remove(user);

			await Clients.All.SendAsync("UpdateUserCount", User.UserList.Count);
			await UpdateRoomListToAll();
			await base.OnDisconnectedAsync(exception);
		}
		private User GetUser(string connectionId)
		{
			return User.UserList.First(u => u.ConnectionId == connectionId);
		}
		private Room GetRoom(string roomId)
		{
			return Room.RoomList.First(x => x.RoomId == roomId);
		}
		private async Task UpdateRoomListToAll()
		{
			string result = GetRoomList();
			//todo 僅對大廳內的人發送
			await Clients.All.SendAsync("UpdateRoomList", result);
		}
		private async Task UpdateRoomListToCaller()
		{
			string result = GetRoomList();
			await Clients.Caller.SendAsync("UpdateRoomList", result);
		}
		private string GetRoomList()
		{
			return JsonSerializer.Serialize(Room.RoomList.Select(x => new
			{
				RoomId = x.RoomId,
				RoomName = x.RoomName,
				PlayerCount = x.PlayerList.Count,
				RoomStatus = x.RoomStatus
			}));
		}
	}
}
