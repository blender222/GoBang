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
		public async Task CreateRoom(string roomName)
		{
			User firstUser = GetUser(Context.ConnectionId);
			Room room = new Room(roomName, firstUser, Clients);
			Room.RoomList.Add(room);

			await Clients.Caller.SendAsync("ReturnRoomId", room.RoomId);
			await Clients.Caller.SendAsync("ReturnPlayers", JsonSerializer.Serialize(room.PlayerList));
			await Clients.Caller.SendAsync("ReturnIsGuest", false);
			await UpdateRoomListToAll();
		}
		public async Task JoinRoom(string roomId)
		{
			Room room = GetRoom(roomId);
			User user = GetUser(Context.ConnectionId);
			bool isGuest = room.AddUser(user);

			await Clients.Caller.SendAsync("ReturnIsGuest", isGuest);
			await UpdateToAllInRoom(room, "ReturnPlayers", room.PlayerList);
			await UpdateToAllInRoom(room, "UpdateReadyPlayer", room.ReadyArray);
			if (room.RoomStatus == (int)Status.Playing)
			{
				await UpdateToAllInRoom(room, "ReturnPlayerColor", room.GetPlayerColor());
				await UpdateToAllInRoom(room, "ReturnPieceData", room.GetPieceData());
			}
			await UpdateRoomListToAll();
		}
		public async Task LeaveRoom(string roomId)
		{
			Room room = GetRoom(roomId);
			User user = GetUser(Context.ConnectionId);

			room.PlayerList.Remove(user);
			room.GuestList.Remove(user);
			if (room.PlayerList.Count == 0 && room.GuestList.Count == 0)
			{
				Room.RoomList.Remove(room);
			}

			await UpdateToAllInRoom(room, "ReturnPlayers", room.PlayerList);
			await UpdateRoomListToAll();
		}
		public async Task Ready(string roomId)
		{
			Room room = GetRoom(roomId);
			User user = GetUser(Context.ConnectionId);

			await room.Ready(user);

			if (room.ReadyArray.Where(x => x == true).Count() == 2)
			{
				await StartGame(room);
			}
		}
		public async Task Unready(string roomId)
		{
			Room room = GetRoom(roomId);
			User user = GetUser(Context.ConnectionId);

			await room.Unready(user);
		}
		public async Task SetPiece(string roomId, int x, int y, string color)
		{
			Room room = GetRoom(roomId);
			await room.SetPiece(x, y, color);
		}
		public async Task SendMessage(string roomId, string message)
		{
			Room room = GetRoom(roomId);
			User user = GetUser(Context.ConnectionId);

			await Clients.Clients(room.GetConnectionIds()).SendAsync("UpdateMessage", user.UserName, message);
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
				room.GuestList.Remove(user);
				//room.ReadyList.Remove(user);
				//2.判斷房間是否沒人並移除
				if (room.PlayerList.Count == 0 && room.GuestList.Count == 0)
				{
					Room.RoomList.Remove(room);
				}
			}
			//3.從UserList移除User
			User.UserList.Remove(user);

			//todo 處理遊戲中斷線

			await Clients.All.SendAsync("UpdateUserCount", User.UserList.Count);
			await UpdateRoomListToAll();
			await base.OnDisconnectedAsync(exception);
		}
		private async Task StartGame(Room room)
		{
			room.RoomStatus = (int)Status.Playing;

			await room.SetColor();
			await room.Start();
			await UpdateRoomListToAll();
		}
		private static User GetUser(string connectionId)
		{
			return User.UserList.First(u => u.ConnectionId == connectionId);
		}
		private static Room GetRoom(string roomId)
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
		private static string GetRoomList()
		{
			return JsonSerializer.Serialize(Room.RoomList.Select(x => new
			{
				RoomId = x.RoomId,
				RoomName = x.RoomName,
				PlayerCount = x.PlayerList.Count,//todo 此處造成大廳顯示房內人數錯誤 Guest沒算到
				RoomStatus = x.RoomStatus
			}));
		}
		private async Task UpdateToAllInRoom<T>(Room room, string method, T data)
		{
			var connectionIds = room.GetConnectionIds();
			await Clients.Clients(connectionIds).SendAsync(method, JsonSerializer.Serialize(data));
		}
	}
}
