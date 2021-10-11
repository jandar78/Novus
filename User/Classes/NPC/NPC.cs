using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using Extensions;
using Triggers;
using Character;

namespace Interfaces
{
    public class NPC : IActor, INPC
    {

        #region private things
        private List<IQuest> _quests;
        #endregion private things

        #region Public Members
        public Dictionary<ObjectId, double> XpTracker;
        public Inventory Inventory { get; private set; }
        public Equipment Equipment { get; private set; }
        
        public List<IQuest> Quests
        {
            get
            {
                if (_quests == null)
                {
                    _quests = new List<IQuest>();
                }
                return _quests;
            }
            set
            {
                _quests = value;
            }
        }
        public Queue<string> Messages;
        public List<ITrigger> Triggers;
        #endregion Public Members

        #region Protected Members
        public List<Character.Attribute> Attributes { get; set; }
        protected Dictionary<string, double> SubAttributes;
        protected HashSet<Languages> KnownLanguages; //this will hold all the languages the player can understand
        protected double _levelModifier;
        public StatBonuses Bonuses { get; set; }

        #region Stances
        protected CharacterStanceState _stanceState;
        protected CharacterActionState _actionState;
        #endregion Stances

        #region Misc
        protected Languages PrimaryLanguage;
        protected Tuple<int, DateTime> KOCount; //this will only ever be zero on initialize until first knockout
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
        public string Password
        {
            get;
            set;
        }

        public ObjectId UserID
        {
            get;
            set;
        }

        public long Experience
        {
            get;
            set;
        }

        public long NextLevelExperience
        {
            get;
            set;
        }

        public long XP { get; set; }

        public bool Leveled
        {
            get;
            set;
        }

        public bool IsLevelUp
        {
            get
            {
                if (Leveled || Experience >= NextLevelExperience)
                {
                    return true;
                }
                return false;
            }
        }

        public ObjectId Id
        {
            get;
            set;
        }

        public string Location
        {
            get;
            set;
        }

        public string LastLocation
        {
            get;
            set;
        }

        public bool IsNPC
        {
            get;
            set;
        }

        public int MobTypeID
        {
            get;
            set;
        }

        public IFsm Fsm
        {
            get;
            set;

        }

        public string AiState { get; set; }

        public string PreviousAiState { get; set; }

        public string AiGlobalState { get; set; }

        public string Title
        {
            get;
            set;
        }

        public string FullName
        {
            get
            {
                return FirstName + " " + LastName;
            }
        }

        public string FullHonors
        {
            get
            {
                return FullName + Title;
            }
        }

        public DateTime NextAiAction
        {
            get;
            set;
        }

        public void Update()
        {
            Fsm.Update(this);
        }

        private bool IsMob
        {
            get;
            set;
        }


        #endregion Properties

        #region Descriptive
        public Genders Gender
        {
            get; set;
        }

        public string MainHand
        {
            get;
            set;
        }


        public string GenderPossesive
        {
            get
            {
                if (Gender == Genders.Male)
                {
                    return "He";
                }
                else if (Gender == Genders.Female)
                {
                    return "She";
                }
                else
                    return "It";
            }
        }

        public BodyBuild Build
        {
            get; set;
        }

        public int Age
        {
            get;
            set;
        }

        public double Weight
        {
            get;
            set;
        }

        public double Height
        {
            get;
            set;
        }

        public string FirstName
        {
            get;
            set;
        }

        public string LastName
        {
            get;
            set;
        }

        public CharacterClass Class
        {
            get; set;
        }

        public CharacterRace Race
        {
            get; set;
        }

        public string Description
        {
            get;
            set;
        }

        public EyeColors EyeColor
        {
            get; set;
        }

        public SkinColors SkinColor
        {
            get; set;
        }

        public SkinType SkinType
        {
            get; set;
        }

        public HairColors HairColor
        {
            get; set;
        }
        #endregion Descriptive

        #region Stances
        public string Action
        {
            get
            {
                return ActionState.ToString().Replace("_", " ").ToLower();
            }
        }

        public string Stance
        {
            get
            {
                return StanceState.ToString().Replace("_", " ").ToLower();
            }
        }

        public CharacterStanceState StanceState
        {
            get
            {
                return _stanceState;
            }
            set
            {
                _stanceState = value;
            }
        }

        public CharacterActionState ActionState
        {
            get
            {
                return _actionState;
            }
            set
            {
                _actionState = value;
            }
        }
        #endregion Stances

        #region Leveling
        public int PointsToSpend
        {
            get
            {
                return _points;
            }
            set
            {
                _points += value;
            }
        }

        public int Level
        {
            get;
            set;
        }

        //not really sure what I was going to do with this since it's not being used anywhere.
        public double LevelModifier
        {
            get
            {
                return _levelModifier;
            }
            set
            {
                var collection = MongoUtils.MongoData.GetCollection<BsonDocument>("Character", "General");
                var result = MongoUtils.MongoData.RetrieveObject<BsonDocument>(collection, c => c["_id"] == "LevelModifier");
                if (result != null)
                {
                    _levelModifier = result[Level.ToString()].AsDouble;
                }
            }
        }
        #endregion Leveling

