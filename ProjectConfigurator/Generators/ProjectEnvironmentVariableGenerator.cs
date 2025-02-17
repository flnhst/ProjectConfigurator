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
    public IDictionary<string, string> Generate(MachineConfiguration machineConfiguration,
        ProjectConfiguration projectConfiguration)
    {
        if (projectConfiguration.EnvironmentVariables == null)
            throw new ConfiguratorException("Missing project environment variables.");

        if (machineConfiguration.ConfigurationVariables == null)
            throw new ConfiguratorException("Missing machine configuration variables.");

        var environmentVariables = new Dictionary<string, string>();

        foreach (var (key, value) in projectConfiguration.EnvironmentVariables)
        {
            if (key == null || value == null)
            {
                logger.LogWarning(
                    "Project '{ProjectName}' missing environment variable information: '{Key}' - '{Value}",
                    projectConfiguration.ProjectName, key, value);

                continue;
            }

            var calculatedValue = value;

            foreach (var (machineKey, machineValue) in machineConfiguration.ConfigurationVariables)
                calculatedValue = calculatedValue.Replace($"${machineKey}$", machineValue);

            environmentVariables.Add(key, calculatedValue);
        }

        return environmentVariables;
    }
}