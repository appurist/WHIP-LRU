﻿// Setup.cs
//
// Author:
//       Ricky Curtice <ricky@rwcproductions.com>
//
// Copyright (c) 2017 Richard Curtice
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;
using System.Threading;
using Chattel;
using LibWhipLru;
using log4net.Config;
using Nini.Config;
using NUnit.Framework;

namespace UnitTests {
	[SetUpFixture]
	public sealed class Setup {
		private readonly string DATABASE_FOLDER_PATH = Path.Combine(TestContext.CurrentContext.TestDirectory, "test");
		private const ulong DATABASE_MAX_SIZE_BYTES = uint.MaxValue;
		private readonly string WRITE_CACHE_FILE_PATH = Path.Combine(TestContext.CurrentContext.TestDirectory, "test.whipwcache");
		private const uint WRITE_CACHE_MAX_RECORD_COUNT = 8;

		private WhipLru _service;

		[OneTimeSetUp]
		public void Init() {
			// Configure Log4Net
			XmlConfigurator.Configure(new FileInfo(Constants.LOG_CONFIG_PATH));

			// Set CWD so that native libs are found.
			Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);

			// Load INI stuff
			var configSource = new ArgvConfigSource(new string[] { });

			// Configure nIni aliases and locale
			Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US", true);

			configSource.Alias.AddAlias("On", true);
			configSource.Alias.AddAlias("Off", false);
			configSource.Alias.AddAlias("True", true);
			configSource.Alias.AddAlias("False", false);
			configSource.Alias.AddAlias("Yes", true);
			configSource.Alias.AddAlias("No", false);

			// Read in the ini file
			configSource.Merge(new IniConfigSource(Constants.INI_PATH));

#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
			try {
				Directory.Delete(DATABASE_FOLDER_PATH, true);
			}
			catch (Exception) {
			}
			try {
				File.Delete(WRITE_CACHE_FILE_PATH);
			}
			catch (Exception) {
			}
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
			Directory.CreateDirectory(DATABASE_FOLDER_PATH);

			// Start booting server
			var pidFileManager = new LibWhipLru.Util.PIDFileManager(Constants.PID_FILE_PATH);

			var chattelConfigRead = new ChattelConfiguration(DATABASE_FOLDER_PATH);
			var chattelConfigWrite = new ChattelConfiguration(DATABASE_FOLDER_PATH);

			var readerLocalStorage = new LibWhipLru.Cache.AssetLocalStorageLmdbPartitionedLRU(
				chattelConfigRead,
				DATABASE_MAX_SIZE_BYTES,
				TimeSpan.FromSeconds(1)
			);
			var chattelReader = new ChattelReader(chattelConfigRead, readerLocalStorage);
			var chattelWriter = new ChattelWriter(chattelConfigWrite, readerLocalStorage);

			var storageManager = new LibWhipLru.Cache.StorageManager(
				readerLocalStorage,
				TimeSpan.FromMinutes(2),
				chattelReader,
				chattelWriter
			);

			_service = new WhipLru(
				Constants.SERVICE_ADDRESS,
				Constants.SERVICE_PORT,
				Constants.PASSWORD,
				pidFileManager,
				storageManager
			);

			_service.Start();
		}

		[OneTimeTearDown]
		public void Cleanup() {
			_service.Stop();

			Thread.Sleep(500);

			// Clear the PID file if it exists.
			File.Delete(Constants.PID_FILE_PATH);

#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
			try {
				Directory.Delete(DATABASE_FOLDER_PATH, true);
			}
			catch (Exception) {
			}
			try {
				File.Delete(WRITE_CACHE_FILE_PATH);
			}
			catch (Exception) {
			}
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
		}
	}
}
