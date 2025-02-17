#region License

// ProjectConfigurator, help configure your projects.
// Copyright (C)  2025  Florian Hester
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectConfigurator;
using ProjectConfigurator.Abstractions;
using ProjectConfigurator.Configurators;
using ProjectConfigurator.Generators;
using ProjectConfigurator.Readers;

var builder = new HostApplicationBuilder();

builder.Services.AddLogging(options => { options.AddConsole(); });

builder.Services
    .AddSingleton<IMachineConfigurationReader, MachineConfigurationReader>()
    .AddSingleton<IMachineConfigurator, MachineConfigurator>()
    .AddSingleton<IProjectEnvironmentVariableGenerator, ProjectEnvironmentVariableGenerator>();

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var configurationReader = host.Services.GetRequiredService<IMachineConfigurationReader>();
var machineConfigurator = host.Services.GetRequiredService<IMachineConfigurator>();

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    try
    {
        // ReSharper disable once AccessToDisposedClosure
        cts.Cancel();
        e.Cancel = true;
    }
    catch (Exception)
    {
        // ignored
    }
};

License.LogShortLicense(logger);

if (args.Contains("--license"))
{
    Console.WriteLine();
    Console.WriteLine(License.LicenseText);

    return;
}

await host.StartAsync();

logger.LogInformation("Started application.");

try
{
    var machineConfiguration = await configurationReader.ReadMachineConfigurationAsync(cts.Token);

    await machineConfigurator.ConfigureMachineAsync(machineConfiguration, cts.Token);
}
catch (Exception e)
{
    logger.LogError(e, "Exception thrown: '{ExceptionMessage}'", e.Message);
}

logger.LogInformation("Stopping application...");

await host.StopAsync();

logger.LogInformation("Stopped application.");