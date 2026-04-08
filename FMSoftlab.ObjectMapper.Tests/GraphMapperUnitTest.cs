using System;
using System.Collections.Generic;
using System.Text;

namespace FMSoftlab.ObjectMapper.Tests
{

    // ── Domain models ────────────────────────────────────────────────────────────

    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Order> Orders { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public string Reference { get; set; }
        public List<Item> Items { get; set; }
    }

    public class Item
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    public class GraphMapperTests
    {
        // xUnit instantiates the class per test, so fields act like [SetUp]
        private readonly List<Person> _persons;
        private readonly List<Order> _orders;
        private readonly List<Item> _items;
        private readonly GraphMap<Person> _map;

        public GraphMapperTests()
        {
            _persons = new List<Person>
        {
            new Person { Id = 1, Name = "Alice" },
            new Person { Id = 2, Name = "Bob" },
        };

            _orders = new List<Order>
        {
            new Order { Id = 10, PersonId = 1, Reference = "ORD-10" },
            new Order { Id = 11, PersonId = 1, Reference = "ORD-11" },
            new Order { Id = 12, PersonId = 2, Reference = "ORD-12" },
        };

            _items = new List<Item>
        {
            new Item { Id = 100, OrderId = 10, Name = "Widget",      Price = 9.99m  },
            new Item { Id = 101, OrderId = 10, Name = "Gadget",      Price = 19.99m },
            new Item { Id = 102, OrderId = 11, Name = "Doohickey",   Price = 4.49m  },
            new Item { Id = 103, OrderId = 12, Name = "Thingamajig", Price = 14.00m },
        };

            _map = GraphMap<Person>.For()
                .HasMany<Order, int>(
                    property: p => p.Orders,
                    parentKey: p => p.Id,
                    childKey: o => o.PersonId,
                    build: orderMap => orderMap
                        .HasMany<Item, int>(
                            property: o => o.Items,
                            parentKey: o => o.Id,
                            childKey: i => i.OrderId
                        )
                );
        }

        private IDictionary<Type, object> BuildData() =>
            new Dictionary<Type, object>
            {
            { typeof(Order), _orders },
            { typeof(Item),  _items  },
            };

        // ── Basic wiring ─────────────────────────────────────────────────────────

        [Fact]
        public void Map_ReturnsAllRootPersons()
        {
            var result = GraphMapper.Map(_persons, BuildData(), _map);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Map_AssignsCorrectOrdersToEachPerson()
        {
            var result = GraphMapper.Map(_persons, BuildData(), _map);

            var alice = result.Find(p => p.Name == "Alice");
            var bob = result.Find(p => p.Name == "Bob");

            Assert.Equal(2, alice.Orders.Count);
            Assert.Single(bob.Orders);
        }

        [Fact]
        public void Map_OrderReferencesAreCorrectForAlice()
        {
            var result = GraphMapper.Map(_persons, BuildData(), _map);

            var alice = result.Find(p => p.Name == "Alice");
            var refs = alice.Orders.ConvertAll(o => o.Reference);

            Assert.Contains("ORD-10", refs);
            Assert.Contains("ORD-11", refs);
            Assert.Equal(2, refs.Count);
        }

        // ── Nested item wiring ───────────────────────────────────────────────────

        [Fact]
        public void Map_AssignsCorrectItemsToOrders()
        {
            var result = GraphMapper.Map(_persons, BuildData(), _map);

            var alice = result.Find(p => p.Name == "Alice");
            var ord10 = alice.Orders.Find(o => o.Reference == "ORD-10");
            var ord11 = alice.Orders.Find(o => o.Reference == "ORD-11");

            Assert.Equal(2, ord10.Items.Count);
            Assert.Equal(1, ord11.Items.Count);
        }

        [Fact]
        public void Map_ItemNamesAreCorrectForOrder10()
        {
            var result = GraphMapper.Map(_persons, BuildData(), _map);

            var alice = result.Find(p => p.Name == "Alice");
            var ord10 = alice.Orders.Find(o => o.Reference == "ORD-10");
            var names = ord10.Items.ConvertAll(i => i.Name);

            Assert.Contains("Widget", names);
            Assert.Contains("Gadget", names);
            Assert.Equal(2, names.Count);
        }

        [Fact]
        public void Map_ItemPricesAreCorrect()
        {
            var result = GraphMapper.Map(_persons, BuildData(), _map);

            var alice = result.Find(p => p.Name == "Alice");
            var ord10 = alice.Orders.Find(o => o.Reference == "ORD-10");
            var widget = ord10.Items.Find(i => i.Name == "Widget");

            Assert.Equal(9.99m, widget.Price);
        }

        // ── Edge cases ───────────────────────────────────────────────────────────

        [Fact]
        public void Map_PersonWithNoOrders_GetsEmptyList()
        {
            _persons.Add(new Person { Id = 99, Name = "Charlie" });

            var result = GraphMapper.Map(_persons, BuildData(), _map);
            var charlie = result.Find(p => p.Name == "Charlie");

            Assert.NotNull(charlie.Orders);
            Assert.Empty(charlie.Orders);
        }

        [Fact]
        public void Map_OrderWithNoItems_GetsEmptyList()
        {
            _orders.Add(new Order { Id = 99, PersonId = 2, Reference = "ORD-99" });

            var result = GraphMapper.Map(_persons, BuildData(), _map);
            var bob = result.Find(p => p.Name == "Bob");
            var empty = bob.Orders.Find(o => o.Reference == "ORD-99");

            Assert.NotNull(empty.Items);
            Assert.Empty(empty.Items);
        }

        [Fact]
        public void Map_EmptyRootList_ReturnsEmptyResult()
        {
            var result = GraphMapper.Map(new List<Person>(), BuildData(), _map);

            Assert.Empty(result);
        }

        [Fact]
        public void Map_MissingDataEntry_ThrowsInvalidOperationException()
        {
            var incompleteData = new Dictionary<Type, object>
        {
            { typeof(Order), _orders }
            // typeof(Item) intentionally missing
        };

            Assert.Throws<InvalidOperationException>(() =>
                GraphMapper.Map(_persons, incompleteData, _map));
        }
    }

}
