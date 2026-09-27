namespace Scanner.Core;
// Offline market labels only. There is no HTTP client or market scanning implementation.
public static class MarketClient
{
 public static readonly Dictionary<string,string> Servers = new() { ["Americas"]="Americas", ["Asia"]="Asia", ["Europe"]="Europe" };
 public static readonly string[] Cities = ["Bridgewatch","Martlock","Thetford","Fort Sterling","Lymhurst","Caerleon","Brecilien"];
}
