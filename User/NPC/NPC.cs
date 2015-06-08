using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Extensions;
using CharacterEnums;
using System.Collections.Concurrent;
using Triggers;

namespace Character {
	public class NPC : Iactor, Inpc {
		#region private things
		private Dictionary<string, double> damageTracker;
		private Inventory _inventory;
		private Equipment _equipment;

		#endregion private things

		#region Public Members
		public Inventory Inventory {
			get {
				return _inventory;
			}
			set {
				_inventory = value;
			}
		}
		public Equipment Equipment {
			get {
				return _equipment;
			}
			set {
				_equipment = value;
			}
		}
		public Queue<string> Messages;
		public List<ITrigger> Triggers;
		#endregion Public Members

		#region Protected Members
		protected Dictionary<string, Attribute> Attributes;
		protected Dictionary<string, double> SubAttributes;
		protected HashSet<CharacterEnums.Languages> KnownLanguages; //this will hold all the languages the player can understand
		protected double _levelModifier;
		private StatBonuses Bonuses;

		#region Stances
		protected CharacterStanceState _stanceState;
		protected CharacterActionState _actionState;
		#endregion Stances

		#region Misc
		protected int _level;
		protected CharacterClass _class;
		protected Languages _primaryLanguage;
		protected Tuple<int, DateTime> _koCount; //this will only ever be zero on initialize until first knockout
		protected int _points;
		#endregion Misc

		#region Bodily descriptions
		protected Genders _gender;
		protected EyeColors _eyeColor;
		protected HairColors _hairColor;
		protected SkinColors _skinColor;
		protected SkinType _skinType;
		protected BodyBuild _build;
		protected CharacterRace _race;
		#endregion Bodily descriptions
		#endregion Protected Members

		#region Private members
		private int points = 0;
		#endregion Private members

		#region  Properties
		public string Password {
			get;
			set;
		}

		public long Experience {
			get;
			set;
		}

		public long NextLevelExperience {
			get;
			set;
		}

		public bool Leveled {
			get;
			set;
		}

		public bool IsLevelUp {
			get {
				if (Leveled || Experience >= NextLevelExperience) {
					return true;
				}
				return false;
			}
		}


		public string ID {
			get;
			set;
		}

		public int Location {
			get;
			set;
		}

		public int LastLocation {
			get;
			set;
		}

		public bool IsNPC {
			get;
			set;
		}

		public int MobTypeID {
			get;
			set;
		}

		public AI.FSM Fsm {
			get;
			set;

		}

		public string Title {
			get;
			set;
		}

		public string FullName {
			get {
				return FirstName + " " + LastName;
			}
		}

		public string FullHonors {
			get {
				return FullName + Title;
			}
		}

		public DateTime NextAiAction {
			get;
			set;
		}

		public void Update() {
			Fsm.Update(this);
		}

		private bool IsMob {
			get;
			set;
		}


		#endregion Properties

		#region Descriptive
		public string Gender {
			get {
				return _gender.ToString().CamelCaseWord();
			}
		}

		public string MainHand {
			get;
			set;
		}


		public string GenderPossesive {
			get {
				if (Gender == "Male") {
					return "He";
				}
				else if (Gender == "Female") {
					return "She";
				}
				else
					return "It";
			}
		}

		public string Build {
			get {
				return _build.ToString().CamelCaseWord();
			}
		}

		public int Age {
			get;
			set;
		}

		public double Weight {
			get;
			set;
		}

		public double Height {
			get;
			set;
		}

		public string FirstName {
			get;
			set;
		}

		public string LastName {
			get;
			set;
		}

		public string Class {
			get {
				return _class.ToString().CamelCaseWord();
			}
		}

		public string Race {
			get {
				return _race.ToString().CamelCaseWord();
			}
		}

		public string Description {
			get;
			set;
		}

		public string EyeColor {
			get {
				return _eyeColor.ToString().CamelCaseWord();
			}
		}

		public string SkinColor {
			get {
				return _skinColor.ToString().CamelCaseWord();
			}
		}

		public string SkinType {
			get {
				return _skinType.ToString().CamelCaseWord();
			}
		}

		public string HairColor {
			get {
				return _hairColor.ToString().CamelCaseWord();
			}
		}
		#endregion Descriptive

		#region Stances
		public string Action {
			get {
				return ActionState.ToString().Replace("_", " ").ToLower();
			}
		}

