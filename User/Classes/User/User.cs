using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interfaces;
using Factories;
using MongoDB.Bson;

namespace Sockets {

    public class User : IUser{

		#region Properties
        public bool HourFormat24 {
            get;
            set;
        }

		public ObjectId UserID {
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

        public IActor Player {
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

		public List<ObjectId> FriendsList {
			get;
			set;
		}

		public ObjectId LogID {
			get {
				return ObjectId.Parse(_userBuffer.LogId);
			}
			set {
				_userBuffer.LogId = value.ToString();
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
		private Messages.MessageBuffer _userBuffer { get; set; }
		private IActor _character;
        private bool _TimeFormat;
		#endregion members

		#region Constructors
		public User(bool npc = false) {
            if (!npc) {
                CurrentState = UserState.JUST_CONNECTED;
                _character = Factory.CreateCharacter(CharacterType.PLAYER);
                _character.UserID = UserID;
                _userBuffer = new Messages.MessageBuffer(UserID.ToString());
                LastDisconnected = DateTime.MinValue;
                LoginCompleted = false;
            }
           
			UserID = new ObjectId();	
		}

        public User() {
            CurrentState = UserState.JUST_CONNECTED;
            _character = Factory.CreateCharacter(CharacterType.PLAYER);
            _character.UserID = UserID;
            _userBuffer = new Messages.MessageBuffer(UserID.ToString());
            LastDisconnected = DateTime.MinValue;
            LoginCompleted = false;
        }
		#endregion Constructors


		/// <summary>
		/// Use this call for players to receive the Message.Room message and for NPCS to parse all messages for triggers
		/// </summary>
		/// <param name="message"></param>
        public void MessageHandler(IMessage message) {
            if (!this.Player.IsNPC) {
                OutBuffer = message.Room;
            }
            else {
                INpc npc = _character as INpc;
                if (npc != null) {
                    npc.ParseMessage(message);
                }
            }
        }

		/// <summary>
		/// This should only be used for players to handle Message.Self
		/// </summary>
		/// <param name="message"></param>
		public void MessageHandler(string message) {
			if (!this.Player.IsNPC) {
				OutBuffer = message;
			}
		}
	}
}
