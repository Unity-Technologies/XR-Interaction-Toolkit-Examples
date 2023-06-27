/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace Oculus.Voice.Core.Utilities
{
    public class DateTimeUtility
    {
        public static DateTime UtcNow
        {
            get => DateTime.UtcNow;
        }

        public static long ElapsedMilliseconds
        {
            get => UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
        }
    }
}
