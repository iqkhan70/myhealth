using SM_MentalHealthApp.Server.Services;

namespace SM_MentalHealthApp.Server.Services.ResponseHandlers
{
    /// <summary>
    /// Factory to select and create appropriate response handlers
    /// </summary>
    public class ResponseHandlerFactory
    {
        private readonly IEnumerable<IResponseHandler> _handlers;
        private readonly ILogger<ResponseHandlerFactory> _logger;

        public ResponseHandlerFactory(IEnumerable<IResponseHandler> handlers, ILogger<ResponseHandlerFactory> logger)
        {
            _handlers = handlers;
            _logger = logger;
        }

        public async Task<IResponseHandler?> GetHandlerAsync(string question, ResponseContext context)
        {
            foreach (var handler in _handlers)
            {
                if (await handler.CanHandleAsync(question, context))
                {
                    _logger.LogInformation("Selected handler: {HandlerType} for question: {Question}", 
                        handler.GetType().Name, question);
                    return handler;
                }
            }

            _logger.LogInformation("No specific handler found, will use default overview handler");
            return _handlers.FirstOrDefault(h => h is OverviewResponseHandler);
        }
    }
}

