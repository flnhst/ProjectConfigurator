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

using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProjectConfigurator.Abstractions;
using ProjectConfigurator.Exceptions;
using ProjectConfigurator.Models;

namespace ProjectConfigurator.Readers;

public class MachineConfigurationReader(ILogger<MachineConfigurationReader> logger) : IMachineConfigurationReader
{
    public async Task<MachineConfiguration> ReadMachineConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var projectConfigurationJsonFilePath =
            Environment.GetEnvironmentVariable("PROJECT_CONFIGURATION_JSON_FILE_PATH");

        if (projectConfigurationJsonFilePath is null)
            throw new ConfiguratorException(
                "Please specify 'PROJECT_CONFIGURATION_JSON_FILE_PATH' environment variable.");

        if (!File.Exists(projectConfigurationJsonFilePath))
        {
            logger.LogInformation("Machine configuration file {MachineConfigurationFileName} not found.",
                projectConfigurationJsonFilePath);

            throw new FileNotFoundException($"Machine configuration file {projectConfigurationJsonFilePath} not found.",
                projectConfigurationJsonFilePath);
        }

        logger.LogInformation("Reading machine configuration from '{MachineConfigurationFilePath}'.",
            projectConfigurationJsonFilePath);

        await using var file = File.OpenRead(projectConfigurationJsonFilePath);

        var machineConfiguration =
            await JsonSerializer.DeserializeAsync<MachineConfiguration>(file, cancellationToken: cancellationToken);

        if (machineConfiguration is null) throw new Exception("Could not read machine configuration.");

        return machineConfiguration;
    }
}