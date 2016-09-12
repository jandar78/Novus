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
using Interfaces;
using System.Linq.Expressions;
using Rooms;

namespace WorldBuilder {
    public partial class Form1 : Form {
        #region Item Stuff
        private async void GetItemsFromDB() {
            this.itemsInDBValue.Items.Clear();
            this._itemList.Clear();

            if (ConnectedToDB) {
                IEnumerable<IItem> result = null;
                if (string.IsNullOrEmpty(filterValue.Text)) {
                    result = await MongoUtils.MongoData.FindAll<Items.Items>(MongoUtils.MongoData.GetCollection<Items.Items>("World", "Items"));
                }
                else {
                    if (filterTypeValue.Text == "_id") {
                        result = await MongoUtils.MongoData.RetrieveObjectsAsync<Items.Items>(MongoUtils.MongoData.GetCollection<Items.Items>("World", "Items"), i => i.Id == ObjectId.Parse(filterValue.Text));
                    }
                    else {
                        result = await MongoUtils.MongoData.RetrieveObjectsAsync<Items.Items>(MongoUtils.MongoData.GetCollection<Items.Items>("World", "Items"), Contains<Items.Items>(filterTypeValue.Text, filterValue.Text));

                    }
                }

                if (result != null) {
                    _itemList = result.ToList();
                }

                foreach (var doc in result) {
                    this.itemsInDBValue.Items.Add(doc.Name);
                }
            }
        }

        private static Expression<Func<T, bool>> Contains<T>(string name, string value) {
            var pe = Expression.Parameter(typeof(T));
            var property = Expression.Property(pe, name);
            var returnExp = Expression.Call(property,
                 "Contains",
                 new Type[] { typeof(String) },
                 Expression.Constant(value));

            return Expression.Lambda<Func<T, bool>>(returnExp, pe);

        }

