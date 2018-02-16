using System;
using System.Linq;
using System.Threading;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Linq;

namespace PetsAndPeople
{
    class Person
    {
        public string Id { get; set; }
        public Pet Pet { get; set; }
    }

    class Pet
    {
        public string Name { get; set; }
    }

    class PersonWithPet
    {
        public string PersonId { get; set; }
        public string PetName { get; set; }
    }

    class PersonIndex : AbstractIndexCreationTask<Person>
    {
        public PersonIndex()
        {
            Map = persons => from person in persons
                             select new PersonWithPet { PersonId = person.Id, PetName = person.Pet.Name };
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var john = new Person { Id = Guid.NewGuid().ToString(), Pet = null };
            var jason = new Person { Id = Guid.NewGuid().ToString(), Pet = new Pet { Name = "Pastice" } };

            using (var store = new DocumentStore
            {
                Urls = new[] { "http://localhost:8080/" },
                Database = "zoo"
            })
            {
                store.Initialize();

                new PersonIndex().Execute(store);
                Thread.Sleep(5000); // wait for index
                using (var session = store.OpenSession())
                {
                    session.Store(john);
                    session.SaveChanges();
                }

                using (var session = store.OpenSession())
                {
                    var query1 = session.Query<Person, PersonIndex>().ProjectInto<PersonWithPet>().Where(p => p.PetName == null);
                    var result1 = query1.ToList().Count();
                    Console.WriteLine("Comparison serverside: " + result1); //0

                    var query2 = session.Query<Person, PersonIndex>().ProjectInto<PersonWithPet>();
                    var result2 = query2.ToList().Where(p => p.PetName == null).Count();
                    Console.WriteLine("Comparison clientside: " + result2); //1
                }
            }
        }
    }
}
