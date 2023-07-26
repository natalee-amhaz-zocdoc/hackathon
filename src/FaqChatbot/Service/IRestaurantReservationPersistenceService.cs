using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using FaqChatbot.Model;

namespace FaqChatbot.Service
{
    public interface IRestaurantReservationPersistenceService
    {
        public Task<RestaurantDto> GetRestaurantById(string restaurantId);

        public Task<PaginatedResponse<List<RestaurantDto>>> GetRestaurants(string pageToken, int limit, CancellationToken cancellationToken);

        public Task<List<ReservationDto>> GetReservationsByRestaurantId(string restaurantId);

        public Task<ReservationDto> GetReservationAsync(string reservationId);

        public Task PutReservationAsync(
            string restaurantId,
            string reservationId,
            string userId,
            OffsetDateTime startTime,
            OffsetDateTime endTime,
            ReservationType? reservationType,
            CancellationToken cancellation
        );

        public Task PutRestaurantAsync(
            string restaurantId,
            string restaurantName,
            string city,
            string state,
            bool isActive,
            CancellationToken cancellation
        );
    }
}
