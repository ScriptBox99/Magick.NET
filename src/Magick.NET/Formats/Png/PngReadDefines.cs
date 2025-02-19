﻿// Copyright Dirk Lemstra https://github.com/dlemstra/Magick.NET.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using ImageMagick.Defines;

namespace ImageMagick.Formats
{
    /// <summary>
    /// Class for defines that are used when a <see cref="MagickFormat.Png"/> image is read.
    /// </summary>
    public sealed class PngReadDefines : ReadDefinesCreator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PngReadDefines"/> class.
        /// </summary>
        public PngReadDefines()
          : base(MagickFormat.Png)
        {
        }

        /// <summary>
        /// Gets or sets the total number of sPLT, text, and unknown chunks that can be stored
        /// (png:chunk-cache-max). 0 means unlimited.
        /// </summary>
        public long? ChunkCacheMax { get; set; }

        /// <summary>
        /// Gets or sets the total memory that a zTXt, sPLT, iTXt, iCCP, or unknown chunk can occupy
        /// when decompressed (png:chuck-malloc-max). 0 means unlimited.
        /// </summary>
        public long? ChunkMallocMax { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the PNG decoder and encoder examine any ICC profile
        /// that is present. By default, the PNG decoder and encoder examine any ICC profile that is present,
        /// either from an iCCP chunk in the PNG input or supplied via an option, and if the profile is
        /// recognized to be the sRGB profile, converts it to the sRGB chunk. You can use this option
        /// to prevent this from happening; in such cases the iCCP chunk will be read. (png:preserve-iCCP).
        /// </summary>
        public bool PreserveiCCP { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the PNG decoder should ignore the CRC when reading the
        /// image. (png:ignore-crc).
        /// </summary>
        public bool IgnoreCrc { get; set; }

        /// <summary>
        /// Gets or sets the profile(s) that should be skipped when the image is read (profile:skip).
        /// </summary>
        public PngProfileTypes? SkipProfiles { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the bytes should be swapped. The PNG specification
        /// requires that any multi-byte integers be stored in network byte order (MSB-LSB endian).
        /// This option allows you to fix any invalid PNG files that have 16-bit samples stored
        /// incorrectly in little-endian order (LSB-MSB). (png:swap-bytes).
        /// </summary>
        public bool SwapBytes { get; set; }

        /// <summary>
        /// Gets the defines that should be set as a define on an image.
        /// </summary>
        public override IEnumerable<IDefine> Defines
        {
            get
            {
                if (ChunkCacheMax.HasValue)
                    yield return CreateDefine("chunk-cache-max", ChunkCacheMax.Value);

                if (ChunkMallocMax.HasValue)
                    yield return CreateDefine("chunk-malloc-max", ChunkMallocMax.Value);

                if (IgnoreCrc)
                    yield return CreateDefine("ignore-crc", IgnoreCrc);

                if (PreserveiCCP)
                    yield return CreateDefine("preserve-iCCP", PreserveiCCP);

                if (SkipProfiles.HasValue)
                {
                    var value = EnumHelper.ConvertFlags(SkipProfiles.Value);

                    if (!string.IsNullOrEmpty(value))
                        yield return new MagickDefine("profile:skip", value);
                }

                if (SwapBytes)
                    yield return CreateDefine("swap-bytes", SwapBytes);
            }
        }
    }
}
