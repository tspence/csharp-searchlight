using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Searchlight.MongoDB
{
    /// <summary>
    /// Verify that a model is safe for use with MongoDB
    /// </summary>
    public class MongoModelChecker
    {
        private static Dictionary<string, bool> _isMongoSafe = new Dictionary<string, bool>();

        /// <summary>
        /// MongoDB fails when querying on decimal values if they are not tagged with a BsonRepresentation
        /// attribute.  This function checks whether all fields that are of type decimal have the necessary
        /// attribute. It tries to work in a way that does not assume 
        /// </summary>
        /// <returns></returns>
        public static bool IsMongoSafe(Type modelType)
        {
            var name = modelType.Name;
            if (_isMongoSafe.TryGetValue(name, out var safe))
            {
                return safe;
            }
            
            // Search for decimal properties
            var decimalProperties = (from prop in modelType.GetProperties()
                where prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(decimal?)
                select prop);
            
            // Check if any decimal properties don't have representations
            var isSafe = true;
            foreach (var prop in decimalProperties)
            {
                var attribute = prop.GetCustomAttribute<BsonRepresentationAttribute>();
                if (attribute == null || attribute.Representation != BsonType.Decimal128)
                {
                    isSafe = false;
                }
            }

            _isMongoSafe[name] = isSafe;
            return isSafe;
        }
    }
}