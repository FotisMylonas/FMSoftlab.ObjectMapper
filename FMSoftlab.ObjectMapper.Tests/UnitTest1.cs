namespace FMSoftlab.ObjectMapper.Tests
{
    public class Person1
    {
        public string Name { get; set; } = string.Empty;
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

    public class Person4
    {
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    public class Person5
    {
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    public class TestMappings
    {

        [Fact]
        public void Test_DefaultMapping()
        {
            ObjectMapper.Register<Person1, Person2>();
            Person1 p1 = new Person1 { Name = "fotis", LastName = "mylonas" };
            Person2 p2 = ObjectMapper.Map<Person1, Person2>(p1);
            Assert.Equal(p1.Name, p2.Name);
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
        public void Test_CustomMapping_Overrides_Default()
        {
            ObjectMapper.Register<Person4, Person5>(cfg =>
            {
                cfg.MapCustom(dest => dest.LastName, src => src.LastName.ToUpper());
            });
            Person4 p4 = new Person4 { Name = "fotis", LastName = "mylonas" };
            Person5 p5 = ObjectMapper.Map<Person4, Person5>(p4);
            Assert.Equal(p4.Name, p5.Name);
            Assert.Equal(p4.LastName.ToUpper(), p5.LastName);
        }
    }
}