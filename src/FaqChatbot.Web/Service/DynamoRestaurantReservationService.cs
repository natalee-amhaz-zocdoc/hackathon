using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using JetBrains.Annotations;
using NodaTime;
using FaqChatbot.Model;
using FaqChatbot.Service;
using Zocdoc.DependencyInjection;
using Zocdoc.Extensions;

/*
 * NOTE: this file is provided for your convenience as an example of dependency injection and AWS DDB/C# best practices.
 * Feel free to customize, remove, or ignore its contents according to your whims.
 */
namespace FaqChatbot.Web.Service
{
    [RegisterFakeableService(ServiceLifetime.Singleton)]
    public class DynamoRestaurantReservationService : IRestaurantReservationPersistenceService
    {
        private readonly IDynamoDBContext _context;

        public DynamoRestaurantReservationService(IDynamoDBContext context)
        {
            _context = context;
        }

        [ItemCanBeNull]
        public async Task<RestaurantDto> GetRestaurantById(string restaurantId)
        {
            // NOTE: this query uses the high level API, making use of an annotated DTO
            // this approach works for simple DDB reads where Table Hash and Range keys are used to query
            var restaurant = await _context.LoadAsync<RestaurantDto>(RestaurantDto.MkPartitionKey(restaurantId),
                RestaurantDto.SortKeyPrefix);
            return restaurant;
        }

        public async Task<PaginatedResponse<List<RestaurantDto>>> GetRestaurants(string pageToken, int limit, CancellationToken cancellationToken)
        {
            var decodedPageToken = pageToken.IsNullOrEmpty()
                ? null
                : System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(pageToken));

            const int maxLimit = 100;
            var clampedLimit = Math.Clamp(limit, 0, maxLimit);

            var table = _context.GetTargetTable<RestaurantDto>();
            var asyncSearch = table.Query(new QueryOperationConfig
            {
                IndexName = "ReservationIdIndex",
                KeyExpression = new Expression
                {
                    ExpressionStatement = "#SK = :val",
                    ExpressionAttributeNames = new Dictionary<string, string>() {{"#SK", "SK"}},
                    // Only return restaurant documents. For restaurant docs we tag them with
                    // "#info" (RestaurantDto.SortKeyPrefix) as a secondary key, this allows us to pull only restaurants.
                    // We might want to make this more explicit with a 'type' filed in the future.
                    ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                        {{":val", RestaurantDto.SortKeyPrefix}}
                },
                Limit = clampedLimit,
                PaginationToken = decodedPageToken,
            });

            var docs = await asyncSearch.GetNextSetAsync(cancellationToken);

            var restaurants = docs
                .Select(_context.FromDocument<RestaurantDto>);

            var encodedPageToken = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(asyncSearch.PaginationToken));

            return new PaginatedResponse<List<RestaurantDto>>
            {
                Limit = asyncSearch.Limit,
                PageToken = encodedPageToken,
                Data = restaurants.ToList()
            };
        }

        public async Task<List<ReservationDto>> GetReservationsByRestaurantId(string restaurantId)
        {
            var reservations = await _context.QueryAsync<ReservationDto>(ReservationDto.MkPartitionKey(restaurantId),
                new DynamoDBOperationConfig()
                {
                    QueryFilter = new List<ScanCondition>
                    {
                        // filter by items beginning with the correct key prefix, to ensure we only load reservations and not restaurant info
                        new ScanCondition(nameof(ReservationDto.SortKey), ScanOperator.BeginsWith,
                            ReservationDto.SortKeyPrefix),
                    }
                }).GetRemainingAsync();
            return reservations;
        }

        [ItemCanBeNull]
        public async Task<ReservationDto> GetReservationAsync(string reservationId)
        {
            // NOTE: this operation makes use of a slightly less abstract table query API, because it
            // specifies a Filter and limit, which aren't available when using the higher level APIs
            var table = _context.GetTargetTable<ReservationDto>();
            var asyncSearch = table.Query(
                new QueryOperationConfig
                {
                    IndexName = "ReservationIdIndex",
                    Filter = new QueryFilter("SK", QueryOperator.Equal, ReservationDto.MkSortKey(reservationId)),
                    Limit = 1
                }
            );
            var result = await asyncSearch.GetRemainingAsync();

            return result
                .Select(doc => _context.FromDocument<ReservationDto>(doc))
                .FirstOrDefault();
        }

        public async Task PutReservationAsync(
            string restaurantId,
            string reservationId,
            string userId,
            OffsetDateTime startTime,
            OffsetDateTime endTime,
            ReservationType? reservationType,
            CancellationToken cancellation
        )
        {
            var table = _context.GetTargetTable<ReservationDto>();
            var doc = _context.ToDocument(new ReservationDto
            {
                StartTime = startTime.ToInstant(),
                EndTime = endTime.ToInstant(),
                PartitionKey = ReservationDto.MkPartitionKey(restaurantId),
                SortKey = ReservationDto.MkSortKey(reservationId),
                ReservationType = reservationType,
            });
            // NOTE: this config specifies a ConditionExpression
            // See https://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_PutItem.html#API_PutItem_RequestSyntax
            // for more details on the available condition expressions in the Dynamo API
            var putItemConfig = new PutItemOperationConfig
            {
                ConditionalExpression = new Expression
                {
                    ExpressionStatement =
                        "attribute_not_exists(RestaurantId) AND attribute_not_exists(ReservationId)",
                    ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>(),
                }
            };

            await table.PutItemAsync(doc, putItemConfig, cancellation);
        }

        public async Task PutRestaurantAsync(
            string restaurantId,
            string restaurantName,
            string city,
            string state,
            bool isActive,
            CancellationToken cancellation
        )
        {
            var table = _context.GetTargetTable<RestaurantDto>();
            var doc = _context.ToDocument(
                new RestaurantDto
                {
                    PartitionKey = RestaurantDto.MkPartitionKey(restaurantId),
                    Name = restaurantName,
                    City = city,
                    State = state,
                    IsActive = isActive,
                }
            );
            var putConfig = new PutItemOperationConfig
            {
                ConditionalExpression = new Expression
                {
                    ExpressionStatement =
                        "attribute_not_exists(RestaurantId)",
                    ExpressionAttributeValues =
                        new Dictionary<string, DynamoDBEntry>(),
                }
            };

            await table.PutItemAsync(doc, putConfig, cancellation);
        }
    }
}
