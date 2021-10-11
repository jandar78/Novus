using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Classes.Mapper
{
    public class CustomSerializers
    {
        //public class HashSetSerializer<T> : IBsonSerializer
        //{
        //    private readonly IBsonSerializer _serializer = BsonSerializer.LookupSerializer(typeof(T));

        //    public Type ValueType => throw new NotImplementedException();

        //    public override object Deserialize(BsonReader bsonReader, Type nominalType, Type actualType, IBsonSerializationOptions options)
        //    {
        //        var set = new HashSet<MyObject>();

        //        bsonReader.ReadStartDocument();
        //        while ((bsonReader.ReadBsonType()) != 0)
        //        {
        //            var name = bsonReader.ReadName();
        //            var element = (MyObject)_serializer.Deserialize(bsonReader, typeof(MyObject), null);
        //            if (element.Name != name)
        //            {
        //                throw new FormatException("Names don't match.");
        //            }
        //            set.Add(element);
        //        }
        //        bsonReader.ReadEndDocument();

        //        return set;
        //    }

        //    public object Deserialize<T>(BsonDeserializationContext context, BsonDeserializationArgs args)
        //    {
        //        var set = new HashSet<T>();

        //        var bsonReader = context.Reader;
        //        bsonReader.ReadStartDocument();
        //        while ((bsonReader.ReadBsonType()) != 0)
        //        {
        //            var name = bsonReader.ReadName();
        //            var element = (T)_serializer.Deserialize(context);
                    
        //            set.Add(element);
        //        }

        //        bsonReader.ReadEndDocument();

        //        return set;
        //    }

        //    public override void Serialize(BsonWriter bsonWriter, Type nominalType, object value, IBsonSerializationOptions options)
        //    {
        //        var set = (HashSet<MyObject>)value;

        //        bsonWriter.WriteStartDocument();
        //        foreach (var element in set)
        //        {
        //            bsonWriter.WriteName(element.Name);
        //            _serializer.Serialize(bsonWriter, typeof(MyObject), element, null);
        //        }
        //        bsonWriter.WriteEndDocument();
        //    }

        //    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        //    {
        //        throw new NotImplementedException();
        //    }
        //}
    }
}
