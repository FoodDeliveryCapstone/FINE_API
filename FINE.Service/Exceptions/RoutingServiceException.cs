namespace FINE.Service.Exceptions
{
    public class RoutingServiceException : Exception
    {
        public RoutingServiceException()
        {
        }

        public RoutingServiceException(string message) : base(message)
        {
        }

        public RoutingServiceException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
