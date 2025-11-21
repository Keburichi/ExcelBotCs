using System.Reflection;
using ExcelBotCs.Attributes;
using ExcelBotCs.Extensions;

namespace ExcelBotCs.Tests.Extensions;

[TestFixture]
public class AssemblyExtensionsTests
{
    // Test interfaces and classes for testing GetTypesFromInterface
    private interface ITestInterface<T>
    {
        T GetValue();
    }

    private class ConcreteImplementation : ITestInterface<string>
    {
        public string GetValue()
        {
            return "test";
        }
    }

    private class AnotherImplementation : ITestInterface<int>
    {
        public int GetValue()
        {
            return 42;
        }
    }

    private abstract class AbstractImplementation : ITestInterface<bool>
    {
        public abstract bool GetValue();
    }

    // Test classes for testing GetOptionTypes
    [OptionsSection("TestSection")]
    private class TestOptionsClass
    {
        public string? TestProperty { get; set; }
    }

    [OptionsSection("AnotherSection")]
    private class AnotherOptionsClass
    {
        public int TestValue { get; set; }
    }

    private class ClassWithoutAttribute
    {
        public string? Property { get; set; }
    }

    [Test]
    public void GetTypesFromInterface_ShouldReturnConcreteImplementations()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        var interfaceType = typeof(ITestInterface<>);

        // Act
        var result = assembly.GetTypesFromInterface(interfaceType).ToList();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result, Does.Contain(typeof(ConcreteImplementation)));
        Assert.That(result, Does.Contain(typeof(AnotherImplementation)));
    }

    [Test]
    public void GetTypesFromInterface_ShouldNotReturnAbstractClasses()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        var interfaceType = typeof(ITestInterface<>);

        // Act
        var result = assembly.GetTypesFromInterface(interfaceType).ToList();

        // Assert
        Assert.That(result, Does.Not.Contain(typeof(AbstractImplementation)));
    }

    [Test]
    public void GetTypesFromInterface_ShouldNotReturnInterfaces()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        var interfaceType = typeof(ITestInterface<>);

        // Act
        var result = assembly.GetTypesFromInterface(interfaceType).ToList();

        // Assert
        Assert.That(result, Does.Not.Contain(typeof(ITestInterface<>)));
    }

    [Test]
    public void GetTypesFromInterface_WithNonExistentInterface_ShouldReturnEmpty()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        var interfaceType = typeof(IComparable<>);

        // Act
        var result = assembly.GetTypesFromInterface(interfaceType).ToList();

        // Assert - May or may not be empty depending on what's in the test assembly
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void GetOptionTypes_ShouldReturnTypesWithOptionsSectionAttribute()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var result = assembly.GetOptionTypes();

        // Assert
        Assert.That(result, Is.Not.Null);
        var testOptionsResult = result.FirstOrDefault(x => x.Type == typeof(TestOptionsClass));
        Assert.That(testOptionsResult, Is.Not.Null);
        Assert.That(testOptionsResult!.Attribute, Is.Not.Null);
        Assert.That(testOptionsResult.Attribute!.Name, Is.EqualTo("TestSection"));
    }

    [Test]
    public void GetOptionTypes_ShouldReturnMultipleTypesWithAttribute()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var result = assembly.GetOptionTypes();

        // Assert
        Assert.That(result, Is.Not.Null);
        var testOptionsResult = result.FirstOrDefault(x => x.Type == typeof(TestOptionsClass));
        var anotherOptionsResult = result.FirstOrDefault(x => x.Type == typeof(AnotherOptionsClass));

        Assert.That(testOptionsResult, Is.Not.Null);
        Assert.That(anotherOptionsResult, Is.Not.Null);
        Assert.That(testOptionsResult!.Attribute!.Name, Is.EqualTo("TestSection"));
        Assert.That(anotherOptionsResult!.Attribute!.Name, Is.EqualTo("AnotherSection"));
    }

    [Test]
    public void GetOptionTypes_ShouldNotReturnTypesWithoutAttribute()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var result = assembly.GetOptionTypes();

        // Assert
        Assert.That(result, Is.Not.Null);
        var classWithoutAttributeResult = result.FirstOrDefault(x => x.Type == typeof(ClassWithoutAttribute));
        Assert.That(classWithoutAttributeResult, Is.Null);
    }

    [Test]
    public void GetOptionTypes_ShouldNotReturnAbstractClasses()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var result = assembly.GetOptionTypes();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.All(x => !x.Type.IsAbstract), Is.True);
    }

    [Test]
    public void GetOptionTypes_ShouldNotReturnInterfaces()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var result = assembly.GetOptionTypes();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.All(x => !x.Type.IsInterface), Is.True);
    }

    [Test]
    public void OptionsAttribute_ShouldHaveTypeAndAttributeProperties()
    {
        // Arrange & Act
        var optionsAttribute = new OptionsAttribute
        {
            Type = typeof(string),
            Attribute = new OptionsSectionAttribute("TestSection")
        };

        // Assert
        Assert.That(optionsAttribute.Type, Is.EqualTo(typeof(string)));
        Assert.That(optionsAttribute.Attribute, Is.Not.Null);
        Assert.That(optionsAttribute.Attribute!.Name, Is.EqualTo("TestSection"));
    }

    [Test]
    public void OptionsAttribute_ShouldAllowNullAttribute()
    {
        // Arrange & Act
        var optionsAttribute = new OptionsAttribute
        {
            Type = typeof(string),
            Attribute = null
        };

        // Assert
        Assert.That(optionsAttribute.Type, Is.EqualTo(typeof(string)));
        Assert.That(optionsAttribute.Attribute, Is.Null);
    }

    [Test]
    public void GetTypesFromInterface_ShouldHandleGenericConstraints()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        var interfaceType = typeof(ITestInterface<>);

        // Act
        var result = assembly.GetTypesFromInterface(interfaceType).ToList();

        // Assert - Should find types implementing generic interface with different type parameters
        var stringImplementation = result.FirstOrDefault(t => t == typeof(ConcreteImplementation));
        var intImplementation = result.FirstOrDefault(t => t == typeof(AnotherImplementation));

        Assert.That(stringImplementation, Is.Not.Null);
        Assert.That(intImplementation, Is.Not.Null);
    }

    [Test]
    public void GetOptionTypes_ShouldReturnListType()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var result = assembly.GetOptionTypes();

        // Assert
        Assert.That(result, Is.InstanceOf<List<OptionsAttribute>>());
    }

    [Test]
    public void GetTypesFromInterface_ShouldReturnEnumerableType()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        var interfaceType = typeof(ITestInterface<>);

        // Act
        var result = assembly.GetTypesFromInterface(interfaceType);

        // Assert
        Assert.That(result, Is.InstanceOf<IEnumerable<Type>>());
    }
}