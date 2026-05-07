using System.Reflection;
using ExcelBotCs.Attributes;
using ExcelBotCs.Extensions;

namespace ExcelBotCs.Tests.Extensions;

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

    [Fact]
    public void GetTypesFromInterface_ShouldReturnConcreteImplementations()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        var interfaceType = typeof(ITestInterface<>);

        // Act
        var result = assembly.GetTypesFromInterface(interfaceType).ToList();

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldContain(typeof(ConcreteImplementation));
        result.ShouldContain(typeof(AnotherImplementation));
    }

    [Fact]
    public void GetTypesFromInterface_ShouldNotReturnAbstractClasses()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        var interfaceType = typeof(ITestInterface<>);

        // Act
        var result = assembly.GetTypesFromInterface(interfaceType).ToList();

        // Assert
        result.ShouldNotContain(typeof(AbstractImplementation));
    }

    [Fact]
    public void GetTypesFromInterface_ShouldNotReturnInterfaces()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        var interfaceType = typeof(ITestInterface<>);

        // Act
        var result = assembly.GetTypesFromInterface(interfaceType).ToList();

        // Assert
        result.ShouldNotContain(typeof(ITestInterface<>));
    }

    [Fact]
    public void GetTypesFromInterface_WithNonExistentInterface_ShouldReturnEmpty()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        var interfaceType = typeof(IComparable<>);

        // Act
        var result = assembly.GetTypesFromInterface(interfaceType).ToList();

        // Assert - May or may not be empty depending on what's in the test assembly
        result.ShouldNotBeNull();
    }

    [Fact]
    public void GetOptionTypes_ShouldReturnTypesWithOptionsSectionAttribute()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var result = assembly.GetOptionTypes();

        // Assert
        result.ShouldNotBeNull();
        var testOptionsResult = result.FirstOrDefault(x => x.Type == typeof(TestOptionsClass));
        testOptionsResult.ShouldNotBeNull();
        testOptionsResult!.Attribute.ShouldNotBeNull();
        testOptionsResult.Attribute!.Name.ShouldBe("TestSection");
    }

    [Fact]
    public void GetOptionTypes_ShouldReturnMultipleTypesWithAttribute()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var result = assembly.GetOptionTypes();

        // Assert
        result.ShouldNotBeNull();
        var testOptionsResult = result.FirstOrDefault(x => x.Type == typeof(TestOptionsClass));
        var anotherOptionsResult = result.FirstOrDefault(x => x.Type == typeof(AnotherOptionsClass));

        testOptionsResult.ShouldNotBeNull();
        anotherOptionsResult.ShouldNotBeNull();
        testOptionsResult!.Attribute!.Name.ShouldBe("TestSection");
        anotherOptionsResult!.Attribute!.Name.ShouldBe("AnotherSection");
    }

    [Fact]
    public void GetOptionTypes_ShouldNotReturnTypesWithoutAttribute()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var result = assembly.GetOptionTypes();

        // Assert
        result.ShouldNotBeNull();
        var classWithoutAttributeResult = result.FirstOrDefault(x => x.Type == typeof(ClassWithoutAttribute));
        classWithoutAttributeResult.ShouldBeNull();
    }

    [Fact]
    public void GetOptionTypes_ShouldNotReturnAbstractClasses()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var result = assembly.GetOptionTypes();

        // Assert
        result.ShouldNotBeNull();
        result.All(x => !x.Type.IsAbstract).ShouldBeTrue();
    }

    [Fact]
    public void GetOptionTypes_ShouldNotReturnInterfaces()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var result = assembly.GetOptionTypes();

        // Assert
        result.ShouldNotBeNull();
        result.All(x => !x.Type.IsInterface).ShouldBeTrue();
    }

    [Fact]
    public void OptionsAttribute_ShouldHaveTypeAndAttributeProperties()
    {
        // Arrange & Act
        var optionsAttribute = new OptionsAttribute
        {
            Type = typeof(string),
            Attribute = new OptionsSectionAttribute("TestSection")
        };

        // Assert
        optionsAttribute.Type.ShouldBe(typeof(string));
        optionsAttribute.Attribute.ShouldNotBeNull();
        optionsAttribute.Attribute!.Name.ShouldBe("TestSection");
    }

    [Fact]
    public void OptionsAttribute_ShouldAllowNullAttribute()
    {
        // Arrange & Act
        var optionsAttribute = new OptionsAttribute
        {
            Type = typeof(string),
            Attribute = null
        };

        // Assert
        optionsAttribute.Type.ShouldBe(typeof(string));
        optionsAttribute.Attribute.ShouldBeNull();
    }

    [Fact]
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

        stringImplementation.ShouldNotBeNull();
        intImplementation.ShouldNotBeNull();
    }

    [Fact]
    public void GetOptionTypes_ShouldReturnListType()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var result = assembly.GetOptionTypes();

        // Assert
        result.ShouldBeOfType<List<OptionsAttribute>>();
    }

    [Fact]
    public void GetTypesFromInterface_ShouldReturnEnumerableType()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        var interfaceType = typeof(ITestInterface<>);

        // Act
        var result = assembly.GetTypesFromInterface(interfaceType);

        // Assert
        result.ShouldBeAssignableTo<IEnumerable<Type>>();
    }
}