        #region Combat
        public ObjectId KillerID
        {
            get;
            set;
        }

        public DateTime TimeOfDeath
        {
            get;
            set;
        }

        public bool CheckUnconscious
        {
            get
            {
                bool result = false;
                double health = Attributes.Where(a => a.Name == "Hitpoints").Single().Value;

                if (health > DeathLimit && health <= 0)
                {
                    result = true;
                    //guy's knocked out let's increment the KO counter
                    if ((KOCount.Item1 > 0 && KOCount.Item1 < 3) && (KOCount.Item2 - DateTime.Now).Minutes < 10)
                    {
                        KOCount = new Tuple<int, DateTime>(KOCount.Item1 + 1, KOCount.Item2);
                    }
                    //ok he got knocked out 3 times in less than 10 minutes he's dead now
                    else if (KOCount.Item1 == 3 && (KOCount.Item2 - DateTime.Now).Minutes < 10)
                    {
                        Attributes.Where(a => a.Name == "Hitpoints").Single().ApplyNegative(100);
                    }
                    //well at this point we'll reset his knockout counter and reset the timer since he hasn't been knocked out in at least 10 minutes
                    else
                    {
                        KOCount = new Tuple<int, DateTime>(1, DateTime.Now); //it's not zero because he's knocked out!!
                    }
                }
                //if no longer unconcious, remove the state
                else if (health > 0)
                {
                    if (ActionState == CharacterActionState.Unconcious)
                    {
                        SetActionState(CharacterActionState.None);
                        SetStanceState(CharacterStanceState.Prone);
                    }
                }

                return result;
            }
        }

        public bool CheckDead
        {
            get
            {
                bool result = false;

                if (GetAttributeValue("Hitpoints") <= DeathLimit)
                {
                    result = true;
                }

                return result;
            }
        }

        public double DeathLimit
        {
            get
            {
                //would probably be a good idea to grab the multiplier from the database
                double value = GetAttributeMax("Hitpoints");
                return (-1 * (0.015 * value));
            }
        }

        public ObjectId CurrentTarget
        {
            get;
            set;
        }

        public ObjectId LastTarget
        {
            get;
            set;
        }

        public bool InCombat
        {
            get;
            set;
        }

        public DateTime LastCombatTime
        {
            get;
            set;
        }
        #endregion Combat

        public NPC()
        {
            Messages = new Queue<string>();

            Fsm = AI.FSM.GetInstance();
            Fsm.state = Fsm.GetStateFromName("Wander");

            Class = CharacterClass.Explorer;
            Race = CharacterRace.Human;
            Gender = Genders.Female;
            SkinColor = SkinColors.Grey;
            SkinType = SkinType.Flesh;
            HairColor = HairColors.Black;
            EyeColor = EyeColors.Grey;
            Build = BodyBuild.Medium;

            KOCount = new Tuple<int, DateTime>(0, DateTime.Now);
            ActionState = CharacterActionState.None;
            StanceState = CharacterStanceState.Standing;

            PrimaryLanguage = Languages.Common;
            KnownLanguages = new HashSet<Languages>();
            KnownLanguages.Add(PrimaryLanguage);

            XpTracker = new Dictionary<ObjectId, double>();
            Triggers = new List<ITrigger>();
            Bonuses = new StatBonuses();
            Quests = new List<IQuest>();

            FirstName = "";
            LastName = "";
            Description = "";
            Age = 17;   //Do we want an age? And are we going to advance it every in game year?  Players could be 400+ years old rather quick.
            Weight = 180.0d; //pounds or kilos?
            Height = 70.0d;  //inches or centimeters?
            Location = "A0";
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
            Bonuses = new StatBonuses();

            Attributes = new List<Character.Attribute>()
            {
                new Character.Attribute(200.0d, "Hitpoints", 200.0d, 0.2d, 1),
                new Character.Attribute(10.0d, "Dexterity", 10.0d, 0.0d, 1),
                new Character.Attribute(10.0d, "Strength", 10.0d, 0.0d, 1),
                new Character.Attribute(10.0d, "Intelligence", 10.0d, 0.0d, 1),
                new Character.Attribute(10.0d, "Endurance", 10.0d, 0.0d, 1),
                new Character.Attribute(10.0d, "Charisma", 10.0d, 0.0d, 1)
            };

            SubAttributes = new Dictionary<string, double>();
            CalculateSubAttributes();
        }

        public NPC(CharacterRace race, CharacterClass characterClass, Genders gender, Languages language, SkinColors skinColor, SkinType skinType, HairColors hairColor, EyeColors eyeColor, BodyBuild build) : this()
        {
            Class = characterClass;
            Race = race;
            Gender = gender;
            SkinColor = skinColor;
            SkinType = skinType;
            HairColor = hairColor;
            EyeColor = eyeColor;
            Build = build;
        }

