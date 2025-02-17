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

using ProjectConfigurator.Abstractions;
using ProjectConfigurator.Models;

namespace ProjectConfigurator.Configurators;

public class LaunchSettingsJsonConfigurator : IConfigurator
{
    public Task ConfigureProjectConfigurationAsync(MachineConfiguration machineConfiguration,
        ProjectConfiguration projectConfiguration, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}