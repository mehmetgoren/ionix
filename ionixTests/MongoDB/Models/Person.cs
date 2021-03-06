﻿namespace ionixTests.MongoDB
{
    using global::MongoDB.Bson;
    using global::MongoDB.Bson.Serialization.Attributes;
    using ionix.Data.MongoDB;


    [MongoCollection(Database = "TestDb", Name = "Person")]
    [MongoIndex("Name", Unique = true)]
    [MongoTextIndex("*")]
    //[BsonIgnoreExtraElements]
    public class Person
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public string Name { get; set; }

        public bool Active { get; set; } = true;

        public string Description { get; set; }
    }
}