		public string Stance {
			get {
				return StanceState.ToString().Replace("_", " ").ToLower();
			}
		}

		public CharacterStanceState StanceState {
			get {
				return _stanceState;
			}
		}

		public CharacterActionState ActionState {
			get {
				return _actionState;
			}
		}
		#endregion Stances

		#region Leveling
		public int PointsToSpend {
			get {
				return _points;
			}
			set {
				_points += value;
			}
		}

		public int Level {
			get;
			set;
		}

		public double LevelModifier {
			get {
				return _levelModifier;
			}
			set {
				MongoUtils.MongoData.ConnectToDatabase();
				MongoDatabase db = MongoUtils.MongoData.GetDatabase("Character");
				MongoCollection charCollection = db.GetCollection("General");
				BsonDocument result = charCollection.FindOneAs<BsonDocument>(Query.EQ("_id", "LevelModifier"));
				if (result != null) {
					_levelModifier = result[Level.ToString()].AsDouble;
				}
			}
		}
		#endregion Leveling

		#region Combat
		public string KillerID {
			get;
			set;
		}

		public DateTime TimeOfDeath {
			get;
			set;
		}

		public bool CheckUnconscious {
			get {
				bool result = false;
				double health = Attributes["Hitpoints"].Value;

				if (health > DeathLimit && health <= 0) {
					result = true;
					//guy's knocked out let's increment the KO counter
					if ((_koCount.Item1 > 0 && _koCount.Item1 < 3) && (_koCount.Item2 - DateTime.Now).Minutes < 10) {
						_koCount = new Tuple<int, DateTime>(_koCount.Item1 + 1, _koCount.Item2);
					}
					//ok he got knocked out 3 times in less than 10 minutes he's dead now
					else if (_koCount.Item1 == 3 && (_koCount.Item2 - DateTime.Now).Minutes < 10) {
						Attributes["Hitpoints"].ApplyNegative(100);
					}
					//well at this point we'll reset his knockout counter and reset the timer since he hasn't been knocked out in at least 10 minutes
					else {
						_koCount = new Tuple<int, DateTime>(1, DateTime.Now); //it's not zero because he's knocked out!!
					}
				}
				//if no longer unconcious, remove the state
				else if (health > 0) {
					if (ActionState == CharacterActionState.Unconcious) {
						SetActionState(CharacterActionState.None);
						SetStanceState(CharacterStanceState.Prone);
					}
				}

				return result;
			}
		}

		public bool CheckDead {
			get {
				bool result = false;

				if (GetAttributeValue("Hitpoints") <= DeathLimit) {
					result = true;
				}

				return result;
			}
		}

		public double DeathLimit {
			get {
				//would probably be a good idea to grab the multiplier from the database
				double value = GetAttributeMax("Hitpoints");
				return (-1 * (0.015 * value));
			}
		}

		public string CurrentTarget {
			get;
			set;
		}

		public string LastTarget {
			get;
			set;
		}

		public bool InCombat {
			get;
			set;
		}

		public DateTime LastCombatTime {
			get;
			set;
		}
		#endregion Combat

		public NPC() {
		}

