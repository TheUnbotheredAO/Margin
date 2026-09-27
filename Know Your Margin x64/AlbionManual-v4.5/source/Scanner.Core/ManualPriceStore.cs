namespace Scanner.Core;
public sealed class ManualPriceStore
{
    public record PriceStamp(decimal Price,DateTimeOffset Updated);
    readonly string path;
    string StampPath=>path+".dates.json";
    Dictionary<string,PriceStamp> stamps=new();
    public string PreferencesPath=>Path.Combine(Path.GetDirectoryName(path)!,"materials-view.json");
    public DateTimeOffset? UpdatedAt(string server,string city,string id){var key=Key(server,city,id);return values.TryGetValue(key,out var price)&&stamps.TryGetValue(key,out var stamp)&&stamp.Price==price?stamp.Updated:null;}
    Dictionary<string, decimal> values;
    public ManualPriceStore(string path)
    {
        this.path = path;
        if(File.Exists(StampPath))stamps=DataFiles.Read<Dictionary<string,PriceStamp>>(StampPath);
        values = File.Exists(path) ? DataFiles.Read<Dictionary<string,decimal>>(path) : new();
        if(values.Any(v=>v.Value<=0 || v.Value>1000000000000m)) throw new InvalidDataException("Saved manual prices must be positive and no greater than 1,000,000,000,000.");
    }
    static string Key(string server,string city,string id) => server+"|"+city+"|"+id;
    public Dictionary<string,decimal> Get(string server,string city,IEnumerable<string> ids) => ids.Distinct().Where(id=>values.ContainsKey(Key(server,city,id))).ToDictionary(id=>id,id=>values[Key(server,city,id)]);
    public void Save(string server,string city,IEnumerable<string> ids,IReadOnlyDictionary<string,decimal> updates)
    {
        if(!MarketClient.Servers.ContainsKey(server)||!MarketClient.Cities.Contains(city))throw new ArgumentException("Invalid market.");
        var scope=ids.Distinct().ToArray();
        if(scope.Any(id=>!DataFiles.ValidId(id))||updates.Any(p=>!scope.Contains(p.Key)||p.Value<=0||p.Value>1000000000000m))throw new ArgumentException("Invalid manual price.");
        var next=new Dictionary<string,decimal>(values);
        foreach(var id in scope) { var key=Key(server,city,id);next.Remove(key);if(updates.TryGetValue(id,out var price))next[key]=price; }
        var nextStamps=new Dictionary<string,PriceStamp>(stamps);
        foreach(var id in scope){var key=Key(server,city,id);if(!updates.TryGetValue(id,out var p))nextStamps.Remove(key);else if(!values.TryGetValue(key,out var old)||old!=p)nextStamps[key]=new(p,DateTimeOffset.Now);}
        DataFiles.Save(StampPath,nextStamps);
        DataFiles.Save(path,next);
        stamps=nextStamps;
        values=next;
    }
}

