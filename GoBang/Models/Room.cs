using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoBang.Models
{
	public class Room
	{
		public static List<Room> RoomList { get; set; }

		public string RoomId { get; set; }
		public string RoomName { get; set; }
		public int RoomStatus { get; set; }
		public List<User> PlayerList { get; set; }
		private int[,] PieceGrid { get; set; }

		public Room(string roomName, User firstUser)
		{
			this.RoomId = Guid.NewGuid().ToString();
			this.RoomName = roomName;
			this.RoomStatus = (int)Status.Waiting;
			this.PlayerList = new List<User> { firstUser };
			this.PieceGrid = new int[15, 15];
		}
		static Room()
		{
			RoomList = new List<Room>();
		}
		public void AddUser(User user)
		{
			if (this.PlayerList.Count >= 2)
			{
				throw new Exception("人數已達上線");
			}
			this.PlayerList.Add(user);
		}
	}
	public enum Status
	{
		Waiting,
		Playing,
	}
}
