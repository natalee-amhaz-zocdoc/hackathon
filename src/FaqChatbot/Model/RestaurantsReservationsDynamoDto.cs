using Amazon.DynamoDBv2.DataModel;
using NodaTime;
using FaqChatbot;
using Zocdoc.Aws.DynamoDb.PropertyConverters;

/*
 * NOTE: this file is provided for your convenience as an example of dependency injection and AWS DDB/C# best practices.
 * Feel free to customize, remove, or ignore its contents according to your whims.
 */
namespace FaqChatbot.Model
{
    [DynamoDBTable("RestaurantsReservations")]
    public abstract class RestaurantTableBase
    {
        [DynamoDBHashKey]
        [DynamoDBProperty("PK")]
        public string PartitionKey { get; set; }

        [DynamoDBRangeKey]
        [DynamoDBProperty("SK")]
        [DynamoDBGlobalSecondaryIndexHashKey]
        public string SortKey { get; set; }
    }

    public class RestaurantDto : RestaurantTableBase
    {
        public const string PartitionKeyPrefix = "#restaurant_";
        public const string SortKeyPrefix = "#info";

        public RestaurantDto()
        {
            SortKey = SortKeyPrefix;
        }

        public string RestaurantId => PartitionKey.Replace(PartitionKeyPrefix, "");

        public static string MkPartitionKey(string restaurantId) => PartitionKeyPrefix + restaurantId;

        public string Name { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public bool IsActive { get; set; }
    }

    public class ReservationDto : RestaurantTableBase
    {
        public const string PartitionKeyPrefix = "#restaurant_";
        public const string SortKeyPrefix = "#reservation_";

        public string RestaurantId => PartitionKey.Replace(PartitionKeyPrefix, "");
        public string ReservationId => SortKey.Replace(SortKeyPrefix, "");

        public static string MkPartitionKey(string restaurantId) => PartitionKeyPrefix + restaurantId;
        public static string MkSortKey(string reservationId) => SortKeyPrefix + reservationId;

        public string UserId { get; set; }

        [DynamoDBProperty(Converter = typeof(InstantConverter))]
        public Instant StartTime { get; set; }

        [DynamoDBProperty(Converter = typeof(InstantConverter))]
        public Instant EndTime { get; set; }

        [DynamoDBProperty(typeof(NullableEnumConverter<ReservationType>))]
        public ReservationType? ReservationType { get; set; }
    }

    public enum ReservationType
    {
        Indoor,
        Outdoor
    }
}
