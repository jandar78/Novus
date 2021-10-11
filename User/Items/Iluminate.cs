using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Interfaces;
using Rooms;

namespace Items {
    public sealed partial class Items : IItem, IWeapon, IEdible, IContainer, IIluminate, IClothing, IKey {
       
        public bool IsLit {
            get;
            set;
        }

        public bool IsLightable {
            get;
            set;
        }

        public double ChargeLowWarning {
            get;
            set;
        }

        //how fast the charge depletes unit/sec
        //if maxcharge is 1500 and decay rate is 1 the charge will last 1500 seconds or 25 minutes (should it be real time or in game time?)
        public double ChargeDecayRate {
            get;
            set;
        }

        public FuelSource FuelSource {
            get;
            set;
        }

        public LightType LightType {
            get;
            set;
        }

        public double MaxCharge {
            get;
            set;
        }

        public double CurrentCharge {
            get;
            set;
        }

        public bool IsChargeable {
            get;
            set;
        }

        public IMessage Ignite() {
            IMessage msg = new Message();
            if (!IsLit) {
                //TODO: get these messages from the DB based on fuel source or type
                msg.Self = "You turn on " + Name + " and can now see in the dark.";
                msg.Room = "{attacker} turns on " + Name + ".";
                IsLit = true;
                this.Save();
                OnIgnited(new ItemEventArgs(ItemEvent.IGNITE, this.Id));
            }
            else {
                msg.Self = "It is already on!";
            }

            return msg;
        }

        public IMessage Extinguish() {
			IMessage msg = new Message();
            if (IsLit) {
                //TODO: get these messages from the DB based on fuel source or type
                msg.Self = "You turn off " + Name + " and can no longer see in the dark.";
                msg.Room = "{0} turns off " + Name + ".";
                IsLit = false;
                Save();
                OnExtinguished(new ItemEventArgs(ItemEvent.EXTINGUISH, this.Id));
            }
            else {
                msg.Self = "It is already off!";
            }

            return msg;
        }

		
        public void Drain() {
			IMessage msg = new Message();
			msg.InstigatorID = Id.ToString();
			msg.InstigatorType = ObjectType.Item;

            CurrentCharge -= ChargeDecayRate;
            IUser temp = Sockets.Server.GetAUser(this.Owner);

                if ((Math.Round(CurrentCharge / MaxCharge, 2) * 100) == ChargeLowWarning)
                {
                    //TODO: these message should be grabbed from the DB and should reflect the type of light it is
                    if (temp != null)
                    {
                        msg.Self = "The light from your " + this.Name.ToLower() + " flickers.";
                        msg.Room = "The light from " + temp.Player.FirstName + "'s " + this.Name.ToLower() + " flickers.";
                    }

                    ChargeLowWarning = ChargeLowWarning / 2;
                }

                if (Math.Round(CurrentCharge, 2) <= 0)
                {
                    CurrentCharge = 0.0;
                    ChargeLowWarning = 10;
                    IsLit = false;
                    //TODO: these message should be grabbed from the DB and should reflect the type of light it is
                    if (temp != null)
                    {
                        msg.Self = "The light from your " + this.Name.ToLower() + " goes out.";
                        msg.Room = "The light from " + temp.Player.FirstName + "'s " + this.Name.ToLower() + " goes out.";
                    }
                }

            if (temp != null)
            {
                if (temp.Player.IsNPC)
                {
                    temp.MessageHandler(msg);
                }
                else
                {
                    temp.MessageHandler(msg.Self);
                }

                Room.GetRoom(temp.Player.Location).InformPlayersInRoom(msg, new List<ObjectId>() { temp.UserID });

                OnDrained(new ItemEventArgs(ItemEvent.DRAIN, this.Id));
                temp.Player.Inventory.GetInventoryAsItemList(temp.Player); //this will force an update on the inventory items 
                temp.Player.Equipment.GetEquipment(temp.Player); //this will force an update on the equipment
            }

            this.Save();
        }

        public void ReCharge(double chargeAmount) {
           //TODO: check to see if player has a fuel source in inventory and consume it.  Set currentCharge = MaxCharge
           //Are all fuel sources the same?  Do they all providea 100% charge or can some of them be partly used and give partial charges?
           //With cloth you know you'll be able to replace the burn out one, but wiht a battery how do you know the charge?
           
            CurrentCharge = chargeAmount;
            if (CurrentCharge > MaxCharge) {
                CurrentCharge = MaxCharge;
            }

            OnRecharged(new ItemEventArgs(ItemEvent.RECHARGE, this.Id));
        }

        public string ExamineCharge() {
            return (IsLit == true ? " It has " + Math.Round(CurrentCharge / MaxCharge, 2) * 100 + "% charge left." : "");
        }
    }
}