        public void Hydrate(ObjectId id)
        {

            Load(id);
            //var npcCollection = MongoUtils.MongoData.GetCollection<NPC>("Characters", "NPCCharacters");
            //var result = MongoUtils.MongoData.RetrieveObjectAsync(npcCollection, n => n.Id == id).Result;

            //Id = document["_id"].AsObjectId;
            //FirstName = document["FirstName"].AsString.CamelCaseWord();
            //LastName = document["LastName"].AsString.CamelCaseWord();
            //_class = (CharacterClass)Enum.Parse(typeof(CharacterClass), document["Class"].AsString);
            //_race = (CharacterRace)Enum.Parse(typeof(CharacterRace), document["Race"].AsString);
            //_gender = (Genders)Enum.Parse(typeof(Genders), document["Gender"].AsString);
            //_skinType = (SkinType)Enum.Parse(typeof(SkinType), document["SkinType"].AsString);
            //_skinColor = (SkinColors)Enum.Parse(typeof(SkinColors), document["SkinColor"].AsString);
            //_hairColor = (HairColors)Enum.Parse(typeof(HairColors), document["HairColor"].AsString);
            //_eyeColor = (EyeColors)Enum.Parse(typeof(EyeColors), document["EyeColor"].AsString);
            //_stanceState = (CharacterStanceState)Enum.Parse(typeof(CharacterStanceState), document["StanceState"].AsString);
            //_actionState = (CharacterActionState)Enum.Parse(typeof(CharacterActionState), document["ActionState"].AsString);
            //Description = document["Description"].AsString;
            //Location = document["Location"].AsString;
            //Height = document["Height"].AsDouble;
            //Weight = document["Weight"].AsDouble;
            //IsNPC = document["IsNPC"].AsBoolean;
            //MobTypeID = document["MobTypeID"].AsInt32;
            //NextAiAction = document["NextAiAction"].ToUniversalTime();
            //InCombat = document["InCombat"].AsBoolean;
            //LastCombatTime = document["LastCombatTime"].ToUniversalTime();
            //CurrentTarget = document["CurrentTarget"].AsObjectId;
            //LastTarget = document["LastTarget"].AsObjectId;
            //Fsm.state = Fsm.GetStateFromName(document["AiState"].AsString);
            //Fsm.previousState = Fsm.GetStateFromName(document["PreviousAiState"].AsString);
            //Fsm.globalState = Fsm.GetStateFromName(document["AiGlobalState"].AsString);
            //Experience = document["Experience"].AsInt64;
            //Level = document["Level"].AsInt32;
            //Title = document["Title"].AsString;
            //KillerID = document["KillerID"].AsObjectId;

            //Attributes.Clear();

            //foreach (BsonDocument attrib in document["Attributes"].AsBsonArray)
            //{
            //    var tempAttrib = new Character.Attribute();
            //    tempAttrib.Name = attrib["Name"].ToString();
            //    tempAttrib.Value = attrib["Value"].AsDouble;
            //    tempAttrib.Max = attrib["Max"].AsDouble;
            //    tempAttrib.RegenRate = attrib["RegenRate"].AsDouble;

            //    Attributes.Add(tempAttrib);
            //}

            //CalculateSubAttributes();


            //foreach (BsonDocument track in document["XpTracker"].AsBsonArray)
            //{
            //    //we just newed this up so it should always have to be refilled
            //    XpTracker.Add(track["_id"].AsObjectId, track["Value"].AsDouble);
            //}

            //if (document.Contains("Triggers"))
            //{
            //    foreach (BsonDocument triggerdoc in document["Triggers"].AsBsonArray)
            //    {
            //        ITrigger trigger = new GeneralTrigger(triggerdoc, TriggerType.NPC);
            //        Triggers.Add(trigger);
            //    }
            //}

            //if (document.Contains("QuestIds"))
            //{
            //    foreach (BsonDocument questDoc in document["QuestIds"].AsBsonArray)
            //    {
            //        Dictionary<ObjectId, int> playerSteps = new Dictionary<ObjectId, int>();

            //        if (questDoc.Contains("PlayerIDs"))
            //        {
            //            foreach (var playerStep in questDoc["PlayerIDs"].AsBsonArray)
            //            {
            //                playerSteps.Add(playerStep["PlayerID"].AsObjectId, playerStep["Step"].AsInt32);
            //            }
            //        }

            //        var quest = new Quests.Quest(questDoc["QuestID"].AsString, playerSteps);

            //        if (questDoc.Contains("AutoPlayers"))
            //        {
            //            foreach (var autoID in questDoc["AutoPlayers"].AsBsonArray)
            //            {
            //                quest.AutoProcessPlayer.Enqueue(autoID.AsObjectId);
            //            }
            //        }

            //        Quests.Add(quest);
            //    }
            //}

            //if (document.Contains("Bonuses"))
            //{
            //    foreach (var bonus in document["Bonuses"]["StatBonuses"].AsBsonArray)
            //    {
            //        Bonuses.Add((BonusTypes)Enum.Parse(typeof(BonusTypes), bonus["Name"].AsString), bonus["Amount"].AsDouble, bonus["Time"]?.AsInt32 ?? 0);
            //    }
            //}

            //Inventory.PlayerID = Id;
            //Equipment.PlayerID = Id;
        }

