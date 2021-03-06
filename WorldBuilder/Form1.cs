﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using MongoDB.Bson;
using Interfaces;

namespace WorldBuilder {
    public partial class Form1 : Form {
        private ItemsType ItemType { get; set; }
        private BsonArray _wieldAffects;
        private List<IItem> _itemList;
        private List<ITrigger> _itemTriggers;
        private bool ConnectedToDB { get; set; }

        public Form1() {
            InitializeComponent();
            this.playerAttackEffectValue.Enabled = false;
            this.attackEffects.Enabled = false;
            this.targetAttackEffectValue.Enabled = false;
            this.attackEffectsTarget.Enabled = false;
            this.idValue.ReadOnly = true;
            _wieldAffects = new BsonArray();
            _itemList = new List<IItem>();
            _itemTriggers = new List<ITrigger>();
            tabControl1.Enabled = false;
            ScriptError = true;

            MongoUtils.ClassMapper.RegisterMappings();

            CheckConnectionStatus();
        }

        

        private async void CheckConnectionStatus() {
            ConnectedToDB = false;
            tabControl1.Enabled = false;
            databaseConnectionStatusValue.Text = "Database OFFLINE";
            databaseConnectionStatusValue.ForeColor = System.Drawing.Color.Red;

            Task<bool> connectedTask = ProbeDatabase();
           
            ConnectedToDB = await connectedTask;

            if (ConnectedToDB) {
                databaseConnectionStatusValue.Text = "Database CONNECTED";
                databaseConnectionStatusValue.ForeColor = System.Drawing.Color.Green;
                tabControl1.Enabled = true;
            }
        }

        private async Task<bool> ProbeDatabase() {
            bool result = false;

            Action establishConnection = delegate {
                while (!MongoUtils.MongoData.IsConnected()) {
                    try {
                        MongoUtils.MongoData.ConnectToDatabase();
                    }
                    catch (Exception ex) {
                        //squashing bugs like no tomorrow muahahahaha
                        continue;
                    }
                }
                result = true;
                ;
            };

            await Task.Run(establishConnection);

            return result;
        }

        private void removeFromContainer_Click(object sender, EventArgs e) {
            if (itemContentsValue.SelectedIndex != -1) {
                itemContentsValue.Items.RemoveAt(itemContentsValue.SelectedIndex);
            }
        }

        private void DisplayErrorBox(string msg) {
            MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void DisplayValidationErrorBox(string msg) {
            MessageBox.Show(msg, "Validation error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

    }
}
