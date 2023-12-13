using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace BancaServices.Application.Services
{
    public class OmitJsonPropertyNameContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (!string.IsNullOrWhiteSpace(property.UnderlyingName))
                property.PropertyName = property.UnderlyingName;

            return property;
        }
    }
}