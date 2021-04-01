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
		public async Task CreateRoom(string roomName, string userName)
		{
			Room.RoomList.Add(roomName, new Room { RoomName = roomName });

			//todo 更新RoomList
			//await Clients.All.SendAsync("UpdateRoomList", JsonSerializer.Serialize());
		}
		public async Task JoinRoom(string userName, string roomName)
		{
			//Context.ConnectionId

			//Room.RoomList[roomName].Users.Add(User.UserList[Context.ConnectionId]);
			Room.RoomList[roomName].AddUser(User.UserList[Context.ConnectionId]);
			//todo 更新RoomList
		}
		public async Task SetPiece(int x, int y, string color)
		{
			await Clients.All.SendAsync("UpdateBoard", x, y, color);
		}
		public async Task CreateUser(string name)
		{
			User user = new User { UserName = name };
			User.UserList.Add(Context.ConnectionId, user);

			await Clients.All.SendAsync("UpdateUserCount", User.UserList.Count);
		}
		public override Task OnConnectedAsync()
		{
			Clients.All.SendAsync("UpdateUserCount", User.UserList.Count);
			return base.OnConnectedAsync();
		}
		public override Task OnDisconnectedAsync(Exception exception)
		{
			User.UserList.Remove(Context.ConnectionId);

			Clients.All.SendAsync("UpdateUserCount", User.UserList.Count);
			return base.OnDisconnectedAsync(exception);
		}
	}
}
