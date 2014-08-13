using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
namespace Items {
    public sealed partial class Items : Iitem, Iweapon, Iedible, Icontainer, Iiluminate, Iclothing, Ikey {
       
        public bool isLit {
            get;
            set;
        }

        public bool isLightable {
            get;
            set;
        }

        public double chargeLowWarning {
            get;
            set;
        }

        //how fast the charge depletes unit/sec
        //if maxcharge is 1500 and decay rate is 1 the charge will last 1500 seconds or 25 minutes (should it be real time or in game time?)
        public double chargeDecayRate {
            get;
            set;
        }

        public FuelSource fuelSource {
            get;
            set;
        }

        public LightType lightType {
            get;
            set;
        }

        public double maxCharge {
            get;
            set;
        }

        public double currentCharge {
            get;
            set;
        }

        public bool isChargeable {
            get;
            set;
        }

        public List<string> Ignite() {
            List<string> msg = new List<string>();
            if (!isLit) {
                //TODO: get these messages from the DB based on fuel source or type
                msg.Add("You turn on " + Name + " and can now see in the dark.");
                msg.Add("{attacker} turns on " + Name + ".");
                isLit = true;
                this.Save();
                OnIgnited(new ItemEventArgs(ItemEvent.IGNITE, this.Id));
            }
            else {
                msg.Add("It is already on!");
            }

            return msg;
        }

        public List<string> Extinguish() {
            List<string> msg = new List<string>();
            if (isLit) {
                //TODO: get these messages from the DB based on fuel source or type
                msg.Add("You turn off " + Name + " and can no longer see in the dark.");
                msg.Add("{0} turns off " + Name + ".");
                isLit = false;
                Save();
                OnExtinguished(new ItemEventArgs(ItemEvent.EXTINGUISH, this.Id));
            }
            else {
                msg.Add("It is already off!");
            }

            return msg;
        }

        public void Drain() {
            currentCharge -= chargeDecayRate;
            User.User temp = MySockets.Server.GetAUser(this.Owner);
            if ((Math.Round(currentCharge/maxCharge,2) * 100) == chargeLowWarning){
                //TODO: these message should be grabbed from the DB and should reflect the type of light it is
                if (temp != null) {
                    temp.MessageHandler("The light from your " + this.Name.ToLower() + " flickers.");
                }
                chargeLowWarning = chargeLowWarning / 2;
               
            }
            if (Math.Round(currentCharge,2) <= 0) {
                currentCharge = 0.0;
                chargeLowWarning = 10;
                isLit = false;
                //TODO: these message should be grabbed from the DB and should reflect the type of light it is
                if (temp != null) {
                    temp.MessageHandler("The light from your " + this.Name.ToLower() + " goes out.");
                }
            }
            OnDrained(new ItemEventArgs(ItemEvent.DRAIN, this.Id));
            temp.Player.GetInventoryAsItemList(); //this will force an update on the inventory items 
            temp.Player.GetEquipment(); //this will force an update on the equipment
            this.Save();
        }

        public void ReCharge(double chargeAmount) {
           //TODO: check to see if player has a fuel source in inventory and consume it.  Set currentCharge = MaxCharge
           //Are all fuel sources the same?  Do they all providea 100% charge or can some of them be partly used and give partial charges?
           //With cloth you know you'll be able to replace the burn out one, but wiht a battery how do you know the charge?
           
            currentCharge = chargeAmount;
            if (currentCharge > maxCharge) {
                currentCharge = maxCharge;
            }

            OnRecharged(new ItemEventArgs(ItemEvent.RECHARGE, this.Id));
        }

        public string ExamineCharge() {
            return (isLit == true ? " It has " + Math.Round(currentCharge / maxCharge, 2) * 100 + "% charge left." : "");
        }
    }
}
