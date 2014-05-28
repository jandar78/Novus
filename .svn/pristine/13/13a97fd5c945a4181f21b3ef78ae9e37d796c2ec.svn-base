using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;

namespace WorldBuilder {
    public partial class Form1 : Form {
        #region Item Stuff
        private void GetItemsFromDB() {
            this.itemsInDBValue.Items.Clear();
            this._itemList.Clear();

            if (ConnectedToDB) {
                MongoCursor<BsonDocument> result = null;
                if (string.IsNullOrEmpty(filterValue.Text)) {
                    result = MongoUtils.MongoData.GetCollection("World", "Items").FindAllAs<BsonDocument>();
                }
                else {
                    if (filterTypeValue.Text == "_id") {
                        result = MongoUtils.MongoData.GetCollection("World", "Items").FindAs<BsonDocument>(Query.EQ(filterTypeValue.Text, ObjectId.Parse(filterValue.Text)));
                    }
                    else {
                        result = MongoUtils.MongoData.GetCollection("World", "Items").FindAs<BsonDocument>(Query.EQ(filterTypeValue.Text, filterValue.Text));
                    }
                }

                if (result != null) {
                    _itemList = result.ToList<BsonDocument>();
                }

                foreach (BsonDocument doc in result) {
                    this.itemsInDBValue.Items.Add(doc["Name"].AsString);
                }
            }
        }

        private void wieldEffectValue_SelectedIndexChanged(object sender, EventArgs e) {
            AffectedForm affectedForm = null;
            if (wieldEffectValue.SelectedItem == "New...") {
                affectedForm = new AffectedForm();
            }
            else if (wieldEffectValue.SelectedItem != null) {
                affectedForm = new AffectedForm(_wieldAffects[wieldEffectValue.SelectedIndex - 1].AsBsonDocument);
            }
            else {
                return;
            }

            affectedForm.ShowDialog();

            if (affectedForm.DialogResult == DialogResult.OK) {
                if (affectedForm.attribute != null) {
                    _wieldAffects.Add(affectedForm.attribute);
                    wieldEffectValue.Items.Add(affectedForm.attribute["k"].AsString + " (" + affectedForm.attribute["v"].AsDouble.ToString() + ")");
                }
            }
            else if (affectedForm.DialogResult == DialogResult.Abort) {
                _wieldAffects.RemoveAt(wieldEffectValue.SelectedIndex - 1);
                wieldEffectValue.Items.RemoveAt(wieldEffectValue.SelectedIndex);
            }
            else if (affectedForm.DialogResult == DialogResult.Cancel) {
                //user cancelled do nothing
            }

            affectedForm.Close();
        }

        private void loadItem_Click(object sender, EventArgs e) {
            MongoCollection itemCollection = MongoUtils.MongoData.GetCollection("World", "Items");
            BsonDocument item = null;
            if (itemsInDBValue.SelectedIndex != -1) {
                item = _itemList[this.itemsInDBValue.SelectedIndex];
            }
            else if (!string.IsNullOrEmpty(idValue.Text)) {
                item = itemCollection.FindOneAs<BsonDocument>(Query.EQ("_id", ObjectId.Parse(idValue.Text)));
            }

            if (item != null) {
                FillControls(item);
            }
        }

