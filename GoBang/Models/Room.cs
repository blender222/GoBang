using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoBang.Models
{
	public class Room
	{
		public static Dictionary<string, Room> RoomList { get; set; }
		private int[][] PieceGrid { get; set; }
		public List<string> Users { get; set; }
	}
}
