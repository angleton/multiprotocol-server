namespace MultiprotocolServer.Protocols.GraphQl;

public class QueryRoot
{
    public string Hello(string? payload) => ProtocolResponse.For("GraphQL");
}