        private void wieldEffectValue_SelectedIndexChanged(object sender, EventArgs e) {
            AffectedForm affectedForm = null;
            if (wieldEffectValue.SelectedItem.ToString() == "New...") {
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
            var itemCollection = MongoUtils.MongoData.GetCollection<Items.Items>("World", "Items");
            IItem item = null;
            if (itemsInDBValue.SelectedIndex != -1) {
                item = _itemList[this.itemsInDBValue.SelectedIndex];
            }
            else if (!string.IsNullOrEmpty(idValue.Text)) {
                item = MongoUtils.MongoData.RetrieveObject<Items.Items>(itemCollection, i => i.Id == ObjectId.Parse(idValue.Text));
            }

            if (item != null) {
                FillControls(item);
            }
        }

        private void FillControls(IItem item) {
            ClearItemCreateForm();
            idValue.Text = item.Id.ToString();
            nameValue.Text = item.Name;
            descriptionValue.Text = item.Description;
            locationValue.Text = item.Location;
            //general stuff
            ownerValue.Text = item.Owner;
            minLevelValue.Text = item.MinimumLevel.ToString();
            conditionValue.Text = item.CurrentCondition.ToString();
            maxConditionValue.Text = item.MaxCondition.ToString();
            weightValue.Text = item.Weight.ToString();

            if (item.ItemType != null) {
                foreach (var value in item.ItemType) {
                    switch (value.Key) {
                        case ItemsType.WEAPON:
                            typeWeaponValue.Checked = true;
                            break;
                        case ItemsType.CLOTHING:
                            typeClothingValue.Checked = true;
                            break;
                        case ItemsType.EDIBLE:
                            typeEdibleValue.Checked = true;
                            break;
                        case ItemsType.DRINKABLE:
                            typeDrinkableValue.Checked = true;
                            break;
                        case ItemsType.CONTAINER:
                            typeContainerValue.Checked = true;
                            break;
                        case ItemsType.ILUMINATION:
                            typeIluminationValue.Checked = true;
                            break;
                        case ItemsType.KEY:
                            typeKeyValue.Checked = true;
                            break;
                    }
                }
            }
            isMovable.Checked = item.IsMovable;
            isWearable.Checked = item.IsWearable;

            //container stuff
            var castItem = item as Items.Items;
            reduceWeightValue.Text = castItem.ReduceCarryWeightBy.ToString();
            weightLimitValue.Text = castItem.WeightLimit.ToString();
            isOpenable.Checked = castItem.IsOpenable;
            isOpened.Checked = castItem.Opened;
            

            if (castItem.Contents != null) {
                foreach (var value in castItem.Contents) {
                    itemContentsValue.Items.Add(value);
                }
            }
            //weapon stuff
            attackSpeedValue.Text = castItem.AttackSpeed.ToString();
            maxDamageValue.Text = castItem.MaxDamage.ToString();
            minDamageValue.Text = castItem.MinDamage.ToString();

            //illumination
            isLightable.Checked = castItem.isLightable;
            isLit.Checked = castItem.isLit;
            decayRateValue.Text = castItem.chargeDecayRate.ToString();


            //added the triggers here, need to test
            if (item.ItemTriggers != null) {
                foreach (var value in item.ItemTriggers) {
                    _itemTriggers.Add(value);
                    triggersValue.Items.Add(value.TriggerOn);
                }
            }
        }

        

        private void button1_Click(object sender, EventArgs e) {
            GetItemsFromDB();
        }

        private void button2_Click(object sender, EventArgs e) {
            if (ConnectedToDB) {
                var item = new Items.Items();
                if (!string.IsNullOrEmpty(idValue.Text)) {
                    item.Id = ObjectId.Parse(idValue.Text);
                }

                item.Location = locationValue.Text;

                //general stuff
                if (!IsEmpty(nameValue.Text)) {
                    item.Name = nameValue.Text;
                }
                if (!IsEmpty(descriptionValue.Text)) {
                    item.Description = descriptionValue.Text;
                }
                if (!IsEmpty(ownerValue.Text)) {
                    item.Owner = ownerValue.Text;
                }
                if (!IsEmpty(minLevelValue.Text)) {
                    item.MinimumLevel = int.Parse(minLevelValue.Text);
                }
                if (!IsEmpty(conditionValue.Text)) {
                    item.CurrentCondition = (ItemCondition)Enum.Parse(typeof(ItemCondition), conditionValue.Text);
                }
                if (!IsEmpty(maxConditionValue.Text)) {
                    item.MaxCondition = (ItemCondition)Enum.Parse(typeof(ItemCondition), maxConditionValue.Text);
                }
                if (!IsEmpty(weightValue.Text)) {
                    item.Weight = double.Parse(weightValue.Text);
                }
                
                
                //attributes
                item.IsMovable = isMovable.Checked;
                item.IsWearable = isWearable.Checked;
                item.IsOpenable = isOpenable.Checked;
                item.Opened = isOpened.Checked;
                item.IsWieldable = isWieldable.Checked;
                item.isLit = isLit.Checked;
                item.isChargeable = isChargeable.Checked;
                item.isLightable = isLightable.Checked;

                //key
                item.SkeletonKey = isSkeletonKey.Checked;
                if (!IsEmpty(doorIdValue.Text)) {
                    item.DoorID = doorIdValue.Text;
                }

                //container stuff
                if (!IsEmpty(reduceWeightValue.Text)) {
                    item.ReduceCarryWeightBy = double.Parse(reduceWeightValue.Text);
                }
                if (!IsEmpty(weightLimitValue.Text)) {
                    item.WeightLimit = double.Parse(weightLimitValue.Text);
                }

                List<String> contentsArray = new List<string>();
                foreach (string value in itemContentsValue.Items) {
                    contentsArray.Add(value);
                }
                if (contentsArray.Count > 0) {
                    item.Contents = contentsArray;
                }
                //weapon stuff
                if (!IsEmpty(attackSpeedValue.Text)) {
                    item.AttackSpeed = double.Parse(attackSpeedValue.Text);
                }
                if (!IsEmpty(maxDamageValue.Text)) {
                    item.MaxDamage = double.Parse(maxDamageValue.Text);
                }
                if (!IsEmpty(minDamageValue.Text)) {
                    item.MinDamage = double.Parse(minDamageValue.Text);
                }

                //clothing
                if (!IsEmpty(maxDefenseValue.Text)) {
                item.MaxDefense = double.Parse(maxDefenseValue.Text);
                }
                if (!IsEmpty(defenseValue.Text)) {
                    item.CurrentDefense = double.Parse(defenseValue.Text);
                }

                //light source
                if (!IsEmpty(decayRateValue.Text)) {
                    item.chargeDecayRate = double.Parse(decayRateValue.Text);
                }
                if (!IsEmpty(lowWarningValue.Text)) {
                    item.chargeLowWarning = double.Parse(lowWarningValue.Text);
                }
                if (!IsEmpty(chargeValue.Text)) {
                    item.currentCharge = double.Parse(chargeValue.Text);
                }
                if (!IsEmpty(maxChargeValue.Text)) {
                    item.maxCharge = double.Parse(maxChargeValue.Text);
                }
                if (!IsEmpty(lightTypeValue.Text)) {
                    item.lightType = (LightType)Enum.Parse(typeof(LightType), lightTypeValue.Text);
                }
                if (!IsEmpty(fuelSourceValue.Text)) {
                    item.fuelSource = (FuelSource)Enum.Parse(typeof(FuelSource), fuelSourceValue.Text);
                }

                //item Type
                var itemTypeDictionary = new Dictionary<ItemsType, int>();
                foreach (CheckBox cb in itemTypeGroup.Controls) {
                    if (cb.Checked) {
                        itemTypeDictionary.Add((ItemsType)Enum.Parse(typeof(ItemsType), cb.Text.ToUpper()), 0);
                    }
                }
                                        
                item.ItemType = itemTypeDictionary;

                if (_itemTriggers.Count > 0) {
                    item.ItemTriggers = _itemTriggers;
                }

                MongoUtils.MongoData.Save<Items.Items>(MongoUtils.MongoData.GetCollection<Items.Items>("World", "Items"), i => i.Id == item.Id, item);
                GetItemsFromDB();
            }
        }

        private void NewButton_Click(object sender, EventArgs e) {
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
            typeClothingValue.Checked = typeContainerValue.Checked = typeEdibleValue.Checked = typeDrinkableValue.Checked = typeIluminationValue.Checked = typeKeyValue.Checked = false;

            //container stuff
            reduceWeightValue.Text = string.Empty;
            weightLimitValue.Text = string.Empty;
            isOpenable.Checked = false;
            isOpened.Checked = false;
            itemContentsValue.Items.Clear();

            foreach (CheckBox cb in itemTypeGroup.Controls) {
                cb.Checked = false;
            }

            //weapon stuff
            _itemTriggers.Clear();
            attackSpeedValue.Text = string.Empty;
            maxDamageValue.Text = string.Empty;
            minDamageValue.Text = string.Empty;
            triggersValue.Items.Clear();
        }

        private void locationValue_Leave(object sender, EventArgs e) {
            if (!string.IsNullOrEmpty(locationValue.Text)) {
                try {
                    var room = MongoUtils.MongoData.RetrieveObject<Room>(MongoUtils.MongoData.GetCollection<Room>("Rooms", locationValue.Text[0].ToString()), r => r.Id == locationValue.Text);
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
                triggersValue.Items.Add(triggerForm.Trigger);
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
                TriggerForm triggerForm = new TriggerForm(_itemTriggers[triggersValue.SelectedIndex]);
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
