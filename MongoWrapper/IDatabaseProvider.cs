using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoWrapper {
    interface IDatabaseProvider {

        //this is for a futuer feature where maybe whoever is using the codebase wants to use another type of databse
        //and not MongoDB, maybe Couch or a SQL server who knows.  The point is that the database should be abstracted and decoupled
        //and that the engine should be able to happily do the same work.  This may end up causing this interface to force
        //users to explicitly provide Serialize and Deserialize methods as well as Save and Load ones.
        //The engine should not have to concern itself with how it accesses the data.
        void ConnectToDatabse();
        bool IsConnected();
       
    }
}
