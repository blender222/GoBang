using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoBang.Models
{
	public class Room
	{
		public static Dictionary<string, Room> RoomList { get; set; }

		public string RoomName { get; set; }
		public int RoomStatus { get; set; }
		public List<User> Users { get; set; }
		private int[,] PieceGrid { get; set; }

		public Room()
		{
			this.PieceGrid = new int[15, 15];
		}
		public void AddUser(User user)
		{
			if (this.Users.Count >= 2)
			{
				throw new Exception("人數已達上線");
			}
			this.Users.Add(user);
		}
	}
	public enum RoomStatus
	{
		Waiting,
		Playing,
	}
}
