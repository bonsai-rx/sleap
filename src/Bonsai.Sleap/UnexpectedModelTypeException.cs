using System;

namespace Bonsai.Sleap
{
    /// <summary>
    /// The exception that is thrown when a SLEAP model is unexpected for the inference operator.
    /// </summary>
    [Serializable]
    public class UnexpectedModelTypeException : InvalidOperationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedModelTypeException"/> class.
        /// </summary>
        public UnexpectedModelTypeException()
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedModelTypeException"/> class with
        /// a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>

        public UnexpectedModelTypeException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedModelTypeException"/> class with
        /// a specified error message and a reference to the inner exception that is the
        /// cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception, or <see langword="null"/>
        /// if no inner exception is specified.
        /// </param>
        public UnexpectedModelTypeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

    }
}
