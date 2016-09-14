using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoUtils;
using Extensions;
using Interfaces;
using Rooms;

namespace Calendar {
	public static class Calendar {

		public static Dictionary<string, string> GetDate() {
			Dictionary<string, string> dateInfo = new Dictionary<string, string>();
			
			BsonDocument calendar = GetCalendarData();

			BsonArray dayArray = calendar["Days"].AsBsonArray;
			BsonArray monthArray = calendar["Months"].AsBsonArray;
			BsonArray yearArray = calendar["Year"].AsBsonArray;
			
			BsonDocument dayInWeek = dayArray[calendar["CurrentDayInWeek"].AsInt32].AsBsonDocument;
		     BsonDocument month = monthArray[calendar["CurrentMonth"].AsInt32].AsBsonDocument;
			BsonDocument year = yearArray[calendar["CurrentYearName"].AsInt32].AsBsonDocument;

			dateInfo.Add("DayInWeek", dayInWeek["Name"].AsString);
			dateInfo.Add("DayInMonth",  calendar["CurrentDayInMonth"].AsInt32.ToString());
			dateInfo.Add("Month", month["Name"].AsString);
			dateInfo.Add("Year", calendar["CurrentYear"].AsInt32.ToString());
			dateInfo.Add("YearOf", year["Name"].AsString);

			return dateInfo;
		}

		public static BsonDocument GetTime() {
              return MongoUtils.MongoData.RetrieveObject<BsonDocument>(MongoUtils.MongoData.GetCollection<BsonDocument>("World", "Globals") , t => t["_id"] == "Time");
		}

		public static bool IsNight() {
			return MongoUtils.MongoData.RetrieveObject<BsonDocument>(MongoUtils.MongoData.GetCollection<BsonDocument>("World", "Globals"), t => t["_id"] == "Time")["TimeOfDay"].AsString.ToUpper() == "NIGHT";
		}

        public static void UpdateClock() {
            var calendar = MongoUtils.MongoData.GetCollection<BsonDocument>("World", "Globals");
            BsonDocument time = MongoUtils.MongoData.RetrieveObject<BsonDocument>(calendar, t => t["_id"] == "Time");
            time["Second"] = time["Second"].AsInt32 + 28;

            if (time["Second"].AsInt32 >= 60) {
                time["Second"] = time["Second"].AsInt32 - 60;
                if (time["Second"].AsInt32 < 0) time["Second"] = time["Second"].AsInt32 * -1;
                time["Minute"] = time["Minute"].AsInt32 + 1;

                if (time["Minute"].AsInt32 >= 60) {
                    time["Minute"] = 0;
                    time["Hour"] = time["Hour"].AsInt32 + 1;
						 
						  time = UpdateTimeOfDay(time);

                    if (time["Hour"].AsInt32 == 24) {
                        time["Hour"] = 0;
                        AdvanceDate();
                    }
                }
            }

            UpdateCalendar(time);
           
        }

		public static void AdvanceDate() {
			BsonDocument calendar = GetCalendarData();
            if (calendar["CurrentMonth"].AsInt32 > calendar["Months"].AsBsonArray.Count - 1) {
                calendar["CurrentMonth"] = 0;
            }
			BsonArray monthArray = calendar["Months"].AsBsonArray;
			BsonDocument months = monthArray[calendar["CurrentMonth"].AsInt32].AsBsonDocument;

			int day = calendar["CurrentDayInMonth"].AsInt32;
			int month = calendar["CurrentMonth"].AsInt32;
			int year = calendar["CurrentYear"].AsInt32;
			int weekDay = calendar["CurrentDayInWeek"].AsInt32;

			//advance a day
			day++;

			if (day > months["Days"].AsInt32) {//we exceeded the days in the month, move on to the next month
				if (month++ > monthArray.Values.Count()-1) {
					year++;
					month = 0;
					day = 1;
				}
				else {
					month++;
					day = 1;
				}
			}
				
			//weekdays advance regardless of the month or year change
			weekDay++;

			if (weekDay > calendar["Days"].AsBsonArray.Values.Count()-1) {
				weekDay = 0;
			}

			calendar["CurrentDayInMonth"] = day; 
			calendar["CurrentMonth"] = month;
			calendar["CurrentYear"] = year;
			calendar["CurrentDayInWeek"] = weekDay;

			UpdateCalendar(calendar);
		}

		public static BsonDocument UpdateTimeOfDay(BsonDocument time) {
			int dayTicks = time["Hour"].AsInt32;
			string oldTime = time["TimeOfDay"].AsString;

			if (Enum.IsDefined(typeof(DayNight), dayTicks) && oldTime != ((DayNight)dayTicks).ToString()) {
				time["TimeOfDay"] = ((DayNight)dayTicks).ToString();

                    IRoom room = null;
                    foreach (Sockets.User u in Sockets.Server.GetCurrentUserList()) {
                        room = Room.GetRoom(u.Player.Location);
                        if (room.IsOutdoors == true && u.CurrentState == UserState.TALKING) {
                            u.MessageHandler(time[((DayNight)dayTicks).ToString()].AsString != "" ? time[((DayNight)dayTicks).ToString()].AsString : "");
                        }
                    }
			}
			return time;
		}

