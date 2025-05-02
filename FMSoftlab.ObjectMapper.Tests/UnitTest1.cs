namespace FMSoftlab.ObjectMapper.Tests
{
    public class Person1
    {
        public string NAME { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    public class Person2
    {
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
    public class Person3
    {
        public string FullName { get; set; } = string.Empty;
    }

    public class Address4
    {
        public string Address { get; set; } = string.Empty;

    }
    public class Address5
    {
        public string Address { get; set; } = string.Empty;

    }

    public class Telephone4
    {
        public string Number { get; set; } = string.Empty;
    }

    public class Telephone5
    {
        public string TelephoneNumber { get; set; } = string.Empty;
    }

    public class Person4
    {
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public IEnumerable<Telephone4> Telephones { get; set; } = Enumerable.Empty<Telephone4>();
        public List<Address4> Addresses { get; set; } = new List<Address4>();
    }


    public class Person5
    {
        public string NAME { get; set; } = string.Empty;
        public string LASTNAME { get; set; } = string.Empty;
        public IEnumerable<Telephone5> Telephones { get; set; } = Enumerable.Empty<Telephone5>();
        public List<Address5> Addresses { get; set; } = new List<Address5>();
    }

    public class Person7
    {
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public Address4 Address { get; } = new Address4();
        public Telephone4 Telephone { get; } = new Telephone4();
    }

    public class Person8
    {
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public Address5 Address { get; } = new Address5();
        public Telephone5 Telephone { get; set; } = new Telephone5();
    }

    public class ItemSource { public string Name { get; set; } = ""; }
    public class ItemTarget { public string Name { get; set; } = ""; }

    public class OrderSource { public List<ItemSource> Items { get; set; } = new(); }
    public class OrderTarget { public List<ItemTarget> Items { get; set; } = new(); }

    public class PersonSource { public OrderSource Order { get; set; } = new(); }
    public class PersonTarget { public OrderTarget Order { get; set; } = new(); }

    public class ItemA { public string Name { get; set; } = ""; }
    public class ItemB { public string Name { get; set; } = ""; }

    public class OrderA { public List<ItemA> Items { get; set; } = new(); }
    public class OrderB { public List<ItemB> Items { get; set; } = new(); }

    public class PersonA { public List<OrderA> Orders { get; set; } = new(); }
    public class PersonB { public List<OrderB> Orders { get; set; } = new(); }


    public class TestMappings
    {

        [Fact]
        public void Test_DefaultMapping_CaseInsensitive()
        {
            ObjectMapper.Register<Person1, Person2>();
            Person1 p1 = new Person1 { NAME = "fotis", LastName = "mylonas" };
            Person2 p2 = ObjectMapper.Map<Person1, Person2>(p1);
            Assert.Equal(p1.NAME, p2.Name);
            Assert.Equal(p1.LastName, p2.LastName);
        }

        [Fact]
        public void Test_CustomMapping()
        {
            ObjectMapper.Register<Person2, Person3>(cfg =>
            {
                cfg.MapCustom(src => src.Name+" "+src.LastName, dest => dest.FullName);
            });
            Person2 p2 = new Person2 { Name = "fotis", LastName = "mylonas" };
            Person3 p3 = ObjectMapper.Map<Person2, Person3>(p2);
            Assert.Equal("fotis mylonas", p3.FullName);
        }

        [Fact]
        public void Test_CustomMapping_Overrides_Default_CaseInsensitive()
        {
            ObjectMapper.Register<Person4, Person5>(cfg =>
            {
                cfg.MapCustom(src => src.LastName.ToUpper(), dest => dest.LASTNAME);
            });
            Person4 p4 = new Person4 { Name = "fotis", LastName = "mylonas" };
            Person5 p5 = ObjectMapper.Map<Person4, Person5>(p4);
            Assert.Equal(p4.Name, p5.NAME);
            Assert.Equal(p4.LastName.ToUpper(), p5.LASTNAME);
        }

        [Fact]
        public void Test_ChildPropertyMapping_List_Or_IEnumerable()
        {
            ObjectMapper.Register<Telephone4, Telephone5>(cfg => { cfg.MapCustom(src => src.Number, dest => dest.TelephoneNumber); });
            ObjectMapper.Register<Address4, Address5>();
            ObjectMapper.Register<Person4, Person5>();
            Person4 p4 = new Person4
            {
                Name = "fotis",
                LastName = "mylonas",
                Addresses={ new Address4 { Address="my address" } },
                Telephones = new List<Telephone4> { new Telephone4 { Number = "123456789" } }
            };
            Person5 p5 = ObjectMapper.Map<Person4, Person5>(p4);
            Assert.Equal(p4.Name, p5.NAME);
            Assert.Equal(p4.LastName, p5.LASTNAME);
            Assert.Equal(p4.Addresses[0].Address, p5.Addresses[0].Address);
            Assert.Equal(p4.Telephones.First().Number, p5.Telephones.First().TelephoneNumber);
        }

        [Fact]
        public void Test_Map_Properties_That_Are_Complex_Objects()
        {
            ObjectMapper.Register<Telephone4, Telephone5>(cfg => { cfg.MapCustom(src => src.Number, dest => dest.TelephoneNumber); });
            ObjectMapper.Register<Address4, Address5>();
            ObjectMapper.Register<Person7, Person8>();
            Person7 p7 = new Person7
            {
                Name = "fotis",
                LastName = "mylonas"
            };
            p7.Address.Address = "my address";
            p7.Telephone.Number = "123456789";
            Person8 p8 = ObjectMapper.Map<Person7, Person8>(p7);
            Assert.Equal(p7.Name, p8.Name);
            Assert.Equal(p7.LastName, p8.LastName);
            Assert.Equal(p7.Address.Address, p8.Address.Address);
            Assert.Equal(p7.Telephone.Number, p8.Telephone.TelephoneNumber);
        }

        [Fact]
        public void Test_Nested_Mapping()
        {
            ObjectMapper.Register<ItemSource, ItemTarget>();
            ObjectMapper.Register<OrderSource, OrderTarget>();
            ObjectMapper.Register<PersonSource, PersonTarget>();

            // Map
            var personSource = new PersonSource
            {
                Order = new OrderSource
                {
                    Items = new List<ItemSource> { new() { Name = "Test" } }
                }
            };

            var personTarget = ObjectMapper.Map<PersonSource, PersonTarget>(personSource);
            Assert.Equal("Test", personTarget.Order.Items[0].Name);
        }

        [Fact]
        public void Test_Nested_Mapping_2()
        {

            ObjectMapper.Register<ItemA, ItemB>();
            ObjectMapper.Register<OrderA, OrderB>();
            ObjectMapper.Register<PersonA, PersonB>();

            var personA = new PersonA
            {
                Orders = new List<OrderA>
    {
        new() { Items = new List<ItemA> { new() { Name = "Item 1" } } }
    }
            };

            var personB = ObjectMapper.Map<PersonA, PersonB>(personA);
            Assert.Equal("Item 1", personB.Orders[0].Items[0].Name);
        }
    }
}