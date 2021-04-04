using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		private int[,] PieceGrid { get; set; }
		private int TimeLeft { get; set; }
		private int TurnIndex { get; set; } //正在下的玩家所在的PlayerList的Index

		public Room(string roomName, User firstUser, IHubCallerClients clients)
		{
			this.Clients = clients;
			this.RoomId = Guid.NewGuid().ToString();
			this.RoomName = roomName;
			this.RoomStatus = (int)Status.Waiting;
			this.PlayerList = new List<User> { firstUser };
			this.GuestList = new List<User>();
			this.ReadyArray = new bool[2];
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
		public async Task SetColor()
		{
			Random rand = new Random();
			int firstIndex = rand.Next(0, 2);

			this.TurnIndex = firstIndex;
			if (firstIndex == 0)
			{
				this.Black = this.PlayerList[0];
				this.White = this.PlayerList[1];
			}
			else
			{
				this.Black = this.PlayerList[1];
				this.White = this.PlayerList[0];
			}
			await Clients.Client(this.Black.ConnectionId).SendAsync("MyColor", "black");
			await Clients.Client(this.White.ConnectionId).SendAsync("MyColor", "white");
		}
		public async Task Start()
		{
			this.PieceGrid = new int[16, 16];
			this.TimeLeft = TurnSeconds;
			this.Counter = new Timer(Countdown, null, 0, 1000);
			var connectionIds = GetConnectionIds();

			await Clients.Clients(connectionIds).SendAsync("ReturnTurnIndex", this.TurnIndex);
			await Clients.Client(this.PlayerList[0].ConnectionId).SendAsync("ControlBoard", this.TurnIndex == 0);
			await Clients.Client(this.PlayerList[1].ConnectionId).SendAsync("ControlBoard", this.TurnIndex == 1);
			await Clients.Clients(connectionIds).SendAsync("StartGame");
			await Clients.Clients(connectionIds).SendAsync("ReturnPlayerColor", JsonSerializer.Serialize(GetPlayerColor()));
		}
		private async void Countdown(object state)
		{
			var connectionIds = GetConnectionIds();
			if (TimeLeft == 0)
			{
				//todo 時間到 決定勝負

			}
			await Clients.Clients(connectionIds).SendAsync("UpdateTimeLeft", this.TimeLeft);

			TimeLeft--;
		}
		public async Task SetPiece(int x, int y, string color)
		{
			this.TurnIndex = (this.TurnIndex == 0) ? 1 : 0;

			this.TimeLeft = TurnSeconds;
			this.Counter.Dispose();
			this.Counter = new Timer(Countdown, null, 0, 1000);

			int pieceInt = ColorToInt(color);
			if (this.PieceGrid[y, x] == 0)
			{
				this.PieceGrid[y, x] = pieceInt;
			}
			else
			{
				throw new Exception($"棋盤資料異常 y = {y}, x = {x}");
			}

			var connectionIds = GetConnectionIds();
			await Clients.Clients(connectionIds).SendAsync("ReturnTurnIndex", this.TurnIndex);
			await Clients.Clients(connectionIds).SendAsync("UpdateBoard", x, y, color);
			await Clients.Client(this.PlayerList[0].ConnectionId).SendAsync("ControlBoard", this.TurnIndex == 0);
			await Clients.Client(this.PlayerList[1].ConnectionId).SendAsync("ControlBoard", this.TurnIndex == 1);

			int tempX = x;
			int tempY = y;

			if (CheckFive(1, 0, x, y, pieceInt) ||
					CheckFive(0, 1, x, y, pieceInt) ||
					CheckFive(1, 1, x, y, pieceInt) ||
					CheckFive(1, -1, x, y, pieceInt))
			{
				ResetGame();
				await Clients.Clients(connectionIds).SendAsync("EndGame", color);
			}

			Debug.WriteLine("   1 2 3 4 5 6 7 8 9 0 1 2 3 4 5");
			for (int i = 1; i <= 15; i++)
			{
				Debug.Write(i.ToString().PadLeft(2) + " ");
				for (int j = 1; j <= 15; j++)
				{
					Debug.Write(this.PieceGrid[i, j] + " ");
				}
				Debug.Write("\n");
			}
			Debug.WriteLine("========================================");
		}
		public List<int[]> GetPieceData()
		{
			List<int[]> data = new List<int[]>();
			for (int y = 1; y <= 15; y++)
			{
				for (int x = 1; x <= 15; x++)
				{
					if (this.PieceGrid[y, x] != 0)
						data.Add(new int[] { x, y, this.PieceGrid[y, x] });
				}
			}
			return data;
		}
		public bool CheckFive(int diffX, int diffY, int x, int y, int pieceInt)
		{
			int total = 1;
			int x0 = x;
			int y0 = y;
			while (true)
			{
				x += diffX;
				y += diffY;
				if (IsInRange(x, y) && this.PieceGrid[y, x] == pieceInt)
					total++;
				else
					break;
			}
			x = x0;
			y = y0;
			while (true)
			{
				x -= diffX;
				y -= diffY;
				if (IsInRange(x, y) && this.PieceGrid[y, x] == pieceInt)
					total++;
				else
					break;
			}
			return total >= 5;
		}
		public static bool IsInRange(int x, int y)
		{
			if (x > 15 || x < 1) return false;
			if (y > 15 || y < 1) return false;
			return true;
		}
		public void ResetGame()
		{
			this.TimeLeft = TurnSeconds;
			this.Counter.Dispose();
			this.RoomStatus = (int)Status.Waiting;
			this.ReadyArray = new bool[2];
			this.Black = null;
			this.White = null;
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
		/// <summary>
		/// 黑 = 1, 白 = 2
		/// </summary>
		private static int ColorToInt(string color)
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
