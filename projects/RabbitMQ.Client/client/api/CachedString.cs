using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace RabbitMQ.Client
{
    /// <summary>
    /// Caches a string's byte representation to be used for certain methods like <see cref="IModel.BasicPublish(CachedString,CachedString,bool,IBasicProperties,ReadOnlyMemory{byte})"/>.
    /// </summary>
    public sealed class CachedString
    {
        public static readonly CachedString Empty = new CachedString(string.Empty, ReadOnlyMemory<byte>.Empty);

        /// <summary>
        /// The string value to cache.
        /// </summary>
        public readonly string Value;
        /// <summary>
        /// Gets the bytes representing the <see cref="Value"/>.
        /// </summary>
        public readonly ReadOnlyMemory<byte> Bytes;

        /// <summary>
        /// Creates a new <see cref="CachedString"/> based on the provided string.
        /// </summary>
        /// <param name="value">The string to cache.</param>
        public CachedString(string value)
        {
            Value = value;
            Bytes = Encoding.UTF8.GetBytes(value);
        }

        /// <summary>
        /// Creates a new <see cref="CachedString"/> based on the provided bytes.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        public CachedString(ReadOnlyMemory<byte> bytes)
        {
#if !NETSTANDARD
            Value = Encoding.UTF8.GetString(bytes.Span);
#else
            unsafe
            {
                fixed (byte* bytePointer = bytes.Span)
                {
                    Value = Encoding.UTF8.GetString(bytePointer, bytes.Length);
                }
            }
#endif
            Bytes = bytes;
        }

        /// <summary>
        /// Creates a new <see cref="CachedString"/> based on the provided values.
        /// </summary>
        /// <param name="value">The string to cache.</param>
        /// <param name="bytes">The byte representation of the string value.</param>
        public CachedString(string value, ReadOnlyMemory<byte> bytes)
        {
            Value = value;
            Bytes = bytes;
        }

        /// <summary>
        /// Gets or creates a <see cref="CachedString"/> from the cache.
        /// </summary>
        /// <param name="memory">The memory representation of the string.</param>
        /// <returns>The <see cref="CachedString"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CachedString FromCache(ReadOnlyMemory<byte> memory)
        {
            return _cachedStringCache.GetOrAdd(memory, _cachedStringFactory);
        }

        /// <summary>
        /// Gets and removes a <see cref="CachedString"/> from the cache.
        /// </summary>
        /// <param name="memory">The memory representation of the string.</param>
        /// <returns>The <see cref="CachedString"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CachedString GetAndRemoveFromCache(ReadOnlyMemory<byte> memory)
        {
            return !_cachedStringCache.TryRemove(memory, out var value) ? value : new CachedString(memory);
        }

        /// <summary>
        /// Gets a collection of all cached values.
        /// </summary>
        public ICollection<CachedString> CachedValues => _cachedStringCache.Values;

        /// <summary>
        /// Purges the full cache.
        /// </summary>
        public static void PurgeCache()
        {
            _cachedStringCache.Clear();
        }

        private static readonly ConcurrentDictionary<ReadOnlyMemory<byte>, CachedString> _cachedStringCache = new ConcurrentDictionary<ReadOnlyMemory<byte>, CachedString>(new Utf8BytesComparer());
        private static readonly Func<ReadOnlyMemory<byte>, CachedString> _cachedStringFactory = key => new CachedString(key);

        private sealed class Utf8BytesComparer : IEqualityComparer<ReadOnlyMemory<byte>>
        {
            public bool Equals(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y)
            {
                return x.Span.SequenceEqual(y.Span);
            }

            public int GetHashCode(ReadOnlyMemory<byte> value)
            {
                var span = value.Span;
                if ((uint)span.Length > 3u)
                {
                    return span.Length << 24 |
                           span[0] << 16 |
                           span[span.Length / 2] << 8 |
                           span[span.Length - 1];
                }

                return span.Length;
            }
        }
    }
}
