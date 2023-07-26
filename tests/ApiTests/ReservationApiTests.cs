using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using NodaTime;
using NUnit.Framework;
using FaqChatbot;
using Zocdoc.Extensions;
using Zocdoc.HttpClient.Interface;
using ZocDoc.Tests.ApiTests;

namespace ApiTests
{
    public class ReservationApiTests
    {
        [SetUp]
        public void Setup()
        {
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => true;
        }

        [Test]
        public async Task VerifyReservationCreation()
        {
            var restaurant = await CreateRestaurant("Balthazar");
            var restaurantId = restaurant.RestaurantId;
            var reservationResponse = await CreateReservation(restaurantId, diningType: null);

            var reservations = await ApiTestHelpers.GetAsync<ReservationsResponse>($"/api/restaurant/{restaurantId}/reservations");
            reservations.Reservations.Should()
                .ContainSingle(r => r.ReservationId == reservationResponse.Reservation.ReservationId);

            var reservation = reservations.Reservations.First(r => r.ReservationId == reservationResponse.Reservation.ReservationId);
            reservation.RestaurantId.Should().Be(restaurantId);
            reservation.ReservationId.Should().NotBe(restaurantId);
            reservation.DiningType.Should().BeNull();

            var restaurantResp = await ApiTestHelpers.GetAsync<Restaurant>($"/api/restaurant/{restaurantId}");

            restaurantResp.Name.Should().Be("Balthazar");
            restaurantResp.RestaurantId.Should().Be(restaurantId);
            restaurantResp.City.Should().Be("New York");
            restaurantResp.State.Should().Be("NY");

            var reservationResp = await ApiTestHelpers.GetAsync<ReservationResponse>(
                $"/api/reservation/{reservation.ReservationId}"
            );

            reservationResp.Reservation.RestaurantId.Should().Be(restaurantId);
            reservationResp.Reservation.ReservationId.Should().NotBe(restaurantId);
            reservationResp.Reservation.DiningType.Should().BeNull();
        }

        [Test]
        public async Task GetRestaurants()
        {
            await CreateRestaurant("Balthazar-north");
            await CreateRestaurant("Balthazar-south");
            await CreateRestaurant("Balthazar-east");
            await CreateRestaurant("Balthazar-west");

            var restaurantsNoArgs = await ApiTestHelpers.GetAsync<ListRestaurantsResponse>("/api/restaurants");
            restaurantsNoArgs.Restaurants.Count.Should().BeGreaterOrEqualTo(4);


            // test limit
            var restaurantsLimit = await ApiTestHelpers.GetAsync<ListRestaurantsResponse>("/api/restaurants?limit=2");
            restaurantsLimit.Restaurants.Count.Should().Be(2);

            // test basic pagination
            var nextSet = await ApiTestHelpers
                .GetAsync<ListRestaurantsResponse>($"/api/restaurants?limit=2&page_token={restaurantsLimit.PageToken}");

            nextSet.Restaurants.Count.Should().Be(2);

            foreach (var aRestaurant in restaurantsLimit.Restaurants)
            {
                foreach (var bRestaurant in nextSet.Restaurants)
                {
                    var overlappingSets = aRestaurant.RestaurantId == bRestaurant.RestaurantId;
                    overlappingSets.Should().BeFalse();
                }
            }

        }

        [Test]
        public async Task CreateReservationWithDiningType()
        {
            var restaurant = await CreateRestaurant("Balthazar");
            var restaurantId = restaurant.RestaurantId;
            var res1 = await CreateReservation(restaurantId, diningType: null);
            var res2 = await CreateReservation(restaurantId, diningType: DiningType.IndoorDining);
            var res3 = await CreateReservation(restaurantId, diningType: DiningType.OutdoorDining);


            async Task VerifyReservationType(FaqChatbot.Reservation reservation, DiningType? diningType)
            {
                var reservationResp = await ApiTestHelpers.GetAsync<ReservationResponse>(
                    $"/api/reservation/{reservation.ReservationId}"
                );

                reservationResp.Reservation.RestaurantId.Should().Be(restaurantId);
                reservationResp.Reservation.ReservationId.Should().Be(reservation.ReservationId);
                reservationResp.Reservation.DiningType.Should().Be(diningType);
            }

            await VerifyReservationType(res1.Reservation, null);
            await VerifyReservationType(res2.Reservation, DiningType.IndoorDining);
            await VerifyReservationType(res3.Reservation, DiningType.OutdoorDining);
        }

        private static async Task<ReservationResponse> CreateReservation(string restaurantId, DiningType? diningType)
        {
            var reservationsUrl = $"/api/restaurant/{restaurantId}/reservations";

            var clock = SystemClock.Instance;
            var eastern = DateTimeZoneProviders.Tzdb.GetZoneOrNull("America/New_York").ThrowIfNull();
            var offset = eastern.GetUtcOffset(clock.GetCurrentInstant());

            var res = await ApiTestHelpers.PostAsync(
                reservationsUrl,
                new ReservationRequest()
                {
                    StartTime = clock.GetCurrentInstant().WithOffset(offset),
                    EndTime = clock.GetCurrentInstant().WithOffset(offset).Plus(Duration.FromHours(1.5)),
                    DiningType = diningType,
                },
                HttpStatusCode.Created
            );
            return await res.Content.FromJson<ReservationResponse>();
        }

        private async Task<Restaurant> CreateRestaurant(string name)
        {
            var restaurantResult = await ApiTestHelpers.PostAsync(
                "/api/restaurants",
                new CreateRestaurantRequest()
                {
                    Name = name,
                    IsActive = true,
                    City = "New York",
                    State = "NY",
                },
                HttpStatusCode.Created
            );

            return await restaurantResult.Content.FromJson<Restaurant>();
        }
    }
}
