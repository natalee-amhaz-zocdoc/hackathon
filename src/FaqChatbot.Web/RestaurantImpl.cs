using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FaqChatbot.Service;
using Zocdoc.DependencyInjection;

/*
 * NOTE: this file is provided for your convenience as an example of dependency injection and AWS DDB/C# best practices.
 * Feel free to customize, remove, or ignore its contents according to your whims.
 */
namespace FaqChatbot.Web
{
    [RegisterService]
    public class RestaurantImpl : IRestaurant
    {
        private readonly IRestaurantReservationPersistenceService _persistence;

        public RestaurantImpl(IRestaurantReservationPersistenceService persistence)
        {
            _persistence = persistence;
        }

        public async Task<GetRestaurantByIdResponse> GetRestaurantById(
            string restaurantId,
            CancellationToken cancellationToken
        )
        {
            var result = await _persistence.GetRestaurantById(restaurantId);
            if (result == null)
            {
                return GetRestaurantByIdResponse.NotFound();
            }

            return GetRestaurantByIdResponse.OK(result.ToRestaurant());
        }

        public async Task<GetRestaurantsResponse> GetRestaurants(string pageToken, int? limit, CancellationToken cancellationToken)
        {
            const int defaultLimit = 10;
            var result = await _persistence.GetRestaurants(pageToken, limit ?? defaultLimit, cancellationToken);

            return GetRestaurantsResponse.OK(new ListRestaurantsResponse
            {
                Restaurants = result.Data.Select(dto => dto.ToRestaurant()).ToList(),
                Limit = result.Limit,
                PageToken = result.PageToken,
            });
        }

        public async Task<PostRestaurantResponse> PostRestaurant(
            CreateRestaurantRequest body,
            CancellationToken cancellationToken
        )
        {
            var newRestaurantId = Guid.NewGuid().ToString();

            await _persistence.PutRestaurantAsync(
                newRestaurantId,
                body.Name,
                body.City,
                body.State,
                body.IsActive,
                cancellationToken
            );

            return PostRestaurantResponse.Created(
                new Restaurant
                {
                    RestaurantId = newRestaurantId,
                    Name = body.Name,
                    City = body.City,
                    State = body.State,
                    IsActive = body.IsActive,
                }
            );
        }
    }
}
