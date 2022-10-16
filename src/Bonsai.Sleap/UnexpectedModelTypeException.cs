using System;

namespace Bonsai.Sleap
{
    [Serializable]
    public class UnexpectedModelTypeException : Exception
    {
        public UnexpectedModelTypeException()
        {
        }

        public UnexpectedModelTypeException(string message)
            : base(message)
        {
        }

        public UnexpectedModelTypeException(string message, Exception inner)
            : base(message, inner)
        {
        }

    }
}
