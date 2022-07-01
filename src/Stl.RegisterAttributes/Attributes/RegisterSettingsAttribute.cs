namespace Stl.RegisterAttributes;

public class RegisterSettingsAttribute : RegisterAttribute
{
    public string? SectionName { get; set; }

    public RegisterSettingsAttribute() { }
    public RegisterSettingsAttribute(string sectionName)
        => SectionName = sectionName;

    public override void Register(IServiceCollection services, Type implementationType)
        => services.AddSettings(implementationType, SectionName);
}
