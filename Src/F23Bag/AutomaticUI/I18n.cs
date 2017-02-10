using System;
using System.Globalization;

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

        /// <summary>
        /// Return the translation of a message with parameters.
        /// </summary>
        /// <param name="message">Message to translate.</param>
        /// <returns>Translation of message.</returns>
        string GetTranslation(I18nMessage message);

        /// <summary>
        /// Set or return the current culture used for formating data.
        /// </summary>
        CultureInfo CurrentFormatingCulture { get; set; }

        /// <summary>
        /// Set or return the current culture used for translate UI.
        /// </summary>
        CultureInfo CurrentResourcingCulture { get; set; }

        /// <summary>
        /// Raise when CurrentFormatingCulture is changed.
        /// </summary>
        event EventHandler CurrentFormatingCultureChanged;

        /// <summary>
        /// Raise when CurrentResourcingCulture is changed.
        /// </summary>
        event EventHandler CurrentResourcingCultureChanged;
    }
}