        public void Hydrate(BsonDocument doc)
        {
            Id = doc["_id"].AsObjectId;
            Load(Id);
        }

        public async void Save()
        {
            //if (Id == null || Id == ObjectId.Empty)
            //{
            //    Id = new ObjectId();
            //}; //new character

            //var npcCharacter = new BsonDocument()
            //{
            //    {"FirstName", FirstName},
            //    {"LastName", LastName},
            //    {"Race", Race.ToString().CamelCaseWord()},
            //    {"Class", Class.ToString().CamelCaseWord()},
            //    {"Gender", Gender.ToString().CamelCaseWord()},
            //    {"SkinColor", SkinColor.ToString().CamelCaseWord()},
            //    {"SkinType", SkinType.ToString().CamelCaseWord()},
            //    {"HairColor", HairColor.ToString().CamelCaseWord()},
            //    {"EyeColor", EyeColor.ToString().CamelCaseWord()},
            //    {"Weight", Weight},
            //    {"Height", Height},
            //    {"Description", Description},
            //    {"Location", Location},
            //    {"ActionState", ActionState.ToString().CamelCaseWord()},
            //    {"StanceState", StanceState.ToString().CamelCaseWord()},
            //    {"AiState", Fsm.state != null ? Fsm.state.ToString() : "None"},
            //    {"PreviousAiState", Fsm.previousState == null ? "" : Fsm.previousState.ToString().CamelCaseWord()},
            //    {"AiGlobalState", Fsm.globalState == null ? "" : Fsm.globalState.ToString().CamelCaseWord()},
            //    {"NextAiAction", NextAiAction.ToUniversalTime()},
            //    {"MobTypeID", MobTypeID},
            //    {"IsNPC", IsNPC},
            //    {"CurrentTarget", CurrentTarget == null ? ObjectId.Empty : CurrentTarget},
            //    {"LastTarget", LastTarget == null ? ObjectId.Empty : LastTarget},
            //    {"InCombat", InCombat},
            //    {"LastCombatTime", LastCombatTime},
            //    {"Experience", Experience},
            //    {"Level", Level},
            //    {"KillerID", KillerID},
            //    {"Title", Title }
            //};

            //BsonArray playerAttributes = new BsonArray();
            //BsonArray xpTracker = new BsonArray();

            //foreach (IAttributes attribute in Attributes)
            //{
            //    BsonDocument attrib = new BsonDocument();
            //    attrib.Add("Name", attribute.Name);
            //    attrib.Add("Value", attribute.Value);
            //    attrib.Add("Max", attribute.Max);
            //    attrib.Add("RegenRate", attribute.RegenRate);
            //    attrib.Add("Rank", attribute.Rank);

            //    playerAttributes.Add(attrib);
            //}
                        
            //foreach (KeyValuePair<ObjectId, double> tracker in XpTracker)
            //{
            //    var track = new BsonDocument()
            //    {
            //        { "ID", ""},
            //        { "Value", 0.0d },
            //        { "ID",tracker.Key },
            //        { "Value", tracker.Value }
            //    };

            //    xpTracker.Add(track);
            //}

            //BsonArray questIds = new BsonArray();
            //foreach (var quest in Quests)
            //{
            //    BsonDocument questDoc = new BsonDocument()
            //    {
            //        { "QuestID", quest.QuestID }
            //    };

            //    BsonArray playerSteps = new BsonArray();
            //    foreach (var playerStep in quest.CurrentPlayerStep)
            //    {
            //        BsonDocument step = new BsonDocument();
            //        step.Add("PlayerID", playerStep.Key);
            //        step.Add("Step", playerStep.Value);
            //        playerSteps.Add(step);
            //    }

            //    BsonArray autoProcess = new BsonArray();
            //    foreach (ObjectId playerStep in quest.AutoProcessPlayer)
            //    {
            //        autoProcess.Add(BsonValue.Create(playerStep));
            //    }

            //    questDoc.Add("AutoPlayers", autoProcess);
            //    questDoc.Add("PlayerIDs", playerSteps);
            //    questIds.Add(questDoc);
            //}

            
            //var statBonuses = new BsonArray();
            //var bonuses = new BsonDocument("StatBonuses", statBonuses);
            //foreach (var bonus in Bonuses.Bonus)
            //{
            //    var statBonus = new BsonDocument()
            //    {
            //        { "BonusType", bonus.Key },
            //        { "Name", bonus.Value.Name },
            //        { "Amount", bonus.Value.Amount },
            //        { "Time", bonus.Value.Time }
            //    };

            //    statBonuses.Add(statBonus);
            //}

            //npcCharacter["Attributes"] = playerAttributes;
            //npcCharacter["QuestIds"] = questIds;
            //npcCharacter["XpTracker"] = xpTracker;
            //npcCharacter["Bonuses"] = bonuses;

            var characterCollection = MongoUtils.MongoData.GetCollection<NPC>("Characters", "NPCCharacters");
            await MongoUtils.MongoData.SaveAsync(characterCollection, npc => npc.Id == Id, this);  //upsert
        }


