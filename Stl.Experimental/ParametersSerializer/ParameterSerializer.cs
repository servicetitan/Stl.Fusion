using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stl.CommandLine;

namespace Stl.ParametersSerializer
{
    // AY: Overall, I like the idea of having unified parameter serialization framework.
    // On a negative side, I feel the design needs some improvements + I'd 
    // consider at least the integration with System.CommandLine here.
    public class ParameterSerializer :IParameterSerializer 
    {
        // I guess this method must be virtual.
        public IEnumerable<CmdPart> Serialize(IParameters parameters)
        {
            foreach (var propertyInfo in parameters.GetType().GetProperties())
            {
                var cliParameterAttribute = propertyInfo
                    .GetCustomAttribute<CliParameterAttribute>();
                var propValue = propertyInfo.GetValue(parameters);

                var serializedParameter = SerializeParameter(cliParameterAttribute, propValue);
                if (serializedParameter == null)
                    continue;

                yield return serializedParameter;
            }
        }

        // This one must be protected virtual...
        private CmdPart? SerializeParameter(CliParameterAttribute parameterPattern, object propValue)
        {
            if (parameterPattern?.ParameterPattern == null || propValue == null)
                return null;
            
            switch (propValue)
            {
                case string propString:
                    return SerializeParameters(parameterPattern.ParameterPattern, propString);
                case Enum propEnum:
                    return SerializeParameters(parameterPattern.ParameterPattern, propEnum);
                case int propInt:
                    return SerializeParameters(parameterPattern.ParameterPattern, propInt.ToString());
                case bool propBool:
                    return SerializeParameters(parameterPattern.ParameterPattern, propBool);
                case IDictionary<string, string> propDict:
                    return SerializeParameters(parameterPattern, propDict);
                default:
                    throw new NotSupportedException($"Type {propValue.GetType()} not supported in ParameterSerializer.");
            }
        }

        // Etc.
        private CmdPart? SerializeParameters<TEnum>(string parameterPattern, TEnum propValue) where TEnum : Enum
        {
            var fi = propValue.GetType().GetField(propValue.ToString());
            var cliValue = fi.GetCustomAttribute<CliValueAttribute>()?.Value ?? propValue.ToString();
            return parameterPattern.Replace("{value}", cliValue);
        }

        private CmdPart? SerializeParameters(string? parameterPattern, string propValue)
            => parameterPattern.Replace("{value}", propValue);
        
        private CmdPart? SerializeParameters(string? parameterPattern, bool propValue)
            => propValue ? parameterPattern : (CmdPart?)null;
        
        private CmdPart? SerializeParameters(CliParameterAttribute parameterPattern, IDictionary<string, string> propDict)
        {
            if (parameterPattern.RepeatPattern == null || parameterPattern.Separator == null)
                return null;
            var elements = propDict.Select(x => 
                parameterPattern.RepeatPattern
                    .Replace("{key}", x.Key)
                    .Replace("{value}", x.Value));
            var value = string.Join(parameterPattern.Separator, elements);
            return parameterPattern.ParameterPattern.Replace("{value}", value);
        }
    }
}
