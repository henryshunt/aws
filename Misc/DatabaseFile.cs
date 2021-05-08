namespace Aws.Misc
{
    /// <summary>
    /// Represents the database files used by the program.
    /// </summary>
    internal enum DatabaseFile
    {
        /// <summary>
        /// The data database, which stores logged data.
        /// </summary>
        Data,

        /// <summary>
        /// The upload database, which stores logged data that has not yet been uploaded.
        /// </summary>
        Upload
    }
}
