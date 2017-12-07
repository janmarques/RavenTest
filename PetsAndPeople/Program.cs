using System.Collections.Generic;
using System.Linq;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;

namespace PetsAndPeople
{
    class Program
    {
        //README First create the zoo database in Raven (4.0.0-rc-40023), then run the code below
        static void Main(string[] args)
        {
            var fluffy = new Pet { Name = "Fluffy" };
            var john = new Person { Name = "John", Pets = new List<Pet> { fluffy } };

            using (var store = new DocumentStore
            {
                Urls = new[] { "http://localhost:8080/" },
                Database = "zoo"
            })
            {
                store.Initialize();
                using (var session = store.OpenSession())
                {
                    session.Store(john);
                    session.SaveChanges();
                }

                // This query execution crashes
                using (var session = store.OpenSession())
                {
                    var allPets = new List<Pet> { fluffy };
                    var query = session.Query<Person>().Where(p => p.Pets.ContainsAny(allPets));
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

    class Person
    {
        public string Name { get; set; }
        public List<Pet> Pets { get; set; }
    }

    class Pet
    {
        public string Name { get; set; }
    }
}
