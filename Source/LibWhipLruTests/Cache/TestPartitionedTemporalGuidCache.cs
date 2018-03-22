﻿// TestPartitionedTemporalGuidCache.cs
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using LibWhipLru.Cache;
using NUnit.Framework;

namespace LibWhipLruTests.Cache {
	[TestFixture]
	public static class TestPartitionedTemporalGuidCache {
		public static readonly string DATABASE_FOLDER_PATH = $"{TestContext.CurrentContext.TestDirectory}/test_ac_lmdb";

		public static void CleanLocalStorageFolder(string dbFolderPath, string writeCacheFilePath) {
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
			try {
				File.Delete(writeCacheFilePath);
			}
			catch {
			}
			try {
				Directory.Delete(dbFolderPath, true);
			}
			catch {
			}
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
		}

		public static void RebuildLocalStorageFolder(string dbFolderPath, string writeCacheFilePath) {
			CleanLocalStorageFolder(dbFolderPath, writeCacheFilePath);
			Directory.CreateDirectory(dbFolderPath);
		}

		[OneTimeSetUp]
		public static void Startup() {
			// Folder has to be there or the config fails.
			RebuildLocalStorageFolder(DATABASE_FOLDER_PATH, TestStorageManager.WRITE_CACHE_FILE_PATH);
		}

		[SetUp]
		public static void BeforeEveryTest() {
			RebuildLocalStorageFolder(DATABASE_FOLDER_PATH, TestStorageManager.WRITE_CACHE_FILE_PATH);
		}

		[TearDown]
		public static void CleanupAfterEveryTest() {
			CleanLocalStorageFolder(DATABASE_FOLDER_PATH, TestStorageManager.WRITE_CACHE_FILE_PATH);
		}

		#region Ctor basic param tests

		[Test]
		public static void TestPartitionedTemporalGuidCache_Ctor_DoesntThrow() {
			Assert.DoesNotThrow(() => new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { }, // Open or create partition
				partPath => { }, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			));
		}


		[Test]
		public static void TestPartitionedTemporalGuidCache_Ctor_PathEmpty_ArgumentNullException() {
			Assert.Throws<ArgumentNullException>(() => new PartitionedTemporalGuidCache(
				string.Empty,
				TimeSpan.FromDays(1),
				partPath => { }, // Open or create partition
				partPath => { }, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			));
		}

		[Test]
		public static void TestPartitionedTemporalGuidCache_Ctor_PathNull_ArgumentNullException() {
			Assert.Throws<ArgumentNullException>(() => new PartitionedTemporalGuidCache(
				null,
				TimeSpan.FromDays(1),
				partPath => { }, // Open or create partition
				partPath => { }, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			));
		}


		[Test]
		public static void TestPartitionedTemporalGuidCache_Ctor_IntervalNegative_ArgumentOutOfRangeException() {
			Assert.Throws<ArgumentOutOfRangeException>(() => new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromTicks(-1),
				partPath => { }, // Open or create partition
				partPath => { }, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			));
		}

		[Test]
		public static void TestPartitionedTemporalGuidCache_Ctor_IntervalZero_ArgumentOutOfRangeException() {
			Assert.Throws<ArgumentOutOfRangeException>(() => new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromTicks(0),
				partPath => { }, // Open or create partition
				partPath => { }, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			));
		}

		[Test]
		public static void TestPartitionedTemporalGuidCache_Ctor_IntervalUnder1Sec_ArgumentOutOfRangeException() {
			Assert.Throws<ArgumentOutOfRangeException>(() => new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromSeconds(0.99),
				partPath => { }, // Open or create partition
				partPath => { }, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			));
		}

		#endregion

		#region Ctor OpenCreateHandler

		[Test]
		public static void TestPartitionedTemporalGuidCache_Ctor_OpenCreateHandlerNull_ArgumentNullException() {
			Assert.Throws<ArgumentNullException>(() => new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				null, // Open or create partition
				partPath => { }, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			));
		}

		[Test]
		public static void TestPartitionedTemporalGuidCache_Ctor_OpenCreateHandler_IsCalledFresh() {
			var handlerCalled = false;

#pragma warning disable RECS0026 // Possible unassigned object created by 'new'
			new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { handlerCalled = true; }, // Open or create partition
				partPath => { }, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			);
