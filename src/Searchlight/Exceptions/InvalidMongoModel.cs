namespace Searchlight.Exceptions
{
    /// <summary>
    /// This MongoDB model has at least one field without a BSON decimal representation
    /// </summary>
    public class InvalidMongoModel : SearchlightException
    {
        public string TableName { get; set; }

        public string ErrorMessage
        {
            get =>
                $"The model {TableName} contains decimal fields that cannot be represented correctly in MongoDB.  "
                + "Please ensure that all fields of type `decimal` have the attribute `[BsonRepresentation(BsonType.Decimal128)]`.";
        }
    }
}