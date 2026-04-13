using FluentAssertions;
using Xunit;

namespace WmsAi.ArchitectureTests;

public class SolutionBootstrapTests
{
    [Theory]
    [InlineData("src/AppHost/WmsAi.AppHost/Program.cs")]
    [InlineData("src/ServiceDefaults/WmsAi.ServiceDefaults/Extensions.cs")]
    [InlineData("src/Gateway/WmsAi.Gateway.Host/WmsAi.Gateway.Host.csproj")]
    [InlineData("web/wms-ai-web/package.json")]
    public void Required_bootstrap_files_should_exist(string relativePath)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

        File.Exists(Path.Combine(root, relativePath)).Should().BeTrue();
    }
}
