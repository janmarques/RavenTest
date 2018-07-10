using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Subscriptions;
using Raven.Client.Exceptions;

namespace PetsAndPeople
{
    class Program
    {
         public class User
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string AddressId { get; set; }
        public int Count { get; set; }
        public int Age { get; set; }
    }

        static void Main(string[] args)
        {
            var _reasonableWaitTime = TimeSpan.FromSeconds(60);

            using (var store = new DocumentStore
            {
                Urls = new[] { "http://127.0.0.1:8080" },
                Database = "zoo",
            })
            {
                store.Initialize();
                using (var session = store.OpenSession())
                {
                    session.Store(new User { Age = 31 }, "users/1");
                    session.Store(new User { Age = 27 }, "users/12");
                    session.Store(new User { Age = 25 }, "users/3");

                    session.SaveChanges();
                }

                var id = store.Subscriptions.Create<User>();
                using (var subscription = store.Subscriptions.GetSubscriptionWorker<User>(new SubscriptionWorkerOptions(id)
                {
                    TimeToWaitBeforeConnectionRetry = TimeSpan.FromSeconds(5)
                }))
                {

                    var keys = new BlockingCollection<string>();
                    var ages = new BlockingCollection<int>();

                    subscription.Run(batch =>
                    {
                        batch.Items.ForEach(x => keys.Add(x.Id));
                        batch.Items.ForEach(x => ages.Add(x.Result.Age));
                    });

                    string key;
                    Assert.True(keys.TryTake(out key, _reasonableWaitTime));
                    Assert.Equal("users/1", key);

                    Assert.True(keys.TryTake(out key, _reasonableWaitTime));
                    Assert.Equal("users/12", key);

                    Assert.True(keys.TryTake(out key, _reasonableWaitTime));
                    Assert.Equal("users/3", key);

                    int age;
                    Assert.True(ages.TryTake(out age, _reasonableWaitTime));
                    Assert.Equal(31, age);

                    Assert.True(ages.TryTake(out age, _reasonableWaitTime));
                    Assert.Equal(27, age);

                    Assert.True(ages.TryTake(out age, _reasonableWaitTime));
                    Assert.Equal(25, age);
                }
            }
            return;
            /*
            using (var store = new DocumentStore
            {
                Urls = new[] { "http://127.0.0.1:8080" },
                Database = "zoo",
            })
            {
                store.Initialize();

                //new PersonWithPetsIndex().Execute(store);

                //new Thread(() =>
                //{

                //    while (true)
                //    {
                //        for (int j = 0; j < 5; j++)
                //        {
                //            using (var session = store.OpenSession())
                //            {
                //                for (int i = 0; i < 10000; i++)
                //                {
                //                    var fluffy = new Pet { Id = Guid.NewGuid().ToString(), Name = "Fluffy" };
                //                    var john = new Person { Id = Guid.NewGuid().ToString(), Name = "John", Pets = new List<Pet> { fluffy } };
                //                    session.Store(fluffy);
                //                    session.Store(john);
                //                    Console.WriteLine(john.Id);
                //                }
                //                session.SaveChanges();
                //            }
                //        }
                //    }
                //})
                //;
                //.Start();

                var peopleWithPetsSubscription = store.Subscriptions.Create<Person>();
                //using (var sub = store.Subscriptions.GetSubscriptionWorker<Person>(peopleWithPetsSubscription))
                using (var sub = store.Subscriptions.GetSubscriptionWorker<Person>(new SubscriptionWorkerOptions("5070")
                {

                    MaxDocsPerBatch = 10
                    ,
                    Strategy = SubscriptionOpeningStrategy.OpenIfFree,
                    TimeToWaitBeforeConnectionRetry = TimeSpan.FromSeconds(5)
                }
                ))
                {
                    var personIds = new BlockingCollection<string>();
                    sub.Run(batch =>
                     {
                         personIds.Add("asdasd");
                         using (var session = batch.OpenSession())
                         {
                             foreach (var item in batch.Items)
                             {
                                 var p = item.Result;
                                 p.Pets = null;
                                 Console.WriteLine($"Found {DateTime.Now.Ticks}");
                                 session.Store(p);
                                 Console.WriteLine($"Edited {DateTime.Now.Ticks}");
                             }
                             try
                             {
                                 session.SaveChanges();
                             }
                             catch (ConcurrencyException)
                             {
                                 return;
                             }
                         }
                     });

                    personIds.TryTake(out var key, TimeSpan.FromSeconds(5));

                    var guid = Console.ReadLine();
                    while (guid != "exit")
                    {
                        using (var session = store.OpenSession())
                        {
                            var person = session.Query<PersonVm, PersonWithPetsIndex>().Customize(x => x.WaitForNonStaleResults(TimeSpan.FromSeconds(2)))
                                .Where(x => x.Id == guid).FirstOrDefault();
                            Console.WriteLine($"Found person: {person != null}");
                            if (person != null)
                            {

                            }
                        }
                        guid = Console.ReadLine();
                    }
                }
            }
            */
        }
    }

    internal class Assert
    {
        internal static void Equal(string v, string key)
        {
        }

        internal static void Equal(int v, int age)
        {
        }

        internal static void True(bool v)
        {
        }
    }

    class PersonWithPetsIndex : AbstractIndexCreationTask<Person>
    {
        public PersonWithPetsIndex()
        {
            Map = persons => from person in persons
                             select new PersonVm
                             {
                                 Id = person.Id,
                                 Name = person.Name,
                                 PetNames = person.Pets.Select(x => x.Name).ToList()
                             };
        }
    }

    class PersonVm
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> PetNames { get; set; }
    }

    class Person
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<Pet> Pets { get; set; }
    }

    class Pet
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
