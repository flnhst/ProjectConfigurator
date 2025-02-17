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

using System.Xml;
using Microsoft.Extensions.Logging;
using ProjectConfigurator.Abstractions;
using ProjectConfigurator.Exceptions;
using ProjectConfigurator.Models;

namespace ProjectConfigurator.Configurators;

public class IntelliJIdeaConfigurator(
    ILogger<IntelliJIdeaConfigurator> logger,
    IProjectEnvironmentVariableGenerator environmentVariableGenerator) : IConfigurator
{
    public Task ConfigureProjectConfigurationAsync(MachineConfiguration machineConfiguration, Project project,
        ProjectConfiguration projectConfiguration,
        CancellationToken cancellationToken = default)
    {
        if (projectConfiguration.Location == null)
        {
            throw new ConfiguratorException("Project configuration location is missing.");
        }

        if (projectConfiguration.Name == null)
        {
            throw new ConfiguratorException("Project configuration name is missing.");
        }

        var workspaceXmlFilePath = Path.Combine(projectConfiguration.Location, ".idea",
            $".idea.{project.Name}", ".idea", "workspace.xml");

        if (!File.Exists(workspaceXmlFilePath))
        {
            throw new ConfiguratorException($"Could not find workspace.xml file at '{workspaceXmlFilePath}'.");
        }

        logger.LogInformation("Reading workspace.xml from '{WorkspaceXmlFilePath}'.", workspaceXmlFilePath);

        var doc = new XmlDocument();

        doc.Load(workspaceXmlFilePath);

        var configurationNode =
            doc.SelectSingleNode(
                $"/project/component[@name='RunManager']/configuration[@name='{projectConfiguration.Name}']");

        if (configurationNode == null)
        {
            throw new ConfiguratorException(
                $"Could not find configuration '{projectConfiguration.Name}' for project '{project.Name}'.");
        }

        var envsNode = configurationNode.SelectSingleNode("envs");

        if (envsNode == null)
        {
            envsNode = doc.CreateElement("envs");

            configurationNode.AppendChild(envsNode);
        }

        envsNode.RemoveAll();

        var environmentVariables = environmentVariableGenerator.Generate(machineConfiguration, project, projectConfiguration);

        foreach (var (key, value) in environmentVariables)
        {
            var envNode = doc.CreateElement("env");

            envNode.SetAttribute("name", key);
            envNode.SetAttribute("value", value);

            envsNode.AppendChild(envNode);
        }

        doc.Save(workspaceXmlFilePath);

        logger.LogInformation("Writing workspace.xml to '{WorkspaceXmlFilePath}'.", workspaceXmlFilePath);

        return Task.CompletedTask;
    }
}