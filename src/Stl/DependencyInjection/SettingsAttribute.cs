using System;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.DependencyInjection
{
    public class SettingsAttribute : ServiceAttributeBase
    {
        public string SectionName { get; set; }

        public SettingsAttribute(string sectionName)
            => SectionName = sectionName;

        public override void Register(IServiceCollection services, Type implementationType)
            => services.AddSettings(implementationType, SectionName);
    }
}