#pragma warning restore RECS0026 // Possible unassigned object created by 'new'

			Assert.True(handlerCalled);
		}

		#endregion

		#region Ctor DeleteHandler

		[Test]
		public static void TestPartitionedTemporalGuidCache_Ctor_DeleteHandlerNull_ArgumentNullException() {
			Assert.Throws<ArgumentNullException>(() => new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { }, // Open or create partition
				null, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			));
		}

		#endregion

		#region Ctor CopyAssetHandler

		[Test]
		public static void TestPartitionedTemporalGuidCache_Ctor_CopyHandlerNull_ArgumentNullException() {
			Assert.Throws<ArgumentNullException>(() => new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { }, // Open or create partition
				partPath => { }, // delete partition
				null, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			));
		}

		#endregion

		#region Ctor PartitionFoundHandler

		[Test]
		public static void TestPartitionedTemporalGuidCache_Ctor_PartitionFoundHandlerNull_ArgumentNullException() {
			Assert.Throws<ArgumentNullException>(() => new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { }, // Open or create partition
				partPath => { }, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				null // partition found. Load it and return the asset IDs and sizes contained.
			));
		}

		#endregion

		#region TryAdd new asset

		[Test]
		public static void TestPartitionedTemporalGuidCache_TryAdd_FirstTime_ReturnsTrue() {
			var cache = new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { }, // Open or create partition
				partPath => { }, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			);
			Assert.True(cache.TryAdd(Guid.NewGuid(), 0, out var dbPath));
		}

		[Test]
		public static void TestPartitionedTemporalGuidCache_TryAdd_Multiple_ReturnsTrue() {
			var cache = new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { }, // Open or create partition
				partPath => { }, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			);
			Assert.True(cache.TryAdd(Guid.NewGuid(), 1, out var dbPath1));
			Assert.True(cache.TryAdd(Guid.NewGuid(), 2, out var dbPath2));
			Assert.True(cache.TryAdd(Guid.NewGuid(), 3, out var dbPath3));
		}

		[Test]
		public static void TestPartitionedTemporalGuidCache_TryAdd_Duplicate_ReturnsFalse() {
			var cache = new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { }, // Open or create partition
				partPath => { }, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			);
			var guid = Guid.NewGuid();
			cache.TryAdd(guid, 1, out var dbPath1);
			Assert.False(cache.TryAdd(guid, 2, out var dbPath2));
		}

		#endregion

		#region Count

		[Test]
		public static void TestPartitionedTemporalGuidCache_Count_Fresh_IsZero() {
			var cache = new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { }, // Open or create partition
				partPath => { }, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			);
			Assert.AreEqual(0, cache.Count);
		}

		[Test]
		public static void TestPartitionedTemporalGuidCache_Count_Returns3AfterAdding3() {
			var cache = new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { }, // Open or create partition
				partPath => { }, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			);
			cache.TryAdd(Guid.NewGuid(), 1, out var dbPath1);
			cache.TryAdd(Guid.NewGuid(), 2, out var dbPath2);
			cache.TryAdd(Guid.NewGuid(), 3, out var dbPath3);
			Assert.AreEqual(3, cache.Count);
		}

		#endregion

		#region Clear

		[Test]
		public static void TestPartitionedTemporalGuidCache_Clear_ResultsInCountZero() {
			var cache = new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { }, // Open or create partition
				partPath => { }, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			);
			cache.TryAdd(Guid.NewGuid(), 0, out var dbPath);
			cache.Clear();
			Assert.AreEqual(0, cache.Count);
		}

		#endregion

		#region Contains

		[Test]
		public static void TestPartitionedTemporalGuidCache_Contains_DoesntFindUnknown() {
			var cache = new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { }, // Open or create partition
				partPath => { }, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			);
			cache.TryAdd(Guid.NewGuid(), 0, out var dbPath);
			Assert.False(cache.Contains(Guid.NewGuid()));
		}

		[Test]
		public static void TestPartitionedTemporalGuidCache_Contains_FindsKnown() {
			var cache = new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { }, // Open or create partition
				partPath => { }, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			);
			var guid = Guid.NewGuid();
			cache.TryAdd(Guid.NewGuid(), 1, out var dbPath1);
			cache.TryAdd(guid, 2, out var dbPath2);
			cache.TryAdd(Guid.NewGuid(), 3, out var dbPath3);
			Assert.True(cache.Contains(guid));
		}

		[Test]
		public static void TestPartitionedTemporalGuidCache_Contains_DoesntFindRemovedItem() {
			var cache = new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { }, // Open or create partition
				partPath => { }, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			);
			var guid = Guid.NewGuid();
			cache.TryAdd(Guid.NewGuid(), 1, out var dbPath1);
			cache.TryAdd(guid, 2, out var dbPath2);
			cache.TryAdd(Guid.NewGuid(), 3, out var dbPath3);
			cache.TryRemove(guid);
			Assert.False(cache.Contains(guid));
		}

		#endregion

		#region AssetSize Get

		[Test]
		public static void TestPartitionedTemporalGuidCache_AssetSizeGet_Known_SizeCorrect() {
			var cache = new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { }, // Open or create partition
				partPath => { }, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			);
			var guid = Guid.NewGuid();
			cache.TryAdd(Guid.NewGuid(), 1, out var dbPath1);
			cache.TryAdd(guid, 2, out var dbPath2);
			cache.TryAdd(Guid.NewGuid(), 3, out var dbPath3);
			Assert.AreEqual(2, cache.AssetSize(guid));
		}

		[Test]
		public static void TestPartitionedTemporalGuidCache_AssetSizeGet_Unknown_Null() {
			var cache = new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { }, // Open or create partition
				partPath => { }, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			);
			var guid = Guid.NewGuid();
			cache.TryAdd(Guid.NewGuid(), 1, out var dbPath1);
			cache.TryAdd(Guid.NewGuid(), 3, out var dbPath3);
			Assert.IsNull(cache.AssetSize(guid));
		}

		#endregion

		#region AssetSize Set

		[Test]
		public static void TestPartitionedTemporalGuidCache_AssetSizeSet_Known_SizeUpdated() {
			var cache = new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { }, // Open or create partition
				partPath => { }, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			);
			var guid = Guid.NewGuid();
			cache.TryAdd(Guid.NewGuid(), 1, out var dbPath1);
			cache.TryAdd(guid, 2, out var dbPath2);
			cache.TryAdd(Guid.NewGuid(), 3, out var dbPath3);

			cache.AssetSize(guid, 10);

			Assert.AreEqual(10, cache.AssetSize(guid));
		}

		[Test]
		public static void TestPartitionedTemporalGuidCache_AssetSizeSet_Unknown_NoChange() {
			var cache = new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { }, // Open or create partition
				partPath => { }, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			);
			var guid1 = Guid.NewGuid();
			var guid2 = Guid.NewGuid();
			var guid3 = Guid.NewGuid();
			cache.TryAdd(guid1, 1, out var dbPath1);
			cache.TryAdd(guid3, 3, out var dbPath3);

			cache.AssetSize(guid2, 10);

			Assert.AreEqual(1, cache.AssetSize(guid1));
			Assert.AreEqual(3, cache.AssetSize(guid3));
		}

		#endregion

		#region ItemsWithPrefix

		[Test]
		public static void TestPartitionedTemporalGuidCache_ItemsWithPrefix_DoesntFindUnknown() {
			var cache = new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { }, // Open or create partition
				partPath => { }, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			);
			cache.TryAdd(Guid.Parse("67bdbe4a-1f93-4316-8c32-ae7a168a00e4"), 1, out var dbPath1);
			cache.TryAdd(Guid.Parse("fcf84364-5fbd-4866-b8a7-35b93a20dbc6"), 2, out var dbPath2);
			cache.TryAdd(Guid.Parse("06fd2e96-4c5e-4e87-918a-f217064330ea"), 3, out var dbPath3);
			Assert.IsEmpty(cache.ItemsWithPrefix("123"));
		}

		[Test]
		public static void TestPartitionedTemporalGuidCache_ItemsWithPrefix_FindsSingularKnown() {
			var cache = new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { }, // Open or create partition
				partPath => { }, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			);
			var guid = Guid.Parse("fcf84364-5fbd-4866-b8a7-35b93a20dbc6");
			cache.TryAdd(guid, 1, out var dbPath1);
			cache.TryAdd(Guid.Parse("67bdbe4a-1f93-4316-8c32-ae7a168a00e4"), 2, out var dbPath2);
			cache.TryAdd(Guid.Parse("06fd2e96-4c5e-4e87-918a-f217064330ea"), 3, out var dbPath3);

			var result = cache.ItemsWithPrefix(guid.ToString("N").Substring(0, 3));
			Assert.AreEqual(1, result.Count());
			Assert.That(result, Contains.Item(guid));
		}

		[Test]
		public static void TestPartitionedTemporalGuidCache_ItemsWithPrefix_FindsMultipleKnown() {
			var cache = new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { }, // Open or create partition
				partPath => { }, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			);
			var guid1 = Guid.Parse("fcf84364-5fbd-4866-b8a7-35b93a20dbc6");
			cache.TryAdd(guid1, 1, out var dbPath1);
			var guid2 = Guid.Parse("fcfdbe4a-1f93-4316-8c32-ae7a168a00e4");
			cache.TryAdd(guid2, 2, out var dbPath2);
			cache.TryAdd(Guid.Parse("67bdbe4a-1f93-4316-8c32-ae7a168a00e4"), 3, out var dbPath3);
			cache.TryAdd(Guid.Parse("06fd2e96-4c5e-4e87-918a-f217064330ea"), 4, out var dbPath4);

			var result = cache.ItemsWithPrefix(guid1.ToString("N").Substring(0, 3));
			Assert.AreEqual(2, result.Count());
			Assert.That(result, Contains.Item(guid1));
			Assert.That(result, Contains.Item(guid2));
		}

		#endregion

		#region TryRemove

		[Test]
		public static void TestPartitionedTemporalGuidCache_TryRemove_DoesntRemoveUnknown() {
			var cache = new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { }, // Open or create partition
				partPath => { }, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			);
			cache.TryAdd(Guid.NewGuid(), 0, out var dbPath);
			Assert.False(cache.TryRemove(Guid.NewGuid()));
			Assert.AreEqual(1, cache.Count);
		}

		[Test]
		public static void TestPartitionedTemporalGuidCache_TryRemove_DoesRemoveKnown() {
			var cache = new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { }, // Open or create partition
				partPath => { }, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			);
			var guid = Guid.NewGuid();
			cache.TryAdd(Guid.NewGuid(), 1, out var dbPath1);
			cache.TryAdd(guid, 2, out var dbPath2);
			cache.TryAdd(Guid.NewGuid(), 3, out var dbPath3);
			Assert.True(cache.TryRemove(guid));
			Assert.AreEqual(2, cache.Count);
			Assert.False(cache.Contains(guid));
		}

		#endregion

		#region Remove

		[Test]
		public static void TestPartitionedTemporalGuidCache_Remove_Empty_ReturnsEmptyAndZero() {
			var cache = new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { Directory.CreateDirectory(partPath); }, // Open or create partition
				Directory.Delete, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			);
			var removed = cache.Remove(100, out var sizeCleared);
			Assert.IsEmpty(removed);
			Assert.AreEqual(0, sizeCleared);
		}

		[Test]
		public static void TestPartitionedTemporalGuidCache_Remove_RemovesItems() {
			var cache = new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { Directory.CreateDirectory(partPath); }, // Open or create partition
				Directory.Delete, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			);
			cache.TryAdd(Guid.NewGuid(), 2, out var dbPath2);
			cache.TryAdd(Guid.NewGuid(), 4, out var dbPath4);
			cache.TryAdd(Guid.NewGuid(), 8, out var dbPath8);

			cache.Remove(5, out var sizeCleared);

			Assert.Less(cache.Count, 3);
		}

		[Test]
		public static void TestPartitionedTemporalGuidCache_Remove_ReportsCorrectSizeCleared() {
			var cache = new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { Directory.CreateDirectory(partPath); }, // Open or create partition
				Directory.Delete, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			);
			cache.TryAdd(Guid.NewGuid(), 2, out var dbPath2);
			cache.TryAdd(Guid.NewGuid(), 4, out var dbPath4);
			cache.TryAdd(Guid.NewGuid(), 8, out var dbPath8);

			cache.Remove(5, out var sizeCleared);

			Assert.AreEqual(6, sizeCleared);
		}

		[Test]
		public static void TestPartitionedTemporalGuidCache_Remove_ReturnsLeastRecentlyAccessedItems() {
			var cache = new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { Directory.CreateDirectory(partPath); }, // Open or create partition
				Directory.Delete, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			);

			var guidRemoved1 = Guid.NewGuid();
			var guidRemoved2 = Guid.NewGuid();
			var guidStays1 = Guid.NewGuid();
			var guidStays2 = Guid.NewGuid();
			var guidStays3 = Guid.NewGuid();

			cache.TryAdd(guidStays1, 2, out var dbPathS1);
			Thread.Sleep(100);
			cache.TryAdd(guidStays2, 2, out var dbPathS2);
			Thread.Sleep(100);
			cache.TryAdd(guidRemoved1, 2, out var dbPathR1);
			Thread.Sleep(100);
			cache.TryAdd(guidRemoved2, 2, out var dbPathR2);
			Thread.Sleep(100);
			cache.TryAdd(guidStays3, 2, out var dbPathS3);
			Thread.Sleep(100);

			// Touch the timestamps
			cache.Contains(guidStays1);
			Thread.Sleep(100);
			cache.Contains(guidStays3);
			Thread.Sleep(100);
			cache.ItemsWithPrefix(guidStays2.ToString("N").Substring(0, 3));
			Thread.Sleep(100);

			var removed = cache.Remove(3, out var sizeCleared);

			Assert.That(removed.Keys, Contains.Item(guidRemoved1));
			Assert.That(removed.Keys, Contains.Item(guidRemoved2));
		}

		[Test]
		public static void TestPartitionedTemporalGuidCache_Remove_CacheDoesntContainRemovedItems() {
			var cache = new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromDays(1),
				partPath => { Directory.CreateDirectory(partPath); }, // Open or create partition
				Directory.Delete, // delete partition
				(assetId, partPathSource, partPathDest) => { }, // copy asset between partitions
				partPath => { return null; } // partition found. Load it and return the asset IDs and sizes contained.
			);

			var guidRemoved1 = Guid.NewGuid();
			var guidRemoved2 = Guid.NewGuid();
			var guidStays1 = Guid.NewGuid();
			var guidStays2 = Guid.NewGuid();
			var guidStays3 = Guid.NewGuid();

			cache.TryAdd(guidStays1, 2, out var dbPathS1);
			Thread.Sleep(100);
			cache.TryAdd(guidStays2, 2, out var dbPathS2);
			Thread.Sleep(100);
			cache.TryAdd(guidRemoved1, 2, out var dbPathR1);
			Thread.Sleep(100);
			cache.TryAdd(guidRemoved2, 2, out var dbPathR2);
			Thread.Sleep(100);
			cache.TryAdd(guidStays3, 2, out var dbPathS3);
			Thread.Sleep(100);

			// Touch the timestamps
			cache.Contains(guidStays1);
			Thread.Sleep(100);
			cache.Contains(guidStays3);
			Thread.Sleep(100);
			cache.ItemsWithPrefix(guidStays2.ToString("N").Substring(0, 3));
			Thread.Sleep(100);

			cache.Remove(3, out var sizeCleared);

			Assert.False(cache.Contains(guidRemoved1));
			Assert.False(cache.Contains(guidRemoved2));
		}

		[Test]
		public static void TestPartitionedTemporalGuidCache_Remove_LeavesMostRecentlyAccessedItems() {
			var fakeDb = new Dictionary<string, Dictionary<Guid, uint>>();

			var cache = new PartitionedTemporalGuidCache(
				DATABASE_FOLDER_PATH,
				TimeSpan.FromSeconds(1),
				partPath => { Directory.CreateDirectory(partPath); fakeDb.Add(partPath, new Dictionary<Guid, uint>()); }, // Open or create partition
				partPath => { Directory.Delete(partPath); fakeDb.Remove(partPath);}, // delete partition
				(assetId, partPathSource, partPathDest) => { fakeDb[partPathDest].Add(assetId, fakeDb[partPathSource][assetId]); }, // copy asset between partitions
				partPath => { return fakeDb[partPath]; } // partition found. Load it and return the asset IDs and sizes contained.
			);

			var guidRemoved1 = Guid.NewGuid();
			var guidRemoved2 = Guid.NewGuid();
			var guidStays1 = Guid.NewGuid();
			var guidStays2 = Guid.NewGuid();
			var guidStays3 = Guid.NewGuid();

			cache.TryAdd(guidStays1, 2, out var dbPathS1);
			fakeDb[dbPathS1].Add(guidStays1, 2);
			cache.TryAdd(guidStays2, 2, out var dbPathS2);
			fakeDb[dbPathS2].Add(guidStays2, 2);
			cache.TryAdd(guidRemoved1, 2, out var dbPathR1);
			fakeDb[dbPathR1].Add(guidRemoved1, 2);
			cache.TryAdd(guidRemoved2, 2, out var dbPathR2);
			fakeDb[dbPathR2].Add(guidRemoved2, 2);
			Thread.Sleep(1100);
			cache.TryAdd(guidStays3, 2, out var dbPathS3);
			fakeDb[dbPathS3].Add(guidStays3, 2);

			// Touch the timestamps
			cache.Contains(guidStays1);
			cache.ItemsWithPrefix(guidStays2.ToString("N").Substring(0, 3));

			cache.Remove(3, out var sizeCleared);

			Assert.True(cache.Contains(guidStays1));
			Assert.True(cache.Contains(guidStays2));
			Assert.True(cache.Contains(guidStays3));
		}

		#endregion
	}
}