        private void FillControls(BsonDocument item) {
            idValue.Text = item["_id"].AsObjectId.ToString();
            nameValue.Text = item["Name"].AsString;
            descriptionValue.Text = item["Description"].AsString;
            //general stuff
            if (item.Contains("Owner") && !item["Owner"].IsBsonNull) {
                ownerValue.Text = item["Owner"].AsString;
            }
            if (item.Contains("MinimumLevel") && !item["MinimumLevel"].IsBsonNull) {
                minLevelValue.Text = item["MinimumLevel"].AsInt32.ToString();
            }
            if (item.Contains("CurrentCondition") && !item["CurrentCondition"].IsBsonNull) {
                conditionValue.Text = ((Items.ItemCondition)item["CurrentCondition"].AsInt32).ToString();
            }
            if (item.Contains("MaxCondition") && !item["MaxCondition"].IsBsonNull) {
                maxConditionValue.Text = ((Items.ItemCondition)item["MaxCondition"].AsInt32).ToString();
            }
            if (item.Contains("Weight") && !item["Weight"].IsBsonNull) {
                weightValue.Text = item["Weight"].AsDouble.ToString();
            }
            if (item.Contains("IsMovable") && !item["IsMovable"].IsBsonNull) {
                isMovable.Checked = item["IsMovable"].AsBoolean;
            }
            if (item.Contains("IsWearable") && !item["IsWearable"].IsBsonNull) {
                isWearable.Checked = item["IsWearable"].AsBoolean;
            }

            //container stuff
            if (item.Contains("ReduceCarryWeightBy") && !item["ReduceCarryWeightBy"].IsBsonNull) {
                reduceWeightValue.Text = item["ReduceCarryWeightBy"].AsDouble.ToString();
            }
            if (item.Contains("WeightLimit") && !item["WeightLimit"].IsBsonNull) {
                weightLimitValue.Text = item["WeightLimit"].AsDouble.ToString();
            }
            if (item.Contains("IsOpenable") && !item["IsOpenable"].IsBsonNull) {
                isOpenable.Checked = item["IsOpenable"].AsBoolean;
            }
            if (item.Contains("Opened") && !item["Opened"].IsBsonNull) {
                isOpened.Checked = item["Opened"].AsBoolean;
            }
            if (item.Contains("Contents") && !item["Contents"].IsBsonNull) {
                foreach (BsonValue value in item["Contents"].AsBsonArray) {
                    itemContentsValue.Items.Add(value.AsString);
                }
            }
            //weapon stuff
            if (item.Contains("AttackSpeed") && !item["AttackSpeed"].IsBsonNull) {
                attackSpeedValue.Text = item["AttackSpeed"].AsDouble.ToString();
            }
            if (item.Contains("MaxDamage") && !item["MaxDamage"].IsBsonNull) {
                maxDamageValue.Text = item["MaxDamage"].AsDouble.ToString();
            }
            if (item.Contains("MinDamage") && !item["MinDamage"].IsBsonNull) {
                minDamageValue.Text = item["MinDamage"].AsDouble.ToString();
            }


        }

        private void button1_Click(object sender, EventArgs e) {
            GetItemsFromDB();
        }

