using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using FaqChatbot.Model;
using FaqChatbot.Service;
using Zocdoc.DependencyInjection;
using Zocdoc.Extensions.Collections;

/*
 * NOTE: this file is provided for your convenience as an example of dependency injection and AWS DDB/C# best practices.
 * Feel free to customize, remove, or ignore its contents according to your whims.
 */
namespace FaqChatbot.Fake.Service
{
    [RegisterFakeService()]
    public class DynamoRestaurantReservationService : IRestaurantReservationPersistenceService
    {
        private readonly ConcurrentDictionary<string, RestaurantDto> _restaurantsById;

        private readonly ConcurrentDictionary<string, List<ReservationDto>> _reservationsByRestaurantId;

        public DynamoRestaurantReservationService()
        {
            _restaurantsById = new ConcurrentDictionary<string, RestaurantDto>();
            _reservationsByRestaurantId = new ConcurrentDictionary<string, List<ReservationDto>>();
        }

        public Task<RestaurantDto> GetRestaurantById(string restaurantId)
        {
            return Task.FromResult(_restaurantsById.GetOrDefault(restaurantId));
        }

        public Task<PaginatedResponse<List<RestaurantDto>>> GetRestaurants(string pageToken, int limit,
            CancellationToken cancellationToken)
        {
            var restaurantList = _restaurantsById.Values.ToList();

            // Dicts are not order stable.
            restaurantList.Sort((a, b) => String.Compare(a.RestaurantId, b.RestaurantId, StringComparison.Ordinal));

            if (pageToken.IsNullOrEmpty())
            {
                pageToken = $"{0}";
            }

            var parsedToken = int.Parse(pageToken);
            var restaurantListSelect = restaurantList.Skip(parsedToken).Take(limit).ToList();

            return Task.FromResult(new PaginatedResponse<List<RestaurantDto>>
            {
                PageToken = $"{limit}",
                Limit = limit,
                Data = restaurantListSelect,
            });
        }

        public Task<List<ReservationDto>> GetReservationsByRestaurantId(string restaurantId)
        {
            var reservations = _reservationsByRestaurantId.GetOrDefault(restaurantId) ?? new List<ReservationDto>();
            return Task.FromResult(reservations);
        }

        public Task<ReservationDto> GetReservationAsync(string reservationId)
        {
            var reservations = _reservationsByRestaurantId.Values
                .SelectMany(r => r)
                .Where(r => r.ReservationId == reservationId)
                .ToList();

            return Task.FromResult(reservations.FirstOrDefault());
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
            var newReservation = new ReservationDto
            {
                PartitionKey = ReservationDto.MkPartitionKey(restaurantId),
                SortKey = ReservationDto.MkSortKey(reservationId),
                UserId = userId,
                StartTime = startTime.ToInstant(),
                EndTime = endTime.ToInstant(),
                ReservationType = reservationType,
            };

            if (!_restaurantsById.ContainsKey(restaurantId))
                throw new ArgumentException("attempted to create reservation for bogus restaurant: "
                                            + restaurantId);

            await Task.FromResult(
                _reservationsByRestaurantId.AddOrUpdate(
                    restaurantId,
                    new List<ReservationDto>() {newReservation},
                    (_, existingList) => existingList.Append(newReservation).ToList()
                )
            );
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
            if (_restaurantsById.ContainsKey(restaurantId))
                throw new ArgumentException("attempted to create restaurant with a pre-existing id: "
                                            + restaurantId);

            await Task.FromResult(
                _restaurantsById[restaurantId] = new RestaurantDto
                {
                    PartitionKey = RestaurantDto.MkPartitionKey(restaurantId),
                    SortKey = RestaurantDto.SortKeyPrefix,
                    Name = restaurantName,
                    City = city,
                    State = state,
                    IsActive = isActive,
                }
            );
        }
    }
}