        public async void Load(ObjectId id)
        {
            var characterCollection = MongoUtils.MongoData.GetCollection<NPC>("Characters", "NPCCharacters");
            var found = await MongoUtils.MongoData.RetrieveObjectAsync<NPC>(characterCollection, n => n.Id == id);

            Id = found.Id;
            FirstName = found.FirstName.CamelCaseWord();
            LastName = found.LastName.CamelCaseWord();
            Class = found.Class;
            Race = found.Race;
            Gender = found.Gender;
            SkinType = found.SkinType;
            SkinColor = found.SkinColor;
            HairColor = found.HairColor;
            EyeColor = found.EyeColor;
            StanceState = found.StanceState;
            ActionState = found.ActionState;
            Description = found.Description;
            Location = found.Location;
            Height = found.Height;
            Weight = found.Weight;
            IsNPC = found.IsNPC;
            MobTypeID = found.MobTypeID;
            NextAiAction = found.NextAiAction.ToUniversalTime();
            InCombat = found.InCombat;
            LastCombatTime = found.LastCombatTime.ToUniversalTime();
            CurrentTarget = found.CurrentTarget;
            LastTarget = found.LastTarget;
            Fsm.state = Fsm.GetStateFromName(found.AiState);
            Fsm.previousState = Fsm.GetStateFromName(found.PreviousAiState);
            Fsm.globalState = Fsm.GetStateFromName(found.AiGlobalState);
            Experience = found.Experience;
            Level = found.Level;
            Title = found.Title;
            KillerID = found.KillerID;

            //if you just use var instead of casting it like this you will be in a world of pain and suffering when dealing with subdocuments.
            Attributes = found.Attributes;
            XpTracker = found.XpTracker;
            Triggers = found.Triggers;
            Quests = found.Quests;
            Inventory = found.Inventory;
            Equipment = found.Equipment;
            //Bonuses = found.Bonuses;

            //if (playerAttributes != null)
            //{
            //    foreach (BsonDocument attrib in playerAttributes)
            //    {

            //        if (!this.Attributes.ContainsKey(attrib["Name"].ToString()))
            //        {
            //            Attribute tempAttrib = new Attribute();
            //            tempAttrib.Name = attrib["Name"].ToString();
            //            tempAttrib.Value = attrib["Value"].AsDouble;
            //            tempAttrib.Max = attrib["Max"].AsDouble;
            //            tempAttrib.RegenRate = attrib["RegenRate"].AsDouble;


            //            this.Attributes.Add(tempAttrib.Name, tempAttrib);
            //        }
            //        else
            //        {
            //            this.Attributes[attrib["Name"].ToString()].Max = attrib["Max"].AsDouble;
            //            this.Attributes[attrib["Name"].ToString()].Value = attrib["Value"].AsDouble;
            //            this.Attributes[attrib["Name"].ToString()].RegenRate = attrib["RegenRate"].AsDouble;
            //        }
            //    }
            //}

            //if (xpTracker != null && xpTracker.Count > 0)
            //{
            //    foreach (BsonDocument track in xpTracker)
            //    {
            //        //we just newed this up so it should always have to be refilled
            //        damageTracker.Add(track["ID"].AsString, track["Value"].AsDouble);
            //    }
            //}

            //foreach (BsonDocument triggerdoc in triggers)
            //{
            //    ITrigger trigger = new GeneralTrigger(triggerdoc, TriggerType.NPC);
            //    Triggers.Add(trigger);
            //}

            //if (questIds != null)
            //{
            //    foreach (BsonDocument questDoc in questIds)
            //    {
            //        Dictionary<string, int> playerSteps = new Dictionary<string, int>();

            //        if (questDoc.Contains("PlayerIDs"))
            //        {
            //            foreach (var playerStep in questDoc["PlayerIDs"].AsBsonArray)
            //            {
            //                playerSteps.Add(playerStep["PlayerID"].AsString, playerStep["Step"].AsInt32);
            //            }
            //        }

            //        var quest = new Quests.Quest(questDoc["QuestID"].AsString, playerSteps);

            //        if (questDoc.Contains("AutoPlayers"))
            //        {
            //            foreach (var autoID in questDoc["AutoPlayers"].AsBsonArray)
            //            {
            //                quest.AutoProcessPlayer.Enqueue(autoID.AsString);
            //            }
            //        }

            //        Quests.Add(quest);
            //    }
            //}

            ////if (bonusesList != null && bonusesList.Count > 0) {
            ////	Bonuses.LoadFromBson(bonusesList);
            ////}

            //Inventory.PlayerID = Id;
            //Equipment.PlayerID = Id;
        }

