using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoBang.Models
{
	public class User
	{
		public static List<User> UserList { get; set; }
		public string ConnectionId { get; set; }
		public string UserName { get; set; }
		static User()
		{
			UserList = new List<User>();
		}
	}
}
