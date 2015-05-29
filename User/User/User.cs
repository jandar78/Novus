using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace User
{
	public class User {

		public enum UserState {NONE, JUST_CONNECTED, LOGGING_IN, CREATING_CHARACTER, LEVEL_UP, TALKING, LIMBO, DISCONNECT,  };

		#region Properties
        public bool HourFormat24 {
            get;
            set;
        }

		public string UserID {
			get;
			set;
		}

		public DateTime LastDisconnected {
			get;
			set;
		}

		public string GroupName {
			get;
			set;
		}

        public Character.Iactor Player {
            get {
                return _character;
            }
            set {
                _character = value;
            }
        }

		public UserState CurrentState {
			get;
			set;
		}

		public bool LoginCompleted {
			get;
			set;
		}

		public List<string> FriendsList {
			get;
			set;
		}

		public string LogID {
			get {
				return _userBuffer.LogId;
			}
			set {
				_userBuffer.LogId = value;
			}
		}

		public string OutBuffer {
			get {
				return _userBuffer.OutgoingBuffer;
			}
			set {
				_userBuffer.OutgoingBuffer = value;
			}
		}

		public string InBuffer {
			get {
				return _userBuffer.IncomingBuffer;
			}
			set {
				_userBuffer.IncomingBuffer = value;
			}
		}

		public string InBufferPeek {
			get {
				return _userBuffer.IncomingBufferPeek.Replace("\r\n","");
			}
		}

		public string telnetBufferPeek {
			get {
				return _userBuffer.IncomingTelnetBufferPeek.Replace("\r\n", "");
			}
		}

		public bool InBufferReady {
			get {
				return _userBuffer.IncomingReady;
			}
			private set { }
		}



		#endregion Properties

		#region members
		private ClientHandling.MessageBuffer _userBuffer;
		private Character.Iactor _character;
        private bool _TimeFormat;
		#endregion members

		#region Constructors
		public User(bool npc = false) {
            if (!npc) {
                CurrentState = UserState.JUST_CONNECTED;
                _character = CharacterFactory.Factory.CreateCharacter(CharacterEnums.CharacterType.PLAYER);
                _userBuffer = new ClientHandling.MessageBuffer(UserID);
                LastDisconnected = DateTime.MinValue;
                LoginCompleted = false;
            }
           
			UserID = Guid.NewGuid().ToString();	
		}

        public User() {
            CurrentState = UserState.JUST_CONNECTED;
            _character = CharacterFactory.Factory.CreateCharacter(CharacterEnums.CharacterType.PLAYER);
            _userBuffer = new ClientHandling.MessageBuffer(UserID);
            LastDisconnected = DateTime.MinValue;
            LoginCompleted = false;
        }
		#endregion Constructors

        public void MessageHandler(string message) {
            if (!this.Player.IsNPC) {
                OutBuffer = message;
            }
            else {
                Character.Inpc npc = _character as Character.Inpc;
                if (npc != null) {
                    npc.ParseMessage(message);
                }
            }
        }
	}
}
