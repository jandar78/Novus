using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;



///Gerneral purpose logger, thought about making this a singleton and figured it would be better if each component had it's own logger running
///instead of one Overseer logger trying to write to one big file that would later be a pain in the ass to go through.  Eventually it will inlcude
///the ability to send out error emails, when errors occur or tickets get created.
///
namespace Logger {
	public class Logger {

		private StreamWriter LogFile {
			get;
			set;
		}

		public string FilePath {
			get {
				return _path;
			}
			private set {
				_path = DirectoryPath + "LogFile(" + DateTime.Now.ToShortDateString().Replace("/", "-") + ").txt";
			}
		}

		public string DirectoryPath {
			get {
				return _directory;
			}
			set {
				if (value[value.Length - 1] != '\\') {
					_directory = value + "\\";
				}
				else {
					_directory = value;
				}
			}
		}

		private string _directory;
		private string _path;

		public Logger(string directoryPath) {
			DirectoryPath = directoryPath;
			FilePath = DirectoryPath;
			CheckExists();
			LogFile = new StreamWriter(FilePath, true);
		}

		public void Log(string message) {
			LogFile.WriteLine(DateTime.Now.ToShortTimeString() + ": " + message);
			LogFile.Flush();
		}

		private void CheckExists() {
			if (!Directory.Exists(DirectoryPath)) {
				Directory.CreateDirectory(DirectoryPath);
			}
		}
	}
}
