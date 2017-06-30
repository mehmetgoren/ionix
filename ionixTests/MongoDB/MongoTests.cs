﻿namespace ionixTests.MongoDB
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Reflection;
    using global::MongoDB.Bson;
    using global::MongoDB.Bson.Serialization;
    using global::MongoDB.Bson.Serialization.Attributes;
    using global::MongoDB.Driver;
    using global::MongoDB.Driver.Linq;
    using ionix.Data.MongoDB;
    using ionix.Data.MongoDB.Migration;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MongoTests
    {
        internal const string MongoAddress = "mongodb://localhost:27017";//"mongodb://172.19.3.171:46000"

        static MongoTests()
        {
            MongoClientProxy.SetConnectionString(MongoAddress);
            MongoHelper.InitializeMongo(new Migration100().GetMigrationsAssembly(),
                MongoAddress, DbContext.DatabaseName, false);
        }

        private static IEnumerable<TEntity> CreateMockData<TEntity>(int limit)
            where TEntity : new()
        {
            var properties = typeof(TEntity).GetTypeInfo().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(pi => pi.GetCustomAttribute<BsonIgnoreAttribute>() == null && pi.GetSetMethod() != null);

            Random rnd = new Random();
            IDictionary<Type, Action<TEntity, PropertyInfo>> dic = new ConcurrentDictionary<Type, Action<TEntity, PropertyInfo>>();
            dic.Add(typeof(ObjectId), (en, pi) => pi.SetValue(en, ObjectId.GenerateNewId()));
            dic.Add(typeof(ObjectId?), (en, pi) => pi.SetValue(en, null));
            dic.Add(typeof(string), (en, pi) => pi.SetValue(en, Guid.NewGuid().ToString()));
            dic.Add(typeof(int), (en, pi) => pi.SetValue(en, rnd.Next(10, int.MaxValue)));
            dic.Add(typeof(bool), (en, pi) => pi.SetValue(en, rnd.Next(2, 100) % 2 == 0));
            dic.Add(typeof(DateTime), (en, pi) => pi.SetValue(en, DateTime.Now));
            dic.Add(typeof(DateTime?), (en, pi) => pi.SetValue(en, DateTime.Now));
            dic.Add(typeof(TimeSpan), (en, pi) => pi.SetValue(en, TimeSpan.MinValue));
            dic.Add(typeof(UInt16), (en, pi) => pi.SetValue(en, (UInt16)rnd.Next(10, UInt16.MaxValue)));
            dic.Add(typeof(byte), (en, pi) => pi.SetValue(en, (byte)rnd.Next(10, byte.MaxValue)));

            List<TEntity> list = new List<TEntity>(limit);
            for (int j = 0; j < limit; ++j)
            {
                TEntity en = new TEntity();
                foreach (var pi in properties)
                {
                    if (!pi.PropertyType.GetTypeInfo().IsEnum)
                    {
                        var f = dic[pi.PropertyType];
                        f(en, pi);
                    }
                }
                list.Add(en);
            }

            return list;
        }

        private const int Limit = 1000;
        private static Task InsertMany<TEntity>()
            where TEntity : new()
        {
            return new MongoRepository<TEntity>(MongoClientProxy.Instance).InsertManyAsync(CreateMockData<TEntity>(Limit));
        }

        private static readonly Mongo Cmd = new Mongo(MongoClientProxy.Instance);

        private static void InsertPersonAddress()
        {
            Stopwatch bench = Stopwatch.StartNew();
            var personList = Cmd.AsQueryable<Person>().ToList();
            var addressList = Cmd.AsQueryable<Address>().ToList();

            Random rnd = new Random();

            foreach (Person person in personList)
            {
                for (int j = 0; j < 3; ++j)
                {
                    PersonAddress en = new PersonAddress();
                    en.PersonId = person.Id;
                    en.AddressId = addressList[rnd.Next(0, Limit)].Id;

                    if (Cmd.AsQueryable<PersonAddress>().FirstOrDefault(p => p.PersonId == en.PersonId) == null)
                    {
                        try
                        {
                            Cmd.InsertOne(en);
                        }
                        catch { }
                    }
                }
            }

            bench.Stop();
            Console.WriteLine($"{typeof(PersonAddress).Name}, Elepsad: {bench.ElapsedTicks}");
        }

        //[TestMethod]
        public void Initialize()
        {

            var db = MongoAdmin.GetDatabase(MongoClientProxy.Instance, DbContext.DatabaseName);
             MongoAdmin.ExecuteScript(db, "db.LdapUser.remove({});");
            // MongoAdmin.ExecuteScript(db, "db.LdapUser.drop();");

            //string json = File.ReadAllText("d:\\sil.txt");


            //var list = Newtonsoft.Json.JsonConvert.DeserializeObject<List<LdapUser>>(json);
            //Cmd.InsertMany(list);


            MongoAdmin.ExecuteScript(db, "db.Person.remove({});");
            MongoAdmin.ExecuteScript(db, "db.Address.remove({});");
            MongoAdmin.ExecuteScript(db, "db.PersonAddress.remove({});");

            InsertMany<Person>().Wait();
            InsertMany<Address>().Wait();
            InsertPersonAddress();

            var result = Cmd.Count<Person>();


            Assert.AreNotEqual(result, 0);
        }

        [TestMethod]
        public void CountTest()
        {
            var result = Cmd.Count<Person>();

            Assert.AreNotEqual(result, 0);
        }

        [TestMethod]
        public async Task CountAsyncTest()
        {
            var result = await Cmd.CountAsync<Person>();

            Assert.AreNotEqual(result, 0);
        }

        [TestMethod]
        public void AsQueryableTest()
        {

            var result = (from a in Cmd.AsQueryable<Person>()
                join at in Cmd.AsQueryable<PersonAddress>() on a.Id equals at.PersonId
                select new { Asset = a, AssetTag = at }).Take(10);

            var resultList = result.ToList();


            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void GetByIdTest()
        {
            var asset = Cmd.AsQueryable<Person>().FirstOrDefault();
            if (null == asset)
                Assert.Fail();

            asset = Cmd.GetById<Person>(asset.Id);

            Assert.IsNotNull(asset);
        }

        [TestMethod]
        public async Task GetByIdAsyncTest()
        {
            var asset = Cmd.AsQueryable<Person>().FirstOrDefault();
            if (null == asset)
                Assert.Fail();

            asset = await Cmd.GetByIdAsync<Person>(asset.Id);

            Assert.IsNotNull(asset);
        }

        private static Person CreateAsset()
        {
            Person asset = new Person();
            asset.Active = false;
            asset.Name = "Yeni_" + Guid.NewGuid().ToString("N");
            asset.Description = "Açıklama_" + Guid.NewGuid().ToString("N");

            return asset;
        }

        [TestMethod]
        public void InsertOneTest()
        {
            Person asset = CreateAsset();
            Cmd.InsertOne(asset);

            asset = Cmd.AsQueryable<Person>().FirstOrDefault(p => p.Name.Contains(asset.Name));

            Assert.IsNotNull(asset);
        }

        [TestMethod]
        public async Task InsertOneAsyncTest()
        {
            Person asset = CreateAsset();
            await Cmd.InsertOneAsync(asset);

            asset = Cmd.AsQueryable<Person>().FirstOrDefault(p => p.Name.Contains(asset.Name));

            Assert.IsNotNull(asset);
        }

        private const int ManyTestLength = 100;

        [TestMethod]
        public void InsertManyTest()
        {
            List<Person> list = new List<Person>(ManyTestLength);
            for (int j = 0; j < ManyTestLength; ++j)
            {
                list.Add(CreateAsset());
            }

            Cmd.InsertMany(list);

            list = Cmd.AsQueryable<Person>().Where(p => p.Name.Contains("Yeni")).ToList();

            Assert.AreNotEqual(list.Count, 0);
        }

        [TestMethod]
        public async Task InsertManyAsyncTest()
        {
            List<Person> list = new List<Person>(ManyTestLength);
            for (int j = 0; j < ManyTestLength; ++j)
            {
                list.Add(CreateAsset());
            }

            await Cmd.InsertManyAsync(list);

            list = Cmd.AsQueryable<Person>().Where(p => p.Name.Contains("Yeni")).ToList();

            Assert.AreNotEqual(list.Count, 0);
        }

        [TestMethod]
        public void ReplaceOneTest()
        {
            var asset = Cmd.AsQueryable<Person>().FirstOrDefault();
            if (null == asset)
                Assert.Fail();

            string orginal = asset.Name;

            asset.Name = "Replaced_" + Guid.NewGuid().ToString("N");
            Cmd.ReplaceOne(p => p.Id == asset.Id, asset);

            asset = Cmd.GetById<Person>(asset.Id);

            bool result = orginal != asset.Name;

            if (result)
            {
                orginal = asset.Name;

                asset.Name = "Replaced_" + Guid.NewGuid().ToString("N");

                Cmd.ReplaceOne(asset);

                asset = Cmd.GetById<Person>(asset.Id);

                result = orginal != asset.Name;
            }

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task ReplaceOneAsyncTest()
        {
            var asset = Cmd.AsQueryable<Person>().FirstOrDefault();
            if (null == asset)
                Assert.Fail();

            string orginal = asset.Name;

            asset.Name = "Replaced_" + Guid.NewGuid().ToString("N");
            await Cmd.ReplaceOneAsync(p => p.Id == asset.Id, asset);

            asset = await Cmd.GetByIdAsync<Person>(asset.Id);

            bool result = orginal != asset.Name;

            if (result)
            {
                orginal = asset.Name;

                asset.Name = "Replaced_" + Guid.NewGuid().ToString("N");

                await Cmd.ReplaceOneAsync(asset);

                asset = await Cmd.GetByIdAsync<Person>(asset.Id);

                result = orginal != asset.Name;
            }

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void UpdateOneTest()
        {
            var asset = Cmd.AsQueryable<Person>().FirstOrDefault();
            if (null == asset)
                Assert.Fail();

            asset.Name = "UpdateOne_" + Guid.NewGuid().ToString("N");

            string assetStr = asset.Name;

            Cmd.UpdateOne<Person>(p => p.Id == asset.Id,
                (builder) => builder.Set(p => p.Name, assetStr));

            bool result = Cmd.GetById<Person>(asset.Id).Name == assetStr;

            if (result)
            {
                asset.Name = "UpdateOne_" + Guid.NewGuid().ToString("N");
                assetStr = asset.Name;

                Cmd.UpdateOne<Person>(asset.Id,
                    (builder) => builder.Set(p => p.Name, assetStr));

                result = Cmd.GetById<Person>(asset.Id).Name == assetStr;

                asset.Name = "Mehmet 2";
                asset.Description = "Gören 2";

                Cmd.UpdateOne(asset, p => p.Name, p => p.Description);
            }

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task UpdateOneAsyncTest()
        {
            var asset = Cmd.AsQueryable<Person>().FirstOrDefault();
            if (null == asset)
                Assert.Fail();

            asset.Name = "UpdateOne_" + Guid.NewGuid().ToString("N");

            string assetStr = asset.Name;

            await Cmd.UpdateOneAsync<Person>(p => p.Id == asset.Id,
                (builder) => builder.Set(p => p.Name, assetStr));

            bool result = (await Cmd.GetByIdAsync<Person>(asset.Id)).Name == assetStr;

            if (result)
            {
                asset.Name = "UpdateOne_" + Guid.NewGuid().ToString("N");
                assetStr = asset.Name;

                await Cmd.UpdateOneAsync<Person>(asset.Id,
                    (builder) => builder.Set(p => p.Name, assetStr));

                result = (await Cmd.GetByIdAsync<Person>(asset.Id)).Name == assetStr;
            }

            Assert.IsTrue(result);
        }


        [TestMethod]
        public void UpdateManyTest()
        {
            var result = Cmd.UpdateMany<Person>(p => p.Active,
                (builder) => builder.Set(p => p.Active, false).Set(p => p.Description, "Mehmet"));

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void DeleteOneTest()
        {
            var asset = Cmd.AsQueryable<Person>().FirstOrDefault();
            if (null == asset)
                Assert.Fail();

            var count = Cmd.Count<Person>();
            Cmd.DeleteOne<Person>(p => p.Id == asset.Id);

            var result = count - Cmd.Count<Person>() == 1;

            if (result)
            {
                count--;

                asset = Cmd.AsQueryable<Person>().FirstOrDefault();
                if (null == asset)
                    Assert.Fail();

                Cmd.DeleteOne<Person>(asset.Id);

                result = count - Cmd.Count<Person>() == 1;
            }

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task BulkReplaceAsyncTest()
        {
            var list = await Cmd.AsQueryable<Person>().OrderBy(p => p.Id).Take(10).ToListAsync();


            list.ForEach(i => i.Name = "Changed By Ela " + Guid.NewGuid().ToString("N"));

            var result = await Cmd.BulkReplaceAsync(list, null, false, p => p.Name);

            list.ForEach(i => i.Description = "Changed By Ela 2");

            result = await Cmd.BulkReplaceAsync(list, null, false);

            list.ForEach(i =>
            {
                i.Id = ObjectId.GenerateNewId();
                i.Name = Guid.NewGuid().ToString("N");
            });

            result = await Cmd.BulkReplaceAsync(list, null, true, p => p.Name);


            list.ForEach(i =>
            {
                i.Id = ObjectId.GenerateNewId();
                i.Name = Guid.NewGuid().ToString("N");
            });

            result = await Cmd.BulkReplaceAsync(list, null, true);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task BulkUpdateAsync()
        {
            const int limit = 10;
            var list = await Cmd.AsQueryable<Person>().OrderBy(p => p.Id).Take(limit).ToListAsync();

            Random rnd = new Random();
            list.ForEach(i => i.Description = rnd.Next(0, 100000).ToString());

            var result = await Cmd.BulkUpdateAsync(list, null, p => p.Description);

            Assert.AreEqual((int)result.ModifiedCount, limit);
        }

        [TestMethod]
        public async Task BulkDeleteAsync()
        {
            const int limit = 3;
            var list = await Cmd.AsQueryable<Person>().OrderByDescending(p => p.Id).Take(limit).ToListAsync();

            var result = await Cmd.BulkDeleteAsync(list, null, p => p.Name);

            Assert.AreEqual((int)result.DeletedCount, limit);

            list = await Cmd.AsQueryable<Person>().OrderByDescending(p => p.Id).Take(limit).ToListAsync();
            result = await Cmd.BulkDeleteAsync(list, null);

            Assert.AreEqual((int)result.DeletedCount, limit);
        }

        [TestMethod]
        public async Task TextSearchTest()
        {
            var list = Cmd.TextSearch<LdapUser>("ağrı şaban");

            list = await  Cmd.TextSearchAsync<LdapUser>("bayburt");

            Assert.IsNotNull(list);
        }

        [TestMethod]
        public void LookupTest()
        {
            var script = @"db.PersonAddress.aggregate([
            {$match: { Active: { $eq: true }, _id:{$ne: ObjectId('59563835645e2f41587c3204')} }},
            {$limit: 1500},
            {$sort: { _id: 1} },
  
            {
                $lookup:
                {
                    from: 'Person',
                    localField: 'PersonId',
                    foreignField: '_id',
                        as: 'Person'
                }
            },
            {
                $unwind: '$Person'
            },
  
            {
                $lookup:
                {
                    from: 'Address',
                    localField: 'AddressId',
                    foreignField: '_id',
                        as: 'Address'
                }
            },
            {
                $unwind: '$Address'
            },
  
            {
                $project:
                {
                    _id: 1,  
                    PersonId: 1,
                    AddressId: 1,
                    Active: 1,
        
                    Person:
                    {
                        _id: 1,
                        Name: 1,
                        Active: 1,
                        Description: 1
                    },
      
                    Address:
                    {
                        _id: 1,
                        Name: 1,
                        Country: 1,
                        City: 1,
                        PostalCode: 1,
                        Street: 1,
                        HouseNumber: 1
                    }
                }
            }
            ]); ";


            var x = Cmd.AsQueryable<Person>().FirstOrDefault();
            var y = DictionarySerializer.Serialize(x);

            var dic = MongoAdmin.ExecuteScript(MongoAdmin.GetDatabase(MongoClientProxy.Instance
                , DbContext.DatabaseName), script).ToDictionary();

            object[] arr = ((IDictionary<string, object>) dic.First().Value).First().Value as object[];

            foreach (var dicItem in arr)
            {
                var myDic = dicItem as IDictionary<string, object>;

                PersonAddress model = DictionarySerializer.Deserialize<PersonAddress>(myDic);

                var personDic = myDic["Person"] as IDictionary<string, object>;
                var person = DictionarySerializer.Deserialize<Person>(personDic);

                var addressDic = myDic["Address"] as IDictionary<string, object>;
                var adress = DictionarySerializer.Deserialize<Address>(addressDic);
            }
            
            Assert.Fail();
        }
    }
}
