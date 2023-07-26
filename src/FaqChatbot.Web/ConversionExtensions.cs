using System;
using NodaTime;
using FaqChatbot.Model;

namespace FaqChatbot.Web
{
    public static class ConversionExtensions
    {
        public static Restaurant ToRestaurant(this RestaurantDto r)
        {
            return new Restaurant
            {
                RestaurantId = r.RestaurantId,
                Name = r.Name,
                City = r.City,
                State = r.State,
                IsActive = r.IsActive,
            };
        }

        public static Reservation ToReservation(this ReservationDto r)
        {
            return new Reservation
            {
                RestaurantId = r.RestaurantId,
                ReservationId = r.ReservationId,
                StartTime = r.StartTime.WithOffset(Offset.Zero),
                EndTime = r.EndTime.WithOffset(Offset.Zero),
                DiningType = r.ReservationType.ToDiningType(),
            };
        }

        public static DiningType? ToDiningType(this ReservationType? reservationType)
        {
            return reservationType switch
            {
                null => null,
                ReservationType.Outdoor => DiningType.OutdoorDining,
                ReservationType.Indoor => DiningType.IndoorDining,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static ReservationType? ToReservationType(this DiningType? diningType)
        {
            return diningType switch
            {
                null => null,
                DiningType.OutdoorDining => ReservationType.Outdoor,
                DiningType.IndoorDining => ReservationType.Indoor,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
