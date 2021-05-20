using System;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.DependencyInjection
{
    public class RegisterSettingsAttribute : RegisterAttribute
    {
        public string SectionName { get; set; }

        public RegisterSettingsAttribute(string sectionName)
            => SectionName = sectionName;

        public override void Register(IServiceCollection services, Type implementationType)
            => services.AddSettings(implementationType, SectionName);
    }
}