        public void CalculateXP()
        {
            if (this.IsDead())
            {
                foreach (var pair in XpTracker)
                {
                    IUser player = Sockets.Server.GetAUser(pair.Key);
                    if (player != null)
                    {
                        double rewardPercentage = ((pair.Value * -1) / GetAttributeMax("Hitpoints"));
                        if (rewardPercentage > 1.0)
                            rewardPercentage = 1.0;
                        long xp = (long)(Experience * rewardPercentage);
                        //ok based on the level of the player should we provide less and less XP based on the target level
                        //to prevent farming of easy targets?  For now sure why the hell not, maybe in the future we'll just increase the level
                        //required by a shit ton each time they level up.  We'll do a four tier approach we''l discount 25% for each level above the target level
                        int levelDifference = player.Player.Level - Level;
                        if (levelDifference == 1)
                        {
                            xp = xp - (long)(xp * 0.25);
                        }
                        else if (levelDifference == 2)
                        {
                            xp = xp - (long)(xp * 0.5);
                        }
                        else if (levelDifference == 3)
                        {
                            xp = xp - (long)(xp * 0.75);
                        }
                        else if (levelDifference >= 4)
                        {
                            xp = 0;
                        }

                        player.Player.RewardXP(Id, xp);
                    }
                }
            }
        }

        public void IncreaseXPReward(ObjectId id, double damage)
        {
            //we only want to deal with base hitpoints, knocking unconcious doesn't add to the XP reward
            if (IsUnconcious())
            {
                damage += 100;
            }

            if (XpTracker.ContainsKey(id))
            {
                XpTracker[id] = XpTracker[id] + (damage * -1);
            }
            else
            {
                XpTracker.Add(id, damage);
            }
            Save();
        }

        public void DecreaseXPReward(double amount)
        {
            double individualAmount = amount / (double)XpTracker.Count;

            var damageList = new Dictionary<ObjectId, double>(XpTracker);
            //totalDecrease needs to be divided amongst all players who are in the XP List
            foreach (var pair in damageList)
            {
                XpTracker[pair.Key] = pair.Value + (individualAmount);
            }

            Save();
        }

        public void ParseMessage(IMessage message)
        {
            //send the message to the AI logic 
            if (!ObjectId.Parse(message.InstigatorID).Equals(this.Id))
            {
                //does the AI need to do something based on this
                Fsm.InterpretMessage(message, this);

                //let's see if we have a general trigger hit
                var parser = new AI.MessageParser(message, this, Triggers.ToList<ITrigger>());
                parser.FindTrigger();

                if (parser.TriggersToExecute.Count > 0)
                {
                    foreach (ITrigger trigger in parser.TriggersToExecute)
                    {
                        trigger.HandleEvent(null, new TriggerEventArgs(this.Id, TriggerEventArgs.IDType.Npc, ObjectId.Parse(message.InstigatorID), (TriggerEventArgs.IDType)Enum.Parse(typeof(TriggerEventArgs.IDType), message.InstigatorType.ToString())));
                    }
                }

                //now let's see if there's a quest that will trigger
                foreach (IQuest quest in Quests)
                {
                    quest.ProcessQuestStep(message, this);
                }
                Save();
            }
        }

        public void SetActionState(CharacterActionState state)
        {
            _actionState = state;
        }

        public void SetStanceState(CharacterStanceState state)
        {
            _stanceState = state;
        }

        public void SetActionStateDouble(double state)
        {
            _actionState = (CharacterActionState)(int)state;
        }

        public void SetStanceStateDouble(double state)
        {
            _stanceState = (CharacterStanceState)(int)state;
        }

        public void IncreasePoints()
        {
            if (Level % 10 == 0)
            {
                _points += 4;
            }
            else if (Level % 5 == 0)
            {
                _points += 3;
            }
            else if (Level % 1 == 0)
            {
                _points += 1;
            }
        }

        public bool IsUnconcious()
        {
            bool result = false;
            if (CheckUnconscious)
            {
                SetActionState(CharacterActionState.Unconcious);
                SetStanceState(CharacterStanceState.Laying_unconcious);
                ClearTarget();
                result = true;
            }
            else
            {
                if (ActionState == CharacterActionState.Unconcious)
                {
                    SetActionState(CharacterActionState.None);
                }
                if (StanceState == CharacterStanceState.Laying_unconcious)
                {
                    SetStanceState(CharacterStanceState.Prone);
                }
            }

            return result;
        }

        public bool IsDead()
        {
            bool result = false;
            if (CheckDead)
            {
                SetActionState(CharacterActionState.Dead);
                SetStanceState(CharacterStanceState.Laying_dead);
                ClearTarget();
                result = true;
            }

            return result;
        }

        public void ClearTarget()
        {
            InCombat = false;
            LastTarget = CurrentTarget;
            CurrentTarget = ObjectId.Empty;
        }

        public void UpdateTarget(ObjectId targetID)
        {
            LastTarget = CurrentTarget == ObjectId.Empty ? ObjectId.Empty : CurrentTarget;
            CurrentTarget = targetID;
        }