		public NPC(CharacterRace race, CharacterClass characterClass, Genders gender, Languages language, SkinColors skinColor, SkinType skinType, HairColors hairColor, EyeColors eyeColor, BodyBuild build) {
			Messages = new Queue<string>();

			Fsm = AI.FSM.GetInstance();
			Fsm.state = Fsm.GetStateFromName("Wander");

			_class = characterClass;
			_race = race;
			_gender = gender;
			_skinColor = skinColor;
			_skinType = skinType;
			_hairColor = hairColor;
			_eyeColor = eyeColor;
			_build = build;

			_koCount = new Tuple<int, DateTime>(0, DateTime.Now);
			_actionState = CharacterActionState.None;
			_stanceState = CharacterStanceState.Standing;

			_primaryLanguage = language;
			KnownLanguages = new HashSet<Languages>();
			KnownLanguages.Add(_primaryLanguage);

			Inventory = new Inventory();
			damageTracker = new Dictionary<string, double>();
			Triggers = new List<ITrigger>();
			Bonuses = new StatBonuses();

			FirstName = "";
			LastName = "";
			Description = "";
			Age = 17;   //Do we want an age? And are we going to advance it every in game year?  Players could be 400+ years old rather quick.
			Weight = 180.0d; //pounds or kilos?
			Height = 70.0d;  //inches or centimeters?
			Location = 1000;
			InCombat = false;
			LastCombatTime = DateTime.MinValue.ToUniversalTime();
			IsNPC = true;
			Leveled = false;
			MainHand = "WIELD_RIGHT";
			NextLevelExperience = 300;
			Level = 1;
			Experience = 0;
			PointsToSpend = 0;
			IsMob = false;

			Inventory = new Inventory();
			Equipment = new Equipment();

			Inventory.playerID = this.ID;
			Equipment.playerID = this.ID;

			Attributes = new Dictionary<string, Attribute>();

			Attributes.Add("Hitpoints", new Attribute(200.0d, "Hitpoints", 200.0d, 0.2d, 1));
			Attributes.Add("Dexterity", new Attribute(10.0d, "Dexterity", 10.0d, 0.0d, 1));
			Attributes.Add("Strength", new Attribute(10.0d, "Strength", 10.0d, 0.0d, 1));
			Attributes.Add("Intelligence", new Attribute(10.0d, "Intelligence", 10.0d, 0.0d, 1));
			Attributes.Add("Endurance", new Attribute(10.0d, "Endurance", 10.0d, 0.0d, 1));
			Attributes.Add("Charisma", new Attribute(10.0d, "Charisma", 10.0d, 0.0d, 1));

			SubAttributes = new Dictionary<string, double>();

			SubAttributes.Add("Agility", 10.0d);
			SubAttributes.Add("Toughness", 10.0d);
			SubAttributes.Add("Cunning", 10.0d);
			SubAttributes.Add("Wisdom", 10.0d);
			SubAttributes.Add("Leadership", 10.0d);
		}

