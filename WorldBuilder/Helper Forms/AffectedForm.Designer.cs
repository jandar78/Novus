namespace WorldBuilder {
    partial class AffectedForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.label1 = new System.Windows.Forms.Label();
            this.amount = new System.Windows.Forms.Label();
            this.amountValue = new System.Windows.Forms.TextBox();
            this.attributeValue = new System.Windows.Forms.ComboBox();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(23, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(46, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Attribute";
            // 
            // amount
            // 
            this.amount.AutoSize = true;
            this.amount.Location = new System.Drawing.Point(26, 54);
            this.amount.Name = "amount";
            this.amount.Size = new System.Drawing.Size(43, 13);
            this.amount.TabIndex = 1;
            this.amount.Text = "Amount";
            // 
            // amountValue
            // 
            this.amountValue.Location = new System.Drawing.Point(79, 51);
            this.amountValue.Name = "amountValue";
            this.amountValue.Size = new System.Drawing.Size(121, 20);
            this.amountValue.TabIndex = 2;
            this.amountValue.TextChanged += new System.EventHandler(this.amountValue_TextChanged);
            // 
            // attributeValue
            // 
            this.attributeValue.AutoCompleteCustomSource.AddRange(new string[] {
            "HEALTH",
            "ENDURANCE",
            "STRENGTH",
            "DEXTERITY",
            "INTELLIGENCE",
            "CHARISMA"});
            this.attributeValue.FormattingEnabled = true;
            this.attributeValue.Items.AddRange(new object[] {
            "Hitpoints",
            "Strength",
            "Dexterity",
            "Intelligence",
            "Charisma",
            "Endurance"});
            this.attributeValue.Location = new System.Drawing.Point(79, 13);
            this.attributeValue.Name = "attributeValue";
            this.attributeValue.Size = new System.Drawing.Size(121, 21);
            this.attributeValue.TabIndex = 3;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(12, 98);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 4;
            this.button1.Text = "OK";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(93, 98);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 5;
            this.button2.Text = "Cancel";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(174, 98);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(75, 23);
            this.button3.TabIndex = 6;
            this.button3.Text = "Delete";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // AffectedForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(253, 133);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.attributeValue);
            this.Controls.Add(this.amountValue);
            this.Controls.Add(this.amount);
            this.Controls.Add(this.label1);
            this.Name = "AffectedForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "AffectedForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label amount;
        private System.Windows.Forms.TextBox amountValue;
        private System.Windows.Forms.ComboBox attributeValue;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
    }
}