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
        public string Name { get; set; }
        public string FirstName { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var people = new List<Person>();
            for (int i = 0; i < 100_000; i++)
            {
                people.Add(new Person
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = Guid.NewGuid().ToString(),
                    FirstName = Guid.NewGuid().ToString()
                });
            }

            using (var store = new DocumentStore
            {
                Urls = new[] { ConfigurationManager.AppSettings["RavenDbUrl"] },
                Database = "zoo"
            })
            {
                store.Initialize();

                using (var bulkinsert = store.BulkInsert())
                {
                    people.ForEach(x => bulkinsert.Store(x));

                    //var tasks = people.Select(x => bulkinsert.StoreAsync(x)).ToArray();
                    //Task.WaitAll(tasks);
                }
            }
        }
    }
}
