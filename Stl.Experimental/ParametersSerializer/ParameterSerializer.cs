using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Stl.ParametersSerializer
{
    public class ParameterSerializer :IParameterSerializer 
    {
        public IEnumerable<string> Serialize(IParameters parameters)
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

        private string? SerializeParameter(CliParameterAttribute parameterPattern, object propValue)
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

        private string? SerializeParameters<TEnum>(string parameterPattern, TEnum propValue) where TEnum : Enum
        {
            var fi = propValue.GetType().GetField(propValue.ToString());
            var cliValue = fi.GetCustomAttribute<CliValueAttribute>()?.Value ?? propValue.ToString();
            return parameterPattern.Replace("{value}", cliValue);
        }

        private string? SerializeParameters(string? parameterPattern, string propValue)
            => parameterPattern.Replace("{value}", propValue);
        
        private string? SerializeParameters(string? parameterPattern, bool propValue)
            => propValue ? parameterPattern : null;
        
        private string? SerializeParameters(CliParameterAttribute parameterPattern, IDictionary<string, string> propDict)
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