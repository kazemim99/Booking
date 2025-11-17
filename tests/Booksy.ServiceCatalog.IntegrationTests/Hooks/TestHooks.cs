using Booksy.ServiceCatalog.Api;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using Booksy.ServiceCatalog.IntegrationTests.Support;
using Reqnroll;
using Reqnroll.BoDi;

namespace Booksy.ServiceCatalog.IntegrationTests.Hooks;

/// <summary>
/// Hooks for test initialization, cleanup, and context setup
/// </summary>
[Binding]
public class TestHooks
{
    private readonly ServiceCatalogReqnrollTestBase _testBase;
    private readonly ScenarioContext _scenarioContext;

    public TestHooks(
        ServiceCatalogReqnrollTestBase testBase,
        ScenarioContext scenarioContext)
    {
        _testBase = testBase;
        _scenarioContext = scenarioContext;
    }

    [BeforeTestRun(Order = 0)]
    public static void RegisterDependencies(IObjectContainer container)
    {
        // Register factory for ServiceCatalogIntegrationTestBase
        // This allows step definitions to inject the abstract base class
        container.RegisterFactoryAs<ServiceCatalogIntegrationTestBase>(() =>
        {
            var factory = container.Resolve<ServiceCatalogTestWebApplicationFactory<Startup>>();
            return new ServiceCatalogReqnrollTestBase(factory);
        });
    }

    [BeforeScenario(Order = 100)]
    public async Task BeforeScenario()
    {
        // Initialize test base (database, client, etc.)
        await _testBase.InitializeAsync();

        // Set up scenario context with helper
        _scenarioContext.Set(new ScenarioContextHelper(_scenarioContext), "Helper");

        // Log scenario start
        Console.WriteLine($"Starting scenario: {_scenarioContext.ScenarioInfo.Title}");
    }

    [AfterScenario]
    public async Task AfterScenario()
    {
        // Clean up test data
        await _testBase.DisposeAsync();

        // Log scenario end
        var status = _scenarioContext.TestError == null ? "PASSED" : "FAILED";
        Console.WriteLine($"Finished scenario: {_scenarioContext.ScenarioInfo.Title} - {status}");

        // Log error if test failed
        if (_scenarioContext.TestError != null)
        {
            Console.WriteLine($"Error: {_scenarioContext.TestError.Message}");
            Console.WriteLine($"Stack trace: {_scenarioContext.TestError.StackTrace}");
        }
    }

    [BeforeStep]
    public void BeforeStep()
    {
        // Log step execution
        Console.WriteLine($"  Executing: {_scenarioContext.StepContext.StepInfo.Text}");
    }

    [AfterStep]
    public void AfterStep()
    {
        // Log step completion with timing if available
        var stepInfo = _scenarioContext.StepContext.StepInfo;
        Console.WriteLine($"  Completed: {stepInfo.Text}");
    }
}
