﻿// Copyright Dirk Lemstra https://github.com/dlemstra/Magick.NET.
// Licensed under the Apache License, Version 2.0.

using ImageMagick;
using Xunit;

namespace Magick.NET.Tests
{
    public partial class MagickSettingsTests
    {
        public class TheSyncWithExifProfileProperty
        {
            [Fact]
            public void ShouldReturnTrueAsTheDefaultValue()
            {
                var readSettings = new MagickReadSettings();
                Assert.True(readSettings.SyncImageWithExifProfile);
            }

            [Fact]
            public void ShouldNotChangeTheDensityOfTheImageWhenSetToFalse()
            {
                using (var image = new MagickImage(Files.EightBimJPG))
                {
                    Assert.Equal(300.0, image.Density.X);
                }

                var readSettings = new MagickReadSettings
                {
                    SyncImageWithExifProfile = false,
                };

                using (var image = new MagickImage())
                {
                    image.Read(Files.EightBimJPG, readSettings);
                    Assert.Equal(72.0, image.Density.X);
                }
            }
        }
    }
}
