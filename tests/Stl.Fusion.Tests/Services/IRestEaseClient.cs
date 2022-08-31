using RestEase;
using Stl.Fusion.Client;
using Stl.Fusion.Tests.UIModels;

namespace Stl.Fusion.Tests.Services;

[RegisterRestEaseReplicaService(Scope = ServiceScope.ClientServices)]
[BasePath("RestEase")]
[SerializationMethods(Query = QuerySerializationMethod.Serialized)]
public interface IRestEaseClient
{
    [Get("getFromQueryImplicit")]
    Task<string> GetFromQueryImplicit(string str, CancellationToken cancellationToken = default);
    [Get("getFromQuery")]
    Task<string> GetFromQuery(string str, CancellationToken cancellationToken = default);
    [Get("getFromQueryComplex")]
    Task<QueryParamModel> GetFromQueryComplex(QueryParamModel str, CancellationToken cancellationToken = default);
    [Get("getJsonString")]
    Task<JsonString> GetJsonString(string str, CancellationToken cancellationToken = default);
    [Get("getFromPath/{str}")]
    Task<string> GetFromPath([Path] string str, CancellationToken cancellationToken = default);

    [Post("postFromQueryImplicit")]
    Task<JsonString> PostFromQueryImplicit(string str, CancellationToken cancellationToken = default);
    [Post("postFromQuery")]
    Task<JsonString> PostFromQuery(string str, CancellationToken cancellationToken = default);
    [Post("postFromPath/{str}")]
    Task<JsonString> PostFromPath([Path] string str, CancellationToken cancellationToken = default);
    [Post("postWithBody")]
    Task<JsonString> PostWithBody([Body] StringWrapper str, CancellationToken cancellationToken = default);
    [Post("concatQueryAndPath/{b}")]
    Task<JsonString> ConcatQueryAndPath(string a, [Path] string b, CancellationToken cancellationToken = default);
    [Post("concatPathAndBody/{a}")]
    Task<JsonString> ConcatPathAndBody([Path] string a, [Body] string b, CancellationToken cancellationToken = default);
}
