﻿#region License

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

public class LaunchSettingsJsonConfigurator(
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

        var launchSettingsFilePath = Path.Combine(projectConfiguration.Location, "Properties", "launchSettings.json");

        if (!File.Exists(launchSettingsFilePath))
        {
            throw new ConfiguratorException($"Could not find launchSettings.json at '{launchSettingsFilePath}'.");
        }

        logger.LogInformation($"Reading launchSettings.json from '{launchSettingsFilePath}'.");

        var rootNode = await ReadJsonAsync(launchSettingsFilePath, cancellationToken);

        if (rootNode == null)
        {
            throw new ConfiguratorException($"Could not parse launchSettings.json at '{launchSettingsFilePath}'.");
        }

        var profilesNode = rootNode["profiles"];

        if (profilesNode == null)
        {
            throw new ConfiguratorException($"Could not find profiles in '{launchSettingsFilePath}'.");
        }

        var profileNode = profilesNode[projectConfiguration.Name];

        if (profileNode == null)
        {
            throw new ConfiguratorException($"Could not find profile '{projectConfiguration.Name}' in '{launchSettingsFilePath}'.");
        }

        var environmentVariablesNode = profileNode["environmentVariables"];

        if (environmentVariablesNode == null || environmentVariablesNode.GetValueKind() != JsonValueKind.Object)
        {
            environmentVariablesNode = new JsonObject();

            profileNode["environmentVariables"] = environmentVariablesNode;
        }

        var environmentVariablesObject = environmentVariablesNode.AsObject();

        environmentVariablesObject.Clear();

        var environmentVariables = environmentVariableGenerator.Generate(machineConfiguration, project, projectConfiguration);

        foreach (var (key, value) in environmentVariables)
        {
            environmentVariablesObject.Add(key, value);
        }

        logger.LogInformation("Writing launchSettings.json to '{LaunchSettingsFilePath}'.", launchSettingsFilePath);

        await WriteJsonAsync(rootNode, launchSettingsFilePath, cancellationToken);
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