        public void ApplyRegen(string attribute)
        {
            bool applied = this.Attributes.Where(a => a.Name == attribute.CamelCaseWord()).Single().ApplyRegen();
            //if we recovered health lets no longer be dead or unconcious and decrease the XP reward to players.
            if (applied && String.Compare(attribute, "hitpoints", true) == 0)
            {
                DecreaseXPReward(this.Attributes.Where(a => a.Name == attribute.CamelCaseWord()).Single().RegenRate * this.Attributes.Where(a => a.Name == attribute.CamelCaseWord()).Single().Max);

                if (Attributes.Where(a => a.Name == attribute.CamelCaseWord()).Single().Value > -10 && Attributes.Where(a => a.Name == attribute.CamelCaseWord()).Single().Value <= 0)
                {
                    this.SetActionState(CharacterActionState.Unconcious);
                }
                else if (Attributes.Where(a => a.Name == attribute.CamelCaseWord()).Single().Value > 0)
                {
                    this.SetActionState(CharacterActionState.Unconcious);
                    this.SetStanceState(CharacterStanceState.Prone);
                }
            }
        }

        public void ApplyEffectOnAttribute(string name, double value)
        {
            if (this.Attributes.Any(a => a.Name == name.CamelCaseWord()))
            {
                this.Attributes.Where(a => a.Name == name.CamelCaseWord()).Single().ApplyEffect(value);
            }
        }

        public double GetAttributeMax(string name)
        {
            if (this.Attributes.Any(a => a.Name == name.CamelCaseWord()))
            {
                return this.Attributes.Where(a => a.Name == name.CamelCaseWord()).Single().Max;
            }
            return 0;
        }

        public double GetAttributeValue(string name)
        {
            if (this.Attributes.Any(a => a.Name == name.CamelCaseWord()))
            {
                return this.Attributes.Where(a => a.Name == name.CamelCaseWord()).Single().Value;
            }
            return 0;
        }

        public int GetAttributeRank(string name)
        {
            if (this.Attributes.Any(a => a.Name == name.CamelCaseWord()))
            {
                return this.Attributes.Where(a => a.Name == name.CamelCaseWord()).Single().Rank;
            }
            return 0;
        }

        public void SetAttributeValue(string name, double value)
        {
            if (this.Attributes.Any(a => a.Name == name.CamelCaseWord()))
            {
                this.Attributes.Where(a => a.Name == name.CamelCaseWord()).Single().Value = value;
            }
            CalculateSubAttributes();
        }

        public void SetMaxAttributeValue(string name, double value)
        {
            if (this.Attributes.Any(a => a.Name == name.CamelCaseWord()))
            {
                this.Attributes.Where(a => a.Name == name.CamelCaseWord()).Single().Max = value;
            }
        }

        public void SeAttributeRegenRate(string name, double value)
        {
            if (this.Attributes.Any(a => a.Name == name.CamelCaseWord()))
            {
                this.Attributes.Where(a => a.Name == name.CamelCaseWord()).Single().RegenRate = value;
            }
        }

        public List<Character.Attribute> GetAttributes()
        {
            return this.Attributes;
        }

        public Dictionary<string, double> GetSubAttributes()
        {
            CalculateSubAttributes();
            return this.SubAttributes;
        }

        public void CalculateSubAttributes()
        {
            if (SubAttributes == null && SubAttributes.Count == 0)
            {
                SubAttributes.Add("Agility", (GetAttributeValue("Strength") + GetAttributeValue("Dexterity")) / 2);
                SubAttributes.Add("Cunning", (GetAttributeValue("Charisma") + GetAttributeValue("Dexterity")) / 2);
                SubAttributes.Add("Leadership", (GetAttributeValue("Intelligence") + GetAttributeValue("Charisma")) / 2);
                SubAttributes.Add("Wisdom", (GetAttributeValue("Intelligence") + GetAttributeValue("Endurance")) / 2);
                SubAttributes.Add("Toughness", (GetAttributeValue("Endurance") + GetAttributeValue("Strength")) / 2);
            }
            else
            {
                SubAttributes["Agility"] = (GetAttributeValue("Strength") + GetAttributeValue("Dexterity")) / 2;
                SubAttributes["Cunning"] = (GetAttributeValue("Charisma") + GetAttributeValue("Dexterity")) / 2;
                SubAttributes["Leadership"] = (GetAttributeValue("Intelligence") + GetAttributeValue("Charisma")) / 2;
                SubAttributes["Wisdom"] = (GetAttributeValue("Intelligence") + GetAttributeValue("Endurance")) / 2;
                SubAttributes["Toughness"] = (GetAttributeValue("Endurance") + GetAttributeValue("Strength")) / 2;
            }
        }

        public void AddLanguage(Languages language)
        {
            if (KnowsLanguage(language))
            {
                KnownLanguages.Add(language);
            }
        }

        public bool KnowsLanguage(Languages language)
        {
            if (KnownLanguages.Contains(language))
                return true;
            return false;
        }

