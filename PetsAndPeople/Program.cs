using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Linq;

namespace PetsAndPeople
{
    class Program
    {
        //README First create the zoo database in Raven (4.0.0-rc-40023), then run the code below
        static void Main(string[] args)
        {
            var fluffy = new Pet { Name = "Fluffy" };
            var john = new Person("John", new List<Pet> { fluffy });

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

                // This query execution crashes
                using (var session = store.OpenSession())
                {
                    var allPets = new List<Pet> { fluffy };
                    var allPetsQueryable = allPets.Select(x => x.Name).ToList();
                    var query = session.Query<Person>().Where(p => p.PetsQueryable.ContainsAny(allPetsQueryable));
                    var result = query.ToList();
                }

                // This query execution crashes as well
                using (var session = store.OpenSession())
                {
                    var query = session.Query<Person>().Where(p => fluffy.In(p.Pets));
                    var result = query.ToList();
                }
            }
        }
    }

    class PersonIndex : AbstractIndexCreationTask<Person>
    {
        public PersonIndex()
        {
            Map = persons => from person in persons
                             select new { person.Name, person.Pets };
        }
    }
    class Person
    {
        public string Name { get; set; }
        public List<Pet> Pets { get; set; }
        public List<string> PetsQueryable { get; set; }
        public Person(string name, List<Pet> pets)
        {
            Name = name;
            Pets = pets;
            PetsQueryable = Pets.Select(x => x.Name).ToList();
        }
    }

    class Pet
    {
        public string Name { get; set; }
    }
}
