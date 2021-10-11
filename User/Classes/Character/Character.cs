using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Driver;
using MongoDB.Bson;
using Extensions;
using Interfaces;
using MongoDB.Bson.Serialization.Attributes;
namespace Character
{

    public class Character : IActor
    {
        #region Public Members
        public Inventory Inventory { get; private set; }
        public Equipment Equipment { get; private set; }

        #endregion Public Members

        #region Protected Members
        protected List<string> Quests { get; set; }
        protected List<string> CompletedQuests { get; set; }
        public List<Attribute> Attributes;
        protected Dictionary<string, double> SubAttributes;
        protected HashSet<Languages> KnownLanguages; //this will hold all the languages the player can understand
        protected double _levelModifier;
        public StatBonuses Bonuses;

        #region Stances
        protected CharacterStanceState _stanceState;
        protected CharacterActionState _actionState;
        #endregion Stances

        #region Misc
        protected int _level;
        protected CharacterClass _class;
        protected Languages PrimaryLanguage;
        protected Tuple<int, DateTime> KOCount; //this will only ever be zero on initialize until first knockout
        protected int Points;
        #endregion Misc

        #endregion Protected Members

        #region  Properties
        public ObjectId UserID { get; set; }

        public string Password
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

        #endregion Properties

        #region Constructors

        public Character()
        {
            Class = CharacterClass.Explorer;
            Race = CharacterRace.Human;
            Gender = Genders.Female;
            SkinColor = SkinColors.Fair;
            SkinType = SkinType.Flesh;
            HairColor = HairColors.Black;
            EyeColor = EyeColors.Brown;
            Build = BodyBuild.Medium;

            KOCount = new Tuple<int, DateTime>(0, DateTime.Now);
            _actionState = CharacterActionState.None;
            _stanceState = CharacterStanceState.Standing;

            PrimaryLanguage = Languages.Common;
            KnownLanguages = new HashSet<Languages>();
            KnownLanguages.Add(PrimaryLanguage);

            FirstName = "";
            LastName = "";
            Description = "";
            Age = 17;   //Do we want an age? And are we going to advance it every in game year?  We'll need a birthdate for this.
            Weight = 180; //pounds or kilos?
            Height = 70;  //inches or centimeters?
            Location = "A0";
            InCombat = false;
            LastCombatTime = DateTime.MinValue.ToUniversalTime();
            IsNPC = false;
            Leveled = false;
            MainHand = "WIELD_RIGHT";
            NextLevelExperience = 300;
            Level = 1;
            Experience = 0;
            PointsToSpend = 0;

            Inventory = new Inventory();
            Equipment = new Equipment();
            Bonuses = new StatBonuses();

            Attributes = new List<Attribute>() {
                new Attribute(150, "Hitpoints", 150, 0.1, 1),
                new Attribute(10, "Dexterity", 5, 0, 1),
                new Attribute(10, "Strength", 5, 0, 1),
                new Attribute(10, "Intelligence", 5, 0, 1),
                new Attribute(10, "Endurance", 5, 0, 1),
                new Attribute(10, "Charisma", 5, 0, 1)
            };

            SubAttributes = new Dictionary<string, double>();

            CalculateSubAttributes();
        }

        /// <summary>
        /// Deep copy constructor
        /// </summary>
        /// <param name="copy"></param>
        public Character(Character copy)
        {
            Class = copy.Class;
            Race = copy.Race;
            Gender = copy.Gender;
            SkinColor = copy.SkinColor;
            SkinType = copy.SkinType;
            HairColor = copy.HairColor;
            EyeColor = copy.EyeColor;
            Build = copy.Build;
            KOCount = copy.KOCount;
            ActionState = copy.ActionState;
            StanceState = copy.StanceState;

            PrimaryLanguage = copy.PrimaryLanguage;
            KnownLanguages = new HashSet<Languages>();
            foreach (Languages lang in copy.KnownLanguages)
            {
                KnownLanguages.Add(lang);
            }

            FirstName = copy.FirstName;
            LastName = copy.LastName;
            Description = copy.Description;
            Age = copy.Age;
            Weight = copy.Weight;
            Height = copy.Height;
            Location = copy.Location;
            InCombat = copy.InCombat;
            LastCombatTime = copy.LastCombatTime;
            IsNPC = copy.IsNPC;
            Leveled = copy.Leveled;
            NextLevelExperience = copy.NextLevelExperience;
            Level = copy.Level;
            Experience = copy.Experience;
            PointsToSpend = copy.PointsToSpend;
            MainHand = copy.MainHand;

            Attributes = copy.Attributes;
            SubAttributes = new Dictionary<string, double>();

            CalculateSubAttributes();

            Inventory = copy.Inventory;
            Equipment = copy.Equipment;
        }

