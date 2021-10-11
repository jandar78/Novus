﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace Interfaces {
    public interface IEdible {

        void GetAttributesAffected(BsonArray attributesToAffect);
        Dictionary<string, double> Consume();
    }
}