		public void Save() {
			MongoUtils.MongoData.ConnectToDatabase();
			MongoDatabase characterDB = MongoUtils.MongoData.GetDatabase("Characters");
			if (this.ID == null) {
				this.ID = new MongoDB.Bson.ObjectId().ToString();
			}; //new character
			MongoCollection characterCollection = characterDB.GetCollection<BsonDocument>("NPCCharacters");
			IMongoQuery search = Query.EQ("_id", ObjectId.Parse(this.ID));
			BsonDocument npcCharacter = characterCollection.FindOneAs<BsonDocument>(search);

			if (npcCharacter == null) {
				//this is the NPC's first save, create everything from scratch
				npcCharacter = new BsonDocument();
				npcCharacter.Add("FirstName", this.FirstName);
				npcCharacter.Add("LastName", this.LastName);
				npcCharacter.Add("Race", this.Race.CamelCaseWord());
				npcCharacter.Add("Class", this.Class.CamelCaseWord());
				npcCharacter.Add("Gender", this.Gender.CamelCaseWord());
				npcCharacter.Add("SkinColor", this.SkinColor.CamelCaseWord());
				npcCharacter.Add("SkinType", this.SkinType.CamelCaseWord());
				npcCharacter.Add("HairColor", this.HairColor.CamelCaseWord());
				npcCharacter.Add("EyeColor", this.EyeColor.CamelCaseWord());
				npcCharacter.Add("Weight", this.Weight);
				npcCharacter.Add("Height", this.Height);
				npcCharacter.Add("ActionState", this.ActionState.ToString().CamelCaseWord());
				npcCharacter.Add("StanceState", this.StanceState.ToString().CamelCaseWord());
				npcCharacter.Add("Description", this.Description);
				npcCharacter.Add("Location", this.Location);
				npcCharacter.Add("AiState", Fsm.state.ToString());
				npcCharacter.Add("previousAiState", Fsm.previousState != null ? Fsm.previousState.ToString() : "");
				npcCharacter.Add("AiGlobalState", Fsm.globalState != null ? Fsm.globalState.ToString() : "");
				npcCharacter.Add("NextAiAction", this.NextAiAction.ToUniversalTime());
				npcCharacter.Add("IsNPC", this.IsNPC);
				npcCharacter.Add("MobTypeID", this.MobTypeID);
				npcCharacter.Add("CurrentTarget", this.CurrentTarget != null ? this.CurrentTarget.ToString() : "");
				npcCharacter.Add("LastTarget", this.LastTarget != null ? this.LastTarget.ToString() : "");
				npcCharacter.Add("InCombat", this.InCombat);
				npcCharacter.Add("LastCombatTime", this.LastCombatTime);
				npcCharacter.Add("Experience", this.Experience);
				npcCharacter.Add("Level", this.Level);
				npcCharacter.Add("KillerID", this.KillerID);
				npcCharacter.Add("Title", this.Title);

				BsonArray attributeList = new BsonArray();

				foreach (Attribute a in this.Attributes.Values) {
					BsonDocument attributes = new BsonDocument();
					attributes.Add("Name", "");
					attributes.Add("Value", "");
					attributes.Add("Max", "");
					attributes.Add("RegenRate", "");

					attributes["Name"] = a.Name;
					attributes["Value"] = a.Value;
					attributes["Max"] = a.Max;
					attributes["RegenRate"] = a.RegenRate;

					attributeList.Add(attributes);
				}
				npcCharacter.Add("Attributes", attributeList);

				BsonArray xpTracker = new BsonArray();

				foreach (KeyValuePair<string, double> tracker in damageTracker) {
					BsonDocument track = new BsonDocument();
					track.Add("ID", "");
					track.Add("Value", 0.0);

					track["ID"] = tracker.Key;
					track["Value"] = tracker.Value;

					xpTracker.Add(track);
				}

				npcCharacter.Add("XpTracker", xpTracker);

				npcCharacter.Add("Bonuses", Bonuses.GetBson());
			}
			else {
				npcCharacter["FirstName"] = this.FirstName;
				npcCharacter["LastName"] = this.LastName;
				npcCharacter["Race"] = this.Race;
				npcCharacter["Class"] = this.Class;
				npcCharacter["Gender"] = this.Gender.CamelCaseWord();
				npcCharacter["SkinColor"] = this.SkinColor.CamelCaseWord();
				npcCharacter["SkinType"] = this.SkinType.CamelCaseWord();
				npcCharacter["HairColor"] = this.HairColor.CamelCaseWord();
				npcCharacter["EyeColor"] = this.EyeColor.CamelCaseWord();
				npcCharacter["Weight"] = this.Weight;
				npcCharacter["Height"] = this.Height;
				npcCharacter["Description"] = this.Description;
				npcCharacter["Location"] = this.Location;
				npcCharacter["ActionState"] = this.ActionState.ToString().CamelCaseWord();
				npcCharacter["StanceState"] = this.StanceState.ToString().CamelCaseWord();
				npcCharacter["AiState"] = Fsm.state.ToString();
				npcCharacter["previousAiState"] = Fsm.previousState == null ? "" : Fsm.previousState.ToString();
				npcCharacter["AiGlobalState"] = Fsm.globalState == null ? "" : Fsm.globalState.ToString();
				npcCharacter["NextAiAction"] = this.NextAiAction.ToUniversalTime();
				npcCharacter["MobTypeID"] = this.MobTypeID;
				npcCharacter["IsNPC"] = this.IsNPC;
				npcCharacter["CurrentTarget"] = this.CurrentTarget == null ? "" : this.CurrentTarget;
				npcCharacter["LastTarget"] = this.LastTarget == null ? "" : this.LastTarget;
				npcCharacter["InCombat"] = this.InCombat;
				npcCharacter["LastCombatTime"] = this.LastCombatTime;
				npcCharacter["Experience"] = this.Experience;
				npcCharacter["Level"] = this.Level;
				npcCharacter["KillerID"] = this.KillerID;
				npcCharacter["Title"] = this.Title;

				BsonArray playerAttributes = new BsonArray();
				BsonArray xpTracker = new BsonArray();

				foreach (KeyValuePair<string, Attribute> attribute in Attributes) {
					BsonDocument attrib = new BsonDocument();
					attrib.Add("Name", "");
					attrib.Add("Value", "");
					attrib.Add("Max", "");
					attrib.Add("RegenRate", "");


					attrib["Name"] = attribute.Key;
					attrib["Value"] = attribute.Value.Value;
					attrib["Max"] = attribute.Value.Max;
					attrib["RegenRate"] = attribute.Value.RegenRate;

					playerAttributes.Add(attrib);
				}

				npcCharacter["Attributes"] = playerAttributes;

				foreach (KeyValuePair<string, double> tracker in damageTracker) {
					BsonDocument track = new BsonDocument();
					track.Add("ID", "");
					track.Add("Value", 0.0d);

					track["ID"] = tracker.Key;
					track["Value"] = tracker.Value;

					xpTracker.Add(track);
				}

				npcCharacter["XpTracker"] = xpTracker;

				npcCharacter["Bonuses"] = Bonuses.GetBson();
			}

			characterCollection.Save(npcCharacter);

			if (this.ID == "000000000000000000000000") {
				this.ID = npcCharacter["_id"].AsObjectId.ToString();
			}

		}

