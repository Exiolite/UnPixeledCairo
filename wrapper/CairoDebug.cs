//
// CairoDebug.cs
//
// Author:
//   Michael Hutchinson (mhutch@xamarin.com)
//
// Copyright (C) 2013 Xamarin Inc. (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Concurrent;

#nullable disable

namespace Cairo {

	public static class CairoDebug
	{
        // ConcurrentDictionary because following line
        // traces[obj] = Environment.StackTrace;
        // sometimes crashes with: System.IndexOutOfRangeException: Index was outside the bounds of the array.
        // according to stack overflow thats apparently a synchronization issue
        static ConcurrentDictionary<IntPtr,string> traces;

		public static bool Enabled;


		static CairoDebug ()
		{
            // Comment out if not needed. Causes large amount of lag if enabled and when cairo is used for redrawing dialogs.
            // According to the performance profiler the call to Environment.StackTrace is very slow.  (in OnAllocated)
            Environment.SetEnvironmentVariable("CAIRO_DEBUG_DISPOSE", "0");

            var dbg = Environment.GetEnvironmentVariable ("CAIRO_DEBUG_DISPOSE");
            if (dbg == null)
            {
                return;
            }

			Enabled = true;
			traces = new ConcurrentDictionary<IntPtr,string> ();
		}

		public static void OnAllocated (IntPtr obj)
		{
			if (!Enabled)
				throw new InvalidOperationException ();

			traces[obj] = Environment.StackTrace;
		}

		public static void OnDisposed<T> (IntPtr obj, bool disposing)
		{
			if (disposing && !Enabled)
				throw new InvalidOperationException ();

            if (Environment.HasShutdownStarted)
            {
                return;
            }

			if (!disposing) {
				Console.WriteLine ("{0} is leaking, programmer is missing a call to Dispose", typeof(T).FullName);
				if (Enabled) {
					string val;
					if (traces.TryGetValue (obj, out val)) {
						Console.WriteLine ("Allocated from:");
						Console.WriteLine (val);
					} else
                    {
						Console.WriteLine("Wicked. Call trace is not in dict? Cant give you a trace this way");
					}
				} else {
					Console.WriteLine ("Set env var CAIRO_DEBUG_DISPOSE to track allocation traces");
				}
			}

            if (Enabled)
            {
                string useless;
                traces.TryRemove(obj, out useless);
            }
		}
	}

}
