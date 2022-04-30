namespace REFIS
{
    public static class Extensions
    {
        public static string ToJson(this object o)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(o);
        }

        public static T FromJson<T>(this string s)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(s);
        }
    }
}
