namespace OpsFlow.Api.Tests;

public class ApiProjectReferenceTests
{
    [Fact]
    public void Api_assembly_is_available_to_tests()
    {
        var assembly = typeof(Program).Assembly;

        Assert.Equal("OpsFlow.Api", assembly.GetName().Name);
    }
}