        public Character(CharacterRace race, CharacterClass characterClass, Genders gender, Languages language, SkinColors skinColor, SkinType skinType, HairColors hairColor, EyeColors eyeColor, BodyBuild build) : this()
        {
            Class = characterClass;
            Race = race;
            Gender = gender;
            SkinColor = skinColor;
            SkinType = skinType;
            HairColor = hairColor;
            EyeColor = eyeColor;
            Build = build;
            PrimaryLanguage = language;
        }
        #endregion Constructors

        public async void Save()
        {
            //if (Id == null || Id == ObjectId.Empty)
            //{
            //    Id = new ObjectId();
            //}; //new character

            //var character = new BsonDocument()
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
            //    {"IsNPC", IsNPC},
            //    {"CurrentTarget", CurrentTarget == null ? ObjectId.Empty : CurrentTarget},
            //    {"LastTarget", LastTarget == null ? ObjectId.Empty : LastTarget},
            //    {"InCombat", InCombat},
            //    {"LastCombatTime", LastCombatTime},
            //    {"Experience", Experience},
            //    {"NextLevelExperience", NextLevelExperience },
            //    {"PointsToSpend", PointsToSpend },
            //    {"Level", Level},
            //    {"KillerID", KillerID},
            //    {"Title", Title }
            //};

            //BsonArray playerAttributes = new BsonArray();

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

            //BsonArray questIds = new BsonArray();
            //foreach (var questId in Quests)
            //{
            //    questIds.Add(questId);
            //}

            //BsonArray completedQuestIds = new BsonArray();
            //foreach (var questId in CompletedQuests)
            //{
            //    completedQuestIds.Add(questId);
            //}

            //var bonuses = new BsonArray();
            //foreach (var bonus in Bonuses.Bonus)
            //{
            //    var statBonus = new BsonDocument()
            //    {
            //        { "BonusType", bonus.Key },
            //        { "Name", bonus.Value.Name },
            //        { "Amount", bonus.Value.Amount },
            //        { "Time", bonus.Value.Time }
            //    };

            //    bonuses.Add(statBonus);
            //}

            //BsonArray inventory = new BsonArray();
            //foreach (var item in Inventory.GetInventoryAsItemList())
            //{
            //    inventory.Add(item.Id);
            //}

            //BsonArray equipment = new BsonArray();
            //foreach (var item in Equipment.GetEquipment())
            //{
            //    equipment.Add(item.Value.Id);
            //}

            //character["Equipment"] = equipment;
            //character["Inventory"] = inventory;
            //character["Attributes"] = playerAttributes;
            //character["Quests"] = questIds;
            //character["CompletedQuests"] = completedQuestIds;
            //character["Bonuses"] = bonuses;
            var characterCollection = MongoUtils.MongoData.GetCollection<Character>("Characters", "PlayerCharacter");
            await MongoUtils.MongoData.SaveAsync(characterCollection, c => c.Id == Id, this);  //upsert
        }

        public void Hydrate(ObjectId id)
        {
            Load(id);
            //var playerCollection = MongoUtils.MongoData.GetCollection<Character>("Characters", "PlayerCharacter");
            //var result = MongoUtils.MongoData.RetrieveObject<Character>(playerCollection, p => p.Id == id);
        }

        public void Hydrate(Character character)
        {
            //Load(Id);
            //character.Id.ToString();
        }


