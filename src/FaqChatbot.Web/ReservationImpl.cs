using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using FaqChatbot.Model;
using FaqChatbot.Service;
using Zocdoc.DependencyInjection;
using Zocdoc.Extensions.Collections;

namespace FaqChatbot.Web
{
    [RegisterService(ServiceLifetime.Singleton)]
    public class ReservationImpl : IReservation
    {
        private readonly IRestaurantReservationPersistenceService _persistence;

        public ReservationImpl(IRestaurantReservationPersistenceService persistence)
        {
            _persistence = persistence;
        }

        public async Task<GetReservationByIdResponse> GetReservationById(
            string reservationId,
            CancellationToken cancellationToken
        )
        {
            var result =
                await _persistence.GetReservationAsync(reservationId);

            if (result == null)
            {
                return GetReservationByIdResponse.NotFound();
            }

            return GetReservationByIdResponse.OK(new ReservationResponse
            {
                Reservation = result.ToReservation(),
            });
        }

        public async Task<GetReservationsByRestaurantIdResponse> GetReservationsByRestaurantId(
            string restaurantId,
            CancellationToken cancellationToken
        )
        {
            var result = await _persistence.GetReservationsByRestaurantId(restaurantId);

            if (result == null)
            {
                return GetReservationsByRestaurantIdResponse.NotFound();
            }

            return GetReservationsByRestaurantIdResponse.OK(
                new ReservationsResponse
                {
                    Reservations = result.Select(r => r.ToReservation()).ToList()
                }
            );
        }

        public async Task<PostReservationResponse> PostReservation(
            string restaurantId,
            ReservationRequest body,
            CancellationToken cancellationToken
        )
        {
            var newReservationId = Guid.NewGuid().ToString();
            var userId = Guid.NewGuid().ToString(); // TODO: eventually take from auth layer

            await _persistence.PutReservationAsync(
                restaurantId,
                newReservationId,
                userId,
                body.StartTime,
                body.EndTime,
                body.DiningType.ToReservationType(),
                cancellationToken
            );

            return PostReservationResponse.Created(new ReservationResponse
            {
                Reservation = new Reservation
                {
                    RestaurantId = restaurantId,
                    ReservationId = newReservationId,
                    StartTime = body.StartTime,
                    EndTime = body.EndTime,
                    DiningType = body.DiningType,
                }
            });
        }
    }
}
