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
    public partial class ModifiersForm : Form {
        public BsonDocument attribute;
        private double Value { get; set; }

        public ModifiersForm(BsonDocument editAttribute = null) {
            InitializeComponent();
            
            attribute = new BsonDocument();
            if (editAttribute != null) {
                amountValue.Text = editAttribute["v"].AsDouble.ToString();
                attributeValue.Text = editAttribute["k"].AsString;
            }
        }

        private void button1_Click(object sender, EventArgs e) {
            if (this.attributeValue.Text != string.Empty) {
                attribute.Add("k", this.attributeValue.Text);
            }
            if (this.amountValue.Text != string.Empty) {
                attribute.Add("v", Value);
            }
            DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void amountValue_TextChanged(object sender, EventArgs e) {
            double parsed = 0d;
            double.TryParse(amountValue.Text, out parsed);
            Value = parsed;
        }

        private void button2_Click(object sender, EventArgs e) {
            DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        private void button3_Click(object sender, EventArgs e) {
            attributeValue.SelectedText = string.Empty;
            amountValue.Text = string.Empty;
            attribute = null;
            DialogResult = System.Windows.Forms.DialogResult.Abort;
        }
    }
}
