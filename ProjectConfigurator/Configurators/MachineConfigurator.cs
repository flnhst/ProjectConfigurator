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
using Microsoft.Extensions.Logging;
using ProjectConfigurator.Abstractions;
using ProjectConfigurator.Exceptions;
using ProjectConfigurator.Models;

namespace ProjectConfigurator.Configurators;

public class MachineConfigurator(IServiceProvider serviceProvider, ILogger<MachineConfigurator> logger)
    : IMachineConfigurator
{
    public async Task ConfigureMachineAsync(MachineConfiguration machineConfiguration,
        CancellationToken cancellationToken = default)
    {
        if (machineConfiguration.Projects == null)
        {
            throw new ConfiguratorException("Could not find any projects to configure.");
        }

        foreach (var project in machineConfiguration.Projects)
        {
            if (project.Configurations != null)
            {
                foreach (var projectConfiguration in project.Configurations)
                {
                    await ConfigureProjectAsync(machineConfiguration, project, projectConfiguration, cancellationToken);
                }
            }
        }
    }

    private async Task ConfigureProjectAsync(MachineConfiguration machineConfiguration, Project project,
        ProjectConfiguration projectConfiguration, CancellationToken cancellationToken = default)
    {
        if (projectConfiguration.Kind == null)
        {
            throw new ConfiguratorException("Missing data for project configuration.");
        }

        await using var scope = serviceProvider.CreateAsyncScope();

        var configurator = InstantiateConfigurator(projectConfiguration.Kind.Value);

        try
        {
            await configurator.ConfigureProjectConfigurationAsync(machineConfiguration, project, projectConfiguration,
                cancellationToken);
        }
        catch (ConfiguratorException e)
        {
            logger.LogWarning(e, "Failed to configure project configuration '{ProjectConfigurationName}'.",
                projectConfiguration.Name);
        }
        finally
        {
            switch (configurator)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync();
                    break;
                // ReSharper disable once SuspiciousTypeConversion.Global
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }
    }

    private IConfigurator InstantiateConfigurator(ProjectConfigurationKind projectConfigurationKind) =>
        projectConfigurationKind switch
        {
            ProjectConfigurationKind.LaunchSettingsJson => ActivatorUtilities
                .CreateInstance<LaunchSettingsJsonConfigurator>(serviceProvider),
            ProjectConfigurationKind.IntelliJIdeaConfiguration => ActivatorUtilities
                .CreateInstance<IntelliJIdeaConfigurator>(serviceProvider),
            ProjectConfigurationKind.AppSettingsJson => ActivatorUtilities
                .CreateInstance<AppSettingsJsonConfigurator>(serviceProvider),
            _ => throw new ArgumentOutOfRangeException(nameof(projectConfigurationKind), projectConfigurationKind, null)
        };
}