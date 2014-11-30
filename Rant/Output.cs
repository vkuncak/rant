﻿using System.Collections;
using System.Collections.Generic;

namespace Rant
{
    /// <summary>
    /// Represents a collection of strings generated by a pattern.
    /// </summary>
    public sealed class Output : IEnumerable<Channel>
    {
        private readonly Dictionary<string, Channel> _channelMap;
        private readonly IEnumerable<Channel> _channels;
        private readonly long _seed;
        private readonly long _startingGen;

        internal Output(long seed, long startingGen, Dictionary<string, Channel> channels)
        {
            _channelMap = channels;
            _channels = channels.Values;
            _seed = seed;
            _startingGen = startingGen;
        }

        /// <summary>
        /// Retrieves the channel with the specified name.
        /// </summary>
        /// <param name="index">The name of the channel.</param>
        /// <returns></returns>
        public Channel this[string index]
        {
            get
            {
                Channel chan;
                return _channelMap.TryGetValue(index, out chan) ? chan : null;
            }
        }

        /// <summary>
        /// The seed used to generate the output.
        /// </summary>
        public long Seed => _seed;

        /// <summary>
        /// The generation at which the RNG was initially set before the pattern was run.
        /// </summary>
        public long BaseGeneration => _startingGen;

        /// <summary>
        /// The main output string.
        /// </summary>
        public string MainValue => _channelMap["main"].Value;

        /// <summary>
        /// Returns an enumerator that iterates through the channels in the collection.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Channel> GetEnumerator()
        {
            return _channels.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _channelMap.GetEnumerator();
        }

        /// <summary>
        /// Returns the output from the "main" channel.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => MainValue;

        /// <summary>
        /// Returns the output from the "main" channel.
        /// </summary>
        /// <returns></returns>
        public static implicit operator string(Output output) => output.MainValue;
    }
}