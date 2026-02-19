using Repositories;
using Entities;
namespace Services
{
    public class RatingService: IRatingService
    {
        private readonly IRatingRepository _ratingRepository;
        public RatingService(IRatingRepository ratingRepository)
        {
            _ratingRepository = ratingRepository;
        }
        public async Task<Rating> AddRating(Rating rating)
        {
            return await _ratingRepository.AddRating(rating);
        }
    }
}
