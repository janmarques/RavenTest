using System;
using System.Linq;
using System.Threading;
using Raven.Client;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Linq;

namespace PetsAndPeople
{
    class Person
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    class PersonVM
    {
        public string Id { get; set; }
        public string ProjectedName { get; set; }
    }

    class PersonIndex : AbstractIndexCreationTask<Person>
    {
        public PersonIndex()
        {
            Map = persons => from person in persons
                             select new PersonVM { Id = person.Id, ProjectedName = person.Name };
            StoresStrings.Add(Constants.Documents.Indexing.Fields.AllFields, FieldStorage.Yes);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var john = new Person { Id = Guid.NewGuid().ToString(), Name = "john" };

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

                using (var session = store.OpenAsyncSession())
                {
                    var query1 = session.Advanced.AsyncRawQuery<PersonVM>(@"from index 'PersonIndex'");
                    var result1 = query1.ToListAsync().Result;
                    Console.WriteLine(result1.First().ProjectedName); // null

                    var query2 = session.Advanced.AsyncRawQuery<PersonVM>($@"from index 'PersonIndex' select {Constants.Documents.Indexing.Fields.AllStoredFields}");
                    var result2 = query2.ToListAsync().Result;
                    Console.WriteLine(result2.First().ProjectedName); // John
                }
            }
        }
    }
}