        public void Hydrate(BsonDocument document)
        {
            //FirstName = document["FirstName"].AsString.CamelCaseWord();
            //LastName = document["LastName"].AsString.CamelCaseWord();
            //_class = (CharacterClass)Enum.Parse(typeof(CharacterClass), document["Class"].AsString);
            //Race = (CharacterRace)Enum.Parse(typeof(CharacterRace), document["Race"].AsString);
            //Gender = (Genders)Enum.Parse(typeof(Genders), document["Gender"].AsString);
            //SkinType = (SkinType)Enum.Parse(typeof(SkinType), document["SkinType"].AsString);
            //SkinColor = (SkinColors)Enum.Parse(typeof(SkinColors), document["SkinColor"].AsString);
            //HairColor = (HairColors)Enum.Parse(typeof(HairColors), document["HairColor"].AsString);
            //EyeColor = (EyeColors)Enum.Parse(typeof(EyeColors), document["EyeColor"].AsString);
            //StanceState = (CharacterStanceState)Enum.Parse(typeof(CharacterStanceState), document["StanceState"].AsString);
            //ActionState = (CharacterActionState)Enum.Parse(typeof(CharacterActionState), document["ActionState"].AsString);
            //Description = document["Description"].AsString;
            //Location = document["Location"].AsString;
            //Height = document["Height"].AsDouble;
            //Weight = document["Weight"].AsDouble;
            //IsNPC = document["IsNPC"].AsBoolean;
            //InCombat = document["InCombat"].AsBoolean;
            //LastCombatTime = document["LastCombatTime"].ToUniversalTime();
            //CurrentTarget = document["CurrentTarget"].AsObjectId;
            //LastTarget = document["LastTarget"].AsObjectId;
            //Password = document["Password"].AsString;
            //NextLevelExperience = document["NextLevelExperience"].AsInt64;
            //Level = document["Level"].AsInt32;
            //Leveled = document["Leveled"].AsBoolean;
            //PointsToSpend = document["PointsToSpend"].AsInt32;
            //Experience = document["Experience"].AsInt64;
            //Level = document["Level"].AsInt32;
            //Title = document["Title"].AsString;
            //KillerID = document["KillerID"].AsObjectId;

            //Attributes.Clear();

            //foreach (BsonDocument attrib in document["Attributes"].AsBsonArray)
            //{
            //    var tempAttrib = new Attribute()
            //    {
            //        Name = attrib["Name"].AsString,
            //        Value = attrib["Value"].AsDouble,
            //        Max = attrib["Max"].AsDouble,
            //        RegenRate = attrib["RegenRate"].AsDouble,
            //        Rank = attrib["Rank"].AsInt32
            //    };

            //    Attributes.Add(tempAttrib);
            //}

            //CalculateSubAttributes();

            //foreach (BsonValue questId in document["Quests"]["InProgress"].AsBsonArray)
            //{
            //    Quests.Add(questId.AsString);
            //}

            //foreach (BsonValue questId in document["Quests"]["Completed"].AsBsonArray)
            //{
            //    CompletedQuests.Add(questId.AsString);
            //}

            //if (document.Contains("Bonuses"))
            //{
            //    foreach (var bonus in document["Bonuses"].AsBsonArray)
            //    {
            //        Bonuses.Add((BonusTypes)Enum.Parse(typeof(BonusTypes), bonus["Name"].AsString), bonus["Amount"].AsDouble, bonus["Time"]?.AsInt32 ?? 0);
            //    }
            //}

            //foreach (BsonDocument item in document["Inventory"].AsBsonArray)
            //{
            //    IItem fullItem = Items.Items.GetByID(item["_id"].AsObjectId).Result;
            //    if (!Inventory.inventory.Contains(fullItem))
            //    {
            //        Inventory.AddItemToInventory(fullItem);
            //    }
            //}

            //foreach (BsonDocument item in document["Equipment"].AsBsonArray)
            //{
            //    IItem fullItem = Items.Items.GetByID(item["_id"].AsObjectId).Result;
            //    if (!Equipment.Equipped.ContainsKey(fullItem.WornOn))
            //    {
            //        Equipment.EquipItem(fullItem, this.Inventory);
            //    }
            //}

            //if (document.Contains("Bonuses"))
            //{
            //    foreach (var bonus in document["Bonuses"].AsBsonArray)
            //    {
            //        Bonuses.Add((BonusTypes)Enum.Parse(typeof(BonusTypes), bonus["Name"].AsString), bonus["Amount"].AsDouble, bonus["Time"]?.AsInt32 ?? 0);
            //    }
            //}
        }

