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

        private void roomRefresh_Click(object sender, EventArgs e) {
            this.roomsListValue.Items.Clear();
            
            if (ConnectedToDB) {
                MongoCursor<BsonDocument> result = null;
                if (string.IsNullOrEmpty(filterValue.Text)) {
                    result = MongoUtils.MongoData.GetCollection("World", "Rooms").FindAllAs<BsonDocument>();
                }
                else {
                    if (filterTypeValue.Text == "_id") {
                        result = MongoUtils.MongoData.GetCollection("World", "Rooms").FindAs<BsonDocument>(Query.EQ(filterTypeValue.Text, ObjectId.Parse(filterValue.Text)));
                    }
                    else {
                        result = MongoUtils.MongoData.GetCollection("World", "Rooms").FindAs<BsonDocument>(Query.EQ(filterTypeValue.Text, filterValue.Text));
                    }
                }

                foreach (BsonDocument doc in result) {
                    this.roomsListValue.Items.Add(doc["Title"].AsString + " (" + doc["_id"].AsInt32 + ")" );
                }
            }
        }

        private void roomLoad_Click(object sender, EventArgs e) {
            if (!IsEmpty(roomIdValue.Text)) {
                if (ConnectedToDB) {
                    BsonDocument result = null;
                    result = MongoUtils.MongoData.GetCollection("World", "Rooms").FindOneAs<BsonDocument>(Query.EQ("_id", roomIdValue.Text));
                    if (result != null) {
                        FillRoomControls(result);
                    }
                }
            }
        }

        private void FillRoomControls(BsonDocument room) {

        }

    }
}