		public void Load(string id) {
			MongoUtils.MongoData.ConnectToDatabase();
			MongoDatabase characterDB = MongoUtils.MongoData.GetDatabase("Characters");
			MongoCollection characterCollection = characterDB.GetCollection<BsonDocument>("NPCCharacters");
			IMongoQuery query = Query.EQ("_id", ObjectId.Parse(id));
			BsonDocument found = characterCollection.FindOneAs<BsonDocument>(query);

			ID = found["_id"].AsObjectId.ToString();
			FirstName = found["FirstName"].AsString.CamelCaseWord();
			LastName = found["LastName"].AsString.CamelCaseWord();
			_class = (CharacterClass)Enum.Parse(typeof(CharacterClass), found["Class"].AsString.CamelCaseWord());
			_race = (CharacterRace)Enum.Parse(typeof(CharacterRace), found["Race"].AsString.CamelCaseWord());
			_gender = (Genders)Enum.Parse(typeof(Genders), found["Gender"].AsString.CamelCaseWord());
			_skinType = (SkinType)Enum.Parse(typeof(SkinType), found["SkinType"].AsString.CamelCaseWord());
			_skinColor = (SkinColors)Enum.Parse(typeof(SkinColors), found["SkinColor"].AsString.CamelCaseWord());
			_skinType = (SkinType)Enum.Parse(typeof(SkinType), found["SkinType"].AsString.CamelCaseWord());
			_hairColor = (HairColors)Enum.Parse(typeof(HairColors), found["HairColor"].AsString.CamelCaseWord());
			_eyeColor = (EyeColors)Enum.Parse(typeof(EyeColors), found["EyeColor"].AsString.CamelCaseWord());
			_stanceState = (CharacterStanceState)Enum.Parse(typeof(CharacterStanceState), found["StanceState"].AsString.CamelCaseWord());
			_actionState = (CharacterActionState)Enum.Parse(typeof(CharacterActionState), found["ActionState"].AsString.CamelCaseWord());
			Description = found["Description"].AsString;
			Location = found["Location"].AsInt32;
			Height = found["Height"].AsDouble;
			Weight = found["Weight"].AsDouble;
			IsNPC = found["IsNPC"].AsBoolean;
			MobTypeID = found["MobTypeID"].AsInt32;
			NextAiAction = found["NextAiAction"].ToUniversalTime();
			InCombat = found["InCombat"].AsBoolean;
			LastCombatTime = found["LastCombatTime"].ToUniversalTime();
			CurrentTarget = found["CurrentTarget"].AsString != "" ? found["CurrentTarget"].AsString : null;
			LastTarget = found["LastTarget"].AsString != "" ? found["LastTarget"].AsString : null;
			Fsm.state = Fsm.GetStateFromName(found["AiState"].AsString);
			Fsm.previousState = Fsm.GetStateFromName(found["previousAiState"].AsString);
			Fsm.globalState = Fsm.GetStateFromName(found["AiGlobalState"].AsString);
			Experience = found["Experience"].AsInt64;
			Level = found["Level"].AsInt32;
			Title = found.Contains("Title") ? found["Title"].AsString : "";
			KillerID = found.Contains("KillerID") ? found["KillerID"].AsString : "";

			//if you just use var instead of casting it like this you will be in a world of pain and suffering when dealing with subdocuments.
			BsonArray playerAttributes = found["Attributes"].AsBsonArray;
			BsonArray xpTracker = found["XpTracker"].AsBsonArray;
			BsonDocument triggers = found["Triggers"].AsBsonDocument;
			BsonArray bonusesList = null;
			if (found.Contains("Bonuses")) {
				bonusesList = found["Bonuses"].AsBsonArray;
			}

			if (playerAttributes != null) {
				foreach (BsonDocument attrib in playerAttributes) {

					if (!this.Attributes.ContainsKey(attrib["Name"].ToString())) {
						Attribute tempAttrib = new Attribute();
						tempAttrib.Name = attrib["Name"].ToString();
						tempAttrib.Value = attrib["Value"].AsDouble;
						tempAttrib.Max = attrib["Max"].AsDouble;
						tempAttrib.RegenRate = attrib["RegenRate"].AsDouble;


						this.Attributes.Add(tempAttrib.Name, tempAttrib);
					}
					else {
						this.Attributes[attrib["Name"].ToString()].Max = attrib["Max"].AsDouble;
						this.Attributes[attrib["Name"].ToString()].Value = attrib["Value"].AsDouble;
						this.Attributes[attrib["Name"].ToString()].RegenRate = attrib["RegenRate"].AsDouble;
					}
				}
			}

			if (xpTracker != null && xpTracker.Count > 0) {
				foreach (BsonDocument track in xpTracker) {
					//we just newed this up so it should always have to be refilled
					damageTracker.Add(track["ID"].AsString, track["Value"].AsDouble);
				}
			}

			ITrigger trigger = new GeneralTrigger(triggers, "NPC");
			Triggers.Add(trigger);

			if (bonusesList != null && bonusesList.Count > 0) {
				Bonuses.LoadFromBson(bonusesList);
			}
		}