        public async void Load(ObjectId id)
        {
            //var characterCollection = MongoUtils.MongoData.GetCollection<Character>("Characters", "PlayerCharacter");
            //var found = await MongoUtils.MongoData.RetrieveObjectAsync<Character>(characterCollection, c => c.Id == this.Id);

            //Id = found.Id;
            //FirstName = found.FirstName.CamelCaseWord();
            //LastName = found.LastName.CamelCaseWord();
            //Class = found.Class;
            //Race = found.Race;
            //Gender = found.Gender;
            //SkinType = found.SkinType;
            //SkinColor = found.SkinColor;
            //HairColor = found.HairColor;
            //EyeColor = found.EyeColor;
            //StanceState = found.StanceState;
            //ActionState = found.ActionState;
            //Description = found.Description;
            //Location = found.Location;
            //Height = found.Height;
            //Weight = found.Weight;
            //IsNPC = found.IsNPC;
            //InCombat = found.InCombat;
            //LastCombatTime = found.LastCombatTime.ToUniversalTime();
            //CurrentTarget = found.CurrentTarget;
            //LastTarget = found.LastTarget;
            //Experience = found.Experience;
            //Level = found.Level;
            //Title = found.Title;
            //KillerID = found.KillerID;

            ////if you just use var instead of casting it like this you will be in a world of pain and suffering when dealing with subdocuments.
            //Inventory = found.Inventory;
            //Equipment = found.Equipment;
            //Attributes = found.Attributes;
            //Quests = found.Quests;
        }

        public string Examine()
        {
            StringBuilder sb = new StringBuilder();
            int inches;
            int.TryParse(Math.Round(Height / 12, 2).ToString().Substring(Math.Round(Height / 12, 2).ToString().Length - 2), out inches);

            if ((Math.Round((double)inches / 100, 1) * 10) < 10) inches = (int)(Math.Round((double)inches / 100, 1) * 10);

            sb.Append(FirstName + " " + LastName + " is a " + Gender.ToString().ToLower() + " " + Race.ToString().ToLower() + " " + Class.ToString().ToLower() + ".  ");
            sb.Append(GenderPossesive + " has " + HairColor.ToString().ToLower() + " colored hair, with " + EyeColor.ToString().ToLower() + " eyes."
                    + GenderPossesive + " skin is " + SkinColor.ToString().ToLower() + " " + SkinType.ToString().ToLower());
            sb.Append(" with a " + Build.ToString().ToLower() + " build, weighing " + Weight + " pounds and measuring " + Math.Round(Height / 12, 0) + " feet " + inches + " inches.");

            return sb.ToString();
        }

        public async void RewardXP(ObjectId id, long xpGained)
        {
            var npcs = MongoUtils.MongoData.GetCollection<NPC>("Characters", "NPCCharacters");
            var npc = await MongoUtils.MongoData.RetrieveObjectAsync<NPC>(npcs, n => n.Id == id); // Query.EQ("_id", ObjectId.Parse(id)));
            IUser temp = Sockets.Server.GetAUser(Id);
            if (string.IsNullOrEmpty(temp.GroupName))
            {
                temp.MessageHandler(string.Format("You gain {0:0.##} XP from {1}", xpGained, npc.FirstName.CamelCaseWord()));
                Experience += xpGained;
            }
            else
            {
                //we want to know how much XP the NPC would give out and cut it by half if player is in a group
                Groups.Groups.GetInstance().RewardXP((long)(npc.XP * 0.5), temp.GroupName);
            }
            if (IsLevelUp && !Leveled)
            { //we don't want the player to just farm a ton of points and continue to level up, we want them to type the command before we show this message again
                temp.MessageHandler("Congratulations! You've leveled up!"); //let's let them know they can level up it's up to them when they actually do level up
                Character tempChar = temp.Player as Character;
                Leveled = true;
                tempChar.NextLevelExperience += (long)(tempChar.NextLevelExperience * 1.25);
                IncreasePoints();
                //increase all the attributes to max, small perk of leveling up.  Maybe a global setting?
                foreach (var attrib in temp.Player.GetAttributes())
                {
                    attrib.Value = attrib.Max;
                }
            }
        }