        public bool Loot(IUser looter, List<string> commands, bool byPassCheck = false)
        {
            var message = new Message();
            message.InstigatorID = looter.UserID.ToString();
            message.InstigatorType = looter.Player.IsNPC == false ? ObjectType.Player : ObjectType.Npc;
            message.TargetID = this.Id.ToString();
            message.TargetType = this.IsNPC == false ? ObjectType.Player : ObjectType.Npc;

            bool looted = false;
            if (IsDead())
            {
                List<IItem> result = new List<IItem>();
                StringBuilder sb = new StringBuilder();
                bool hasLoot = false;
                if (!byPassCheck)
                {
                    //Let's see if who's looting was the killer otherwise we check the time of death
                    //also check if looter is part of a group if so then the group will provide the loot logic.
                    if (looter.UserID.Equals(((IActor)this).KillerID))
                    {
                        if (!CanLoot(looter.UserID))
                        {
                            //looter not the killer not in group and time to loot has not expired
                            looter.MessageHandler("You did not deal the killing blow and can not loot this corpse at this time.");
                            return false;
                        }
                    }


                    //let's check if looter is in a group
                    if (!string.IsNullOrEmpty(looter.GroupName))
                    {
                        //looter is part of a group, let's see if the group loot rule is free for all first
                        Groups.Group group = Groups.Groups.GetInstance().GetGroup(looter.GroupName);
                        if (group.GroupRuleForLooting != Groups.GroupLootRule.First_to_loot)
                        {
                            group.Loot(looter, commands, this);
                            return false;
                        }
                    }
                }

                if (commands.Contains("all"))
                {
                    sb.AppendLine("You loot the following items from " + FirstName + ":");
                    Inventory.GetInventoryAsItemList(this).ForEach(i =>
                    {
                        sb.AppendLine(i.Name);
                        looter.Player.Inventory.AddItemToInventory(Inventory.RemoveInventoryItem(i, this), looter.Player);
                    });

                    looted = true;
                }
                else if (commands.Count > 3)
                { //the big one, should allow to loot individual item from the inventory
                    string itemName = Items.Items.ParseItemName(commands);
                    int index = 1;
                    int position = 1;
                    string[] positionString = commands[0].Split('.'); //we are separating based on using the decimal operator after the name of the npc/item
                    if (positionString.Count() > 1)
                    {
                        int.TryParse(positionString[positionString.Count() - 1], out position);
                    }

                    Inventory.GetInventoryAsItemList(this).ForEach(i =>
                    {
                        if (string.Equals(i.Name, itemName, StringComparison.InvariantCultureIgnoreCase) && index == position)
                        {
                            looter.Player.Inventory.AddItemToInventory(Inventory.RemoveInventoryItem(i, this), looter.Player);

                            sb.AppendLine("You loot " + i.Name + " from " + FirstName);
                            message.Room = string.Format("{0} loots {1} from {3}'s lifeless body.", looter.Player.FirstName, i.Name, FirstName);
                            index = -1; //we found it and don't need this to match anymore
                            looted = true;
                        }
                        else
                        {
                            index++;
                        }
                    });
                }
                else
                {
                    sb.AppendLine(FirstName + " was carrying: ");

                    foreach (var item in Inventory.GetInventoryAsItemList(this))
                    {
                        sb.AppendLine(item.Name);
                        hasLoot = true;
                    }
                }
                message.Self = hasLoot ? sb.ToString() : sb.ToString() + "Nothing\r\n";
            }

            if (looter.Player.IsNPC)
            {
                looter.MessageHandler(message);
            }
            else
            {
                looter.MessageHandler(message.Self);
            }

            Rooms.Room.GetRoom(looter.Player.Location).InformPlayersInRoom(message, new List<ObjectId>() { Id });

            return looted;
        }

        public bool CanLoot(ObjectId looterId)
        {
            bool youCanLootMe = true;
            if (looterId.Equals(((IActor)this).KillerID))
            {
                if (DateTime.UtcNow < ((IActor)this).TimeOfDeath.AddSeconds(30))
                {
                    youCanLootMe = false;
                }
            }

            return youCanLootMe;
        }

        public void RewardXP(ObjectId id, long amount)
        {
            //NPC's get no XP reward
        }

        /// <summary>
        /// Add a bonus for the passed in type.  Adding to an already existing type increases the amount/time.
        /// Passing in a negative number reduces it by that amount. To just increase time pass zero for amount.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="amount"></param>
        /// <param name="time"></param>
        public void AddBonus(BonusTypes type, string name, double amount, int time = 0)
        {
            Bonuses.Add(type, amount, time);
        }

        /// <summary>
        /// Remove a bonus for the type passed in.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="bonus"></param>
        public void RemoveBonus(BonusTypes type, string name, double bonus)
        {
            if (Bonuses != null)
            {
                Bonuses.Remove(type);
            }
        }

        /// <summary>
        /// Removes any bonuses whose time has expired.
        /// </summary>
        public void CleanupBonuses()
        {
            if (Bonuses != null)
            {
                Bonuses.Cleanup();
            }
        }

        public double GetBonus(BonusTypes type)
        {
            return Bonuses.GetBonus(type);
        }
    }
}
