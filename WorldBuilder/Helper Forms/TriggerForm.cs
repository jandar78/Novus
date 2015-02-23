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

namespace WorldBuilder {
    public partial class TriggerForm : Form {
        public BsonDocument Trigger { get; set; }

        public TriggerForm(BsonDocument trigger = null) {
            InitializeComponent();
            if (trigger != null) {
                triggerValue.Text = trigger["Trigger"].AsString;
                chanceToTriggerValue.Text = trigger["ChanceToTrigger"].ToString();
                scriptIdValue.Text = trigger["ScriptID"].AsString;

                foreach (BsonValue msg in trigger["Overrides"].AsBsonArray) {
                    if (msg != "New...") {
                        messageOverrideValue.Items.Add(msg);
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e) {
            BsonDocument doc = new BsonDocument();
            doc.Add("Trigger", triggerValue.Text);
            doc.Add("ChanceToTrigger", double.Parse(chanceToTriggerValue.Text));
            doc.Add("ScriptID", scriptIdValue.Text);
            
            BsonArray overrides = new BsonArray();
            foreach (string msg in messageOverrideValue.Items) {
                if (msg != "New...") {
                    overrides.Add(msg);
                }
            }

            doc.Add("Overrides", overrides);

            Trigger = doc;
            DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void button2_Click(object sender, EventArgs e) {
            DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        private void button3_Click(object sender, EventArgs e) {
            DialogResult = System.Windows.Forms.DialogResult.Abort;
        }

        private void DisplayMessageForm(bool isNew = true) {
            TextBoxForm textForm = new TextBoxForm();
            
            if (!isNew && messageOverrideValue.SelectedIndex != -1) {
                textForm.TextValue = (string)messageOverrideValue.Items[messageOverrideValue.SelectedIndex].ToString();
            }

            textForm.ShowDialog();
            
            if (textForm.DialogResult == System.Windows.Forms.DialogResult.OK) {
                if (messageOverrideValue.SelectedIndex > 0) {
                    messageOverrideValue.Items[messageOverrideValue.SelectedIndex] = textForm.TextValue;
                }
                else {
                    messageOverrideValue.Items.Add(textForm.TextValue);
                }
            }
            else if (textForm.DialogResult == System.Windows.Forms.DialogResult.Abort) {
                messageOverrideValue.Items.RemoveAt(messageOverrideValue.SelectedIndex);
            }
            else {
            }

            textForm.Close();
        }

        private void scriptIdValue_TextChanged(object sender, EventArgs e) {
            //can't think of any validation to do yet.  Maybe just see if the script ID exists in the DB?
        }

        private void messageOverrideValue_DoubleClick(object sender, EventArgs e) {
            if (messageOverrideValue.SelectedIndex == 0) {
                DisplayMessageForm();
            }
            else {
                DisplayMessageForm(false);
            }
        }

        private void chanceToTriggerValue_Leave(object sender, EventArgs e) {
            if (chanceToTriggerValue.Text.Length > 0) {
                int chance;
                int.TryParse(chanceToTriggerValue.Text, out chance);
                if (chance < 0 || chance > 100){
                    MessageBox.Show("Enter a whole number as the percentage.\n75 = 0.75%", "Validation Error", MessageBoxButtons.OK);
                }
            }
        }
    }
}
