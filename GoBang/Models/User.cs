using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoBang.Models
{
	public class User
	{
		public static Dictionary<string, User> UserList { get; set; }
		public string UserName { get; set; }
		static User()
		{
			UserList = new Dictionary<string, User>();
		}
	}
}
