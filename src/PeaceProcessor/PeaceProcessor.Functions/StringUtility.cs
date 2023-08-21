﻿namespace PeaceProcessor.Functions
{
    internal static class StringUtility
    {
        /// <summary>
        /// Blob storage metadata does not support newlines or non-ASCII characters. This method
        /// removes not supported characters and trims the topic to 50 characters.
        /// </summary>
        public static string FormatForTopicMetadata(string topic)
        {
            return new string(topic[..Math.Min(topic.Length, 50)]
                .Replace("\n", "").Where(c => c < 128).ToArray());
        }
    }
}