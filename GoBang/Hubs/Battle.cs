using GoBang.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoBang.Hubs
{
	public class Battle : Hub
	{
		public async Task CreateGroup(string groupName, string userName)
		{
			Room.RoomList.Add(groupName, new Room());
			//await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

			//Clients.Group(groupName);
		}
		public async Task JoinGroup(string userName, string roomId)
		{
			Room.RoomList[roomId].Users.Add(userName);
		}
		public async Task SetPiece(int x, int y, string color)
		{
			await Clients.All.SendAsync("UpdateBoard", x, y, color);
		}
		public override Task OnConnectedAsync()
		{
			return base.OnConnectedAsync();
		}
	}
}
