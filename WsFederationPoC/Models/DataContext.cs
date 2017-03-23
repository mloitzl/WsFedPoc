using System.Collections.Generic;

namespace WsFederationPoC.Models
{
    public class DataContext
    {
        public static IEnumerable<Item> GetItems()
        {
            return new[]
            {
                new Item {Id = 1, Name = "First"},
                new Item {Id = 2, Name = "Second"},
                new Item {Id = 3, Name = "Third"},
                new Item {Id = 4, Name = "Fourth"},
                new Item {Id = 5, Name = "Fifth"},
                new Item {Id = 6, Name = "Sixth"}
            };
        }
    }
}