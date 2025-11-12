namespace SM_MentalHealthApp.Server.Services.ResponseHandlers
{
    /// <summary>
    /// Interface for response handlers that process different types of questions
    /// </summary>
    public interface IResponseHandler
    {
        /// <summary>
        /// Determines if this handler can process the given question
        /// </summary>
        Task<bool> CanHandleAsync(string question, ResponseContext context);

        /// <summary>
        /// Processes the question and generates a response
        /// </summary>
        Task<string> HandleAsync(string question, ResponseContext context);
    }
}

