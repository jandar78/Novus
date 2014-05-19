using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Timer
{

	public static class Timer 
	{
		public static System.Timers.Timer timer;	

		public static void Start() {
			timer = new System.Timers.Timer(30 * 1000);
			timer.AutoReset = true;
			timer.Enabled = true;
		}
	}
}
