//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Threading;

namespace InjectBinarySecretToken
{
    public struct TimeoutHelper
    {
        public static TimeSpan DefaultTimeout { get { return TimeSpan.FromMinutes(2); } }

        public static TimeSpan DefaultShortTimeout { get { return TimeSpan.FromSeconds(4); } }

        public static TimeSpan Infinite { get { return TimeSpan.MaxValue; } }

        private readonly DateTime _deadline;
        private TimeSpan _originalTimeout;

        public TimeoutHelper(TimeSpan timeout)
        {
            if (timeout < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout), $"timeout cannot be less than TimeSpan.Zero: timeout: '{timeout}'.");

            _originalTimeout = timeout;
            if (timeout == TimeSpan.MaxValue)
                _deadline = DateTime.MaxValue;
            else
                _deadline = DateTime.UtcNow + timeout;
        }

        public TimeSpan OriginalTimeout
        {
            get => _originalTimeout;
        }

        public TimeSpan RemainingTime()
        {
            if (_deadline == DateTime.MaxValue)
                return TimeSpan.MaxValue;

            TimeSpan remaining = _deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
                return TimeSpan.Zero;

            return remaining;
        }

        public void SetTimer(TimerCallback callback, Object state)
        {
            Timer timer = new Timer(callback, state, TimeoutHelper.ToMilliseconds(RemainingTime()), Timeout.Infinite);
        }

        public static TimeSpan FromMilliseconds(int milliseconds)
        {
            if (milliseconds == Timeout.Infinite)
                return TimeSpan.MaxValue;

            return TimeSpan.FromMilliseconds(milliseconds);
        }

        public static int ToMilliseconds(TimeSpan timeout)
        {
            if (timeout == TimeSpan.MaxValue)
                return Timeout.Infinite;

            long ticks = Ticks.FromTimeSpan(timeout);
            if (ticks / TimeSpan.TicksPerMillisecond > int.MaxValue)
                return int.MaxValue;

            return Ticks.ToMilliseconds(ticks);
        }

        public static TimeSpan Add(TimeSpan timeout1, TimeSpan timeout2)
        {
            return Ticks.ToTimeSpan(Ticks.Add(Ticks.FromTimeSpan(timeout1), Ticks.FromTimeSpan(timeout2)));
        }

        public static DateTime Add(DateTime time, TimeSpan timeout)
        {
            if (timeout >= TimeSpan.Zero && DateTime.MaxValue - time <= timeout)
                return DateTime.MaxValue;

            if (timeout <= TimeSpan.Zero && DateTime.MinValue - time >= timeout)
                return DateTime.MinValue;

            return time + timeout;
        }

        public static DateTime Subtract(DateTime time, TimeSpan timeout)
        {
            return Add(time, TimeSpan.Zero - timeout);
        }

        public static TimeSpan Divide(TimeSpan timeout, int factor)
        {
            if (timeout == TimeSpan.MaxValue)
                return TimeSpan.MaxValue;

            return Ticks.ToTimeSpan((Ticks.FromTimeSpan(timeout) / factor) + 1);
        }

        public static bool WaitOne(WaitHandle waitHandle, TimeSpan timeout, bool exitSync)
        {
            if (timeout == TimeSpan.MaxValue)
            {
                waitHandle.WaitOne();
                return true;
            }

            TimeSpan maxWait = TimeSpan.FromMilliseconds(Int32.MaxValue);

            while (timeout > maxWait)
            {
                bool signaled = waitHandle.WaitOne(maxWait, exitSync);
                if (signaled)
                    return true;

                timeout -= maxWait;
            }

            return waitHandle.WaitOne(timeout, exitSync);
        }

        static class Ticks
        {
            public static long FromMilliseconds(int milliseconds)
            {
                return (long)milliseconds * TimeSpan.TicksPerMillisecond;
            }

            public static int ToMilliseconds(long ticks)
            {
                return checked((int)(ticks / TimeSpan.TicksPerMillisecond));
            }

            public static long FromTimeSpan(TimeSpan duration)
            {
                return duration.Ticks;
            }

            public static TimeSpan ToTimeSpan(long ticks)
            {
                return new TimeSpan(ticks);
            }

            public static long Add(long firstTicks, long secondTicks)
            {
                if (firstTicks == long.MaxValue || firstTicks == long.MinValue)
                    return firstTicks;

                if (secondTicks == long.MaxValue || secondTicks == long.MinValue)
                    return secondTicks;

                if (firstTicks >= 0 && long.MaxValue - firstTicks <= secondTicks)
                    return long.MaxValue - 1;

                if (firstTicks <= 0 && long.MinValue - firstTicks >= secondTicks)
                    return long.MinValue + 1;

                return checked(firstTicks + secondTicks);
            }
        }
    }
}