        private void button2_Click(object sender, EventArgs e) {
            if (ConnectedToDB) {
                BsonDocument item = new BsonDocument();
                if (!string.IsNullOrEmpty(idValue.Text)) {
                    item["_id"] = ObjectId.Parse(idValue.Text);
                }

                //general stuff
                if (!IsEmpty(nameValue.Text)) {
                    item["Name"] = nameValue.Text;
                }
                if (!IsEmpty(descriptionValue.Text)) {
                    item["Description"] = descriptionValue.Text;
                }
                if (!IsEmpty(ownerValue.Text)) {
                    item["Owner"] = ownerValue.Text;
                }
                if (!IsEmpty(minLevelValue.Text)) {
                    item["MinimumLevel"] = int.Parse(minLevelValue.Text);
                }
                if (!IsEmpty(conditionValue.Text)) {
                    item["CurrentCondition"] = (Items.ItemCondition)Enum.Parse(typeof(Items.ItemCondition), conditionValue.Text);
                }
                if (!IsEmpty(maxConditionValue.Text)) {
                    item["MaxCondition"] = (Items.ItemCondition)Enum.Parse(typeof(Items.ItemCondition), maxConditionValue.Text);
                }
                if (!IsEmpty(weightValue.Text)) {
                    item["Weight"] = double.Parse(weightValue.Text);
                }
                
                
                //attributes
                item["IsMovable"] = isMovable.Checked;
                item["IsWearable"] = isWearable.Checked;
                item["IsOpenable"] = isOpenable.Checked;
                item["Opened"] = isOpened.Checked;
                item["IsWieldable"] = isWieldable.Checked;
                item["isLit"] = isLit.Checked;
                item["isChargeable"] = isChargeable.Checked;
                item["isLightable"] = isLightable.Checked;

                //key
                item["SkeletonKey"] = isSkeletonKey.Checked;
                if (!IsEmpty(doorIdValue.Text)) {
                    item["DoorID"] = doorIdValue.Text;
                }

                //container stuff
                if (!IsEmpty(reduceWeightValue.Text)) {
                    item["ReduceCarryWeightBy"] = double.Parse(reduceWeightValue.Text);
                }
                if (!IsEmpty(weightLimitValue.Text)) {
                    item["WeightLimit"] = double.Parse(weightLimitValue.Text);
                }

                foreach (string value in itemContentsValue.Items) {
                    item["Contents"].AsBsonArray.Add(value);
                }

                //weapon stuff
                if (!IsEmpty(attackSpeedValue.Text)) {
                    item["AttackSpeed"] = double.Parse(attackSpeedValue.Text);
                }
                if (!IsEmpty(maxDamageValue.Text)) {
                    item["MaxDamage"] = double.Parse(maxDamageValue.Text);
                }
                if (!IsEmpty(minDamageValue.Text)) {
                    item["MinDamage"] = double.Parse(minDamageValue.Text);
                }

                //clothing
                if (!IsEmpty(maxDefenseValue.Text)) {
                item["MaxDefense"] = double.Parse(maxDefenseValue.Text);
                }
                if (!IsEmpty(defenseValue.Text)) {
                    item["CurrentDefense"] = double.Parse(defenseValue.Text);
                }

                //light source
                if (!IsEmpty(decayRateValue.Text)) {
                    item["chargeDecayRate"] = double.Parse(decayRateValue.Text);
                }
                if (!IsEmpty(lowWarningValue.Text)) {
                    item["chargeLowWarning"] = double.Parse(lowWarningValue.Text);
                }
                if (!IsEmpty(chargeValue.Text)) {
                    item["currentCharge"] = double.Parse(chargeValue.Text);
                }
                if (!IsEmpty(maxChargeValue.Text)) {
                    item["maxCharge"] = double.Parse(maxChargeValue.Text);
                }
                if (!IsEmpty(lightTypeValue.Text)) {
                    item["lightType"] = (Items.LightType)Enum.Parse(typeof(Items.LightType), lightTypeValue.Text);
                }
                if (!IsEmpty(fuelSourceValue.Text)) {
                    item["fuelSource"] = (Items.FuelSource)Enum.Parse(typeof(Items.FuelSource), fuelSourceValue.Text);
                }


                Items.Iitem result = null;

                result = BsonSerializer.Deserialize<Items.Items>(item);
                result.Save();
            }
        }

        private void button3_Click(object sender, EventArgs e) {
            ClearItemCreateForm();
        }

        private bool IsEmpty(string text) {
            if (string.IsNullOrEmpty(text)) {
                return true;
            }

            return false;
        }

        private void ClearItemCreateForm() {
            //general stuff
            idValue.Text = string.Empty;
            nameValue.Text = string.Empty;
            descriptionValue.Text = string.Empty;
            ownerValue.Text = string.Empty;
            minLevelValue.Text = string.Empty;
            conditionValue.Text = "GOOD";
            maxConditionValue.Text = "GOOD";
            weightValue.Text = string.Empty;
            isMovable.Checked = false;
            isWearable.Checked = false;

            //container stuff
            reduceWeightValue.Text = string.Empty;
            weightLimitValue.Text = string.Empty;
            isOpenable.Checked = false;
            isOpened.Checked = false;
            itemContentsValue.Items.Clear();

            //weapon stuff
            attackSpeedValue.Text = string.Empty;
            maxDamageValue.Text = string.Empty;
            minDamageValue.Text = string.Empty;
        }

        private void locationValue_Leave(object sender, EventArgs e) {
            if (locationValue.Text != "-1" && !string.IsNullOrEmpty(locationValue.Text)) {
                try {
                    BsonDocument room = MongoUtils.MongoData.GetCollection("World", "Rooms").FindOneAs<BsonDocument>(Query.EQ("_id", int.Parse(locationValue.Text)));
                    if (room == null) {
                        DisplayValidationErrorBox("That is not a valid room location");
                    }
                }
                catch (FormatException fe) {
                    DisplayErrorBox(fe.Message);
                    locationValue.Text = string.Empty;
                    locationValue.Focus();
                }
            }
        }

