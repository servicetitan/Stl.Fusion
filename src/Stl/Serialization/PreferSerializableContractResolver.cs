using System;
using System.Runtime.Serialization;
using Newtonsoft.Json.Serialization;

namespace Stl.Serialization
{
    public class PreferSerializableContractResolver : DefaultContractResolver
    {
        protected override JsonContract CreateContract(Type objectType)
        {
            if (typeof(ISerializable).IsAssignableFrom(objectType))
                return CreateISerializableContract(objectType);

            return base.CreateContract(objectType);
        }
    }
}
