namespace F23Bag.AutomaticUI
{
    /// <summary>
    /// Interface for UI translation.
    /// </summary>
    public interface I18n
    {
        /// <summary>
        /// Return the translation of a message.
        /// </summary>
        /// <param name="message">Message to translate.</param>
        /// <returns>Translation of message.</returns>
        string GetTranslation(string message);
    }
}
