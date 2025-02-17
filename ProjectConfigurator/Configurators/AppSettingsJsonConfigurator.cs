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
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using ProjectConfigurator.Abstractions;
using ProjectConfigurator.Exceptions;
using ProjectConfigurator.Models;

namespace ProjectConfigurator.Configurators;

public class AppSettingsJsonConfigurator(
    ILogger<LaunchSettingsJsonConfigurator> logger,
    IProjectEnvironmentVariableGenerator environmentVariableGenerator) : IConfigurator
{
    private static readonly JsonSerializerOptions WriteJsonSerializerOptions = new()
    {
        IndentSize = 2,
        WriteIndented = true
    };

    public async Task ConfigureProjectConfigurationAsync(MachineConfiguration machineConfiguration, Project project,
        ProjectConfiguration projectConfiguration, CancellationToken cancellationToken = default)
    {
        if (projectConfiguration.Location == null)
        {
            throw new ConfiguratorException("Project configuration location is missing.");
        }

        if (projectConfiguration.Name == null)
        {
            throw new ConfiguratorException("Project configuration name is missing.");
        }

        var appSettingsJsonFilePath = Path.Combine(projectConfiguration.Location, projectConfiguration.Name);

        if (!File.Exists(appSettingsJsonFilePath))
        {
            throw new ConfiguratorException($"Could not find '{projectConfiguration.Name}' at '{appSettingsJsonFilePath}'.");
        }

        logger.LogInformation("Reading '{ProjectConfigurationName}' from '{AppSettingsJsonFilePath}'.", projectConfiguration.Name, appSettingsJsonFilePath);

        var rootNode = await ReadJsonAsync(appSettingsJsonFilePath, cancellationToken);

        if (rootNode == null)
        {
            throw new ConfiguratorException($"Could not parse '{appSettingsJsonFilePath}' at '{appSettingsJsonFilePath}'.");
        }

        if (rootNode.GetValueKind() != JsonValueKind.Object)
        {
            throw new ConfiguratorException($"Could not parse '{appSettingsJsonFilePath}' at '{appSettingsJsonFilePath}', expected object as root.");
        }

        var rootObject = rootNode as JsonObject;

        var environmentVariables = environmentVariableGenerator.Generate(machineConfiguration, project, projectConfiguration);

        foreach (var (key, value) in environmentVariables)
        {
            rootNode[key] = value;
        }

        logger.LogInformation("Writing '{ProjectConfigurationName}' to '{LaunchSettingsFilePath}'.", projectConfiguration.Name, appSettingsJsonFilePath);

        await WriteJsonAsync(rootNode, appSettingsJsonFilePath, cancellationToken);
    }

    private async Task<JsonNode?> ReadJsonAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var launchSettingsJsonFile = File.OpenRead(filePath);

        return await JsonNode.ParseAsync(launchSettingsJsonFile, cancellationToken: cancellationToken);
    }

    private async Task WriteJsonAsync(JsonNode jsonNode, string filePath, CancellationToken cancellationToken = default)
    {
        await using var launchSettingsJsonFile = File.Open(filePath, FileMode.Truncate, FileAccess.Write);

        await JsonSerializer.SerializeAsync(launchSettingsJsonFile, jsonNode, WriteJsonSerializerOptions, cancellationToken: cancellationToken);
    }
}