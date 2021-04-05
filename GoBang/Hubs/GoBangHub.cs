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
	public class GoBangHub : Hub
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
			await UpdateRoomListToHall();
		}
		public async Task JoinRoom(string roomId)
		{
			Room room = GetRoom(roomId);
			User user = GetUser(Context.ConnectionId);
			bool isGuest = room.AddUser(user);
			bool isPlaying = room.RoomStatus == (int)Status.Playing;

			await Clients.Caller.SendAsync("ReturnIsGuest", isGuest);
			await Clients.Caller.SendAsync("ReturnIsPlaying", isPlaying);
			await UpdateToAllInRoom(room, "ReturnPlayers", room.PlayerList);

			if (isPlaying)
			{
				await UpdateToAllInRoom(room, "ReturnPlayerColor", room.GetPlayerColor());
				await UpdateToAllInRoom(room, "ReturnPieceData", room.GetPieceData());
				await Clients.Caller.SendAsync("ReturnTurnIndex", room.TurnIndex);
				await Clients.Caller.SendAsync("UpdateTimeLeft", room.TimeLeft);
			}
			else
			{
				await UpdateToAllInRoom(room, "UpdateReadyPlayer", room.ReadyArray);
			}
			await UpdateRoomListToHall();
		}
		public async Task LeaveRoom(string roomId)
		{
			Room room = GetRoom(roomId);
			User user = GetUser(Context.ConnectionId);

			room.PlayerList.Remove(user);
			room.GuestList.Remove(user);
			if (room.PlayerList.Count + room.GuestList.Count == 0)
			{
				Room.RoomList.Remove(room);
			}

			await UpdateToAllInRoom(room, "ReturnPlayers", room.PlayerList);
			await UpdateRoomListToHall();
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
			var room = Room.RoomList.FirstOrDefault(x => x.PlayerList.Contains(user) || x.GuestList.Contains(user));

			if (room != null)
			{
				int playerIndex = room.PlayerList.IndexOf(user);
				//2.Player or Guest?
				if (playerIndex != -1)
				{
					room.PlayerList.Remove(user);
					//3.是否在遊戲中
					if (room.RoomStatus == (int)Status.Playing)
					{
						string winner;
						if (room.Black.Equals(user))
							winner = "white";
						else
							winner = "black";
						await Clients.Clients(room.GetConnectionIds()).SendAsync("EndGame", winner);
						room.ResetGame();
					}
					else
					{
						room.ReadyArray[playerIndex] = false;
					}
				}
				else
				{
					room.GuestList.Remove(user);
				}

				//4.判斷房間是否沒人並移除
				if (room.PlayerList.Count + room.GuestList.Count == 0)
				{
					Room.RoomList.Remove(room);
				}
			}
			//5.從UserList移除User
			User.UserList.Remove(user);

			await Clients.All.SendAsync("UpdateUserCount", User.UserList.Count);
			await UpdateRoomListToHall();
			await base.OnDisconnectedAsync(exception);
		}
		private async Task StartGame(Room room)
		{
			room.RoomStatus = (int)Status.Playing;

			await room.SetColor();
			await room.Start();
			await UpdateRoomListToHall();
		}
		private static User GetUser(string connectionId)
		{
			return User.UserList.First(u => u.ConnectionId == connectionId);
		}
		private static Room GetRoom(string roomId)
		{
			return Room.RoomList.First(x => x.RoomId == roomId);
		}
		private async Task UpdateRoomListToHall()
		{
			//todo 僅對大廳內的人發送(非必要)
			await Clients.All.SendAsync("UpdateRoomList", Room.GetRoomList());
		}
		private async Task UpdateRoomListToCaller()
		{
			await Clients.Caller.SendAsync("UpdateRoomList", Room.GetRoomList());
		}
		private async Task UpdateToAllInRoom<T>(Room room, string method, T data)
		{
			var connectionIds = room.GetConnectionIds();
			await Clients.Clients(connectionIds).SendAsync(method, JsonSerializer.Serialize(data));
		}
	}
}
