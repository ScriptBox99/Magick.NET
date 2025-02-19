﻿// Copyright Dirk Lemstra https://github.com/dlemstra/Magick.NET.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using ImageMagick.Defines;

namespace ImageMagick.Formats
{
    /// <summary>
    /// Class for defines that are used when a <see cref="MagickFormat.Psd"/> image is read.
    /// </summary>
    public sealed class PsdReadDefines : ReadDefinesCreator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PsdReadDefines"/> class.
        /// </summary>
        public PsdReadDefines()
          : base(MagickFormat.Psd)
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether alpha unblending should be enabled or disabled (psd:alpha-unblend).
        /// </summary>
        public bool? AlphaUnblend
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the opacity mask of a layer should be preserved and add it back to
        /// the layer when the image is saved. This option should only be used when converting from a PSD file to another
        /// PSD file (psd:preserve-opacity-mask).
        /// </summary>
        public bool? PreserveOpacityMask
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the profile should be replicate to all the layers (psd:replicate-profile).
        /// </summary>
        public bool? ReplicateProfile
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the defines that should be set as a define on an image.
        /// </summary>
        public override IEnumerable<IDefine> Defines
        {
            get
            {
                if (AlphaUnblend.Equals(false))
                    yield return CreateDefine("alpha-unblend", false);

                if (PreserveOpacityMask.HasValue)
                    yield return CreateDefine("preserve-opacity-mask", PreserveOpacityMask.Value);

                if (ReplicateProfile.HasValue)
                    yield return CreateDefine("replicate-profile", ReplicateProfile.Value);
            }
        }
    }
}