		public void CalculateXP() {
			if (this.IsDead()) {
				foreach (KeyValuePair<string, double> pair in damageTracker) {
					User.User player = MySockets.Server.GetAUser(pair.Key);
					if (player != null) {
						double rewardPercentage = ((pair.Value * -1) / GetAttributeMax("Hitpoints"));
						if (rewardPercentage > 1.0)
							rewardPercentage = 1.0;
						long xp = (long)(Experience * rewardPercentage);
						//ok based on the level of the player should we provide less and less XP based on the target level
						//to prevent farming of easy targets?  For now sure why the hell not, maybe in the future we'll just increase the level
						//required by a shit ton each time they level up.  We'll do a four tier approach we''l discount 25% for each level above the target level
						int levelDifference = player.Player.Level - Level;
						if (levelDifference == 1) {
							xp = xp - (long)(xp * 0.25);
						}
						else if (levelDifference == 2) {
							xp = xp - (long)(xp * 0.5);
						}
						else if (levelDifference == 3) {
							xp = xp - (long)(xp * 0.75);
						}
						else if (levelDifference >= 4) {
							xp = 0;
						}

						player.Player.RewardXP(ID, xp);
					}
				}
			}
		}

		public void IncreaseXPReward(string id, double damage) {
			//we only want to deal with base hitpoints, knocking unconcious doesn't add to the XP reward
			if (IsUnconcious()) {
				damage += 100;
			}

			if (damageTracker.ContainsKey(id)) {
				damageTracker[id] = damageTracker[id] + (damage * -1);
			}
			else {
				damageTracker.Add(id, damage);
			}
			Save();
		}

		public void DecreaseXPReward(double amount) {
			double individualAmount = amount / (double)damageTracker.Count;

			var damageList = new Dictionary<string, double>(damageTracker);
			//totalDecrease needs to be divided amongst all players who are in the XP List
			foreach (KeyValuePair<string, double> pair in damageList) {
				damageTracker[pair.Key] = pair.Value + (individualAmount);
			}

			Save();
		}

		public void ParseMessage(string message) {
			Fsm.InterpretMessage(message, this);
		}

		public void SetActionState(CharacterActionState state) {
			_actionState = state;
		}

		public void SetStanceState(CharacterStanceState state) {
			_stanceState = state;
		}

		public void SetActionStateDouble(double state) {
			_actionState = (CharacterActionState)(int)state;
		}

		public void SetStanceStateDouble(double state) {
			_stanceState = (CharacterStanceState)(int)state;
		}

		public void IncreasePoints() {
			if (Level % 10 == 0) {
				_points += 4;
			}
			else if (Level % 5 == 0) {
				_points += 3;
			}
			else if (Level % 1 == 0) {
				_points += 1;
			}
		}

		public bool IsUnconcious() {
			bool result = false;
			if (CheckUnconscious) {
				SetActionState(CharacterEnums.CharacterActionState.Unconcious);
				SetStanceState(CharacterStanceState.Laying_unconcious);
				ClearTarget();
				result = true;
			}
			else {
				if (ActionState == CharacterActionState.Unconcious) {
					SetActionState(CharacterActionState.None);
				}
				if (StanceState == CharacterStanceState.Laying_unconcious) {
					SetStanceState(CharacterStanceState.Prone);
				}
			}

			return result;
		}

		public bool IsDead() {
			bool result = false;
			if (CheckDead) {
				SetActionState(CharacterActionState.Dead);
				SetStanceState(CharacterStanceState.Laying_dead);
				ClearTarget();
				result = true;
			}

			return result;
		}

