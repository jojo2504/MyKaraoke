namespace MyKaraoke.Service.Logging.Events {
    public enum LogEventLevel {
        /// <summary>
        /// Anything and everything you might want to know about
        /// a running block of code.
        /// </summary>
        Verbose,

        /// <summary>
        /// Internal system events that aren't necessarily
        /// observable from the outside.
        /// </summary>
        Debug,

        /// <summary>
        /// The lifeblood of operational intelligence - things
        /// happen.
        /// </summary>
        Information,

        /// <summary>
        /// Service is degraded or endangered.
        /// </summary>
        Warning,

        /// <summary>
        /// Functionality is unavailable, invariants are broken
        /// or data is lost.
        /// </summary>
        Error,

        /// <summary>
        /// If you have a pager, it goes off when one of these
        /// occurs.
        /// </summary>
        Fatal,

        /// <summary>
        /// When logic has returned the expected outcome
        /// </summary>
        Success,
        
        /// <summary>
        /// Any important logs that should be highlighted,
        /// Is an extend of information
        /// </summary>
        Important,
    }
}