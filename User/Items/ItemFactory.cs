﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson;
using Extensions;
using MongoDB.Bson.Serialization;
using Triggers;
using Interfaces;

namespace Items {
   public class ItemFactory {

        public async static Task<IItem> CreateItem(ObjectId id){
            IItem tempItem = null;

            if (id != null) { //id got passed in so we are looking for a specific edible item
                var itemCollection = MongoUtils.MongoData.GetCollection<Items>("World","Items");
                tempItem = await MongoUtils.MongoData.RetrieveObjectAsync<Items>(itemCollection, i => i.Id == id);
            }

            return tempItem;
        }

        /// <summary>
        /// Loops through the triggers array in the BsonDocument and adds them to the Item
        /// </summary>
        /// <param name="result"></param>
        /// <param name="tempItem"></param>
        /// <returns></returns>
        private static IItem AddTriggersToItem(IItem result, IItem tempItem) {
            //This method could probably just return an ITriggers List instead
            result.ItemTriggers = new List<ITrigger>();
            //  result.SpeechTriggers = new List<ITrigger>();

            if (tempItem.ItemTriggers.Count > 0) {
                //loop through the triggers, an item can have multiple triggers for different things
                foreach (BsonDocument doc in ((Items)tempItem).Trigger) {
                    ItemTrigger trigger = new ItemTrigger(doc);
                    SubscribeToCorrectEvent(result, trigger);
                    //for most scripts we are going to want the playerID to then get anything else we may want within it like rooms, items, etc
                    trigger.script.AddVariable(result.Owner, "ownerID");

                    result.ItemTriggers.Add(trigger);
                }
            }

            return result;
        }

        /// <summary>
        /// Subscribes the trigger to an item event based on the ItemTrigger.TriggerOn property
        /// </summary>
        /// <param name="result"></param>
        /// <param name="trigger"></param>
        private static void SubscribeToCorrectEvent(IItem result, ItemTrigger trigger) {
			if (trigger.TriggerOn.Count > 0) {
				foreach (var on in trigger.TriggerOn) {
					switch (on.ToUpper()) {
						case "OPEN":
							result.ContainerOpened += new EventHandler<ItemEventArgs>(trigger.HandleEvent);
							break;
						case "CLOSE":
							result.ContainerClosed += new EventHandler<ItemEventArgs>(trigger.HandleEvent);
							break;
						case "DETERIORATE":
							result.Deteriorated += new EventHandler<ItemEventArgs>(trigger.HandleEvent);
							break;
						case "IMPROVE":
							result.Improved += new EventHandler<ItemEventArgs>(trigger.HandleEvent);
							break;
						case "WORN":
							result.ItemWorn += new EventHandler<ItemEventArgs>(trigger.HandleEvent);
							break;
						case "IGNITE":
							result.Ignited += new EventHandler<ItemEventArgs>(trigger.HandleEvent);
							break;
						case "EXTINGUISH":
							result.Extinguished += new EventHandler<ItemEventArgs>(trigger.HandleEvent);
							break;
						case "DRAIN":
							result.Drained += new EventHandler<ItemEventArgs>(trigger.HandleEvent);
							break;
						case "RECHARGE":
							result.Recharged += new EventHandler<ItemEventArgs>(trigger.HandleEvent);
							break;
						case "EXAMINE":
							result.Examined += new EventHandler<ItemEventArgs>(trigger.HandleEvent);
							break;
						case "LOOKIN":
							result.LookedIn += new EventHandler<ItemEventArgs>(trigger.HandleEvent);
							break;
						case "STORE":
							result.Stored += new EventHandler<ItemEventArgs>(trigger.HandleEvent);
							break;
						case "RETRIEVE":
							result.Retrieved += new EventHandler<ItemEventArgs>(trigger.HandleEvent);
							break;
						case "CONSUME":
							result.Consumed += new EventHandler<ItemEventArgs>(trigger.HandleEvent);
							break;
						case "WIELD":
							result.Wielded += new EventHandler<ItemEventArgs>(trigger.HandleEvent);
							break;
						default:
							break;
					}
				}
			}
        }

    }
}
