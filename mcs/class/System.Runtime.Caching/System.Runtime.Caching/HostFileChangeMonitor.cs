//
// HostFileChangeMonitor.cs
//
// Authors:
//      Marek Habersack <mhabersack@novell.com>
//
// Copyright (C) 2010 Novell, Inc. (http://novell.com/)
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Caching.Hosting;
using System.Text;

namespace System.Runtime.Caching
{
	public sealed class HostFileChangeMonitor : FileChangeMonitor
	{
		static readonly object notificationSystemLock = new object ();
		static IFileChangeNotificationSystem notificationSystem;
		static bool notificationSystemSetFromHost;
		
		ReadOnlyCollection <string> filePaths;
		DateTime lastModified;
		string uniqueId;
		bool disposeNotificationSystem;

		Dictionary <string, object> notificationStates;
		
		public override ReadOnlyCollection<string> FilePaths {
			get { return filePaths;}
		}
		
		public override DateTimeOffset LastModified {
			get { return lastModified; }
		}
		
		public override string UniqueId {
			get { return uniqueId; }
		}
		
		public HostFileChangeMonitor (IList<string> filePaths)
		{
			if (filePaths == null)
				throw new ArgumentNullException ("filePaths");

			if (filePaths.Count == 0)
				throw new ArgumentException ("filePaths contains zero items.");

			var list = new List <string> (filePaths.Count);
			var sb = new StringBuilder ();
			lastModified = DateTime.MinValue;
			DateTime lastWrite;
			long size;
			
			foreach (string p in filePaths) {
				if (String.IsNullOrEmpty (p))
					throw new ArgumentException ("A path in the filePaths list is null or an empty string.");

				if (!Path.IsPathRooted (p))
					throw new ArgumentException ("Absolute path information is required.");
				
				list.Add (p);
				
				if (Directory.Exists (p)) {
					var di = new DirectoryInfo (p);
					lastWrite = di.LastWriteTimeUtc;
					size = 0;
				} else if (File.Exists (p)) {
					var fi = new FileInfo (p);
					lastWrite = fi.LastWriteTimeUtc;
					size = fi.Length;
				} else
					// TODO: what happens if the entry doesn't exist?
					continue;

				if (lastWrite > lastModified)
					lastModified = lastWrite;

				sb.AppendFormat ("{0}{1:x}{2:x}", p, lastWrite.Ticks, size);
			}

			this.filePaths = new ReadOnlyCollection <string> (list);
			this.uniqueId = sb.ToString ();

			InitMonitoring ();
		}

		void InitMonitoring ()
		{
			IServiceProvider host;
			if (!notificationSystemSetFromHost && (host = ObjectCache.Host) != null) {
				lock (notificationSystemLock) {
					if (!notificationSystemSetFromHost) {
						if (host != null)
							notificationSystem = host.GetService (typeof (IFileChangeNotificationSystem)) as IFileChangeNotificationSystem;

						if (notificationSystem != null)
							notificationSystemSetFromHost = true;
					}
				}
			}

			if (!notificationSystemSetFromHost) {
				notificationSystem = new FileChangeNotificationSystem ();
				disposeNotificationSystem = true;
			}

			object state;

			// TODO: what are these two used for?
			DateTimeOffset lastWriteTime;
			long fileSize;
			
			notificationStates = new Dictionary <string, object> (filePaths.Count, Helpers.StringEqualityComparer);
			foreach (string path in filePaths) {
				if (notificationStates.ContainsKey (path))
					continue; // silently ignore dupes
				
				notificationSystem.StartMonitoring (path, OnChanged, out state, out lastWriteTime, out fileSize);
			}
		}

		void OnChanged (object state)
		{
		}
		
		protected override void Dispose (bool disposing)
		{
			if (disposing && disposeNotificationSystem && notificationSystem != null) {
				var fcns = notificationSystem as FileChangeNotificationSystem;
				if (fcns != null) {
					try {
						fcns.Dispose ();
					} finally {
						notificationSystem = null;
					}
				}
			}
		}
	}
}
