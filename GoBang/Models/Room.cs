using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GoBang.Models
{
	public class Room
	{
		public static List<Room> RoomList { get; set; }

		public IHubCallerClients Clients;

		public int TurnSeconds = 10;
		public string RoomId { get; set; }
		public string RoomName { get; set; }
		public int RoomStatus { get; set; }
		public List<User> PlayerList { get; set; }
		public List<User> GuestList { get; set; }
		public bool[] ReadyArray { get; set; }
		public User Black { get; set; }
		public User White { get; set; }
		private Timer Counter { get; set; }
		private int TimeLeft { get; set; }
		private bool TurnFlag { get; set; } //ture輪到黑 false輪到白
		private int[,] PieceGrid { get; set; }

		public Room(string roomName, User firstUser, IHubCallerClients clients)
		{
			this.Clients = clients;
			this.RoomId = Guid.NewGuid().ToString();
			this.RoomName = roomName;
			this.RoomStatus = (int)Status.Waiting;
			this.PlayerList = new List<User> { firstUser };
			this.GuestList = new List<User>();
			this.ReadyArray = new bool[2];
			this.PieceGrid = new int[15, 15];
			this.TurnFlag = true;
		}
		static Room()
		{
			RoomList = new List<Room>();
		}
		public bool AddUser(User user)
		{
			if (this.PlayerList.Count < 2)
			{
				this.PlayerList.Add(user);
				return false;
			}
			else
			{
				this.GuestList.Add(user);
				return true;
			}
		}
		public async Task Ready(User user)
		{
			if (this.PlayerList.Contains(user))
			{
				int index = this.PlayerList.IndexOf(user);
				this.ReadyArray[index] = true;

				await Clients.Clients(GetConnectionIds())
					.SendAsync("UpdateReadyPlayer", JsonSerializer.Serialize(this.ReadyArray));
			}
			else
			{
				throw new Exception("準備錯誤 不包含此User");
			}
		}
		public async Task Unready(User user)
		{
			if (this.PlayerList.Contains(user))
			{
				int index = this.PlayerList.IndexOf(user);
				this.ReadyArray[index] = false;

				await Clients.Clients(GetConnectionIds())
					.SendAsync("UpdateReadyPlayer", JsonSerializer.Serialize(this.ReadyArray));
			}
			else
			{
				throw new Exception("取消準備錯誤 不包含此User");
			}
		}
		public void SetColor(User user1, User user2)
		{
			Random rand = new Random();
			int seed = rand.Next(1, 3);

			if (seed == 1)
			{
				this.Black = user1;
				this.White = user2;
			}
			else
			{
				this.Black = user2;
				this.White = user1;
			}
		}
		public async Task Start()
		{
			this.TimeLeft = TurnSeconds;
			this.Counter = new Timer(Countdown, null, 0, 1000);
			var connectionIds = GetConnectionIds();

			await Clients.Client(this.Black.ConnectionId).SendAsync("ReturnColor", "black");
			await Clients.Client(this.White.ConnectionId).SendAsync("ReturnColor", "white");
			await Clients.Clients(connectionIds).SendAsync("WhosTurn", this.TurnFlag);
			await Clients.Clients(connectionIds).SendAsync("StartGame");
			await Clients.Clients(connectionIds).SendAsync("ReturnPlayerColor", JsonSerializer.Serialize(GetPlayerColor()));
		}
		private async void Countdown(object state)
		{
			if (TimeLeft == 0)
			{
				this.TurnFlag = !this.TurnFlag;
				this.TimeLeft = TurnSeconds;
				//todo 時間到 決定勝負
			}
			var connectionIds = GetConnectionIds();
			await Clients.Clients(connectionIds).SendAsync("UpdateTimeLeft", this.TimeLeft);

			TimeLeft--;
		}
		public async Task SetPiece(int x, int y, string color)
		{
			this.TurnFlag = !this.TurnFlag;
			this.TimeLeft = TurnSeconds;
			this.Counter.Dispose();
			this.Counter = new Timer(Countdown, null, 0, 1000);

			int pieceInt = GetColorInt(color);
			this.PieceGrid[x, y] = pieceInt;
			//todo 檢查勝負

			await Clients.Clients(GetConnectionIds()).SendAsync("UpdateBoard", x, y, color);
		}
		public IEnumerable<string> GetConnectionIds()
		{
			return this.PlayerList.Union(this.GuestList).Select(u => u.ConnectionId);
		}
		public string[] GetPlayerColor()
		{
			string[] result = new string[2];
			result[this.PlayerList.IndexOf(this.Black)] = "black";
			result[this.PlayerList.IndexOf(this.White)] = "white";

			return result;
		}
		private int GetColorInt(string color) //黑1 白2
		{
			if (color == "black") return 1;
			if (color == "white") return 2;
			throw new Exception("color error");
		}
	}
	public enum Status
	{
		Waiting,
		Playing,
	}
}
