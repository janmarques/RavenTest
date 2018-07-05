using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Raven.Client;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Linq;

namespace PetsAndPeople
{
    class Person
    {
        public string Id { get; set; }
        public bool? Active { get; set; }
        public bool? Alive { get; set; }
    }

    class PersonVm
    {
        public string Id { get; set; }
        public bool? Active { get; set; }
        public bool? Alive { get; set; }
    }

    class PersonIndex : AbstractIndexCreationTask<Person, PersonVm>
    {
        public PersonIndex()
        {
            Map = persons => from person in persons
                             select new PersonVm
                             {
                                 Id = person.Id,
                                 Active = person.Active,
                                 Alive = person.Alive,
                             };
            StoresStrings.Add(Constants.Documents.Indexing.Fields.AllFields, FieldStorage.Yes);
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            var john = new Person { Id = Guid.NewGuid().ToString(), Active = true, Alive = true };

            using (var store = new DocumentStore
            {
                Urls = new[] { ConfigurationManager.AppSettings["RavenDbUrl"] },
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
                    var query1 = session.Query<PersonVm>("PersonIndex")
                        .Where(x => x.Active.Value && x.Alive.HasValue); // fails
                    //var query1 = session.Query<PersonVm>("PersonIndex")
                    //    .Where(x => x.Active.Value == true && x.Alive.HasValue == true); // passes
                    var result1 = query1.ToListAsync().Result;
                }
            }
        }
    }
}
