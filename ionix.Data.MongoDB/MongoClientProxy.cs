﻿namespace ionix.Data.MongoDB
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::MongoDB.Bson;
    using global::MongoDB.Driver;
    using global::MongoDB.Driver.Core.Clusters;
    using Utils;

    public sealed class MongoClientProxy : Singleton, IMongoClient
    {
        private static string ConnectionString = "mongodb://localhost:27017";
        public static void SetConnectionString(string value)
        {
            if (String.IsNullOrEmpty(value))
                throw new ArgumentNullException(nameof(value));

            ConnectionString = value;

            Concrete = new MongoClient(ConnectionString);
        }

        private static IMongoClient Concrete = new MongoClient(ConnectionString);

        public static readonly MongoClientProxy Instance = new MongoClientProxy();

        private MongoClientProxy()
        {

        }

        public void DropDatabase(string name, CancellationToken cancellationToken = new CancellationToken())
        {
            Concrete.DropDatabase(name, cancellationToken);
        }

        public Task DropDatabaseAsync(string name, CancellationToken cancellationToken = new CancellationToken())
        {
            return Concrete.DropDatabaseAsync(name, cancellationToken);
        }

        public IMongoDatabase GetDatabase(string name, MongoDatabaseSettings settings = null)
        {
            return Concrete.GetDatabase(name, settings);
        }

        public IAsyncCursor<BsonDocument> ListDatabases(CancellationToken cancellationToken = new CancellationToken())
        {
            return Concrete.ListDatabases(cancellationToken);
        }

        public Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return Concrete.ListDatabasesAsync(cancellationToken);
        }

        public IMongoClient WithReadConcern(ReadConcern readConcern)
        {
            return Concrete.WithReadConcern(readConcern);
        }

        public IMongoClient WithReadPreference(ReadPreference readPreference)
        {
            return Concrete.WithReadPreference(readPreference);
        }

        public IMongoClient WithWriteConcern(WriteConcern writeConcern)
        {
            return Concrete.WithWriteConcern(writeConcern);
        }

        public ICluster Cluster { get; } = Concrete.Cluster;
        public MongoClientSettings Settings { get; } = Concrete.Settings;
    }
}


//The MongoClient instance actually represents a pool of connections to the database; you will only need one instance of class MongoClient even with multiple threads.
//Typically you only create one MongoClient instance for a given cluster and use it across your application.Creating multiple MongoClients will,
//    however, still share the same pool of connections if and only if the connection strings are identical.
