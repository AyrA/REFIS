namespace REFIS
{
    /// <summary>
    /// Global extensions
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Serialize this instance into JSON
        /// </summary>
        /// <param name="o">object</param>
        /// <returns>JSON string</returns>
        public static string ToJson(this object o)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(o);
        }

        /// <summary>
        /// Deserializes an object from JSON
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="s">JSON string</param>
        /// <returns>Deserialized object</returns>
        public static T FromJson<T>(this string s)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(s);
        }
    }
}