		private async static void UpdateCalendar(BsonDocument calendar) {
             await MongoUtils.MongoData.SaveAsync<BsonDocument>(MongoUtils.MongoData.GetCollection<BsonDocument>("World", "Globals"), c => c["_id"] == calendar["_id"], calendar);
		}

		private static BsonDocument GetCalendarData() {
            var calendarCollection = MongoUtils.MongoData.GetCollection<BsonDocument>("World", "Globals");
		    return MongoUtils.MongoData.RetrieveObject<BsonDocument>(calendarCollection, c => c["_id"] == "Calendar");
		}


		//this method needs to be broken down a bit more
		//i'm going to comment the hell out of this because it's confusing even to me
		public static void ApplyWeather(List<string> zone){
              BsonDocument weather = MongoUtils.MongoData.RetrieveObject<BsonDocument>(MongoUtils.MongoData.GetCollection<BsonDocument>("World", "Globals"), w => w["_id"] == "Weather");
			   
			//if the time for the wheather to change has elapsed let's change it
			if (HasTimeElapsed(DateTime.Parse(weather["StartTime"].AsString), weather["Duration"].AsInt32)){
                   string type = "";		
			    //we want to restart the timer so we don't have another pattern getting applied while the other one 
			    //is still going through the auto script process
			    weather["StartTime"] = DateTime.Now.ToString();
			    weather["Duration"] = Extensions.RandomNumber.GetRandomNumber().NextNumber(0,5);
                MongoUtils.MongoData.Save<BsonDocument>(MongoUtils.MongoData.GetCollection<BsonDocument>("World", "Globals"), c => c["_id"] == "Weather", weather);
			    		
			    //choose a new type of weather pattern
                   Weather weatherEnum = Weather.Clear;
                   BsonDocument weatherType = GetNewWeatherPattern(weather, out weatherEnum, out type);
                   if (weatherType == null) {
                       return;
                   }
                   
                   BsonArray intensityArray = weatherType["Intensities"].AsBsonArray;
                   
			    //lets see how strong the weather pattern will be
                   WeatherStrength strength = (WeatherStrength)Extensions.RandomNumber.GetRandomNumber().NextNumber(0, intensityArray.Values.Count(b => b.AsBsonDocument.ElementCount > 0));
			    int intensity = (int)strength;
                   
			    BsonDocument pattern = null;
			    //not all patterns may have an intensity sequence like CLEAR for example so let's check
			    if (intensityArray.Values.Count(b => b.AsBsonDocument.ElementCount > 0) > 0) {
			    	pattern = intensityArray[intensity][strength.ToString().CamelCaseWord()].AsBsonDocument;
			    }
                   
                   ApplyWeatherPattern(weather, weatherEnum, intensityArray, intensity, pattern, zone);
                   
			    //let's update the DB
			    weather["CurrentMessage"] = (pattern != null ? pattern["Message"].AsString : "");
			    weather["CurrentType"] = weatherEnum.ToString().ToUpper();
			    weather["CurrentIntensity"] = intensity;
                MongoUtils.MongoData.Save<BsonDocument>(MongoUtils.MongoData.GetCollection<BsonDocument>("World", "Globals"), c => c["_id"] == "Weather", weather);
            }
		}

          private static void ApplyWeatherPattern(BsonDocument weather, Weather weatherEnum, BsonArray intensityArray, int intensity, BsonDocument pattern, List<string> zones) {
              //this tuple will contain the increase/decrease sequence
              Tuple<string, int> tupe = null;

              //if the previous pattern is not CLEAR and the previous pattern is the same as the previous
              //then we are going to see if the intensity maybe changed
              if (weather["CurrentType"].AsString != "Clear" && weather["CurrentType"].AsString == weatherEnum.ToString()) {
                  BsonDocument oldPattern = intensityArray[weather["CurrentIntensity"].AsInt32][((WeatherStrength)weather["CurrentIntensity"].AsInt32).ToString().CamelCaseWord()].AsBsonDocument;
                  //intensity increased
                  if (intensity > weather["CurrentIntensity"].AsInt32) {
                      tupe = Tuple.Create<string, int>(oldPattern["Increase"].AsString, 30);
                  }
                  //intensity decreased
                  else if (intensity < weather["CurrentIntensity"].AsInt32) {
                      tupe = Tuple.Create<string, int>(oldPattern["Decrease"].AsString, 30);
                  }
              }
              //the previous pattern is now clearing because the new pattern is clear
              else if (weatherEnum.ToString() == "Clear") {
                  //let's get the end sequence
                  BsonDocument oldPattern = intensityArray[weather["CurrentIntensity"].AsInt32][((WeatherStrength)weather["CurrentIntensity"].AsInt32).ToString().CamelCaseWord()].AsBsonDocument;
                  BsonArray endSequence = oldPattern["EndSequence"].AsBsonArray;
                  Func<BsonArray, Tuple<string, int>, string, string, bool> endScript = GetScript(zones);
                  //execute the script
                  while (endScript(endSequence, tupe, weatherEnum.ToString().ToUpper(), pattern["Message"].AsString)) { tupe = null; }
              }
              //it's other than CLEAR and it's not the same intensity
              if (weatherEnum != Weather.Clear && weather["CurrentIntensity"].AsInt32 != intensity) {
                  BsonArray startSequence = pattern["StartSequence"].AsBsonArray;
                  Func<BsonArray, Tuple<string, int>, string, string, bool> script = GetScript(zones);
                  while (script(startSequence, tupe, weatherEnum.ToString().ToUpper(), pattern["Message"].AsString)) { tupe = null; }
              }
          }

