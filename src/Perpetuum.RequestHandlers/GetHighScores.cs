using System;
using Perpetuum.Host.Requests;
using Perpetuum.Services.HighScores;

namespace Perpetuum.RequestHandlers
{
    public class GetHighScores : IRequestHandler
    {
        private static readonly DateTimeRange _timeRange = DateTime.Now.ToRange(-TimeSpan.FromDays(30));
        private readonly IHighScoreService _highScoreService;

        public GetHighScores(IHighScoreService highScoreService)
        {
            _highScoreService = highScoreService;
        }

        public void HandleRequest(IRequest request)
        {
            var result = _highScoreService.GetHighScores(_timeRange).ToDictionary("h", h => h.ToDictionary());
            Message.Builder.FromRequest(request).WithData(result).WithEmpty().Send();
        }
    }
}