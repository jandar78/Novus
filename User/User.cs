using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace User
{
	public class User {

		public enum UserState {JustConnected, LoggingIn, CreatingCharacter, Talking, None };

		#region Properties
		public string UserID {
			get;
			set;
		}

		public Character.Character Player {
			get {
				return _character;
			}
		   set{
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

		public bool InBufferReady {
			get {
				return _userBuffer.IncomingReady;
			}
			private set { }
		}



		#endregion Properties

		#region members
		private ClientHandling.MessageBuffer _userBuffer;
		private Character.Character _character;
		#endregion members

		#region Constructors
		public User() {
			_userBuffer = new ClientHandling.MessageBuffer();
			CurrentState = UserState.JustConnected;
			UserID = new Guid().ToString();
			_character = CharacterFactory.Factory.CreateCharacter(CharacterEnums.CharacterType.PLAYER, CharacterEnums.CharacterRace.HUMAN, CharacterEnums.CharacterClass.FIGHTER);
			LoginCompleted = false;
		}
		#endregion Constructors


	}
}