        private void weightValue_Leave(object sender, EventArgs e) {
            if (!IsEmpty(weightValue.Text)) {
                if (weightValue.Text != "0" || weightValue.Text != "0.0") {
                    if (ParseDouble(weightValue.Text) == 0d) {
                        DisplayValidationErrorBox("Enter a valid number greater than zero.");
                        weightValue.Focus();
                    }
                }
            }
        }

        private double ParseDouble(string input) {
            double result;
            double.TryParse(input, out result);
            return result;
        }

        private int ParseInt(string input) {
            int result;
            int.TryParse(input, out result);
            return result;
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e) {
            if (tabControl1.SelectedIndex == 0) {
                //don't have these set-up in the game yet

                GetItemsFromDB();
            }
        }


        private void itemsInDBValue_DoubleClick(object sender, EventArgs e) {
            loadItem_Click(null, null);
        }

        private void reduceWeightValue_Leave(object sender, EventArgs e) {
            if (!IsEmpty(reduceWeightValue.Text)) {
                if (reduceWeightValue.Text != "0" || reduceWeightValue.Text != "0.0") {
                    if (ParseDouble(reduceWeightValue.Text) == 0d) {
                        DisplayValidationErrorBox("Enter a valid decimal number greater than zero.");
                        reduceWeightValue.Focus();
                    }
                }
            }
        }

        private void weightLimitValue_Leave(object sender, EventArgs e) {
            if (!IsEmpty(weightLimitValue.Text)) {
                if (weightLimitValue.Text != "0") {
                    if (ParseInt(weightLimitValue.Text) == 0d) {
                        DisplayValidationErrorBox("Enter a valid integer number greater than zero.");
                        weightLimitValue.Focus();
                    }
                }
            }
        }

        private void minLevelValue_Leave(object sender, EventArgs e) {
            if (!IsEmpty(minLevelValue.Text)) {
                if (minLevelValue.Text != "0" || minLevelValue.Text != "0.0") {
                    if (ParseDouble(minLevelValue.Text) == 0d) {
                        DisplayValidationErrorBox("Enter a valid decimal number greater than zero.");
                        minLevelValue.Focus();
                    }
                }
            }
        }

        private void decayRateValue_Leave(object sender, EventArgs e) {
            if (!IsEmpty(decayRateValue.Text)) {
                if (decayRateValue.Text != "0" || decayRateValue.Text != "0.0") {
                    if (ParseDouble(decayRateValue.Text) == 0d) {
                        DisplayValidationErrorBox("Enter a valid decimal number greater than zero.");
                        decayRateValue.Focus();
                    }
                }
            }
        }

        private void ownerValue_TextChanged(object sender, EventArgs e) {
            //doesn't need validation yet
        }

  
        private void lowWarningValue_Leave(object sender, EventArgs e) {
            if (!IsEmpty(lowWarningValue.Text)) {
                if (lowWarningValue.Text != "0" || lowWarningValue.Text != "0.0") {
                    if (ParseDouble(lowWarningValue.Text) == 0d) {
                        DisplayValidationErrorBox("Enter a valid decimal number greater than zero.");
                        lowWarningValue.Focus();
                    }
                }
            }
        }

        private void maxChargeValue_Leave(object sender, EventArgs e) {
            if (!IsEmpty(maxChargeValue.Text)) {
                if (maxChargeValue.Text != "0" || maxChargeValue.Text != "0.0") {
                    if (ParseDouble(maxChargeValue.Text) == 0d) {
                        DisplayValidationErrorBox("Enter a valid decimal number greater than zero.");
                        maxChargeValue.Focus();
                    }
                }
            }
        }

        private void chargeValue_Leave(object sender, EventArgs e) {
            if (!IsEmpty(chargeValue.Text)) {
                if (chargeValue.Text != "0" || chargeValue.Text != "0.0") {
                    if (ParseDouble(chargeValue.Text) == 0d) {
                        DisplayValidationErrorBox("Enter a valid decimal number greater than zero.");
                        chargeValue.Focus();
                    }
                }
            }
        }

