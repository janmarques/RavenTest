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
        public string Name { get; set; }
        public Identity ExtraId { get; set; }
    }

    class Identity
    {
        public Guid Guid { get; set; }
    }

    class PersonVM
    {
        public string Name { get; set; }
        public IdentityVM ExtraId { get; set; }
    }

    class IdentityVM
    {
        public Guid Guid { get; set; }
    }


    class PersonIndex : AbstractIndexCreationTask<Person>
    {
        public PersonIndex()
        {
            Map = persons => from person in persons
                             select new PersonVM
                             {
                                 Name = person.Name,
                                 ExtraId = new IdentityVM
                                 {
                                     Guid = person.ExtraId.Guid
                                 }
                             };
            StoresStrings.Add(Constants.Documents.Indexing.Fields.AllFields, FieldStorage.Yes);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var john = new Person { ExtraId = new Identity { Guid = Guid.NewGuid() }, Name = "john" };
            var jeff = new Person { ExtraId = null, Name = "jeff" };

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
                    session.Store(jeff);
                    session.SaveChanges();
                }

                using (var session = store.OpenAsyncSession())
                {
                    var query1 = session.Query<PersonVM>("PersonIndex");//.ProjectInto<PersonVM>();
                    var result1 = query1.ToListAsync().Result;
                }
            }
        }
    }
}
