namespace FINE.Service.Exceptions
{
    public class MapServiceException : Exception
    {
        public String Code { get; set; }
        public MapServiceException()
        {
        }


        public MapServiceException(string message) : base(message)
        {
        }


        public MapServiceException(string message, string code) : base(message)
        {
            Code = code;
        }

        public MapServiceException(string message, Exception innerException) : base(message, innerException)
        {
        }


    }
}