		public void ClearTarget() {
			InCombat = false;
			LastTarget = CurrentTarget;
			CurrentTarget = "";
		}

		public void UpdateTarget(string targetID) {
			LastTarget = CurrentTarget ?? null;
			CurrentTarget = targetID;
		}

		public void ApplyRegen(string attribute) {
			bool applied = this.Attributes[attribute].ApplyRegen();
			//if we recovered health lets no longer be dead or unconcious and decrease the XP reward to players.
			if (applied && String.Compare(attribute, "hitpoints", true) == 0) {
				DecreaseXPReward(this.Attributes[attribute].RegenRate * this.Attributes[attribute].Max);

				if (Attributes[attribute.CamelCaseWord()].Value > -10 && Attributes[attribute.CamelCaseWord()].Value <= 0) {
					this.SetActionState(CharacterActionState.Unconcious);
				}
				else if (Attributes[attribute].Value > 0) {
					this.SetActionState(CharacterActionState.Unconcious);
					this.SetStanceState(CharacterStanceState.Prone);
				}
			}
		}

		public void ApplyEffectOnAttribute(string name, double value) {
			if (this.Attributes.ContainsKey(name.CamelCaseWord())) {
				this.Attributes[name.CamelCaseWord()].ApplyEffect(value);
			}
		}

		public double GetAttributeMax(string attribute) {
			if (this.Attributes.ContainsKey(attribute.CamelCaseWord())) {
				return this.Attributes[attribute.CamelCaseWord()].Max;
			}
			return 0;
		}

		public double GetAttributeValue(string attribute) {
			if (this.Attributes.ContainsKey(attribute.CamelCaseWord())) {
				return this.Attributes[attribute.CamelCaseWord()].Value;
			}
			return 0;
		}

		public int GetAttributeRank(string attribute) {
			if (this.Attributes.ContainsKey(attribute.CamelCaseWord())) {
				return this.Attributes[attribute.CamelCaseWord()].Rank;
			}
			return 0;
		}

		public void SetAttributeValue(string name, double value) {
			if (this.Attributes.ContainsKey(name.CamelCaseWord())) {
				this.Attributes[name.CamelCaseWord()].Value = value;
			}
			CalculateSubAttributes();
		}

		public void SetMaxAttributeValue(string name, double value) {
			if (this.Attributes.ContainsKey(name.CamelCaseWord())) {
				this.Attributes[name.CamelCaseWord()].Max = value;
			}
		}

		public void SeAttributeRegenRate(string name, double value) {
			if (this.Attributes.ContainsKey(name.CamelCaseWord())) {
				this.Attributes[name.CamelCaseWord()].RegenRate = value;
			}
		}

		public Dictionary<string, Attribute> GetAttributes() {
			return this.Attributes;
		}

		public Dictionary<string, double> GetSubAttributes() {
			CalculateSubAttributes();
			return this.SubAttributes;
		}

		public void CalculateSubAttributes() {
			if (SubAttributes.Count == 0) {
				SubAttributes.Add("Agility", (GetAttributeValue("Strength") + GetAttributeValue("Dexterity")) / 2);
				SubAttributes.Add("Cunning", (GetAttributeValue("Charisma") + GetAttributeValue("Dexterity")) / 2);
				SubAttributes.Add("Leadership", (GetAttributeValue("Intelligence") + GetAttributeValue("Charisma")) / 2);
				SubAttributes.Add("Wisdom", (GetAttributeValue("Intelligence") + GetAttributeValue("Endurance")) / 2);
				SubAttributes.Add("Toughness", (GetAttributeValue("Endurance") + GetAttributeValue("Strength")) / 2);
			}
			else {
				SubAttributes["Agility"] = (GetAttributeValue("Strength") + GetAttributeValue("Dexterity")) / 2;
				SubAttributes["Cunning"] = (GetAttributeValue("Charisma") + GetAttributeValue("Dexterity")) / 2;
				SubAttributes["Leadership"] = (GetAttributeValue("Intelligence") + GetAttributeValue("Charisma")) / 2;
				SubAttributes["Wisdom"] = (GetAttributeValue("Intelligence") + GetAttributeValue("Endurance")) / 2;
				SubAttributes["Toughness"] = (GetAttributeValue("Endurance") + GetAttributeValue("Strength")) / 2;
			}
		}