        #region General
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
        #endregion General

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
                else return "It";
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

        //Title will be titles the player can earn by completing quests/events and choose to display as the primary title
        public string Title
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
                return Points;
            }
            set
            {
                Points += value;
            }
        }

        public int Level
        {
            get;
            set;
        }

        public double LevelModifier
        {
            get
            {
                return _levelModifier;
            }
            set
            {
                MongoUtils.MongoData.ConnectToDatabase();
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

        public void SetActionState(CharacterActionState state)
        {
            _actionState = state;
        }

        public void SetActionStateDouble(double state)
        {
            _actionState = (CharacterActionState)(int)state;
        }

        public void SetStanceState(CharacterStanceState state)
        {
            _stanceState = state;
        }

        public void SetStanceStateDouble(double state)
        {
            _stanceState = (CharacterStanceState)(int)state;
        }

        public void IncreasePoints()
        {
            if (Level % 10 == 0)
            {
                Points += 4;
            }
            else if (Level % 5 == 0)
            {
                Points += 3;
            }
            else if (Level % 1 == 0)
            {
                Points += 1;
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
            LastTarget = CurrentTarget.Equals(ObjectId.Empty) ? ObjectId.Empty : CurrentTarget;
            CurrentTarget = targetID;
        }

        public void ApplyRegen(string attribute)
        {
            bool applied = this.Attributes.Where(a => a.Name == attribute.CamelCaseWord()).Single().ApplyRegen();
            //if we recovered health let's no longer be dead or unconcious
            if (applied && String.Compare(attribute, "hitpoints", true) == 0)
            {
                if (Attributes.Where(a => a.Name == attribute.CamelCaseWord()).Single().Value > -10 && Attributes.Where(a => a.Name == attribute.CamelCaseWord()).Single().Value <= 0)
                {
                    this.SetActionState(CharacterActionState.Unconcious);
                }
                else if (Attributes.Where(a => a.Name == attribute.CamelCaseWord()).Single().Value > 0)
                {
                    this.SetActionState(CharacterActionState.None);
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

        public List<Attribute> GetAttributes()
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
            if (SubAttributes.Count == 0)
            {
                SubAttributes.Add("Agility", Math.Round((GetAttributeValue("Strength") + GetAttributeValue("Dexterity")) / 2, 0, MidpointRounding.AwayFromZero));
                SubAttributes.Add("Cunning", Math.Round((GetAttributeValue("Charisma") + GetAttributeValue("Dexterity")) / 2, 0, MidpointRounding.AwayFromZero));
                SubAttributes.Add("Leadership", Math.Round((GetAttributeValue("Intelligence") + GetAttributeValue("Charisma")) / 2, 0, MidpointRounding.AwayFromZero));
                SubAttributes.Add("Wisdom", Math.Round((GetAttributeValue("Intelligence") + GetAttributeValue("Endurance")) / 2, 0, MidpointRounding.AwayFromZero));
                SubAttributes.Add("Toughness", Math.Round((GetAttributeValue("Endurance") + GetAttributeValue("Strength")) / 2, 0, MidpointRounding.AwayFromZero));
            }
            else
            {
                SubAttributes["Agility"] = Math.Round((GetAttributeValue("Strength") + GetAttributeValue("Dexterity")) / 2, 0, MidpointRounding.AwayFromZero);
                SubAttributes["Cunning"] = Math.Round((GetAttributeValue("Charisma") + GetAttributeValue("Dexterity")) / 2, 0, MidpointRounding.AwayFromZero);
                SubAttributes["Leadership"] = Math.Round((GetAttributeValue("Intelligence") + GetAttributeValue("Charisma")) / 2, 0, MidpointRounding.AwayFromZero);
                SubAttributes["Wisdom"] = Math.Round((GetAttributeValue("Intelligence") + GetAttributeValue("Endurance")) / 2, 0, MidpointRounding.AwayFromZero);
                SubAttributes["Toughness"] = Math.Round((GetAttributeValue("Endurance") + GetAttributeValue("Strength")) / 2, 0, MidpointRounding.AwayFromZero);
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
            if (KnownLanguages.Contains(language)) return true;
            return false;
        }

        public Wearable GetMainHandWeapon()
        {
            if (MainHand != null)
            {
                return (Wearable)Enum.Parse(typeof(Wearable), MainHand);
            }
            return Wearable.NONE;
        }

        public bool Loot(IUser looter, List<string> commands, bool bypassCheck = false)
        {
            bool looted = false;
            if (IsDead())
            {
                List<IItem> result = new List<IItem>();
                StringBuilder sb = new StringBuilder();

                if (!bypassCheck)
                {
                    if (CanLoot(looter.UserID))
                    {
                        looter.MessageHandler("You did not deal the killing blow and can not loot this corpse at this time.");
                        return false;
                    }
                }

                if (commands.Contains("all"))
                {
                    sb.AppendLine("You loot the following items from " + FirstName + ":");
                    Inventory.GetInventoryAsItemList(this).ForEach(i =>
                    {
                        sb.AppendLine(i.Name);
                        looter.Player.Inventory.AddItemToInventory(i, looter.Player);
                    });
                    looted = true;
                }
                else if (commands.Count > 2)
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
                            looter.Player.Inventory.AddItemToInventory(i, looter.Player);
                            sb.AppendLine("You loot " + i.Name + " from " + FirstName);
                            index = -1; //we found it and don't need this to match anymore
                            looted = true;
                            //no need to break since we are checking on index and I doubt a player will have so many items in their inventory that it will
                            //take a long time to go through each of them
                        }
                        else
                        {
                            index++;
                        }
                    });
                }
                else
                {
                    sb.AppendLine(FirstName + " is carrying: ");
                    Inventory.GetInventoryAsItemList(this).ForEach(i => sb.AppendLine(i.Name));
                }
            }
            return looted;
        }

        public bool CanLoot(ObjectId looterID)
        {
            bool youCanLootMe = true;
            if (looterID.Equals(((IActor)this).KillerID))
            {
                if (DateTime.UtcNow < ((IActor)this).TimeOfDeath.AddSeconds(30))
                {
                    youCanLootMe = false;
                }
            }

            return youCanLootMe;
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
            Bonuses.Remove(type);
        }

        /// <summary>
        /// Removes any bonuses whose time has expired.
        /// </summary>
        public void CleanupBonuses()
        {
            Bonuses.Cleanup();
        }

        public double GetBonus(BonusTypes type)
        {
            return Bonuses.GetBonus(type);
        }
    }

    #region Extensions
    public static class PlayerExtensionMethods
    {
        //Todo: Get these values from database
        const double HighHealth = 0.75;
        const double LowHealth = 0.25;

        public static string GetAttributeColorized(this IActor character, string name)
        {

            string result = "";
            name = name.CamelCaseWord();

            double value = character.GetAttributeValue(name);
            double max = character.GetAttributeMax(name);

            if (value >= max * HighHealth) result = value.ToString().FontColor(Utils.FontForeColor.GREEN);
            else if (value < max * HighHealth && value >= max * LowHealth) result = value.ToString().FontColor(Utils.FontForeColor.YELLOW);
            else if (value < max * LowHealth && value > 0) result = value.ToString().FontColor(Utils.FontForeColor.RED);
            else result = value.ToString().FontColor(Utils.FontForeColor.RED).FontStyle(Utils.FontStyles.BOLD);

            return result;
        }
    }
    #endregion Extensions

    public class Feats
    {
        public Feats()
        {
        }
    }
}
