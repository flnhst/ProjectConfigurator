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

using Microsoft.Extensions.Logging;
using ProjectConfigurator.Abstractions;
using ProjectConfigurator.Exceptions;
using ProjectConfigurator.Models;

namespace ProjectConfigurator.Generators;

public class ProjectEnvironmentVariableGenerator(ILogger<ProjectEnvironmentVariable> logger)
    : IProjectEnvironmentVariableGenerator
{
    public IDictionary<string, string> Generate(MachineConfiguration machineConfiguration, Project project,
        ProjectConfiguration projectConfiguration)
    {
        if (projectConfiguration.EnvironmentVariables == null)
        {
            throw new ConfiguratorException("Missing project configuration environment variables.");
        }

        if (machineConfiguration.ConfigurationVariables == null)
        {
            throw new ConfiguratorException("Missing machine configuration variables.");
        }

        var environmentVariables = new Dictionary<string, string>();

        if (project.EnvironmentVariables != null)
        {
            GenerateEnvironmentVariables(machineConfiguration, projectConfiguration, project.EnvironmentVariables,
                environmentVariables);
        }

        GenerateEnvironmentVariables(machineConfiguration, projectConfiguration, projectConfiguration.EnvironmentVariables, environmentVariables);

        return environmentVariables;
    }

    private void GenerateEnvironmentVariables(MachineConfiguration machineConfiguration, ProjectConfiguration projectConfiguration,
        IDictionary<string, string?> projectEnvironmentVariables, Dictionary<string, string> environmentVariables)
    {
        if (machineConfiguration.ConfigurationVariables == null)
        {
            throw new ConfiguratorException("Missing machine configuration variables.");
        }

        foreach (var (key, value) in projectEnvironmentVariables)
        {
            if (value == null)
            {
                logger.LogWarning(
                    "Project configuration '{ProjectConfigurationName}' missing environment variable information: '{Key}' - '{Value}'",
                    projectConfiguration.Name, key, value);

                continue;
            }

            var calculatedValue = value;

            foreach (var (machineKey, machineValue) in machineConfiguration.ConfigurationVariables)
            {
                calculatedValue = calculatedValue.Replace($"${machineKey}$", machineValue);
            }

            environmentVariables[key] = calculatedValue;
        }
    }
}