		public void AddLanguage(Languages language) {
			if (KnowsLanguage(language)) {
				KnownLanguages.Add(language);
			}
		}

		public bool KnowsLanguage(Languages language) {
			if (KnownLanguages.Contains(language))
				return true;
			return false;
		}

		public bool Loot(User.User looter, List<string> commands, bool byPassCheck = false) {
			bool looted = false;
			if (IsDead()) {
				List<Items.Iitem> result = new List<Items.Iitem>();
				StringBuilder sb = new StringBuilder();

				if (!byPassCheck) {
					//Let's see if who's looting was the killer otherwise we check the time of death
					//also check if looter is part of a group if so then the group will provide the loot logic.
					if (!string.Equals(looter.UserID, ((Iactor)this).KillerID, StringComparison.InvariantCultureIgnoreCase)) {
						if (!CanLoot(looter.UserID)) {
							//looter not the killer not in group and time to loot has not expired
							looter.MessageHandler("You did not deal the killing blow and can not loot this corpse at this time.");
							return false;
						}
					}


					//let's check if looter is in a group
					if (!string.IsNullOrEmpty(looter.GroupName)) {
						//looter is part of a group, let's see if the group loot rule is free for all first
						Groups.Group group = Groups.Groups.GetInstance().GetGroup(looter.GroupName);
						if (group.GroupRuleForLooting != Groups.GroupLootRule.First_to_loot) {
							group.Loot(looter, commands, this);
							return false;
						}
					}
				}

				if (commands.Contains("all")) {
					sb.AppendLine("You loot the following items from " + FirstName + ":");
					Inventory.GetInventoryAsItemList().ForEach(i => {
						sb.AppendLine(i.Name);
						looter.Player.Inventory.AddItemToInventory(Inventory.RemoveInventoryItem(i, this.Equipment));
					});

					looted = true;
				}
				else if (commands.Count > 2) { //the big one, should allow to loot individual item from the inventory
					string itemName = Items.Items.ParseItemName(commands);
					int index = 1;
					int position = 1;
					string[] positionString = commands[0].Split('.'); //we are separating based on using the decimal operator after the name of the npc/item
					if (positionString.Count() > 1) {
						int.TryParse(positionString[positionString.Count() - 1], out position);
					}

					Inventory.GetInventoryAsItemList().ForEach(i => {
						if (string.Equals(i.Name, itemName, StringComparison.InvariantCultureIgnoreCase) && index == position) {
							looter.Player.Inventory.AddItemToInventory(Inventory.RemoveInventoryItem(i, this.Equipment));

							sb.AppendLine("You loot " + i.Name + " from " + FirstName);
							Rooms.Room.GetRoom(looter.Player.Location).InformPlayersInRoom(string.Format("{0} loots {1} from {3}'s lifeless body.", looter.Player.FirstName, i.Name, FirstName), new List<string>() { ID });
							index = -1; //we found it and don't need this to match anymore
							looted = true;
						}
						else {
							index++;
						}
					});
				}
				else {
					sb.AppendLine(FirstName + " was carrying: ");
					Inventory.GetInventoryAsItemList().ForEach(i => sb.AppendLine(i.Name));
				}

				looter.MessageHandler(sb.ToString());
			}
			return looted;
		}

		public bool CanLoot(string looterID) {
			bool youCanLootMe = true;
			if (DateTime.UtcNow < ((Iactor)this).TimeOfDeath.AddSeconds(30)) {
				youCanLootMe = false;
			}

			return youCanLootMe;
		}

		public void RewardXP(string id, long amount) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Add a bonus for the passed in type.  Adding to an already existing type increases the amount/time.
		/// Passing in a negative number reduces it by that amount. To just increase time pass zero for amount.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="name"></param>
		/// <param name="amount"></param>
		/// <param name="time"></param>
		public void AddBonus(BonusTypes type, string name, double amount, int time = 0) {
			Bonuses.Add(type, amount, time);
		}

		/// <summary>
		/// Remove a bonus for the type passed in.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="name"></param>
		/// <param name="bonus"></param>
		public void RemoveBonus(BonusTypes type, string name, double bonus) {
			Bonuses.Remove(type);
		}

		/// <summary>
		/// Removes any bonuses whose time has expired.
		/// </summary>
		public void CleanupBonuses() {
			Bonuses.Cleanup();
		}

		public double GetBonus(BonusTypes type) {
			return Bonuses.GetBonus(type);
		}
	}
}
