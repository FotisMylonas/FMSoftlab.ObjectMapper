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
                cfg.MapCustom(dest => dest.FullName, src => src.Name+" "+src.LastName);
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
                cfg.MapCustom(dest => dest.LASTNAME, src => src.LastName.ToUpper());
            });
            Person4 p4 = new Person4 { Name = "fotis", LastName = "mylonas" };
            Person5 p5 = ObjectMapper.Map<Person4, Person5>(p4);
            Assert.Equal(p4.Name, p5.NAME);
            Assert.Equal(p4.LastName.ToUpper(), p5.LASTNAME);
        }

        [Fact]
        public void Test_ChildPropertyMapping_List_Or_IEnumerable()
        {
            ObjectMapper.Register<Telephone4, Telephone5>(cfg => { cfg.MapCustom(dest => dest.TelephoneNumber, src => src.Number); });
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
            ObjectMapper.Register<Telephone4, Telephone5>(cfg => { cfg.MapCustom(dest => dest.TelephoneNumber, src => src.Number); });
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

    }
}