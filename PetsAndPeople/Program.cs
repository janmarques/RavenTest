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
        static void Main(string[] args)
        {

            using (var store = new DocumentStore
            {
                Urls = new[] { "http://127.0.0.1:8080" },
                Database = "zoo",
            })
            {
                store.Initialize();

                //using (var session = store.OpenSession())
                //{
                //    var john = new Person { Id = Guid.NewGuid().ToString(), Name = "John", Pets = new List<Pet>() };
                //    for (int i = 0; i < 5; i++)
                //    {
                //        john.Pets.Add(new Pet { Id = Guid.NewGuid().ToString(), Name = "JohnsPet" + i });
                //        john.Pets.Add(new Pet { Id = Guid.NewGuid().ToString(), Name = "JohnsPet" + i });
                //    }
                //    session.Store(john);

                //    var steve = new Person { Id = Guid.NewGuid().ToString(), Name = "Steve", Pets = new List<Pet>() };
                //    for (int i = 0; i < 5; i++)
                //    {
                //        steve.Pets.Add(new Pet { Id = Guid.NewGuid().ToString(), Name = "StevesPet" + i });
                //        steve.Pets.Add(new Pet { Id = Guid.NewGuid().ToString(), Name = "StevesPet" + i });
                //    }
                //    session.Store(steve);
                //    session.SaveChanges();
                //}
                //store.ExecuteIndex(new PersonWithPetsIndex());

                using (var session = store.OpenSession())
                {
                    for (int i = 0; i < 20; i++)
                    {
                        var pets = session.Query<PetVm, PersonWithPetsIndex>().ProjectInto<PetVm>().Take(1).Skip(i)
                            //.Distinct()
                            .ToList();
                        foreach (var pet in pets)
                        {
                            Console.WriteLine($"{pet.PetName}");
                        }
                    }
                }
            }
        }
    }

    class PersonWithPetsIndex : AbstractIndexCreationTask<Person>
    {
        public PersonWithPetsIndex()
        {
            Map = persons => from person in persons
                             from pet in person.Pets
                             select new PetVm
                             {
                                 PersonName = person.Name,
                                 PetName = pet.Name
                             };
            StoreAllFields(FieldStorage.Yes);
        }
    }

    class PersonVm
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> PetNames { get; set; }
    }

    class PetVm
    {
        public string PersonName { get; set; }
        public string PetName { get; set; }
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
