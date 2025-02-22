﻿using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Common.Caching;
using TheGame.Common.Dto;
using TheGame.SharedKernel;
using static TheGame.SharedKernel.Helpers.ExceptionHelper;

namespace TheGame.Commands.SaveMatchData
{
    internal class SaveGameMatchDataHandler : IRequestHandler<SaveGameMatchDataRequest, SaveGameMatchDataResponse>
    {
        private readonly ITheGameCacheProvider _cacheProvider;
        private readonly ILogger<SaveGameMatchDataHandler> _logger;
        private const string PlayerNotFound = "The informed player was not found.";
        private const string GameNotFound = "The informed game was not found.";

        public SaveGameMatchDataHandler(
            ITheGameCacheProvider cacheProvider,
            ILogger<SaveGameMatchDataHandler> logger)
        {
            _cacheProvider = cacheProvider ?? throw ArgNullEx(nameof(cacheProvider));
            _logger = logger ?? throw ArgNullEx(nameof(logger));
        }

        public async Task<SaveGameMatchDataResponse> Handle(SaveGameMatchDataRequest request, CancellationToken cancellationToken)
        {
            if (cancellationToken == CancellationToken.None)
                return new SaveGameMatchDataResponse { Result = OperationResult.Failure(new FailureDetail("CancellationToken", "CancellationToken argument cannot be null.")) };
            
            var players = await _cacheProvider.GetPlayersListAsync(cancellationToken);
            if ((players?.Count() ?? 0) == 0 || !players.Any(p => p == request.PlayerId))
                return new SaveGameMatchDataResponse { Result = OperationResult.Failure(PlayerNotFound) };

            var games = await _cacheProvider.GetGamesListAsync(cancellationToken);
            if ((games?.Count() ?? 0) == 0 || !games.Any(p => p == request.GameId))
                return new SaveGameMatchDataResponse { Result = OperationResult.Failure(GameNotFound) };

            var cachedGameMatches = await _cacheProvider.GetGameMatchesAsync(cancellationToken);
            
            var gameMatches = cachedGameMatches == null ? new List<CacheItem<GameMatchDataDto>>() : cachedGameMatches.ToList();

            gameMatches.Add(CacheItem<GameMatchDataDto>.Create(MapFrom(request)));

            await _cacheProvider.StoreGameMatchesAsync(gameMatches, null, cancellationToken);

            return new SaveGameMatchDataResponse
            {
                Result = OperationResult.Successful()
            };
        }

        private static GameMatchDataDto MapFrom(SaveGameMatchDataRequest request)
            => new GameMatchDataDto
            {
                PlayerId = request.PlayerId,
                GameId = request.GameId,
                Win = request.Win,
                MatchDate = request.MatchDate
            };
    }
}