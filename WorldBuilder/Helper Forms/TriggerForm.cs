using System;
using System.Linq;
using System.Windows.Forms;
using MongoDB.Bson;
using Interfaces;

namespace WorldBuilder {
    public partial class TriggerForm : Form {
        public ITrigger Trigger { get; set; }

        public TriggerForm(ITrigger trigger = null) {
            InitializeComponent();
            if (trigger != null) {
                trigger.TriggerOn.ForEach(t => { triggerValue.Text += t + "\n"; });
                chanceToTriggerValue.Text = trigger.ChanceToTrigger.ToString();
                scriptIdValue.Text = trigger.TriggerId;

                foreach (BsonValue msg in trigger.MessageOverrides) {
                    if (msg != "New...") {
                        messageOverrideValue.Items.Add(msg);
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e) {
            ITrigger trigger = new Triggers.GeneralTrigger();
            trigger.TriggerOn = triggerValue.Text.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            trigger.ChanceToTrigger = double.Parse(chanceToTriggerValue.Text);
            trigger.TriggerId = scriptIdValue.Text;
            
            Trigger = trigger;
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