          private static BsonDocument GetNewWeatherPattern(BsonDocument weather, out Weather weatherEnum, out string type) {
              int rand = Extensions.RandomNumber.GetRandomNumber().NextNumber(0, weather["Types"].AsBsonArray.Count);
              //we need enums for the previous and now new pattern
              weatherEnum = Weather.Clear;
              Weather previousWeather = Weather.Clear;
              //define the new pattern
              weatherEnum = (Weather)rand;

              //Did this to prevent a Null Ref Exception
              type = weatherEnum.ToString().ToUpper();

              //get the wheather pattern we want to run through the auto script, we have to peel back a few layers first
              BsonArray drizArray = weather["Types"].AsBsonArray;

              //if the weather is clear 
              if (weatherEnum == Weather.Clear) {
                  //and the previous weather was clear we are done
                  if (weather["CurrentType"].AsString.ToUpper() == "CLEAR") {
                      return null;
                  }
                  //if it's not CLEAR grab the previous weather type so we can run the end sequence script
                  previousWeather = (Weather)Enum.Parse(typeof(Weather), weather["CurrentType"].AsString.ToUpper());
                  type = previousWeather.ToString(); //since it's clear we want all the previous information so we can run the end sequence properly
              }

              //keep peeling back layers now that we have the weather type
              BsonDocument weatherType = new BsonDocument();
              string typeNotAsOut = type; //lambdas don't like ref or out values, the things you learn...
              return drizArray.Where(d => d["Name"].AsString.ToUpper() == typeNotAsOut).SingleOrDefault().AsBsonDocument;
              
          }

          private static bool HasTimeElapsed(DateTime dateTime, int time) {
              TimeSpan ts = DateTime.Now - dateTime;
		    if (ts.Minutes >= time) {
                  return true;
              }
              return false;
          }

        //Neat way of making a pseudo auto script for automatic wheather messages
        private static Func<BsonArray, Tuple<string, int>, string, string, bool> GetScript(List<string> zones) {
            int step = 0;

            Func<BsonArray, Tuple<string, int>, string, string, bool> result = delegate (BsonArray sequence, Tuple<string, int> tuple, string type, string weatherMsg) {
            IRoom room = null;
            //the sequence has ended nothing left to do here
            if (step >= sequence.Count) {
                    foreach (string zone in zones) { //loop through each zone
                        for (int low = 0; low <= 999; low++) { //apply to it to rooms in zone Todo: get the number of rooms in the zone and use that as the low, high numbers
                            room = Room.GetRoom(zone + low);
                            ((Room)room).Weather = type;
                            ((Room)room).WeatherMessage = weatherMsg;
                        }
                    }
					return false;
				}

				IMessage message = new Message();
                foreach (string zone in zones) {
                    for (int low = 0; low <= 999; low++) {//Todo: get low/high numbers from room count in zone from database
                        room = Room.GetRoom(zone + low);
						message.InstigatorID = room.Id;
						message.InstigatorType = ObjectType.Room;

						if (room.IsOutdoors) {
                            //ok if the tuple is not null then we are going to process it first. As of now it is just one single message with a wait time
                            //this can be modified to include more steps easily by making item1 a list and using steps to keep track
                            if (tuple != null) {
								message.Room = tuple.Item1;
                                room.InformPlayersInRoom(message, new List<ObjectId>() { });

								System.Threading.Thread.Sleep(tuple.Item2 * 1000);
                                return true; //we still want the other sequence to execute we will set the tuple to null in the body of the while loop
                            }
							//run the main sequence script
							message.Room = sequence[step]["Step"].AsString;
                            room.InformPlayersInRoom(message, new List<ObjectId>() { });
                        }
                    }
                }

				System.Threading.Thread.Sleep(sequence[step]["Wait"].AsInt32 * 1000);
				step++;
				return true;
			};

			return result;
		}

	}


	//not sure if keeping these enums here quite yet, may be a pain to maintain later when more wheather types are added.
	//might just have it all on the DB
	internal enum Weather { 
			Clear, 
			Rain,
			Snow, 
			Fog 
		}

	internal enum WeatherStrength {
		Low,
		Medium,
		Strong,
		Insane
	}

		//this gives us 13 hours of daylight and 11 of night time.
	   //the numbers are based on a 24 hour clock
	internal enum DayNight {
			Dawn = 6,
			Morning = 8,
			Afternoon = 12,
			Evening = 17,
			Dusk = 19,
			Night = 21
		}
}
