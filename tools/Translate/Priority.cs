using System.Collections.Generic;
using System;

namespace Translate
{
    /// <summary>
    /// This contains the current translators in a priority queue
    /// </summary>
    public class Priority
    {
        readonly SortedDictionary<int, ITranslator> translators 
            = new SortedDictionary<int, ITranslator>();

        /// <summary>
        /// Add a new translator to the priority queue. Smaller priority numbers mean a higher
        /// priority and thus earlier usage
        /// </summary>
        /// <param name="priority">the priority to use</param>
        /// <param name="translator">the new translator to add</param>
        /// <typeparam name="T">the type of the translator</typeparam>
        public void AddTranslator<T>(int priority, T translator)
            where T: ITranslator
        {
            if (translators.ContainsKey(priority))
                throw new ArgumentException($"priority {priority} already used", nameof(priority));
            translators.Add(priority, translator);
        }

        /// <summary>
        /// Get the priority of the translator key
        /// </summary>
        /// <param name="key">the translator key</param>
        /// <returns>the used priority or <c>null</c> if not found</returns>
        public int? GetPriority(string key)
        {
            foreach (var (priority, translator) in translators)
                if (translator.Key == key)
                    return priority;
            return null;
        }

        /// <summary>
        /// Tries to get a usable translator. <paramref="maxPriority" /> limits the translators to
        /// use. If it is <c>null</c> any translator can be used otherwise the priority has to be
        /// smaller than <paramref="maxPriority" />.
        /// </summary>
        /// <param name="value">the value to translate</param>
        /// <param name="maxPriority">the upper limit (exclusive) that can be used for priority.</param>
        /// <param name="minPriority">the lower limit (exclusive) that can be used for priority.</param>
        /// <returns>a useable translator</returns>
        public ITranslator? GetTranslator(string value, int? maxPriority = null, int? minPriority = null)
        {
            foreach (var (priority, translator) in translators)
            {
                if ((maxPriority is not null && priority >= maxPriority)
                    || (minPriority is not null && priority <= minPriority)
                    )
                    continue;
                if (translator.CanTranslate(value))
                    return translator;
            }
            return null;
        }
    }
}