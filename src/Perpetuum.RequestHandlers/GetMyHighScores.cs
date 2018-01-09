using System;
using Perpetuum.Host.Requests;
using Perpetuum.Services.HighScores;

namespace Perpetuum.RequestHandlers
{
    public class GetMyHighScores : IRequestHandler
    {
        private static readonly DateTimeRange _timeRange = DateTime.Now.ToRange(-TimeSpan.FromDays(30));
        private readonly IHighScoreService _highScoreService;

        public GetMyHighScores(IHighScoreService highScoreService)
        {
            _highScoreService = highScoreService;
        }

        public void HandleRequest(IRequest request)
        {
            var result = _highScoreService.GetCharacterHighScores(request.Session.Character.Id, _timeRange).ToDictionary();
            Message.Builder.FromRequest(request).WithData(result).WithEmpty().Send();
        }
    }
}