        private void attackSpeedValue_Leave(object sender, EventArgs e) {
            if (!IsEmpty(attackSpeedValue.Text)) {
                if (attackSpeedValue.Text != "0" || attackSpeedValue.Text != "0.0") {
                    if (ParseDouble(attackSpeedValue.Text) == 0d) {
                        DisplayValidationErrorBox("Enter a valid decimal number greater than zero.");
                        attackSpeedValue.Focus();
                    }
                }
            }
        }

        private void maxDamageValue_Leave(object sender, EventArgs e) {
            if (maxDamageValue.Text.Length > 0) {
                if (maxDamageValue.Text != "0" || maxDamageValue.Text != "0.0") {
                    if (ParseDouble(maxDamageValue.Text) == 0d) {
                        DisplayValidationErrorBox("Enter a valid decimal number greater than zero.");
                        maxDamageValue.Focus();
                    }
                }
            }
        }

        private void minDamageValue_Leave(object sender, EventArgs e) {
            if (!IsEmpty(minDamageValue.Text)) {
                if (minDamageValue.Text != "0" || minDamageValue.Text != "0.0") {
                    if (ParseDouble(minDamageValue.Text) == 0d) {
                        DisplayValidationErrorBox("Enter a valid decimal number greater than zero.");
                        minDamageValue.Focus();
                    }
                }
            }
        }

        private void maxDefenseValue_Leave(object sender, EventArgs e) {
            if (!IsEmpty(maxDefenseValue.Text)) {
                if (maxDefenseValue.Text != "0" || maxDefenseValue.Text != "0.0") {
                    if (ParseDouble(maxDefenseValue.Text) == 0d) {
                        DisplayValidationErrorBox("Enter a valid decimal number greater than zero.");
                        maxDefenseValue.Focus();
                    }
                }
            }
        }

        private void defenseValue_Leave(object sender, EventArgs e) {
            if (!IsEmpty(defenseValue.Text)) {
                if (defenseValue.Text != "0" || defenseValue.Text != "0.0") {
                    if (ParseDouble(defenseValue.Text) == 0d) {
                        DisplayValidationErrorBox("Enter a valid decimal number greater than zero.");
                        defenseValue.Focus();
                    }
                }
            }
        }

        private void addTrigger_Click(object sender, EventArgs e) {
            TriggerForm triggerForm = new TriggerForm();
            triggerForm.ShowDialog();
            GetTriggerResult(triggerForm);
        }

        private void GetTriggerResult(TriggerForm triggerForm) {
            if (triggerForm.DialogResult == System.Windows.Forms.DialogResult.OK) {
                _itemTriggers.Add(triggerForm.Trigger);
                triggersValue.Items.Add(triggerForm.Trigger["Trigger"].AsString);
            }
            else if (triggerForm.DialogResult == System.Windows.Forms.DialogResult.Abort) {
                if (triggersValue.SelectedIndex != -1) {
                    _itemTriggers.RemoveAt(triggersValue.SelectedIndex);
                    triggersValue.Items.RemoveAt(triggersValue.SelectedIndex);
                }
            }
            else {
                //do nothing
            }

            triggerForm.Close();
        }
        #endregion Item Stuff

        private void triggersValue_SelectedIndexChanged(object sender, EventArgs e) {
            if (triggersValue.SelectedIndex != -1) {
                TriggerForm triggerForm = new TriggerForm(_itemTriggers[triggersValue.SelectedIndex].AsBsonDocument);
                triggerForm.ShowDialog();
                GetTriggerResult(triggerForm);
            }
        }

        private void addToContainer_Click(object sender, EventArgs e) {
            TextBoxForm tbf = new TextBoxForm();
            tbf.ShowDialog();
            if (tbf.DialogResult == System.Windows.Forms.DialogResult.OK) {
                string values = ((TextBox)tbf.Controls["textBoxValue"]).Text;
                if (!itemContentsValue.Items.Contains(values)) {
                    itemContentsValue.Items.Add(values);
                }
            }
            else if (tbf.DialogResult == System.Windows.Forms.DialogResult.Cancel) {
                //do nothing
            }
            else if (tbf.DialogResult == System.Windows.Forms.DialogResult.Abort) {
               //do nothing,because we don't delete from this form
            }
            tbf.Close();
        }
    }
}
